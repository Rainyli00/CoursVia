import json
import re
import unicodedata
from dataclasses import dataclass
from pathlib import Path
from types import SimpleNamespace

import torch
from tokenizers import Tokenizer

import config
from model import MiniCoursViaLLM

# Modelin ürettiği cevabın Web/API tarafına dönecek standart veri modeli.

@dataclass
class GenerateResult:
    # Web/API tarafına dönen üretim sonucunun standart veri modeli.
    success: bool
    output: str
    raw_output: str
    fallback_used: bool
    errors: list[str]


class Generator:
    # Tokenizer ve eğitilmiş MiniCoursVia modelini yükleyip üretim yapan ana sınıf.
    def __init__(self, model_path: str | Path | None = None):
        # CUDA varsa GPU, yoksa CPU kullanılır.
        self.device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

        if not config.TOKENIZER_FILE.exists():
            raise FileNotFoundError(f"Tokenizer dosyası bulunamadı: {config.TOKENIZER_FILE}")

        self.tokenizer = Tokenizer.from_file(str(config.TOKENIZER_FILE))

        # Dışarıdan model yolu verilmezse önce best checkpoint denenir.
        self.model_path = Path(model_path) if model_path else config.BEST_MODEL_FILE

        # Best model yoksa final checkpoint'e düşülür.
        if not self.model_path.exists():
            self.model_path = config.FINAL_MODEL_FILE

        if not self.model_path.exists():
            raise FileNotFoundError(f"Model dosyası bulunamadı: {self.model_path}")

        # Checkpoint hem eski hem yeni kayıt formatlarına uyumlu okunur.
        checkpoint = self._load_checkpoint(self.model_path)

        model_config = self._checkpoint_config(checkpoint)

        # Checkpoint config'i ile aynı mimaride model oluşturulur.
        self.model = MiniCoursViaLLM(model_config).to(self.device)

        state_dict = self._state_dict_from_checkpoint(checkpoint)
        self.model.load_state_dict(state_dict)
        # Model eval moduna alınır, böylece dropout ve batchnorm gibi katmanlar üretimde pasif olur.
        self.model.eval()

        # Üretimde erken durmak için end-of-text token id'si saklanır.
        self.eos_token_id = self.tokenizer.token_to_id("<|endoftext|>")

    def _load_checkpoint(self, path):
        # Yeni PyTorch sürümlerinde weights_only parametresi desteklenir, eskilerde fallback yapılır.
        try:
            return torch.load(path, map_location=self.device, weights_only=False)
        except TypeError:
            return torch.load(path, map_location=self.device)

    def _checkpoint_config(self, checkpoint):
        # Checkpoint içinde mimari config varsa onu kullanır; yoksa config.py varsayılanlarına döner.
        if isinstance(checkpoint, dict):
            cfg = checkpoint.get("config", {})

            return SimpleNamespace(
                VOCAB_SIZE=int(checkpoint.get("VOCAB_SIZE", cfg.get("VOCAB_SIZE", self.tokenizer.get_vocab_size()))),
                BLOCK_SIZE=int(checkpoint.get("BLOCK_SIZE", cfg.get("BLOCK_SIZE", config.BLOCK_SIZE))),
                N_EMBD=int(checkpoint.get("N_EMBD", cfg.get("N_EMBD", config.N_EMBD))),
                N_HEAD=int(checkpoint.get("N_HEAD", cfg.get("N_HEAD", config.N_HEAD))),
                N_LAYER=int(checkpoint.get("N_LAYER", cfg.get("N_LAYER", config.N_LAYER))),
                DROPOUT=float(checkpoint.get("DROPOUT", cfg.get("DROPOUT", 0.0))),
            )

        return SimpleNamespace(
            VOCAB_SIZE=self.tokenizer.get_vocab_size(),
            BLOCK_SIZE=config.BLOCK_SIZE,
            N_EMBD=config.N_EMBD,
            N_HEAD=config.N_HEAD,
            N_LAYER=config.N_LAYER,
            DROPOUT=0.0,
        )

    def _state_dict_from_checkpoint(self, checkpoint):
        # Farklı eğitim kayıt formatlarını tek state_dict'e indirir.
        if isinstance(checkpoint, dict) and "model_state_dict" in checkpoint:
            return checkpoint["model_state_dict"]

        if isinstance(checkpoint, dict) and "state_dict" in checkpoint:
            return checkpoint["state_dict"]

        return checkpoint

    def encode(self, text: str):
        # Metni tokenizer id dizisine çevirir.
        return self.tokenizer.encode(text).ids

    def decode(self, ids):
        # Token id dizisini özel tokenları koruyarak metne çevirir.
        return self.tokenizer.decode([int(i) for i in ids], skip_special_tokens=False)

    def generate_raw(
        self,
        prompt: str,
        max_new_tokens: int | None = None,
        temperature: float | None = None,
        top_k: int | None = None,
    ):
        # Parametre verilmezse config.py içindeki varsayılan üretim ayarları kullanılır.
        max_new_tokens = max_new_tokens if max_new_tokens is not None else config.GENERATE_MAX_NEW_TOKENS
        temperature = temperature if temperature is not None else config.TEMPERATURE
        top_k = top_k if top_k is not None else config.TOP_K

        # Prompt token id dizisine çevrilir ve modele tek batch olarak verilir.
        input_ids = self.encode(prompt)

        if not input_ids:
            raise ValueError("Prompt encode edilemedi.")

        idx = torch.tensor([input_ids], dtype=torch.long, device=self.device)

        # Üretim sırasında gradient gerekmez.
        with torch.no_grad():
            out = self.model.generate(
                idx,
                max_new_tokens=max_new_tokens,
                temperature=temperature,
                top_k=top_k,
                repetition_penalty=config.REPETITION_PENALTY,
                eos_token_id=self.eos_token_id,
            )

        return self.decode(out[0].tolist())

    def generate(self, prompt: str, max_new_tokens=500, temperature=0.0):
        # Ham model çıktısından sadece [YANIT] sonrası cevap bölümü alınıp temizlenir.
        raw = self.generate_raw(
            prompt=prompt,
            max_new_tokens=max_new_tokens,
            temperature=temperature,
            top_k=0,
        )

        return clean_text(extract_answer(raw))

    def analyze_egitmen(self, data: dict) -> GenerateResult:
        # Eğitmen JSON verisi prompta çevrilir, model cevabı üretilir ve doğrulanır.

        
        prompt = build_egitmen_prompt(data)

