import random
from pathlib import Path

import config
from course_bank import (
    KURS_VERITABANI,
    BENZERSIZ_KURS_KOKLERI,
    BENZERSIZ_BOLUM_KOKLERI,
    BENZERSIZ_DERS_KOKLERI,
)


def temiz_ad(text: str) -> str:
    return " ".join(
        str(text)
        .replace("\n", " ")
        .replace("[", "")
        .replace("]", "")
        .split()
    ).strip()


def tum_kurslar() -> list[dict]:
    result = []

    for items in KURS_VERITABANI.values():
        result.extend(items)

    return result


def benzersiz_kurs_uret(index: int) -> dict:
    kod = f"CVX-{index:05d}"

    kurs_kok = random.choice(BENZERSIZ_KURS_KOKLERI)

    bolumler = [
        f"{random.choice(BENZERSIZ_BOLUM_KOKLERI)} {kod}",
        f"{random.choice(BENZERSIZ_BOLUM_KOKLERI)} {kod}-B",
        f"{random.choice(BENZERSIZ_BOLUM_KOKLERI)} {kod}-C",
    ]

    dersler = [
        f"{random.choice(BENZERSIZ_DERS_KOKLERI)} {kod}-1",
        f"{random.choice(BENZERSIZ_DERS_KOKLERI)} {kod}-2",
        f"{random.choice(BENZERSIZ_DERS_KOKLERI)} {kod}-3",
        f"{random.choice(BENZERSIZ_DERS_KOKLERI)} {kod}-4",
    ]

    return {
        "kurs": f"{kurs_kok} {kod}",
        "bolumler": bolumler,
        "dersler": dersler,
    }


def kurs_sec(index: int, benzersiz_oran: float = 0.45) -> dict:
    # Copy-fidelity için önemli:
    # Model çok sayıda benzersiz kurs/bölüm/ders adı görürse
    # inputtaki alanları ezberdeki yakın örneğe kaydırmak yerine kopyalamayı öğrenir.
    if random.random() < benzersiz_oran:
        return benzersiz_kurs_uret(index)

    return random.choice(tum_kurslar())


def prompt_egitmen(
    kurs: str,
    ogrenci_sayisi: int,
    ortalama_puan: float,
    tamamlanma: int,
    bolum: str,
    dersler: list[str],
    yanlis_orani: int,
) -> str:
    return f"""[GOREV] EGITMEN_KURS_ANALIZI
[KURS] {kurs}
[OGRENCI_SAYISI] {ogrenci_sayisi}
[ORTALAMA_PUAN] {ortalama_puan}
[TAMAMLANMA] %{tamamlanma}
[ZORLANILAN_BOLUM] {bolum}
[ZORLANILAN_DERSLER] {", ".join(dersler)}
[YANLIS_ORANI] %{yanlis_orani}
[YANIT]
"""


def prompt_ogrenci(
    kurs: str,
    sinav_puani: int,
    gecme_puani: int,
    bolum: str,
    dersler: list[str],
) -> str:
    return f"""[GOREV] OGRENCI_CALISMA_ONERISI
[KURS] {kurs}
[SINAV_PUANI] {sinav_puani}
[GECME_PUANI] {gecme_puani}
[ZORLANILAN_BOLUM] {bolum}
[ZORLANILAN_DERSLER] {", ".join(dersler)}
[YANIT]
"""


