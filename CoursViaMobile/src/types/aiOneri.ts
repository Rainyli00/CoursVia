// Mobil AI öneriler API tipleri.
// Öğrenci ve eğitmen aynı ekranı kullanır.

// Liste ekranında desteklenen sıralama değerleri.
export type MobileAiOneriSiralama = "yeni" | "eski" | "kurs-az" | "kurs-za";

// Tek bir AI öneri kaydı; öneri kursa bağlı olmayabilir.
export type MobileAiOneriItem = {
    oneriId: number;

    oneriTipId: number;
    oneriTipAdi: string;

    kursId: number | null;
    kursAdi: string | null;

    oneriMetni: string;

    olusturmaTarihi: string;
};

// Öneri listesi sayfalama bilgileriyle birlikte döner.
export type MobileAiOnerilerResponse = {
    basarili: boolean;

    arama: string | null;
    siralama: MobileAiOneriSiralama;

    toplamKayit: number;
    sayfa: number;
    sayfaBoyutu: number;
    toplamSayfa: number;

    oncekiSayfaVarMi: boolean;
    sonrakiSayfaVarMi: boolean;

    oneriler: MobileAiOneriItem[];
};

// Öneri silme işleminden dönen sade cevap.
export type MobileAiOneriSilResponse = {
    basarili: boolean;
    mesaj: string;
};
