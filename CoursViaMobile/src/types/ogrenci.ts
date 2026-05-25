// Öğrenci mobil ekranlarında kullanılacak TypeScript tipleri.
// Backend'den gelen öğrenci API JSON cevaplarını burada tanımlıyoruz.

// Ortak kategori filtre seçeneği.
// Kurslarım ve Keşfet ekranlarında kategori filtresi için kullanılır.
export type MobileOgrenciKategoriSecenek = {
    kategoriId: number;
    kategoriAdi: string;
    kayitSayisi: number;
};

// Kurslarım ekranı için query tipi.
// Kurslarım endpointinde arama, kategori ve sayfalama var.
export type MobileOgrenciKursListeQuery = {
    arama?: string | null;
    kategoriId?: number | null;
    sayfa?: number;
    sayfaBasinaKayit?: number;
};

// Keşfet ekranı için query tipi.
// Keşfet endpointinde arama, kategori, sıralama ve sayfalama var.
export type MobileOgrenciKesfetListeQuery = {
    arama?: string | null;
    kategoriId?: number | null;
    sirala?:
    | "guncel"
    | "puan-yuksek"
    | "populer"
    | "degerlendirme-cok"
    | "ad-az"
    | "ad-za";
    sayfa?: number;
    sayfaBasinaKayit?: number;
};

// Sınavlarım ekranı için query tipi.
// Sınavlarda kategori filtresi kaldırıldı.
export type MobileOgrenciSinavListeQuery = {
    arama?: string | null;
    sayfa?: number;
    sayfaBasinaKayit?: number;
};

// Sertifikalarım ekranı için query tipi.
// Sertifikalarda kategori filtresi yok.
export type MobileOgrenciSertifikaListeQuery = {
    arama?: string | null;
    sayfa?: number;
    sayfaBasinaKayit?: number;
};

// Dashboard altında gösterilecek son kurs kartı tipi.
export type MobileOgrenciDashboardKurs = {
    kursKayitId: number;
    kursId: number;

    kursAdi: string;
    kapakGorselUrl: string | null;

    egitmenAdSoyad: string;

    durumId: number;
    durumAdi: string;
    guncelleniyorMu: boolean;
    devamEdilebilirMi: boolean;

    toplamDersSayisi: number;
    tamamlananDersSayisi: number;
    ilerlemeYuzdesi: number;

    kursTamamlandiMi: boolean;
};

// GET /api/mobile/ogrenci/dashboard endpointinden dönen cevap tipi.
export type MobileOgrenciDashboardResponse = {
    basarili: boolean;
    mesaj: string;

    kayitliKursSayisi: number;
    devamEdenKursSayisi: number;
    tamamlananKursSayisi: number;

    ortalamaIlerlemeYuzdesi: number;

    sertifikaSayisi: number;
    okunmamisBildirimSayisi: number;

    sonKurslar: MobileOgrenciDashboardKurs[];
};

// Kurslarım ekranında listelenecek kurs kartı tipi.
export type MobileOgrenciKursItem = {
    kursKayitId: number;
    kursId: number;

    kursAdi: string;
    kapakGorselUrl: string | null;

    egitmenAdSoyad: string;

    durumId: number;
    durumAdi: string;
    guncelleniyorMu: boolean;
    devamEdilebilirMi: boolean;

    kategoriler: string[];

    kayitTarihi: string;

    toplamDersSayisi: number;
    tamamlananDersSayisi: number;
    ilerlemeYuzdesi: number;

    kursTamamlandiMi: boolean;
    tamamlanmaTarihi: string | null;

    degerlendirmeVarMi: boolean;
    kendiPuan: number | null;
    kendiYorumMetni: string | null;
};

// GET /api/mobile/ogrenci/kurslarim endpointinden dönen cevap tipi.
export type MobileOgrenciKurslarimResponse = {
    basarili: boolean;
    mesaj: string;

    arama: string | null;
    kategoriId: number | null;

    toplamKayit: number;
    sayfa: number;
    sayfaBasinaKayit: number;
    toplamSayfa: number;

    kategoriler: MobileOgrenciKategoriSecenek[];

    kurslar: MobileOgrenciKursItem[];
};

// Kurslarım detayında ders altında gösterilecek materyal tipi.
// URL yok; sadece materyal adı ve tipi gösterilir.
export type MobileOgrenciDersMateryalItem = {
    materyalId: number;
    baslik: string;
    materyalTipAdi: string;
};

// Kurs detayında gösterilecek ders tipi.
export type MobileOgrenciDersItem = {
    dersId: number;
    dersAdi: string;
    siraNo: number;

    tamamlandiMi: boolean;

    materyalVarMi: boolean;
    materyalSayisi: number;
    materyaller: MobileOgrenciDersMateryalItem[];
};

// Kurs detayında gösterilecek bölüm tipi.
export type MobileOgrenciBolumItem = {
    bolumId: number;
    bolumAdi: string;
    siraNo: number;

    toplamDersSayisi: number;
    tamamlananDersSayisi: number;
    ilerlemeYuzdesi: number;

    dersler: MobileOgrenciDersItem[];
};

// GET /api/mobile/ogrenci/kurslarim/{kursKayitId} endpointinden dönen cevap tipi.
export type MobileOgrenciKursDetayResponse = {
    basarili: boolean;
    mesaj: string;

    kursKayitId: number;
    kursId: number;

    kursAdi: string;
    aciklama: string | null;
    kapakGorselUrl: string | null;

    egitmenAdSoyad: string;

    durumId: number;
    durumAdi: string;
    guncelleniyorMu: boolean;
    devamEdilebilirMi: boolean;

    kategoriler: string[];

    kayitTarihi: string;

    toplamDersSayisi: number;
    tamamlananDersSayisi: number;
    ilerlemeYuzdesi: number;

    kursTamamlandiMi: boolean;

    degerlendirmeVarMi: boolean;
    kendiPuan: number | null;
    kendiYorumMetni: string | null;

    bolumler: MobileOgrenciBolumItem[];
};