EGITMEN_SABLONLARI = [
    {
        "genel": "{kurs} kursundaki veriler incelendiğinde öğrencilerin genel ilerleyişi değerlendirilmiştir. Ortalama puan {ortalama_puan} ve tamamlanma oranı %{tamamlanma} seviyesindedir.",
        "bolum": "Zorlanmanın özellikle {bolum} bölümünde yoğunlaştığı görülmektedir. Bu bölümdeki hata oranı %{yanlis_orani} olduğu için içeriklerin daha açıklayıcı hale getirilmesi faydalı olur.",
        "gelistirme": "{ders1} ve {ders2} dersleri öğrencilerin daha fazla desteğe ihtiyaç duyduğu alanlar olarak öne çıkmaktadır. Bu derslerde örnek akışların artırılması, önemli noktaların sadeleştirilmesi ve mevcut materyallerin gözden geçirilmesi önerilir.",
        "aksiyon": "Öncelikle {bolum} bölümünü tekrar inceleyin. Ardından {ders1} ve {ders2} dersleri için kısa özet, örnek anlatım ve kontrol soruları hazırlayın.",
    },
    {
        "genel": "{kurs} kursu için sistem verileri, öğrencilerin bazı derslerde ek desteğe ihtiyaç duyduğunu göstermektedir. Öğrenci sayısı {ogrenci_sayisi}, ortalama puan {ortalama_puan} ve tamamlanma oranı %{tamamlanma} olarak görünmektedir.",
        "bolum": "{bolum} bölümü, kurs içinde öncelikli iyileştirme alanı olarak değerlendirilmelidir. %{yanlis_orani} yanlış oranı bu bölümde anlatımın yeniden ele alınmasını gerekli kılmaktadır.",
        "gelistirme": "Özellikle {ders1} ve {ders2} derslerinde konu anlatımı daha adım adım ilerlemelidir. Öğrencilerin sık hata yaptığı noktalar için ek açıklama ve CoursVia içi destek materyali eklenebilir.",
        "aksiyon": "{bolum} bölümünü sadeleştirin, {ders1} dersine ek açıklama ekleyin ve {ders2} dersi için örnek çözüm veya kısa tekrar içeriği hazırlayın.",
    },
    {
        "genel": "{kurs} kursunda genel performans verileri analiz edilmiştir. Mevcut tablo, kursun güçlü tarafları olsa da bazı bölüm ve derslerde geliştirme ihtiyacı bulunduğunu göstermektedir.",
        "bolum": "En belirgin zorlanma {bolum} bölümünde görülmektedir. Bu bölümdeki %{yanlis_orani} yanlış oranı, öğrencilerin konu bağlantılarını tam kuramadığını gösterebilir.",
        "gelistirme": "{ders1} ve {ders2} dersleri için daha açık örnekler, kısa tekrar notları ve hata odaklı açıklamalar hazırlanabilir. Bu iyileştirme, öğrencilerin ilgili bölümü daha rahat takip etmesine yardımcı olur.",
        "aksiyon": "İlk adımda {bolum} bölümünü güncelleyin. Sonra {ders1} ve {ders2} derslerinde sık yapılan hataları açıklayan ek içerikler oluşturun.",
    },
]


OGRENCI_SABLONLARI_BASARISIZ = [
    {
        "genel": "{kurs} sınavında geçme puanına ulaşamadın. Bu sonuç, özellikle {bolum} bölümündeki temel kavramları tekrar gözden geçirmen gerektiğini gösteriyor.",
        "ders": "Yanlışların {ders_liste} derslerinde yoğunlaşmış görünüyor. Bu derslerdeki konu mantığını ve örnekleri yeniden incelemen faydalı olacaktır.",
        "bolum": "{bolum} bölümü tekrar etmen gereken ana alan olarak öne çıkıyor. Bu bölümdeki dersler birbirini desteklediği için konuları bağlantılı şekilde ele almalısın.",
        "plan": "Önce {ders1} dersini tekrar incele. Ardından {ders2} ve {ders3} derslerindeki örnekleri gözden geçir. CoursVia içindeki ilgili kaynaklar ve önceki hataların üzerinden pratik yaparak eksiklerini toparlamaya çalış.",
    },
    {
        "genel": "{kurs} sınav sonucuna göre bazı konularda eksiklerin var. Geçme puanı {gecme_puani} iken aldığın puan {sinav_puani} olduğu için çalışmanı özellikle {bolum} bölümüne yöneltmelisin.",
        "ders": "{ders_liste} dersleri tekrar etmen gereken öncelikli konulardır. Bu derslerde işlem veya kavram adımlarını dikkatlice incelemelisin.",
        "bolum": "{bolum} bölümündeki eksikler sınav sonucunu doğrudan etkilemiş görünüyor. Bu bölümü yüzeysel değil, adım adım tekrar etmen daha doğru olur.",
        "plan": "Öncelikle {ders1} konusundaki temel açıklamaları tekrar et. Sonra {ders2} ve {ders3} derslerinde yaptığın hataların nedenini analiz et. Çalışmanı CoursVia ders içerikleri üzerinden sürdür.",
    },
]