# Model cevabı ham olarak alınır, sadece [YANIT] sonrası temizlenir ve doğrulama yapılır.
        raw = self.generate_raw(prompt)
        answer = clean_text(extract_answer(raw))

        errors = validate_output(answer, "egitmen", data)

        # Format/veri koruma hatası varsa güvenli fallback metni kullanılır.
        if errors:
            return GenerateResult(
                success=True,
                output=fallback_egitmen(data),
                raw_output=raw,
                fallback_used=True,
                errors=errors,
            )

        return GenerateResult(
            success=True,
            output=answer,
            raw_output=raw,
            fallback_used=False,
            errors=[],
        )

    def analyze_ogrenci(self, data: dict) -> GenerateResult:
        # Öğrenci JSON verisi prompta çevrilir, model cevabı üretilir ve doğrulanır.
        prompt = build_ogrenci_prompt(data)

        raw = self.generate_raw(prompt)
        answer = clean_text(extract_answer(raw))

        errors = validate_output(answer, "ogrenci", data)

        # Format/veri koruma hatası varsa güvenli fallback metni kullanılır.
        if errors:
            return GenerateResult(
                success=True,
                output=fallback_ogrenci(data),
                raw_output=raw,
                fallback_used=True,
                errors=errors,
            )

        return GenerateResult(
            success=True,
            output=answer,
            raw_output=raw,
            fallback_used=False,
            errors=[],
        )


