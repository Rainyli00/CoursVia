// Eğitmen mobil ekranlarında kullanılacak TypeScript tipleri.
// Backend'den gelen eğitmen API JSON cevaplarını burada tanımlıyoruz.

// Eğitmen kurs liste query tipi.
// Kurslarım endpointinde arama, durum filtresi, kategori filtresi, sıralama ve sayfalama var.
export type MobileEgitmenKursListeQuery = {
    arama?: string | null;
    durumId?: number | null;
    kategoriId?: number | null;
    sirala?:
    | "guncel"
    | "eski"
    | "ad-az"
    | "ad-za"
    | "puan-yuksek"
    | "puan-dusuk"
    | "ogrenci-cok"
    | "ogrenci-az";
    sayfa?: number;
    sayfaBasinaKayit?: number;
};

// Eğitmen öğrencilerim query tipi.
// Öğrencilerim endpointinde sadece arama, kurs filtresi ve sayfalama var.
export type MobileEgitmenOgrenciListeQuery = {
    arama?: string | null;
    kursId?: number | null;
    sayfa?: number;
    sayfaBasinaKayit?: number;
};

// Dashboard son kurslarda kullanılacak sade kurs tipi.
export type MobileEgitmenDashboardKursItem = {
    kursId: number;

    kursAdi: string;
    durumAdi: string;

    ogrenciSayisi: number;
    dersSayisi: number;
};

// GET /api/mobile/egitmen/dashboard response tipi.
// Ana ekrandaki sayaçlar ve son kurs listesi tek cevapta toplanır.
export type MobileEgitmenDashboardResponse = {
    basarili: boolean;
    mesaj: string;

    toplamKursSayisi: number;
    yayindakiKursSayisi: number;
    toplamOgrenciSayisi: number;
    okunmamisBildirimSayisi: number;

    sonKurslar: MobileEgitmenDashboardKursItem[];
};

// Eğitmen kurslarım kategori filtre seçeneği tipi.
// Sadece eğitmenin kendi kurslarında kullanılan kategoriler döner.
export type MobileEgitmenKategoriSecenek = {
    kategoriId: number;
    kategoriAdi: string;
    kayitSayisi: number;
};

// Dashboard dışında kurslarım ekranında kullanılan eğitmen kurs kartı tipi.
export type MobileEgitmenKursItem = {
    kursId: number;

    kursAdi: string;
    kapakGorselUrl: string | null;

    kategoriler: string[];

    durumId: number;
    durumAdi: string;

    ogrenciSayisi: number;
    tamamlayanOgrenciSayisi: number;

    dersSayisi: number;

    degerlendirmeSayisi: number;
    ortalamaPuan: number;

    olusturmaTarihi: string;
    guncellemeTarihi: string | null;
};

// GET /api/mobile/egitmen/kurslarim response tipi.
// Filtrelerin seçili değerleri response içinde geri döner.
export type MobileEgitmenKurslarimResponse = {
    basarili: boolean;
    mesaj: string;

    arama: string | null;
    durumId: number | null;
    kategoriId: number | null;
    sirala: string;

    toplamKayit: number;
    sayfa: number;
    sayfaBasinaKayit: number;
    toplamSayfa: number;

    kategoriler: MobileEgitmenKategoriSecenek[];
    kurslar: MobileEgitmenKursItem[];
};

// Kurs detayında ders altında gösterilecek materyal tipi.
// URL yok; sadece materyal adı ve tipi gösterilir.
export type MobileEgitmenDersMateryalItem = {
    materyalId: number;
    baslik: string;
    materyalTipAdi: string;
};

// Kurs detayında bölüm altındaki ders tipi.
export type MobileEgitmenDersItem = {
    dersId: number;
    dersAdi: string;
    siraNo: number;
    aktifMi: boolean;

    materyalVarMi: boolean;
    materyalSayisi: number;
    materyaller: MobileEgitmenDersMateryalItem[];
};

// Kurs detayında gösterilecek bölüm tipi.
export type MobileEgitmenBolumItem = {
    bolumId: number;
    bolumAdi: string;
    siraNo: number;
    dersSayisi: number;

    dersler: MobileEgitmenDersItem[];
};

// Eğitmen kurs detayında gösterilecek son yorum tipi.
export type MobileEgitmenKursYorumItem = {
    degerlendirmeId: number;

    kullaniciId: number;
    ogrenciAdSoyad: string;

    puan: number;
    yorumMetni: string;

    degerlendirmeTarihi: string;
};

// GET /api/mobile/egitmen/kurslarim/{kursId} response tipi.
export type MobileEgitmenKursDetayResponse = {
    basarili: boolean;
    mesaj: string;

    kursId: number;

    kursAdi: string;
    aciklama: string | null;
    kapakGorselUrl: string | null;

    kategoriler: string[];

    durumId: number;
    durumAdi: string;

    ogrenciSayisi: number;
    tamamlayanOgrenciSayisi: number;

    bolumSayisi: number;
    dersSayisi: number;

    degerlendirmeSayisi: number;
    ortalamaPuan: number;

    sinavVarMi: boolean;
    sinavAdi: string | null;
    sinavSoruSayisi: number | null;
    sinavSureDakika: number | null;
    sinavGecmeNotu: number | null;

    olusturmaTarihi: string;
    guncellemeTarihi: string | null;

    bolumler: MobileEgitmenBolumItem[];

    sonYorumlar: MobileEgitmenKursYorumItem[];
};

// Eğitmen öğrenci kartı tipi.
// Aynı öğrenci birden fazla kursa kayıtlı olsa bile tek kayıt döner.
export type MobileEgitmenOgrenciItem = {
    kullaniciId: number;

    ogrenciAdSoyad: string;
    profilFotoUrl: string | null;

    kayitliKursSayisi: number;
};

// GET /api/mobile/egitmen/ogrencilerim response tipi.
export type MobileEgitmenOgrencilerimResponse = {
    basarili: boolean;
    mesaj: string;

    arama: string | null;
    kursId: number | null;

    toplamKayit: number;
    sayfa: number;
    sayfaBasinaKayit: number;
    toplamSayfa: number;

    ogrenciler: MobileEgitmenOgrenciItem[];
};

// Kurs taslağa çekme gibi işlem response tipi.
// Detay gerektirmeyen eğitmen aksiyonlarında bu ortak yapı kullanılır.
export type MobileEgitmenIslemResponse = {
    basarili: boolean;
    mesaj: string;
};
