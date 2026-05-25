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

//  Ana API client'ımız. Tüm API istekleri bu client üzerinden yapılacak. Interceptor'lar burada tanımlanır.
// Tüm API isteklerini bu client üzerinden göndereceğiz.
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

// Access token yenileme işlemi sırasında birden fazla API isteği 401 dönerse,
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

        if (!originalRequest || status !== 401) {
            return Promise.reject(error);
        }

        const url = originalRequest.url ?? "";

        const authEndpointMi =
            url.includes("/api/mobile/auth/login") ||
            url.includes("/api/mobile/auth/refresh") ||
            url.includes("/api/mobile/auth/logout");

        if (authEndpointMi || originalRequest._retry) {
            await clearAuth();
            router.replace("/login" as any);

            return Promise.reject(error);
        }

        originalRequest._retry = true;

        try {
            const yeniAccessToken = await accessTokenYenile();

            if (!yeniAccessToken) {
                await clearAuth();
                router.replace("/login" as any);

                return Promise.reject(error);
            }

            originalRequest.headers.Authorization = `Bearer ${yeniAccessToken}`;

            return api(originalRequest);
        } catch (refreshError) {
            await clearAuth();
            router.replace("/login" as any);

            return Promise.reject(refreshError);
        }
    }
);

async function accessTokenYenile() {
    if (!refreshPromise) {
        refreshPromise = refreshTokenIleYenile();
    }

    try {
        return await refreshPromise;
    } finally {
        refreshPromise = null;
    }
}

async function refreshTokenIleYenile() {
    const refreshToken = await getRefreshToken();

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