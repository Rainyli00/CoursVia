// Mobil auth işlemlerinde kullanılacak TypeScript tipleri.
// Backend'den gelen JSON cevaplarının şeklini burada tanımlıyoruz.

// Login başarılı olduğunda backend'in döndürdüğü kullanıcı bilgisi.
export type MobileKullanici = {
    kullaniciId: number;
    adSoyad: string;
    eposta: string;
    profilFotoUrl: string | null;
    roller: string[];
};

// POST /api/mobile/auth/login endpointine göndereceğimiz veri tipi.
export type MobileLoginRequest = {
    eposta: string;
    sifre: string;
};

// POST /api/mobile/auth/login endpointinden dönen cevap tipi.
export type MobileLoginResponse = {
    basarili: boolean;
    mesaj: string;

    accessToken: string | null;
    accessTokenBitisTarihi: string | null;

    refreshToken: string | null;
    refreshTokenBitisTarihi: string | null;

    kullanici: MobileKullanici | null;
};

// POST /api/mobile/auth/refresh endpointine göndereceğimiz veri tipi.
export type MobileRefreshTokenRequest = {
    refreshToken: string;
};

// POST /api/mobile/auth/refresh endpointinden dönen cevap tipi.
export type MobileRefreshTokenResponse = {
    basarili: boolean;
    mesaj: string;

    accessToken: string | null;
    accessTokenBitisTarihi: string | null;

    refreshToken: string | null;
    refreshTokenBitisTarihi: string | null;
};

// POST /api/mobile/auth/logout endpointine göndereceğimiz veri tipi.
export type MobileLogoutRequest = {
    refreshToken: string;
};

// Logout gibi işlem endpointlerinden dönen genel cevap tipi.
export type MobileAuthIslemResponse = {
    basarili: boolean;
    mesaj: string;
};

// POST /api/mobile/auth/sifremi-unuttum endpointine gönderilecek veri.
export type MobileSifremiUnuttumRequest = {
    eposta: string;
};

// POST /api/mobile/auth/sifremi-unuttum endpointinden dönen cevap.
export type MobileSifremiUnuttumResponse = {
    basarili: boolean;
    mesaj: string;
    eposta: string | null;
    kodGonderildiMi: boolean;
};

// POST /api/mobile/auth/sifre-sifirla endpointine gönderilecek veri.
export type MobileSifreSifirlaRequest = {
    eposta: string;
    kod: string;
    yeniSifre: string;
};

// POST /api/mobile/auth/sifre-sifirla endpointinden dönen cevap.
export type MobileSifreSifirlaResponse = {
    basarili: boolean;
    mesaj: string;
};