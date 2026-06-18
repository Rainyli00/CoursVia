import {
    clearAuth,
    getAccessToken,
    getRefreshToken,
    saveTokens,
} from "@/src/auth/authStorage";
import type { MobileRefreshTokenResponse } from "@/src/types/auth";
import axios, { AxiosError, type InternalAxiosRequestConfig } from "axios";
import { router } from "expo-router";

// Backend API adresi.
// Android emulator için 10.0.2.2 host makineyi temsil eder.
// Gerçek cihazda burayı PC'nin IPv4 adresiyle değiştirmek gerekir.
// Örnek: "http://192.168.1.25:5024"
export const API_BASE_URL = "http://10.0.2.2:5024";


// Retry kontrolü için özel config tipi.
type RetryableRequestConfig = InternalAxiosRequestConfig & {
    _retry?: boolean;
};

// Uygulamanın ortak API client'ı; token ekleme, 401 yakalama ve refresh sonrası isteği yeniden deneme akışı burada yönetilir.
export const api = axios.create({
    baseURL: API_BASE_URL,
    timeout: 15000,
    headers: {
        "Content-Type": "application/json",
    },
});

// Refresh işlemi için ayrı axios instance.
// Buna interceptor bağlamıyoruz ki refresh isteği kendi kendini döngüye sokmasın.
const authApi = axios.create({
    baseURL: API_BASE_URL,
    timeout: 15000,
    headers: {
        "Content-Type": "application/json",
    },
});

// Aynı anda birden fazla API isteği 401 dönerse tek refresh isteği çalışsın diye
// devam eden refresh promise'i burada tutulur.
let refreshPromise: Promise<string | null> | null = null;

// Her API isteğinden önce access token varsa Authorization header'a ekler.
// Access token DB'ye yazılmaz; sadece SecureStore'dan okunup header'a eklenir.
api.interceptors.request.use(async (config) => {
    const accessToken = await getAccessToken();

    if (accessToken) {
        config.headers.Authorization = `Bearer ${accessToken}`;
    }

    return config;
});

// API 401 dönerse:
// 1. Refresh token ile yeni access token almaya çalışır.
// 2. Başarılıysa eski isteği yeni access token ile tekrar dener.
// 3. Refresh başarısızsa auth temizlenir ve login ekranına gönderilir.
api.interceptors.response.use(
    (response) => response,
    async (error: AxiosError) => {
        const status = error.response?.status;
        const originalRequest = error.config as RetryableRequestConfig | undefined;

        // 401 dışındaki hatalar auth akışına sokulmadan çağırana bırakılır.
        if (!originalRequest || status !== 401) {
            return Promise.reject(error);
        }

        const url = originalRequest.url ?? "";

        // Auth endpointlerinde refresh denemek sonsuz döngüye yol açabileceği için bu istekler hariç tutulur.
        const authEndpointMi =
            url.includes("/api/mobile/auth/login") ||
            url.includes("/api/mobile/auth/refresh") ||
            url.includes("/api/mobile/auth/logout");

        // Aynı istek ikinci kez 401 alırsa oturum artık geçersiz kabul edilir.
        if (authEndpointMi || originalRequest._retry) {
            await clearAuth();
            router.replace("/login" as any);

            return Promise.reject(error);
        }

        originalRequest._retry = true;

        try {
            // Refresh token geçerliyse yeni access token alınır.
            const yeniAccessToken = await accessTokenYenile();

            if (!yeniAccessToken) {
                await clearAuth();
                router.replace("/login" as any);

                return Promise.reject(error);
            }

            originalRequest.headers.Authorization = `Bearer ${yeniAccessToken}`;

            // Yeni token ile başarısız olan orijinal istek bir kez daha denenir.
            return api(originalRequest);
        } catch (refreshError) {
            await clearAuth();
            router.replace("/login" as any);

            return Promise.reject(refreshError);
        }
    }
);

// Devam eden bir refresh işlemi varsa ona katılır; yoksa yeni refresh başlatır.
// Böylece eş zamanlı 401 hataları backend'e tek refresh isteği olarak gider.
async function accessTokenYenile() {
    if (!refreshPromise) {
        refreshPromise = refreshTokenIleYenile();
    }

    try {
        return await refreshPromise;
    } finally {
        // Refresh tamamlanınca kilit temizlenir; sonraki 401 yeni bir refresh başlatabilir.
        refreshPromise = null;
    }
}

// SecureStore'daki refresh token ile backend'den yeni token çifti ister.
// Cevap beklenen alanları taşıyorsa token'ları kaydeder ve yeni access token'ı döner.
async function refreshTokenIleYenile() {
    const refreshToken = await getRefreshToken();

    // Refresh token yoksa kullanıcının yeniden login olması gerekir.
    if (!refreshToken) {
        return null;
    }

    const response = await authApi.post<MobileRefreshTokenResponse>(
        "/api/mobile/auth/refresh",
        {
            refreshToken,
        }
    );

    const data = response.data;

    // Eksik veya başarısız cevaplarda bozuk token kaydetmemek için refresh geçersiz sayılır.
    if (
        !data.basarili ||
        !data.accessToken ||
        !data.accessTokenBitisTarihi ||
        !data.refreshToken ||
        !data.refreshTokenBitisTarihi
    ) {
        return null;
    }

    await saveTokens(
        data.accessToken,
        data.accessTokenBitisTarihi,
        data.refreshToken,
        data.refreshTokenBitisTarihi
    );

    return data.accessToken;
}