OGRENCI_SABLONLARI_BASARILI = [
    {
        "genel": "{kurs} sınavında geçme puanını aşmış olsan da {bolum} bölümündeki bazı noktalar gelişim alanı olarak görünüyor.",
        "ders": "{ders_liste} derslerinde yaptığın hatalar, bu konuları tamamen bırakmaman gerektiğini gösteriyor.",
        "bolum": "{bolum} bölümünü kısa bir tekrar ile güçlendirebilirsin. Bu tekrar sonraki konular için daha sağlam ilerlemeni sağlar.",
        "plan": "Önce {ders1} dersindeki hatalarını incele. Sonra {ders2} ve {ders3} derslerindeki CoursVia kaynaklarına kısa bir göz atarak bilgini pekiştir.",
    },
]


def completion_egitmen(
    kurs: str,
    ogrenci_sayisi: int,
    ortalama_puan: float,
    tamamlanma: int,
    bolum: str,
    dersler: list[str],
    yanlis_orani: int,
) -> str:
    sablon = random.choice(EGITMEN_SABLONLARI)

    d1 = dersler[0]
    d2 = dersler[1]

    values = {
        "kurs": kurs,
        "ogrenci_sayisi": ogrenci_sayisi,
        "ortalama_puan": ortalama_puan,
        "tamamlanma": tamamlanma,
        "bolum": bolum,
        "yanlis_orani": yanlis_orani,
        "ders1": d1,
        "ders2": d2,
    }

    return f"""Genel Kurs Yorumu:
{sablon["genel"].format(**values)}

Zorlanılan Bölüm Yorumu:
{sablon["bolum"].format(**values)}

Geliştirme Önerisi:
{sablon["gelistirme"].format(**values)}

Öncelikli Aksiyon Planı:
{sablon["aksiyon"].format(**values)}
"""


def completion_ogrenci(
    kurs: str,
    sinav_puani: int,
    gecme_puani: int,
    bolum: str,
    dersler: list[str],
) -> str:
    basarili = sinav_puani >= gecme_puani

    sablonlar = OGRENCI_SABLONLARI_BASARILI if basarili else OGRENCI_SABLONLARI_BASARISIZ
    sablon = random.choice(sablonlar)

    d1 = dersler[0]
    d2 = dersler[1] if len(dersler) > 1 else dersler[0]
    d3 = dersler[2] if len(dersler) > 2 else d2

    values = {
        "kurs": kurs,
        "sinav_puani": sinav_puani,
        "gecme_puani": gecme_puani,
        "bolum": bolum,
        "ders_liste": ", ".join(dersler),
        "ders1": d1,
        "ders2": d2,
        "ders3": d3,
    }

    return f"""Genel Durum:
{sablon["genel"].format(**values)}

Zorlandığın Dersler:
{sablon["ders"].format(**values)}

Tekrar Etmen Gereken Bölüm:
{sablon["bolum"].format(**values)}

Öncelikli Çalışma Planı:
{sablon["plan"].format(**values)}
"""


