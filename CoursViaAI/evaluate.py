import random
import re
from pathlib import Path

import config
from generate import Generator, extract_answer, clean_text, validate_output


def read_examples(path: Path) -> list[str]:
    # Test dosyasındaki örnekleri <|endoftext|> sınırına göre ayırır.
    raw = path.read_text(encoding="utf-8")

    examples = []

    for item in raw.split("<|endoftext|>"):
        item = item.strip()

        if "[GOREV]" in item and "[YANIT]" in item:
            examples.append(item + "\n<|endoftext|>")

    return examples


def parse_prompt_and_expected(example: str):
    # Örneği model girdisi olan prompt ve beklenen cevap bölümü olarak ikiye ayırır.
    prompt, expected = example.split("[YANIT]", 1)

    prompt = prompt.strip() + "\n[YANIT]\n"
    expected = expected.split("<|endoftext|>", 1)[0].strip()

    return prompt, expected


def get_tag(prompt: str, tag: str, default=""):
    # Prompt içindeki [TAG] değerini satır bazlı regex ile okur.
    pattern = rf"^\[{re.escape(tag)}\]\s*(.+)$"

    match = re.search(pattern, prompt, flags=re.MULTILINE)

    if match:
        return match.group(1).strip()

    return default


def parse_data_from_prompt(prompt: str) -> tuple[str, dict]:
    # Prompt etiketlerinden validate_output fonksiyonunun beklediği data sözlüğü yeniden kurulur.
    mode = "egitmen" if "EGITMEN_KURS_ANALIZI" in prompt else "ogrenci"

    dersler = [
        x.strip()
        for x in get_tag(prompt, "ZORLANILAN_DERSLER").split(",")
        if x.strip()
    ]

    data = {
        "kurs": get_tag(prompt, "KURS"),
        "zorlanilan_bolum": get_tag(prompt, "ZORLANILAN_BOLUM"),
        "zorlanilan_dersler": dersler,
    }

    if mode == "egitmen":
        data.update(
            {
                "ogrenci_sayisi": get_tag(prompt, "OGRENCI_SAYISI"),
                "ortalama_puan": get_tag(prompt, "ORTALAMA_PUAN"),
                "tamamlanma": get_tag(prompt, "TAMAMLANMA"),
                "yanlis_orani": get_tag(prompt, "YANLIS_ORANI"),
            }
        )
    else:
        data.update(
            {
                "sinav_puani": get_tag(prompt, "SINAV_PUANI"),
                "gecme_puani": get_tag(prompt, "GECME_PUANI"),
            }
        )

    return mode, data


def run_automated_test(num_tests=100):
    # Test setinden rastgele örnekler seçip modelin format/veri koruma başarısını ölçer.
    if not config.TEST_FILE.exists():
        raise FileNotFoundError("Önce data_generator.py çalıştırılmalı.")

    examples = read_examples(config.TEST_FILE)

    if not examples:
        raise RuntimeError("Test dosyasında geçerli örnek bulunamadı.")

    # İstenen test sayısı dosyadaki örnek sayısını aşarsa mevcut örnek sayısı kadar test yapılır.
    selected = random.sample(examples, min(num_tests, len(examples)))

    ai = Generator()

    total = len(selected)

    success = 0
    fallback_needed = 0
    format_error = 0
    copy_error = 0
    leak_error = 0
    short_error = 0

    sample_failures = []

    print("=" * 70)
    print("MiniCoursViaLLM V3 Blind Test")
    print("=" * 70)

    # Her örnekte beklenen cevap kullanılmaz; model prompttan sıfırdan üretim yapar.
    for index, example in enumerate(selected, start=1):
        prompt, _ = parse_prompt_and_expected(example)
        mode, data = parse_data_from_prompt(prompt)

        raw = ai.generate_raw(
            prompt=prompt,
            max_new_tokens=config.GENERATE_MAX_NEW_TOKENS,
            temperature=0.0,
            top_k=0,
        )

        answer = clean_text(extract_answer(raw))

        errors = validate_output(answer, mode, data)

        # validate_output hata döndürmezse cevap doğrudan kullanılabilir kabul edilir.
        if not errors:
            success += 1
        else:
            fallback_needed += 1

            if any("Eksik başlık" in e for e in errors):
                format_error += 1

            if any("korunmadı" in e for e in errors):
                copy_error += 1

            if any("etiketi" in e for e in errors):
                leak_error += 1

            if any("kısa" in e or "boş" in e for e in errors):
                short_error += 1

            # İlk birkaç hata örneği sonradan incelemek için saklanır.
            if len(sample_failures) < 5:
                sample_failures.append(
                    {
                        "mode": mode,
                        "prompt": prompt,
                        "answer": answer,
                        "errors": errors,
                    }
                )

        print(f"[{index}/{total}] test edildi...", end="\r")

    # Başarı oranı fallback gerektirmeyen cevapların toplam teste oranıdır.
    success_rate = success / total * 100

    print("\n" + "=" * 70)
    print("MiniCoursViaLLM V3 Test Karnesi")
    print("=" * 70)
    print(f"Test sayısı                 : {total}")
    print(f"Doğrudan geçerli çıktı       : {success} / {total} (%{success_rate:.2f})")
    print(f"Fallback gerektiren çıktı    : {fallback_needed}")
    print(f"Format hatası               : {format_error}")
    print(f"Veri kopyalama hatası        : {copy_error}")
    print(f"Sistem etiketi sızıntısı     : {leak_error}")
    print(f"Kısa/boş çıktı               : {short_error}")
    print("=" * 70)

    if sample_failures:
        print("\nİlk hata örnekleri:")
        print("-" * 70)

        for item in sample_failures:
            print(f"Mode: {item['mode']}")
            print("Errors:")

            for err in item["errors"]:
                print(f"- {err}")

            print("Prompt Preview:")
            print(item["prompt"][:500])
            print("Answer Preview:")
            print(item["answer"][:700])
            print("-" * 70)

    if success_rate >= 90:
        print("Sonuç: İyi. Model web entegrasyonunda fallback ile güvenli kullanılabilir.")
    elif success_rate >= 70:
        print("Sonuç: Orta. Veri bağlı fallback zorunlu kullanılmalı.")
    else:
        print("Sonuç: Zayıf görünüyor; ancak hata örnekleri tekrar incelenmeli.")


if __name__ == "__main__":
    run_automated_test(100)
