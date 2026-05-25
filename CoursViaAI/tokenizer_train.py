from tokenizers import Tokenizer
from tokenizers.models import BPE
from tokenizers.trainers import BpeTrainer
from tokenizers.pre_tokenizers import Whitespace

import config


SPECIAL_TOKENS = [
    "<|endoftext|>",
    "[PAD]",
    "[UNK]",
    "[GOREV]",
    "[KURS]",
    "[YANIT]",
    "[OGRENCI_SAYISI]",
    "[ORTALAMA_PUAN]",
    "[TAMAMLANMA]",
    "[ZORLANILAN_BOLUM]",
    "[ZORLANILAN_DERSLER]",
    "[YANLIS_ORANI]",
    "[SINAV_PUANI]",
    "[GECME_PUANI]",
]


def train_tokenizer():
    if not config.TRAIN_FILE.exists():
        raise FileNotFoundError(
            f"Önce data_generator.py çalıştırılmalı. Bulunamadı: {config.TRAIN_FILE}"
        )

    print("Tokenizer eğitiliyor...")

    tokenizer = Tokenizer(BPE(unk_token="[UNK]"))
    tokenizer.pre_tokenizer = Whitespace()

    trainer = BpeTrainer(
        vocab_size=config.VOCAB_SIZE,
        min_frequency=2,
        special_tokens=SPECIAL_TOKENS,
        show_progress=True,
    )

    tokenizer.train(
        files=[
            str(config.TRAIN_FILE),
            str(config.VAL_FILE),
        ],
        trainer=trainer,
    )

    tokenizer.save(str(config.TOKENIZER_FILE))

    print(f"Tokenizer kaydedildi: {config.TOKENIZER_FILE}")
    print(f"Vocab size: {tokenizer.get_vocab_size()}")


if __name__ == "__main__":
    train_tokenizer()