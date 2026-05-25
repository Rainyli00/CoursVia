import argparse
import json
import re
import sys

from generate import Generator


EGITMEN_DEMO_DATA = {
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


OGRENCI_DEMO_DATA = {
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


def print_json(payload):
    print(json.dumps(payload, ensure_ascii=False), flush=True)


def stdin_oku() -> str:
    try:
        raw_bytes = sys.stdin.buffer.read()

        if not raw_bytes:
            return ""

        # utf-8-sig BOM varsa temizler.
        return raw_bytes.decode("utf-8-sig", errors="replace")

    except Exception:
        return sys.stdin.read()


def json_parcasi_ayikla(raw: str) -> str:
    raw = (raw or "").strip()
    raw = raw.lstrip("\ufeff").strip()

    if not raw:
        return ""

    # Bazı durumlarda stdout/stderr karışımı veya fazladan karakter olabilir.
    # İlk { ile son } arasını alıyoruz.
    ilk = raw.find("{")
    son = raw.rfind("}")

    if ilk >= 0 and son > ilk:
        return raw[ilk:son + 1].strip()

    return raw


def parse_json_or_prompt(raw: str):
    raw = (raw or "").strip()

    if not raw:
        return {
            "__parse_error": "Python tarafına JSON veri gelmedi. Stdin boş.",
            "__raw": ""
        }

    raw = raw.lstrip("\ufeff").strip()

    # Prompt geldiyse destekle
    if "[GOREV]" in raw and "[YANIT]" in raw:
        return {
            "prompt": raw
        }

    json_text = json_parcasi_ayikla(raw)

    if not json_text:
        return {
            "__parse_error": "JSON veri boş geldi.",
            "__raw": raw[:1000]
        }

    try:
        return json.loads(json_text)
    except Exception as ex:
        return {
            "__parse_error": f"JSON okunamadı: {ex}",
            "__raw": raw[:1000],
            "__json_text": json_text[:1000],
        }


def read_request(args):
    if args.demo:
        return dict(EGITMEN_DEMO_DATA) if args.mode == "egitmen" else dict(OGRENCI_DEMO_DATA)

    if args.json:
        return parse_json_or_prompt(args.json)

    raw = stdin_oku()

    if raw.strip():
        return parse_json_or_prompt(raw)

    return {
        "__parse_error": "JSON veri gelmedi. Web tarafı MiniCoursVia'ya gerçek sistem verisini JSON olarak göndermeli.",
        "__raw": ""
    }


def get_any(data: dict, *keys, default=None):
    for key in keys:
        if key in data and data[key] is not None:
            return data[key]

    return default


def normalize_lessons(value):
    if value is None:
        return []

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
        return [
            x.strip()
            for x in value.split(",")
            if x.strip()
        ]

    return []


def get_tag(prompt: str, tag: str, default=""):
    pattern = rf"^\[{re.escape(tag)}\]\s*(.+)$"
    match = re.search(pattern, prompt, flags=re.MULTILINE)

    if match:
        return match.group(1).strip()

    return default


def parse_prompt_to_data(prompt: str, mode: str):
    dersler_text = get_tag(prompt, "ZORLANILAN_DERSLER")

    if not dersler_text:
        tek_ders = get_tag(prompt, "ZORLANILAN_DERS")
        dersler = [tek_ders] if tek_ders else []
    else:
        dersler = [
            x.strip()
            for x in dersler_text.split(",")
            if x.strip()
        ]

    if mode == "egitmen":
        return {
            "kurs": get_tag(prompt, "KURS"),
            "ogrenci_sayisi": get_tag(prompt, "OGRENCI_SAYISI", "0"),
            "ortalama_puan": get_tag(prompt, "ORTALAMA_PUAN", "0"),
            "tamamlanma": get_tag(prompt, "TAMAMLANMA", "%0"),
            "zorlanilan_bolum": get_tag(prompt, "ZORLANILAN_BOLUM"),
            "zorlanilan_dersler": dersler,
            "yanlis_orani": get_tag(prompt, "YANLIS_ORANI", "%0"),
        }

    return {
        "kurs": get_tag(prompt, "KURS"),
        "sinav_puani": get_tag(prompt, "SINAV_PUANI", "0"),
        "gecme_puani": get_tag(prompt, "GECME_PUANI", "0"),
        "zorlanilan_bolum": get_tag(prompt, "ZORLANILAN_BOLUM"),
        "zorlanilan_dersler": dersler,
    }


def build_egitmen_data(request: dict):
    if "prompt" in request and str(request["prompt"]).strip():
        return parse_prompt_to_data(str(request["prompt"]), "egitmen")

    return {
        "kurs": get_any(
            request,
            "kurs",
            "kurs_adi",
            "KursAdi",
            "courseName",
            default=""
        ),
        "ogrenci_sayisi": get_any(
            request,
            "ogrenci_sayisi",
            "toplam_ogrenci_sayisi",
            "ToplamOgrenciSayisi",
            "studentCount",
            default=0
        ),
        "ortalama_puan": get_any(
            request,
            "ortalama_puan",
            "OrtalamaPuan",
            "averageScore",
            default=0
        ),
        "tamamlanma": get_any(
            request,
            "tamamlanma",
            "genel_tamamlanma_orani",
            "GenelTamamlanmaOrani",
            "completionRate",
            default="%0"
        ),
        "zorlanilan_bolum": get_any(
            request,
            "zorlanilan_bolum",
            "ZorlanilanBolum",
            "hardSection",
            default=""
        ),
        "zorlanilan_dersler": normalize_lessons(
            get_any(
                request,
                "zorlanilan_dersler",
                "ZorlanilanDersler",
                "hardLessons",
                default=[]
            )
        ),
        "yanlis_orani": get_any(
            request,
            "yanlis_orani",
            "YanlisOrani",
            "wrongRate",
            default="%0"
        ),
    }


def build_ogrenci_data(request: dict):
    if "prompt" in request and str(request["prompt"]).strip():
        return parse_prompt_to_data(str(request["prompt"]), "ogrenci")

    return {
        "kurs": get_any(
            request,
            "kurs",
            "kurs_adi",
            "KursAdi",
            "courseName",
            default=""
        ),
        "sinav_puani": get_any(
            request,
            "sinav_puani",
            "SinavPuani",
            "examScore",
            default=0
        ),
        "gecme_puani": get_any(
            request,
            "gecme_puani",
            "GecmePuani",
            "passingScore",
            default=0
        ),
        "zorlanilan_bolum": get_any(
            request,
            "zorlanilan_bolum",
            "yanlislarin_yogunlastigi_bolum",
            "YanlislarinYogunlastigiBolum",
            "hardSection",
            default=""
        ),
        "zorlanilan_dersler": normalize_lessons(
            get_any(
                request,
                "zorlanilan_dersler",
                "yanlis_yapilan_dersler",
                "YanlisYapilanDersler",
                "hardLessons",
                default=[]
            )
        ),
    }


def validate_required(mode: str, data: dict):
    errors = []

    if not str(data.get("kurs", "")).strip():
        errors.append("kurs boş olamaz.")

    if not str(data.get("zorlanilan_bolum", "")).strip():
        errors.append("zorlanilan_bolum boş olamaz.")

    if not data.get("zorlanilan_dersler"):
        errors.append("zorlanilan_dersler boş olamaz.")

    if mode == "egitmen":
        if data.get("ogrenci_sayisi") is None:
            errors.append("ogrenci_sayisi boş olamaz.")

        if data.get("ortalama_puan") is None:
            errors.append("ortalama_puan boş olamaz.")

        if not str(data.get("tamamlanma", "")).strip():
            errors.append("tamamlanma boş olamaz.")

        if not str(data.get("yanlis_orani", "")).strip():
            errors.append("yanlis_orani boş olamaz.")

    else:
        if data.get("sinav_puani") is None:
            errors.append("sinav_puani boş olamaz.")

        if data.get("gecme_puani") is None:
            errors.append("gecme_puani boş olamaz.")

    return errors


def main():
    parser = argparse.ArgumentParser()

    parser.add_argument(
        "--mode",
        choices=["egitmen", "ogrenci"],
        required=True
    )

    parser.add_argument(
        "--json",
        default=None,
        help="JSON verisini direkt argüman olarak gönderir."
    )

    parser.add_argument(
        "--demo",
        action="store_true",
        help="Sadece terminal testi için demo veri kullanır."
    )

    args = parser.parse_args()

    try:
        request = read_request(args)

        if "__parse_error" in request:
            print_json(
                {
                    "success": False,
                    "model": "MiniCoursViaLLM",
                    "output": "",
                    "fallback_used": False,
                    "errors": [
                        request["__parse_error"],
                        f"Raw input preview: {request.get('__raw', '')}",
                        f"JSON text preview: {request.get('__json_text', '')}"
                    ],
                    "raw_output": "",
                    "input": request,
                }
            )
            return

        if args.mode == "egitmen":
            data = build_egitmen_data(request)
        else:
            data = build_ogrenci_data(request)

        validation_errors = validate_required(args.mode, data)

        if validation_errors:
            print_json(
                {
                    "success": False,
                    "model": "MiniCoursViaLLM",
                    "output": "",
                    "fallback_used": False,
                    "errors": validation_errors,
                    "raw_output": "",
                    "input": data,
                }
            )
            return

        ai = Generator()

        if args.mode == "egitmen":
            result = ai.analyze_egitmen(data)
        else:
            result = ai.analyze_ogrenci(data)

        print_json(
            {
                "success": result.success,
                "model": "MiniCoursViaLLM",
                "output": result.output,
                "fallback_used": result.fallback_used,
                "errors": result.errors,
                "raw_output": result.raw_output,
                "input": data,
            }
        )

    except Exception as ex:
        print_json(
            {
                "success": False,
                "model": "MiniCoursViaLLM",
                "output": "",
                "fallback_used": False,
                "errors": [str(ex)],
                "raw_output": "",
            }
        )


if __name__ == "__main__":
    main()