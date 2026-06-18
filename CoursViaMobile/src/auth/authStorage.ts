import type { MobileKullanici } from "@/src/types/auth";
import * as SecureStore from "expo-secure-store";

// SecureStore içinde kullanılacak anahtarlar.
// Access token, refresh token, bitiş tarihleri, kullanıcı bilgisi ve seçilen aktif rol burada saklanır.
const ACCESS_TOKEN_KEY = "coursvia_mobile_access_token";
const ACCESS_TOKEN_EXPIRES_AT_KEY = "coursvia_mobile_access_token_expires_at";

const REFRESH_TOKEN_KEY = "coursvia_mobile_refresh_token";
const REFRESH_TOKEN_EXPIRES_AT_KEY = "coursvia_mobile_refresh_token_expires_at";

const USER_KEY = "coursvia_mobile_user";
const SELECTED_ROLE_KEY = "coursvia_selected_role";

// Uygulama arka plana alındıktan sonra tamamen kapatılırsa bunu anlamak için kullanılır.
const APP_BACKGROUND_FLAG_KEY = "coursvia_app_background_flag";

// Login başarılı olduğunda access token, refresh token ve kullanıcı bilgisini güvenli şekilde saklar.
export async function saveAuth(
    accessToken: string,
    accessTokenBitisTarihi: string | null,
    refreshToken: string,
    refreshTokenBitisTarihi: string | null,
    kullanici: MobileKullanici
) {
    await saveTokens(
        accessToken,
        accessTokenBitisTarihi,
        refreshToken,
        refreshTokenBitisTarihi
    );

    // Kullanıcı bilgisi JSON string olarak saklanır.
    await SecureStore.setItemAsync(USER_KEY, JSON.stringify(kullanici));
}

// Refresh işleminden sonra sadece tokenları günceller.
// Kullanıcı bilgisi ve seçilen rol korunur.
export async function saveTokens(
    accessToken: string,
    accessTokenBitisTarihi: string | null,
    refreshToken: string,
    refreshTokenBitisTarihi: string | null
) {
    await SecureStore.setItemAsync(ACCESS_TOKEN_KEY, accessToken);
    await SecureStore.setItemAsync(REFRESH_TOKEN_KEY, refreshToken);

    if (accessTokenBitisTarihi) {
        await SecureStore.setItemAsync(
            ACCESS_TOKEN_EXPIRES_AT_KEY,
            accessTokenBitisTarihi
        );
    } else {
        await SecureStore.deleteItemAsync(ACCESS_TOKEN_EXPIRES_AT_KEY);
    }

    if (refreshTokenBitisTarihi) {
        await SecureStore.setItemAsync(
            REFRESH_TOKEN_EXPIRES_AT_KEY,
            refreshTokenBitisTarihi
        );
    } else {
        await SecureStore.deleteItemAsync(REFRESH_TOKEN_EXPIRES_AT_KEY);
    }
}

// Saklanan access token'ı getirir.
// Access token süresi dolmuş olsa bile direkt döndürür.
// Süre dolmuşsa backend 401 döner, api/client.ts refresh token ile yeniler.
export async function getAccessToken() {
    return SecureStore.getItemAsync(ACCESS_TOKEN_KEY);
}

// Saklanan refresh token'ı getirir.
// Refresh endpointinde kullanılır.
export async function getRefreshToken() {
    return SecureStore.getItemAsync(REFRESH_TOKEN_KEY);
}

// Access token bitiş tarihini getirir.
export async function getAccessTokenBitisTarihi() {
    return SecureStore.getItemAsync(ACCESS_TOKEN_EXPIRES_AT_KEY);
}

// Refresh token bitiş tarihini getirir.
export async function getRefreshTokenBitisTarihi() {
    return SecureStore.getItemAsync(REFRESH_TOKEN_EXPIRES_AT_KEY);
}

// Access token süresi dolmuş mu kontrol eder.
// Sadece bilgi amaçlıdır; access token doldu diye auth temizlenmez.
export async function isAccessTokenExpired() {
    const accessToken = await SecureStore.getItemAsync(ACCESS_TOKEN_KEY);
    const bitisTarihiText = await SecureStore.getItemAsync(
        ACCESS_TOKEN_EXPIRES_AT_KEY
    );

    if (!accessToken) {
        return true;
    }

    if (!bitisTarihiText) {
        return false;
    }

    const bitisTarihi = new Date(bitisTarihiText);

    if (Number.isNaN(bitisTarihi.getTime())) {
        return true;
    }

    return bitisTarihi.getTime() <= Date.now();
}