// POST işlemlerinde ortak dönebilecek basit cevap tipi.
export type MobileOgrenciIslemResponse = {
    basarili: boolean;
    mesaj: string;
};

// POST /api/mobile/ogrenci/kurslarim/{kursKayitId}/degerlendir body tipi.
export type MobileOgrenciDegerlendirRequest = {
    puan: number;
    yorumMetni: string | null;
};

// Keşfet ekranında listelenecek kurs kartı tipi.
export type MobileOgrenciKesfetKursItem = {
    kursId: number;

    kursAdi: string;
    aciklama: string | null;
    kapakGorselUrl: string | null;

    egitmenAdSoyad: string;

    durumId: number;
    durumAdi: string;
    guncelleniyorMu: boolean;
    devamEdilebilirMi: boolean;

    kategoriler: string[];

    toplamBolumSayisi: number;
    toplamDersSayisi: number;

    kayitliOgrenciSayisi: number;

    ortalamaPuan: number;
    degerlendirmeSayisi: number;

    kayitliMi: boolean;
    kayitOlabilirMi: boolean;
    kendiKursuMu: boolean;
};

// GET /api/mobile/ogrenci/kesfet endpointinden dönen cevap tipi.
export type MobileOgrenciKesfetResponse = {
    basarili: boolean;
    mesaj: string;

    arama: string | null;
    kategoriId: number | null;
    sirala: string;

    toplamKayit: number;
    sayfa: number;
    sayfaBasinaKayit: number;
    toplamSayfa: number;

    kategoriler: MobileOgrenciKategoriSecenek[];

    kurslar: MobileOgrenciKesfetKursItem[];
};

// Keşfet detayında gösterilecek ders tipi.
// Keşfet detayda materyal adı gösterilmez, sadece materyal sayısı gösterilir.
export type MobileOgrenciKesfetDers = {
    dersId: number;
    dersAdi: string;
    siraNo: number;

    materyalVarMi: boolean;
    materyalSayisi: number;
};

// Keşfet detayında gösterilecek bölüm tipi.
export type MobileOgrenciKesfetBolum = {
    bolumId: number;
    bolumAdi: string;
    siraNo: number;

    dersSayisi: number;

    dersler: MobileOgrenciKesfetDers[];
};

// GET /api/mobile/ogrenci/kesfet/{kursId} endpointinden dönen cevap tipi.
export type MobileOgrenciKesfetDetayResponse = {
    basarili: boolean;
    mesaj: string;

    kursId: number;

    kursAdi: string;
    aciklama: string | null;
    kapakGorselUrl: string | null;

    egitmenAdSoyad: string;

    durumId: number;
    durumAdi: string;
    guncelleniyorMu: boolean;
    devamEdilebilirMi: boolean;

    kategoriler: string[];

    toplamBolumSayisi: number;
    toplamDersSayisi: number;

    kayitliOgrenciSayisi: number;

    ortalamaPuan: number;
    degerlendirmeSayisi: number;

    sinavVarMi: boolean;
    gecmeNotu: number | null;

    kayitliMi: boolean;
    kayitOlabilirMi: boolean;
    kendiKursuMu: boolean;

    bolumler: MobileOgrenciKesfetBolum[];
};

// Sınavlarım ekranında gösterilecek sınav durum kartı tipi.
export type MobileOgrenciSinavItem = {
    kursKayitId: number;
    kursId: number;

    kursAdi: string;
    kapakGorselUrl: string | null;

    durumId: number;
    durumAdi: string;
    guncelleniyorMu: boolean;
    devamEdilebilirMi: boolean;

    sinavId: number | null;
    sinavAdi: string | null;

    gecmeNotu: number | null;

    derslerTamamlandiMi: boolean;
    toplamDersSayisi: number;
    tamamlananDersSayisi: number;

    girisSayisi: number;
    kalanHak: number;

    sonPuan: number | null;
    sonucGectiMi: boolean | null;
    sonSinavTarihi: string | null;

    durumMetni: string;
};

// GET /api/mobile/ogrenci/sinavlarim endpointinden dönen cevap tipi.
export type MobileOgrenciSinavlarimResponse = {
    basarili: boolean;
    mesaj: string;

    arama: string | null;

    toplamKayit: number;
    sayfa: number;
    sayfaBasinaKayit: number;
    toplamSayfa: number;

    sinavlar: MobileOgrenciSinavItem[];
};

// Sertifikalarım ekranında gösterilecek sertifika kartı tipi.
export type MobileOgrenciSertifikaItem = {
    sertifikaId: number;

    kursId: number;
    kursAdi: string;
    kapakGorselUrl: string | null;

    egitmenAdSoyad: string;

    sertifikaKodu: string;
    verilmeTarihi: string;
};

// GET /api/mobile/ogrenci/sertifikalarim endpointinden dönen cevap tipi.
export type MobileOgrenciSertifikalarimResponse = {
    basarili: boolean;
    mesaj: string;

    arama: string | null;

    toplamKayit: number;
    sayfa: number;
    sayfaBasinaKayit: number;
    toplamSayfa: number;

    sertifikalar: MobileOgrenciSertifikaItem[];
};
