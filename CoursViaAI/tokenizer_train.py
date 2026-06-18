from tokenizers import Tokenizer
from tokenizers.models import BPE
from tokenizers.trainers import BpeTrainer
from tokenizers.pre_tokenizers import Whitespace

import config


# Prompt formatındaki görev ve alan etiketleri özel token olarak korunur.
# Böylece tokenizer bu kontrol etiketlerini parçalamadan öğrenir.
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
    # Tokenizer eğitimi için önce sentetik train verisinin üretilmiş olması gerekir.
    if not config.TRAIN_FILE.exists():
        raise FileNotFoundError(
            f"Önce data_generator.py çalıştırılmalı. Bulunamadı: {config.TRAIN_FILE}"
        )

    print("Tokenizer eğitiliyor...")

    # MiniCoursVia için BPE tokenizer kullanılır.
    tokenizer = Tokenizer(BPE(unk_token="[UNK]"))
    tokenizer.pre_tokenizer = Whitespace()

    # Vocab boyutu config'ten gelir; özel tokenlar mutlaka vocab içinde yer alır.
    trainer = BpeTrainer(
        vocab_size=config.VOCAB_SIZE,
        min_frequency=2,
        special_tokens=SPECIAL_TOKENS,
        show_progress=True,
    )

    # Train ve validation dosyaları tokenizer'a birlikte gösterilir.
    tokenizer.train(
        files=[
            str(config.TRAIN_FILE),
            str(config.VAL_FILE),
        ],
        trainer=trainer,
    )

    # Eğitilmiş tokenizer model klasörüne JSON olarak kaydedilir.
    tokenizer.save(str(config.TOKENIZER_FILE))

    print(f"Tokenizer kaydedildi: {config.TOKENIZER_FILE}")
    print(f"Vocab size: {tokenizer.get_vocab_size()}")


if __name__ == "__main__":
    train_tokenizer()