def generate_egitmen_analizi(index: int) -> str:
    kurs_data = kurs_sec(index)

    kurs = temiz_ad(kurs_data["kurs"])
    bolum = temiz_ad(random.choice(kurs_data["bolumler"]))
    dersler = [temiz_ad(x) for x in random.sample(kurs_data["dersler"], 2)]

    ogrenci_sayisi = random.randint(5, 2500)
    ortalama_puan = round(random.uniform(1.3, 4.9), 2)
    tamamlanma = random.randint(15, 98)
    yanlis_orani = random.randint(15, 90)

    prompt = prompt_egitmen(
        kurs=kurs,
        ogrenci_sayisi=ogrenci_sayisi,
        ortalama_puan=ortalama_puan,
        tamamlanma=tamamlanma,
        bolum=bolum,
        dersler=dersler,
        yanlis_orani=yanlis_orani,
    )

    completion = completion_egitmen(
        kurs=kurs,
        ogrenci_sayisi=ogrenci_sayisi,
        ortalama_puan=ortalama_puan,
        tamamlanma=tamamlanma,
        bolum=bolum,
        dersler=dersler,
        yanlis_orani=yanlis_orani,
    )

    return f"{prompt}{completion}<|endoftext|>"


def generate_ogrenci_onerisi(index: int) -> str:
    kurs_data = kurs_sec(index)

    kurs = temiz_ad(kurs_data["kurs"])
    bolum = temiz_ad(random.choice(kurs_data["bolumler"]))

    ders_sayisi = random.choice([2, 3])
    dersler = [temiz_ad(x) for x in random.sample(kurs_data["dersler"], ders_sayisi)]

    gecme_puani = random.choice([50, 60, 65, 70, 75, 80])

    # CoursVia gerçek akışı:
    # Öğrenci AI önerisi çoğunlukla sınavdan kalınca üretilecek.
    if random.random() < 0.90:
        sinav_puani = random.randint(0, max(0, gecme_puani - 1))
    else:
        sinav_puani = random.randint(gecme_puani, 100)

    prompt = prompt_ogrenci(
        kurs=kurs,
        sinav_puani=sinav_puani,
        gecme_puani=gecme_puani,
        bolum=bolum,
        dersler=dersler,
    )

    completion = completion_ogrenci(
        kurs=kurs,
        sinav_puani=sinav_puani,
        gecme_puani=gecme_puani,
        bolum=bolum,
        dersler=dersler,
    )

    return f"{prompt}{completion}<|endoftext|>"


def save_items(items: list[str], path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text("\n".join(items), encoding="utf-8")


def build_dataset(num_samples: int = 40000, seed: int = 42) -> None:
    random.seed(seed)

    dataset = []
    half = num_samples // 2

    print("MiniCoursVia V3 veri üretimi başladı.")
    print("Amaç: Kurs, bölüm ve ders adlarını birebir koruyan CoursVia örnekleri.")

    for i in range(half):
        dataset.append(generate_egitmen_analizi(i))
        dataset.append(generate_ogrenci_onerisi(i + half))

        if (i + 1) % 5000 == 0:
            print(f"{(i + 1) * 2}/{num_samples} örnek üretildi...")

    random.shuffle(dataset)

    train_end = int(len(dataset) * 0.80)
    val_end = int(len(dataset) * 0.90)

    train_data = dataset[:train_end]
    val_data = dataset[train_end:val_end]
    test_data = dataset[val_end:]

    save_items(train_data, config.TRAIN_FILE)
    save_items(val_data, config.VAL_FILE)
    save_items(test_data, config.TEST_FILE)

    print("=" * 60)
    print("Veri üretimi tamamlandı.")
    print(f"Train: {len(train_data)} -> {config.TRAIN_FILE}")
    print(f"Val  : {len(val_data)} -> {config.VAL_FILE}")
    print(f"Test : {len(test_data)} -> {config.TEST_FILE}")
    print("=" * 60)


if __name__ == "__main__":
    build_dataset(40000, config.RANDOM_SEED)