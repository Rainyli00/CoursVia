import json
import random
import time
from contextlib import nullcontext
from types import SimpleNamespace

import numpy as np
import torch
from tokenizers import Tokenizer

import config
from model import MiniCoursViaLLM


# Ampere ve üstü GPU'larda matmul/cudnn için TF32 hız avantajı sağlar.
torch.backends.cuda.matmul.allow_tf32 = True
torch.backends.cudnn.allow_tf32 = True


def seed_everything(seed: int):
    # Tekrarlanabilir eğitim için Python, NumPy ve Torch random seedleri eşitlenir.
    random.seed(seed)
    np.random.seed(seed)
    torch.manual_seed(seed)

    if torch.cuda.is_available():
        torch.cuda.manual_seed_all(seed)


def config_namespace():
    # Model sınıfı config'i attribute olarak beklediği için SimpleNamespace'e çevrilir.
    return SimpleNamespace(
        VOCAB_SIZE=int(config.VOCAB_SIZE),
        BLOCK_SIZE=int(config.BLOCK_SIZE),
        N_EMBD=int(config.N_EMBD),
        N_HEAD=int(config.N_HEAD),
        N_LAYER=int(config.N_LAYER),
        DROPOUT=float(config.DROPOUT),
    )


def read_token_ids(path, tokenizer: Tokenizer) -> np.ndarray:
    # Metin veri dosyası tek token id dizisine çevrilir.
    text = path.read_text(encoding="utf-8")
    ids = tokenizer.encode(text).ids
    return np.array(ids, dtype=np.int32)


def get_batch(data, block_size, batch_size, device):
    # Dil modeli eğitimi için rastgele ardışık bloklar seçilir.
    if len(data) <= block_size + 1:
        raise ValueError("Veri block_size değerinden kısa.")

    ix = torch.randint(len(data) - block_size - 1, (batch_size,))

    # x mevcut tokenlar, y ise bir token kaydırılmış hedef dizidir.
    x = torch.stack([
        torch.from_numpy(data[i:i + block_size].astype(np.int64))
        for i in ix
    ])

    y = torch.stack([
        torch.from_numpy(data[i + 1:i + 1 + block_size].astype(np.int64))
        for i in ix
    ])

    return x.to(device), y.to(device)


@torch.no_grad()
def estimate_loss(model, train_data, val_data, device, ctx):
    # Eval sırasında gradient hesaplanmaz; train ve validation loss ortalaması alınır.
    out = {}
    model.eval()

    for split, data in [("train", train_data), ("val", val_data)]:
        losses = torch.zeros(config.EVAL_ITERS)

        for k in range(config.EVAL_ITERS):
            x, y = get_batch(
                data=data,
                block_size=config.BLOCK_SIZE,
                batch_size=config.BATCH_SIZE,
                device=device,
            )

            with ctx:
                _, loss = model(x, y)

            losses[k] = loss.item()

        out[split] = losses.mean().item()

    model.train()
    return out


def save_checkpoint(path, model, optimizer, iter_no, best_val_loss, model_config):
    # Model ağırlıklarıyla birlikte eğitim durumu ve mimari config'i saklanır.
    payload = {
        "iter_no": iter_no,
        "best_val_loss": best_val_loss,
        "model_state_dict": model.state_dict(),
        "optimizer_state_dict": optimizer.state_dict() if optimizer is not None else None,

        "VOCAB_SIZE": model_config.VOCAB_SIZE,
        "BLOCK_SIZE": model_config.BLOCK_SIZE,
        "N_EMBD": model_config.N_EMBD,
        "N_HEAD": model_config.N_HEAD,
        "N_LAYER": model_config.N_LAYER,
        "DROPOUT": model_config.DROPOUT,

        "config": {
            "VOCAB_SIZE": model_config.VOCAB_SIZE,
            "BLOCK_SIZE": model_config.BLOCK_SIZE,
            "N_EMBD": model_config.N_EMBD,
            "N_HEAD": model_config.N_HEAD,
            "N_LAYER": model_config.N_LAYER,
            "DROPOUT": model_config.DROPOUT,
        },
    }

    torch.save(payload, path)


