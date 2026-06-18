// Admin mobil ekranlarında kullanılacak TypeScript tipleri.
// Backend'den gelen admin API JSON cevaplarını burada tanımladım.

// Select/dropdown filtrelerinde kullanılan ortak id-ad seçeneği.
export type MobileAdminSecenek = {
    id: number;
    ad: string;
};

// Admin onay, red, durum değiştirme gibi basit işlem cevapları.
export type MobileAdminIslemResponse = {
    basarili: boolean;
    mesaj: string;
};

// Dashboard
export type MobileAdminDashboardResponse = {
    basarili: boolean;
    mesaj: string;

    toplamKullaniciSayisi: number;
    onlineKullaniciSayisi: number;
    bekleyenEgitmenBasvuruSayisi: number;
    bekleyenKursOnaySayisi: number;
    okunmamisBildirimSayisi: number;

    sonLoglar: MobileAdminLogItem[];
};

// Kullanıcılar
export type MobileAdminKullaniciListeQuery = {
    arama?: string | null;
    rolId?: number | null;
    durumId?: number | null;
    sayfa?: number;
    sayfaBasinaKayit?: number;
};

export type MobileAdminKullaniciItem = {
    kullaniciId: number;
    adSoyad: string;
    profilFotoUrl: string | null;
    roller: string;
    durumId: number;
    durumAdi: string;
    onlineMi: boolean;
};

// Kullanıcılar ekranı, detay ekranı ve detaydan açılan kurslar ekranında roller ve durumlar için ortak tipler.
export type MobileAdminKullanicilarResponse = {
    basarili: boolean;
    mesaj: string;

    arama: string | null;
    rolId: number | null;
    durumId: number | null;

    toplamKayit: number;
    sayfa: number;
    sayfaBasinaKayit: number;
    toplamSayfa: number;

    roller: MobileAdminSecenek[];
    durumlar: MobileAdminSecenek[];

    kullanicilar: MobileAdminKullaniciItem[];
};

export type MobileAdminKullaniciDetayResponse = {
    basarili: boolean;
    mesaj: string;

    kullaniciId: number;
    adSoyad: string;
    eposta: string;
    telefon: string | null;
    profilFotoUrl: string | null;

    roller: string;

    durumId: number;
    durumAdi: string;

    onlineMi: boolean;

    kayitTarihi: string;
    sonGirisTarihi: string | null;
    sonIpAdresi: string | null;

    kayitliKursSayisi: number;
    tamamlananKursSayisi: number;
    sertifikaSayisi: number;
    egitmenKursSayisi: number;

    egitmenProfiliVarMi: boolean;
    egitmenProfilId: number | null;
    egitmenDurumId: number | null;
    egitmenDurumAdi: string | null;
    uzmanlikAlani: string | null;
    biyografi: string | null;
    deneyimYili: number | null;
    websiteUrl: string | null;
    branslar: string[];
};

// Kullanıcı detayından açılan kayıtlı kurslar ekranı
export type MobileAdminKullaniciKursItem = {
    kursKayitId: number;
    kursId: number;

    kursAdi: string;
    egitmenAdSoyad: string;

    kayitTarihi: string;

    aktifMi: boolean;
    tamamlandiMi: boolean;
};

export type MobileAdminKullaniciKurslarResponse = {
    basarili: boolean;
    mesaj: string;

    kullaniciId: number;
    adSoyad: string;

    arama: string | null;

    toplamKayit: number;
    sayfa: number;
    sayfaBasinaKayit: number;
    toplamSayfa: number;

    kurslar: MobileAdminKullaniciKursItem[];
};

