// Sertifika doğrulama başarılıysa ekranda gösterilecek sertifika bilgileri.
export type MobileSertifikaDogrulamaDetay = {
    sertifikaId: number;
    sertifikaKodu: string;
    ogrenciAdSoyad: string;
    kursAdi: string;
    verilmeTarihi: string;
};

// Kod geçersizse sertifika null gelir, mesaj kullanıcıya gösterilebilir.
export type MobileSertifikaDogrulamaResponse = {
    basarili: boolean;
    gecerliMi: boolean;
    mesaj: string;
    sertifika: MobileSertifikaDogrulamaDetay | null;
};
