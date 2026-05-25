// Mobil AI öneriler API tipleri.
// Öğrenci ve eğitmen aynı ekranı kullanır.

export type MobileAiOneriSiralama = "yeni" | "eski" | "kurs-az" | "kurs-za";

export type MobileAiOneriItem = {
    oneriId: number;

    oneriTipId: number;
    oneriTipAdi: string;

    kursId: number | null;
    kursAdi: string | null;

    oneriMetni: string;

    olusturmaTarihi: string;
};

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

export type MobileAiOneriSilResponse = {
    basarili: boolean;
    mesaj: string;
};