def normalize_lessons(value) -> list[str]:
    # Ders listesi dict/list/string formatında gelebilir; hepsi düz string listesine çevrilir.
    if isinstance(value, list):
        result = []

        for item in value:
            if isinstance(item, dict):
                name = (
                    item.get("ders")
                    or item.get("ders_adi")
                    or item.get("DersAdi")
                    or item.get("dersAdi")
                    or item.get("lesson")
                    or item.get("Lesson")
                    or ""
                )

                if str(name).strip():
                    result.append(str(name).strip())

            elif str(item).strip():
                result.append(str(item).strip())

        return result

    if isinstance(value, str):
        return [x.strip() for x in value.split(",") if x.strip()]

    return []


def build_egitmen_prompt(data: dict) -> str:
    # Web tarafından gelen eğitmen verisini modelin eğitimde gördüğü etiketli prompt formatına çevirir.
    lessons = normalize_lessons(data.get("zorlanilan_dersler", []))

    return f"""[GOREV] EGITMEN_KURS_ANALIZI
[KURS] {data["kurs"]}
[OGRENCI_SAYISI] {data["ogrenci_sayisi"]}
[ORTALAMA_PUAN] {data["ortalama_puan"]}
[TAMAMLANMA] {data["tamamlanma"]}
[ZORLANILAN_BOLUM] {data["zorlanilan_bolum"]}
[ZORLANILAN_DERSLER] {", ".join(lessons)}
[YANLIS_ORANI] {data["yanlis_orani"]}
[YANIT]
"""


def build_ogrenci_prompt(data: dict) -> str:
    # Web tarafından gelen öğrenci verisini modelin eğitimde gördüğü etiketli prompt formatına çevirir.
    lessons = normalize_lessons(data.get("zorlanilan_dersler", []))

    return f"""[GOREV] OGRENCI_CALISMA_ONERISI
[KURS] {data["kurs"]}
[SINAV_PUANI] {data["sinav_puani"]}
[GECME_PUANI] {data["gecme_puani"]}
[ZORLANILAN_BOLUM] {data["zorlanilan_bolum"]}
[ZORLANILAN_DERSLER] {", ".join(lessons)}
[YANIT]
"""


def extract_answer(text: str) -> str:
    # Model bazen promptu da geri basabilir; sadece [YANIT] sonrası cevap bölümü tutulur.
    if "[YANIT]" in text:
        text = text.split("[YANIT]", 1)[1]

    cut_markers = [
        "<|endoftext|>",
        "[GOREV]",
        "[KURS]",
        "[YANIT]",
        "[OGRENCI_SAYISI]",
        "[SINAV_PUANI]",
        "[ZORLANILAN_BOLUM]",
    ]

    # Yeni bir sistem etiketi başladıysa cevap burada kesilir.
    for marker in cut_markers:
        if marker in text:
            text = text.split(marker, 1)[0]

    return text.strip()


def clean_text(text: str) -> str:
    # Tokenizer/model kaynaklı boşluk ve noktalama bozulmalarını düzeltir.
    text = text.replace("\r\n", "\n").replace("\r", "\n")

    # Noktalama öncesi boşlukları temizle:
    # "oranı % 64 ." -> "oranı % 64."
    text = re.sub(r"\s+([.,;:!?])", r"\1", text)

    # Parantez çevresi
    text = re.sub(r"\(\s+", "(", text)
    text = re.sub(r"\s+\)", ")", text)

    # Tokenizer kaynaklı tire boşluklarını temizle:
    # "Ural - Altay" -> "Ural-Altay"
    # "CVX - 09" -> "CVX-09"
    text = re.sub(r"(?<=\w)\s*-\s*(?=\w)", "-", text)

    # CVX kodlarında tokenizer boşluklarını düzelt:
    # "CVX-09 231" -> "CVX-09231"
    # "CVX-050 59" -> "CVX-05059"
    def fix_cvx(match):
        raw = match.group(0)
        raw = re.sub(r"\s+", "", raw)
        return raw

    text = re.sub(r"CVX-\d[\d\s]*", fix_cvx, text)

    # Yüzde boşluk düzeltmesi:
    # "% 64" -> "%64"
    text = re.sub(r"%\s+(\d)", r"%\1", text)

    # Ondalık sayı boşluk düzeltmesi:
    # "4 . 4" -> "4.4"
    text = re.sub(r"(\d)\s*\.\s*(\d)", r"\1.\2", text)

    # Fazla boşluklar
    text = re.sub(r"[ \t]+", " ", text)
    text = re.sub(r"\n{3,}", "\n\n", text)

    return text.strip()