// Refresh token süresi dolmuş mu kontrol eder.
// Refresh token süresi dolduysa artık kullanıcı tekrar login olmalıdır.
export async function isRefreshTokenExpired() {
    const refreshToken = await SecureStore.getItemAsync(REFRESH_TOKEN_KEY);
    const bitisTarihiText = await SecureStore.getItemAsync(
        REFRESH_TOKEN_EXPIRES_AT_KEY
    );

    // Refresh token yoksa zaten süresi dolmuş sayılır.
    if (!refreshToken) {
        return true;
    }

    // Refresh token varsa ama bitiş tarihi yoksa, bu token'ın süresiz olduğunu varsayıyoruz.
    if (!bitisTarihiText) {
        return false;
    }

    const bitisTarihi = new Date(bitisTarihiText);

    if (Number.isNaN(bitisTarihi.getTime())) {
        return true;
    }

    return bitisTarihi.getTime() <= Date.now();
}

// Uygulama arka plana alındığında işaret bırakır.
// Eğer uygulama normal şekilde tekrar aktif olursa bu işaret silinir.
// Eğer uygulama tamamen kapatılırsa işaret kalır ve sonraki açılışta tekrar login istenir.
export async function markAppBackgrounded() {
    await SecureStore.setItemAsync(APP_BACKGROUND_FLAG_KEY, new Date().toISOString());
}

// Uygulama tekrar aktif olduğunda background işaretini temizler.
export async function clearAppBackgroundFlag() {
    await SecureStore.deleteItemAsync(APP_BACKGROUND_FLAG_KEY);
}

// Önceki kullanımda uygulama arka plandayken tamamen kapatılmış mı kontrol eder.
export async function wasAppKilledAfterBackground() {
    const flag = await SecureStore.getItemAsync(APP_BACKGROUND_FLAG_KEY);


    return !!flag;
}

// Eski isimlerle import edilmiş yerler varsa bozulmasın diye uyumluluk alias'ı.
// Yeni kodda getAccessToken kullan.
export async function getToken() {
    return getAccessToken();
}

// Eski sistemde bu fonksiyon access token süresi dolunca auth temizliyordu.
// Yeni refresh token sisteminde bunu yapmıyoruz.
// Çünkü access token dolsa bile refresh token ile yenilenebilir.
export async function getValidToken() {
    return getAccessToken();
}

// Saklanan kullanıcı bilgisini getirir.
// Kullanıcı bilgisi JSON string olarak tutulduğu için tekrar objeye çeviriyoruz.
export async function getUser() {
    const rawUser = await SecureStore.getItemAsync(USER_KEY);

    if (!rawUser) {
        return null;
    }

    try {
        return JSON.parse(rawUser) as MobileKullanici;
    } catch {
        await clearAuth();
        return null;
    }
}

// Çok rollü kullanıcılar için seçilen aktif rolü saklar.
// Örnek değerler: "Öğrenci", "Eğitmen", "Admin"
export async function saveSelectedRole(role: string) {
    await SecureStore.setItemAsync(SELECTED_ROLE_KEY, role);
}

// Daha önce seçilmiş aktif rolü getirir.
export async function getSelectedRole() {
    return SecureStore.getItemAsync(SELECTED_ROLE_KEY);
}

// Sadece seçilen rolü temizler.
export async function clearSelectedRole() {
    await SecureStore.deleteItemAsync(SELECTED_ROLE_KEY);
}

// Logout işleminde access token, refresh token, kullanıcı bilgisi, seçilen rol ve background işareti temizlenir.
export async function clearAuth() {
    await SecureStore.deleteItemAsync(ACCESS_TOKEN_KEY);
    await SecureStore.deleteItemAsync(ACCESS_TOKEN_EXPIRES_AT_KEY);

    await SecureStore.deleteItemAsync(REFRESH_TOKEN_KEY);
    await SecureStore.deleteItemAsync(REFRESH_TOKEN_EXPIRES_AT_KEY);

    await SecureStore.deleteItemAsync(USER_KEY);
    await SecureStore.deleteItemAsync(SELECTED_ROLE_KEY);

    await SecureStore.deleteItemAsync(APP_BACKGROUND_FLAG_KEY);
}