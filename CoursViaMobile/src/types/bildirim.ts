// Mobil ortak bildirim API tipleri.
// Öğrenci, eğitmen ve admin tarafı aynı bildirim tiplerini kullanır.

// Bildirim liste filtresi.
export type MobileBildirimDurum = "tum" | "okunmamis" | "okunmus";

// Tek bir bildirim kartı tipi.
export type MobileBildirimItem = {
    bildirimId: number;

    bildirimTipId: number;
    bildirimTipAdi: string;

    baslik: string;
    mesaj: string;

    okunduMu: boolean;
    olusturmaTarihi: string;
};

// GET /api/mobile/bildirimler response tipi.
// Liste cevabı ekranda rozet için okunmamış bildirim sayısını da taşır.
export type MobileBildirimlerResponse = {
    basarili: boolean;
    mesaj: string;

    durum: MobileBildirimDurum;

    toplamKayit: number;
    sayfa: number;
    sayfaBasinaKayit: number;
    toplamSayfa: number;

    okunmamisBildirimSayisi: number;

    bildirimler: MobileBildirimItem[];
};

// GET /api/mobile/bildirimler/ozet response tipi.
export type MobileBildirimOzetResponse = {
    basarili: boolean;
    mesaj: string;

    toplamBildirimSayisi: number;
    okunmamisBildirimSayisi: number;
};

// POST/DELETE işlemlerinde dönen ortak response tipi.
// İşlemden sonra badge sayısını güncellemek için yeni okunmamış sayı döner.
export type MobileBildirimIslemResponse = {
    basarili: boolean;
    mesaj: string;

    okunmamisBildirimSayisi: number;
};