def canonical_text(text: str) -> str:
    # Validasyon için metni aksan, boşluk ve tire farklarından arındırılmış karşılaştırma formuna çevirir.
    text = clean_text(str(text))

    # Unicode normalizasyon
    text = unicodedata.normalize("NFKD", text)

    # Türkçe ve aksan farklarını yumuşat
    text = "".join(ch for ch in text if not unicodedata.combining(ch))
    text = text.replace("ı", "i").replace("İ", "i")

    text = text.lower()

    # Tire varyasyonları
    text = text.replace("‐", "-").replace("–", "-").replace("—", "-")

    # Kelime/sayı arası tire boşluklarını yok say
    text = re.sub(r"(?<=\w)\s*-\s*(?=\w)", "-", text)

    # Tüm boşlukları kaldır.
    # Böylece "CVX-09231" ile "CVX - 09 231" aynı kabul edilir.
    text = re.sub(r"\s+", "", text)

    return text


def contains_field(output_text: str, field: str) -> bool:
    # Alanın çıktı içinde korunup korunmadığını esnek metin karşılaştırmasıyla kontrol eder.
    return canonical_text(field) in canonical_text(output_text)


def required_fields(mode: str, data: dict) -> list[str]:
    # Model çıktısında mutlaka korunması gereken kurs/bölüm/ders alanları.
    lessons = normalize_lessons(data.get("zorlanilan_dersler", []))

    return [
        str(data["kurs"]),
        str(data["zorlanilan_bolum"]),
        *lessons,
    ]


def validate_output(text: str, mode: str, data: dict) -> list[str]:
    # Cevabın başlık, veri koruma, sistem etiketi sızıntısı ve minimum uzunluk şartlarını kontrol eder.
    errors = []

    if not text or len(text) < 80:
        errors.append("Çıktı boş veya çok kısa.")

    if mode == "egitmen":
        headings = [
            "Genel Kurs Yorumu",
            "Zorlanılan Bölüm Yorumu",
            "Geliştirme Önerisi",
            "Öncelikli Aksiyon Planı",
        ]
    else:
        headings = [
            "Genel Durum",
            "Zorlandığın Dersler",
            "Tekrar Etmen Gereken Bölüm",
            "Öncelikli Çalışma Planı",
        ]

    for heading in headings:
        if not contains_field(text, heading):
            errors.append(f"Eksik başlık: {heading}")

    for field in required_fields(mode, data):
        if field and not contains_field(text, field):
            errors.append(f"Verilen alan korunmadı: {field}")

    forbidden_markers = [
        "[GOREV]",
        "[KURS]",
        "[YANIT]",
        "[PAD]",
        "[UNK]",
        "[SINAV_PUANI]",
        "[OGRENCI_SAYISI]",
    ]

    for marker in forbidden_markers:
        if marker in text:
            errors.append(f"Sistem etiketi sızdı: {marker}")

    return errors