// Kurslar
export type MobileAdminKursListeQuery = {
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

export type MobileAdminKursItem = {
    kursId: number;

    kursAdi: string;
    kapakGorselUrl: string | null;

    egitmenAdSoyad: string;

    kategoriler: string[];

    durumId: number;
    durumAdi: string;

    ogrenciSayisi: number;
    dersSayisi: number;

    degerlendirmeSayisi: number;
    ortalamaPuan: number;

    olusturmaTarihi: string;
    guncellemeTarihi: string | null;
};

export type MobileAdminKurslarResponse = {
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

    durumlar: MobileAdminSecenek[];
    kategoriler: MobileAdminSecenek[];

    kurslar: MobileAdminKursItem[];
};

// Kurs detayında bölüm -> ders -> materyal ağacının en küçük parçası.
export type MobileAdminKursDersMateryalItem = {
    materyalId: number;
    baslik: string;
    materyalTipAdi: string;
};

export type MobileAdminKursDersItem = {
    dersId: number;
    dersAdi: string;
    siraNo: number;
    aktifMi: boolean;

    materyalVarMi: boolean;
    materyalSayisi: number;
    materyaller: MobileAdminKursDersMateryalItem[];
};

export type MobileAdminKursBolumItem = {
    bolumId: number;
    bolumAdi: string;
    siraNo: number;
    dersSayisi: number;

    dersler: MobileAdminKursDersItem[];
};

export type MobileAdminKursDetayResponse = {
    basarili: boolean;
    mesaj: string;

    kursId: number;

    kursAdi: string;
    aciklama: string | null;
    kapakGorselUrl: string | null;

    egitmenAdSoyad: string;

    durumId: number;
    durumAdi: string;

    kategoriler: string[];

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

    bolumler: MobileAdminKursBolumItem[];
};

// Eğitmen Başvuruları
export type MobileAdminEgitmenBasvuruListeQuery = {
    arama?: string | null;
    durumId?: number | null;
    sayfa?: number;
    sayfaBasinaKayit?: number;
};

export type MobileAdminEgitmenBasvuruItem = {
    egitmenProfilId: number;
    kullaniciId: number;

    adSoyad: string;
    eposta: string;

    durumId: number;
    durumAdi: string;

    sonIslemTarihi: string | null;
};

export type MobileAdminEgitmenBasvurulariResponse = {
    basarili: boolean;
    mesaj: string;

    arama: string | null;
    durumId: number | null;

    toplamKayit: number;
    sayfa: number;
    sayfaBasinaKayit: number;
    toplamSayfa: number;

    durumlar: MobileAdminSecenek[];

    basvurular: MobileAdminEgitmenBasvuruItem[];
};

export type MobileAdminEgitmenBasvuruDetayResponse = {
    basarili: boolean;
    mesaj: string;

    egitmenProfilId: number;
    kullaniciId: number;

    adSoyad: string;
    eposta: string;
    profilFotoUrl: string | null;

    biyografi: string | null;
    uzmanlikAlani: string | null;
    deneyimYili: number | null;
    websiteUrl: string | null;

    branslar: string[];

    durumId: number;
    durumAdi: string;

    sonIslemTarihi: string | null;
    aciklama: string | null;
};

// Başvuru onay/red kararında admin açıklaması opsiyonel gönderilir.
export type MobileAdminEgitmenBasvuruKararRequest = {
    aciklama?: string | null;
};

// Admin Logları
export type MobileAdminLogListeQuery = {
    arama?: string | null;
    kategori?: string;
    sirala?: "yeni" | "eski";
    sayfa?: number;
    sayfaBasinaKayit?: number;
};

export type MobileAdminLogKategori = {
    kategori: string;
    kategoriAdi: string;
};

export type MobileAdminLogItem = {
    adminLogId: number;

    adminAdSoyad: string;
    islemTipi: string;
    aciklama: string;

    ipAdresi: string | null;
    islemTarihi: string;
};

export type MobileAdminLoglarResponse = {
    basarili: boolean;
    mesaj: string;

    arama: string | null;
    kategori: string;
    sirala: string;

    toplamKayit: number;
    sayfa: number;
    sayfaBasinaKayit: number;
    toplamSayfa: number;

    kategoriler: MobileAdminLogKategori[];
    loglar: MobileAdminLogItem[];
};
