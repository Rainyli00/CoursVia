export type MobileSertifikaDogrulamaDetay = {
    sertifikaId: number;
    sertifikaKodu: string;
    ogrenciAdSoyad: string;
    kursAdi: string;
    verilmeTarihi: string;
};

export type MobileSertifikaDogrulamaResponse = {
    basarili: boolean;
    gecerliMi: boolean;
    mesaj: string;
    sertifika: MobileSertifikaDogrulamaDetay | null;
};