def fallback_egitmen(data: dict) -> str:
    # Model çıktısı validasyondan geçmezse eğitmen için veri koruyan güvenli cevap üretilir.
    lessons = normalize_lessons(data.get("zorlanilan_dersler", []))

    d1 = lessons[0] if len(lessons) > 0 else "ilgili ders"
    d2 = lessons[1] if len(lessons) > 1 else d1

    return f"""Genel Kurs Yorumu:
{data["kurs"]} kursundaki sistem verileri incelendiğinde öğrencilerin özellikle {data["zorlanilan_bolum"]} bölümünde ek desteğe ihtiyaç duyduğu görülmektedir.

Zorlanılan Bölüm Yorumu:
{data["zorlanilan_bolum"]} bölümündeki zorlanma, öğrencilerin {d1} ve {d2} derslerindeki konu bağlantılarını daha net görmesi gerektiğini göstermektedir.

Geliştirme Önerisi:
{d1} ve {d2} dersleri için anlatımın daha sade hale getirilmesi, örneklerin artırılması ve öğrencilerin sık hata yaptığı noktaların ayrıca açıklanması önerilir.

Öncelikli Aksiyon Planı:
Öncelikle {data["zorlanilan_bolum"]} bölümünü gözden geçirin. Ardından {d1} ve {d2} dersleri için kısa özet, örnek anlatım ve kontrol soruları hazırlayın."""


def fallback_ogrenci(data: dict) -> str:
    # Model çıktısı validasyondan geçmezse öğrenci için veri koruyan güvenli cevap üretilir.
    lessons = normalize_lessons(data.get("zorlanilan_dersler", []))

    d1 = lessons[0] if len(lessons) > 0 else "ilgili ders"
    d2 = lessons[1] if len(lessons) > 1 else d1
    d3 = lessons[2] if len(lessons) > 2 else d2

    ders_liste = ", ".join(lessons) if lessons else "ilgili dersler"

    return f"""Genel Durum:
{data["kurs"]} sınav sonucuna göre {data["zorlanilan_bolum"]} bölümündeki bazı konuları tekrar gözden geçirmen gerekiyor.

Zorlandığın Dersler:
Yanlışların özellikle {ders_liste} derslerinde yoğunlaşmış görünüyor. Bu derslerdeki konu mantığını ve örnekleri yeniden incelemen faydalı olacaktır.

Tekrar Etmen Gereken Bölüm:
{data["zorlanilan_bolum"]} bölümü tekrar etmen gereken ana alan olarak öne çıkıyor. Bu bölümdeki dersleri bağlantılı şekilde ele almalısın.

Öncelikli Çalışma Planı:
Önce {d1} dersini tekrar incele. Ardından {d2} ve {d3} derslerindeki önceki hatalarını analiz et. CoursVia içindeki ilgili ders kaynakları üzerinden eksiklerini toparlamaya çalış."""


if __name__ == "__main__":
    ai = Generator()

    egitmen_data = {
        "kurs": "Türkçe Dil Bilgisi",
        "ogrenci_sayisi": 42,
        "ortalama_puan": 4.4,
        "tamamlanma": "%81",
        "zorlanilan_bolum": "Cümle Bilgisi ve Anlam",
        "zorlanilan_dersler": [
            "Cümlenin Ögeleri",
            "Fiilimsi Türleri",
        ],
        "yanlis_orani": "%64",
    }

    ogrenci_data = {
        "kurs": "Türkçe Dil Bilgisi",
        "sinav_puani": 58,
        "gecme_puani": 70,
        "zorlanilan_bolum": "Cümle Bilgisi ve Anlam",
        "zorlanilan_dersler": [
            "Cümlenin Ögeleri",
            "Fiilimsi Türleri",
            "Paragrafta Yardımcı Düşünce",
        ],
    }

    print("EĞİTMEN TESTİ")
    result = ai.analyze_egitmen(egitmen_data)
    print(result.output)
    print("Fallback:", result.fallback_used)
    print("Errors:", result.errors)

    print("\n" + "=" * 80 + "\n")

    print("ÖĞRENCİ TESTİ")
    result = ai.analyze_ogrenci(ogrenci_data)
    print(result.output)
    print("Fallback:", result.fallback_used)
    print("Errors:", result.errors)