def train():
    # Eğitim giriş noktası: veri, tokenizer, model, optimizer ve döngü burada hazırlanır.
    seed_everything(config.RANDOM_SEED)

    if not config.TRAIN_FILE.exists():
        raise FileNotFoundError("Önce data_generator.py çalıştırılmalı.")

    if not config.VAL_FILE.exists():
        raise FileNotFoundError("Validation dosyası bulunamadı. Önce data_generator.py çalıştır.")

    if not config.TOKENIZER_FILE.exists():
        raise FileNotFoundError("Önce tokenizer_train.py çalıştırılmalı.")

    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

    print("=" * 70)
    print("MiniCoursViaLLM V3 eğitimi başlıyor")
    print("=" * 70)
    print(f"Device: {device}")

    # CUDA varsa mixed precision ile bellek kullanımı ve hız iyileştirilir.
    if device.type == "cuda":
        print(f"GPU: {torch.cuda.get_device_name(0)}")

    tokenizer = Tokenizer.from_file(str(config.TOKENIZER_FILE))

    # Tokenizer gerçek vocab değerini model config'e yazıyoruz.
    config.VOCAB_SIZE = tokenizer.get_vocab_size()
    model_config = config_namespace()

    if device.type == "cuda":
        ptdtype = torch.bfloat16 if torch.cuda.is_bf16_supported() else torch.float16
        ctx = torch.amp.autocast(device_type="cuda", dtype=ptdtype)
        scaler = torch.amp.GradScaler("cuda", enabled=(ptdtype == torch.float16))
    else:
        ptdtype = None
        ctx = nullcontext()
        scaler = torch.amp.GradScaler("cuda", enabled=False)

    print(f"Tokenizer vocab size: {config.VOCAB_SIZE}")
    print(f"Model: N_EMBD={config.N_EMBD}, N_HEAD={config.N_HEAD}, N_LAYER={config.N_LAYER}, BLOCK_SIZE={config.BLOCK_SIZE}")
    print(f"Batch: {config.BATCH_SIZE}, Accumulation: {config.GRAD_ACCUM_STEPS}")
    print(f"AMP dtype: {ptdtype if ptdtype else 'kapalı'}")

    print("Train/Val verisi token id olarak yükleniyor...")

    # Train/val dosyaları token id dizilerine çevrilir ve RAM'de tutulur.
    train_data = read_token_ids(config.TRAIN_FILE, tokenizer)
    val_data = read_token_ids(config.VAL_FILE, tokenizer)

    print(f"Train token: {len(train_data):,}")
    print(f"Val token  : {len(val_data):,}")

    model = MiniCoursViaLLM(model_config).to(device)

    param_count = sum(p.numel() for p in model.parameters())
    print(f"Parametre sayısı: {param_count:,}")

    # CUDA destekliyorsa fused AdamW kullanılır; destek yoksa klasik AdamW'a düşülür.
    try:
        optimizer = torch.optim.AdamW(
            model.parameters(),
            lr=config.LEARNING_RATE,
            weight_decay=config.WEIGHT_DECAY,
            fused=(device.type == "cuda"),
        )
    except TypeError:
        optimizer = torch.optim.AdamW(
            model.parameters(),
            lr=config.LEARNING_RATE,
            weight_decay=config.WEIGHT_DECAY,
        )

    best_val_loss = float("inf")
    t0 = time.time()

    model.train()

    # Ana eğitim döngüsü.
    for iter_no in range(config.MAX_ITERS):
        if iter_no % config.EVAL_INTERVAL == 0 or iter_no == config.MAX_ITERS - 1:
            losses = estimate_loss(
                model=model,
                train_data=train_data,
                val_data=val_data,
                device=device,
                ctx=ctx,
            )

            print(
                f"\nADIM {iter_no:5d} | "
                f"Train Loss: {losses['train']:.4f} | "
                f"Val Loss: {losses['val']:.4f}"
            )

            # Validation loss iyileşirse best checkpoint güncellenir.
            if losses["val"] < best_val_loss:
                best_val_loss = losses["val"]

                save_checkpoint(
                    path=config.BEST_MODEL_FILE,
                    model=model,
                    optimizer=optimizer,
                    iter_no=iter_no,
                    best_val_loss=best_val_loss,
                    model_config=model_config,
                )

                print(f"Yeni best model kaydedildi: {config.BEST_MODEL_FILE}")

        optimizer.zero_grad(set_to_none=True)

        total_loss = 0.0

        # Küçük batch'ler birkaç adım biriktirilerek etkili batch büyütülür.
        for _ in range(config.GRAD_ACCUM_STEPS):
            x, y = get_batch(
                data=train_data,
                block_size=config.BLOCK_SIZE,
                batch_size=config.BATCH_SIZE,
                device=device,
            )

            with ctx:
                _, loss = model(x, y)
                loss = loss / config.GRAD_ACCUM_STEPS

            scaler.scale(loss).backward()
            total_loss += loss.item()

        # Gradient clip patlayan gradientleri sınırlamak için optimizer step öncesi uygulanır.
        scaler.unscale_(optimizer)
        torch.nn.utils.clip_grad_norm_(model.parameters(), config.GRAD_CLIP)

        scaler.step(optimizer)
        scaler.update()

        if iter_no % 10 == 0:
            t1 = time.time()
            print(
                f"Iter: {iter_no:5d} | "
                f"Loss: {total_loss:.4f} | "
                f"Süre: {(t1 - t0) * 1000:.0f}ms",
                end="\r",
            )
            t0 = t1

    # Eğitim bitince son model ayrıca final checkpoint olarak kaydedilir.
    save_checkpoint(
        path=config.FINAL_MODEL_FILE,
        model=model,
        optimizer=optimizer,
        iter_no=config.MAX_ITERS,
        best_val_loss=best_val_loss,
        model_config=model_config,
    )

    # Eğitim özeti entegrasyon ve takip için JSON olarak yazılır.
    summary = {
        "final_model": str(config.FINAL_MODEL_FILE),
        "best_model": str(config.BEST_MODEL_FILE),
        "best_val_loss": best_val_loss,
        "config": {
            "VOCAB_SIZE": model_config.VOCAB_SIZE,
            "BLOCK_SIZE": model_config.BLOCK_SIZE,
            "N_EMBD": model_config.N_EMBD,
            "N_HEAD": model_config.N_HEAD,
            "N_LAYER": model_config.N_LAYER,
            "DROPOUT": model_config.DROPOUT,
        },
    }

    (config.OUTPUTS_DIR / "train_summary.json").write_text(
        json.dumps(summary, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )

    print("\n" + "=" * 70)
    print("Eğitim tamamlandı")
    print("=" * 70)
    print(f"Final model: {config.FINAL_MODEL_FILE}")
    print(f"Best model : {config.BEST_MODEL_FILE}")
    print(f"Best val loss: {best_val_loss:.4f}")


if __name__ == "__main__":
    train()
