import { Feather } from "@expo/vector-icons";
import { router } from "expo-router";
import { type ReactNode, useEffect, useState } from "react";
import {
    ActivityIndicator,
    Alert,
    AppState,
    Modal,
    Platform,
    Pressable,
    RefreshControl,
    SafeAreaView,
    ScrollView,
    StatusBar,
    type StyleProp,
    StyleSheet,
    Text,
    View,
    type ViewStyle,
} from "react-native";

import { api } from "@/src/api/client";
import {
    clearAppBackgroundFlag,
    clearAuth,
    getRefreshToken,
    getUser,
    markAppBackgrounded,
    wasAppKilledAfterBackground,
} from "@/src/auth/authStorage";
import type { MobileKullanici } from "@/src/types/auth";
import type {
    MobileBildirimItem,
    MobileBildirimlerResponse,
} from "@/src/types/bildirim";

// Sol menüde gösterilecek menü item tipi.
export type PanelMenuItem = {
    key: string;
    label: string;
    description?: string;
    icon: keyof typeof Feather.glyphMap;
    href?: string;
    disabled?: boolean;
};

// PanelLayout dışarıdan bu bilgileri alır.
// Öğrenci, Eğitmen ve Admin ekranlarında ortak kullanılacak.
type PanelLayoutProps = {
    title: string;
    eyebrow?: string;
    subtitle?: string;
    notificationCount?: number;
    refreshing?: boolean;
    onRefresh?: () => void;
    onNotificationsPress?: () => void;
    children: ReactNode;
    contentStyle?: StyleProp<ViewStyle>;

    // Sol sidebar menü elemanları.
    menuItems?: PanelMenuItem[];

    // Aktif menü item'ını vurgulamak için kullanılır.
    activeMenuKey?: string;
};

// Ortak panel layout componenti.
// Üstte hamburger menü, başlık, bildirim, profil değiştir ve logout bulunur.
// Soldan açılan sidebar menüyü de burada yönetiyoruz.
export default function PanelLayout({
    title,
    eyebrow = "CoursVia",
    subtitle,
    notificationCount = 0,
    refreshing = false,
    onRefresh,
    onNotificationsPress,
    children,
    contentStyle,
    menuItems = [],
    activeMenuKey,
}: PanelLayoutProps) {
    // Logout sırasında butonu kilitlemek ve loading göstermek için kullanılır.
    const [loggingOut, setLoggingOut] = useState(false);

    // Sol menü açık/kapalı durumunu tutar.
    const [menuOpen, setMenuOpen] = useState(false);

    // SecureStore'dan gelen kullanıcı bilgisini tutar.
    // Birden fazla rol varsa header'da profil değiştirme butonu gösterilir.
    const [kullanici, setKullanici] = useState<MobileKullanici | null>(null);

    // Bildirim ön izleme modalının açık/kapalı durumunu tutar.
    const [bildirimModalAcik, setBildirimModalAcik] = useState(false);

    // Header bildirim modalında gösterilecek ilk 3 bildirimi tutar.
    const [sonBildirimler, setSonBildirimler] = useState<MobileBildirimItem[]>([]);

    // Bildirim ön izleme yüklenme durumunu tutar.
    const [bildirimYukleniyor, setBildirimYukleniyor] = useState(false);

    // Bildirim ön izleme hata mesajını tutar.
    const [bildirimHata, setBildirimHata] = useState<string | null>(null);

    // Header aksiyonları için cihazda saklanan kullanıcı bilgisi tek kez okunur.
    useEffect(() => {
        async function kullaniciBilgisiniGetir() {
            const storedUser = await getUser();
            setKullanici(storedUser);
        }

        kullaniciBilgisiniGetir();
    }, []);

    // Açılışta oturum sürekliliği kontrol edilir, uygulama durum değişimlerinde online bilgisi güncellenir.
    useEffect(() => {
        async function ilkAcilisKontrolu() {
            const appKillSonrasiAcildiMi = await wasAppKilledAfterBackground();

            // Arka planda kapatılıp tekrar açıldıysa güvenlik için yeniden giriş istenir.
            if (appKillSonrasiAcildiMi) {
                await zorunluOturumKapat();
                return;
            }

            await clearAppBackgroundFlag();
            await onlineDurumGonder(true);
        }

        ilkAcilisKontrolu();

        // AppState ile kullanıcı aktifken online, arka plandayken offline olarak işaretlenir.
        const subscription = AppState.addEventListener("change", async (nextState) => {
            if (nextState === "background") {
                await onlineDurumGonder(false);
                await markAppBackgrounded();
                return;
            }

            if (nextState === "active") {
                await clearAppBackgroundFlag();
                await onlineDurumGonder(true);
            }
        });

        return () => {
            subscription.remove();
        };
    }, []);

    // Birden fazla rolü olan kullanıcılar panel değiştirme ekranına dönebilir.
    const profilDegistirilebilirMi = (kullanici?.roller?.length ?? 0) > 1;

    // Bildirim sayısı 99'dan büyükse rozette 99+ gösteriyoruz.
    const badgeText = notificationCount > 99 ? "99+" : String(notificationCount);

    // Kullanıcının online/offline durumunu backend'e gönderir.
    // Bu işlem hata verirse ekranda kullanıcıya hata göstermiyoruz.
    async function onlineDurumGonder(onlineMi: boolean) {
        try {
            await api.post("/api/mobile/auth/online-durum", {
                onlineMi,
            });
        } catch {
            // Online durumu kritik ekran akışını bozmasın diye sessiz geçiyoruz.
        }
    }

    // Bildirim ikonuna basılınca ilk 3 bildirimi getirip modal açar.
    async function bildirimOnizlemeAc() {
        setBildirimModalAcik(true);
        await sonBildirimleriGetir();
    }

    // Header için ilk 3 bildirimi API'den çeker.
    async function sonBildirimleriGetir() {
        try {
            setBildirimYukleniyor(true);
            setBildirimHata(null);

            const response = await api.get<MobileBildirimlerResponse>(
                "/api/mobile/bildirimler",
                {
                    params: {
                        durum: "tum",
                        sayfa: 1,
                        sayfaBasinaKayit: 3,
                    },
                }
            );

            setSonBildirimler(response.data.bildirimler ?? []);
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Bildirimler alınırken hata oluştu.";

            setBildirimHata(mesaj);
            setSonBildirimler([]);
        } finally {
            setBildirimYukleniyor(false);
        }
    }

    // Bildirimler sayfasına gider.
    function bildirimlerSayfasinaGit() {
        setBildirimModalAcik(false);

        if (onNotificationsPress) {
            onNotificationsPress();
            return;
        }

        const bildirimMenuItem = menuItems.find((x) => x.key === "bildirimler");

        if (bildirimMenuItem?.href) {
            router.push(bildirimMenuItem.href as any);
            return;
        }

        router.push("/ogrenci/bildirimler" as any);
    }

    // Çok rollü kullanıcılar için profil seçme ekranına gider.
    function profilDegistirSayfasinaGit() {
        router.push("/role-select" as any);
    }

    // Sidebar item'ına basılınca çalışır.
    function menuItemSec(item: PanelMenuItem) {
        if (item.disabled || !item.href) {
            Alert.alert("Yakında", "Bu ekran sonraki adımda eklenecek.");
            return;
        }

        setMenuOpen(false);

        // Expo Router typed route uyarısı vermesin diye any kullandık.
        router.push(item.href as any);
    }

    // Uygulama arka plandayken tamamen kapatılıp tekrar açıldıysa zorunlu çıkış yaptırır.
    // Backend'e ulaşılamazsa bile cihazdaki oturum temizlenir.
    async function zorunluOturumKapat() {
        try {
            const refreshToken = await getRefreshToken();

            if (refreshToken) {
                try {
                    await onlineDurumGonder(false);

                    await api.post("/api/mobile/auth/logout", {
                        refreshToken,
                    });
                } catch {
                    // Backend'e ulaşılamazsa bile cihazdaki oturum temizlenir.
                }
            }

            await clearAuth();
            router.replace("/login" as any);
        } catch {
            await clearAuth();
            router.replace("/login" as any);
        }
    }

    // Logout için kullanıcıdan onay alır.
    function cikisOnayiAl() {
        if (loggingOut) {
            return;
        }

        Alert.alert("Çıkış Yap", "Oturumu kapatmak istiyor musun?", [
            {
                text: "Vazgeç",
                style: "cancel",
            },
            {
                text: "Çıkış Yap",
                style: "destructive",
                onPress: cikisYap,
            },
        ]);
    }

    // Çıkış işlemi.
    // Backend logout endpointine refresh token gönderir.
    // Sunucuya ulaşılamasa bile cihazdaki token ve kullanıcı bilgisi temizlenir.
    async function cikisYap() {
        try {
            setLoggingOut(true);

            try {
                const refreshToken = await getRefreshToken();

                await onlineDurumGonder(false);

                if (refreshToken) {
                    await api.post("/api/mobile/auth/logout", {
                        refreshToken,
                    });
                }
            } catch {
                // Sunucuya ulaşılamasa bile cihazdaki oturumu temizliyoruz.
            }

            await clearAuth();

            // Login ekranına döner.
            router.replace("/login" as any);
        } catch {
            Alert.alert("Hata", "Çıkış yapılırken bir sorun oluştu.");
        } finally {
            setLoggingOut(false);
        }
    }

    return (
        <SafeAreaView style={styles.safeArea}>
            <StatusBar barStyle="dark-content" backgroundColor="#ffffff" />

            {/* Sol sidebar menü */}
            <Modal
                visible={menuOpen}
                transparent
                animationType="fade"
                onRequestClose={() => setMenuOpen(false)}
            >
                <View style={styles.sidebarRoot}>
                    <Pressable
                        style={styles.sidebarBackdrop}
                        onPress={() => setMenuOpen(false)}
                    />

                    <View style={styles.sidebarPanel}>
                        <View style={styles.sidebarHeader}>
                            <Text style={styles.sidebarBrand}>CoursVia</Text>
                            <Text style={styles.sidebarSubtitle}>Mobil Panel</Text>
                        </View>

                        <View style={styles.sidebarMenu}>
                            {menuItems.map((item) => {
                                const aktifMi = item.key === activeMenuKey;

                                return (
                                    <Pressable
                                        key={item.key}
                                        onPress={() => menuItemSec(item)}
                                        style={({ pressed }) => [
                                            styles.sidebarItem,
                                            aktifMi ? styles.sidebarItemActive : null,
                                            item.disabled ? styles.sidebarItemDisabled : null,
                                            pressed ? styles.sidebarItemPressed : null,
                                        ]}
                                    >
                                        <View
                                            style={[
                                                styles.sidebarIconBox,
                                                aktifMi ? styles.sidebarIconBoxActive : null,
                                            ]}
                                        >
                                            <Feather
                                                name={item.icon}
                                                size={18}
                                                color={aktifMi ? "#ffffff" : "#2563eb"}
                                            />
                                        </View>

                                        <View style={styles.sidebarItemTextArea}>
                                            <Text
                                                style={[
                                                    styles.sidebarItemLabel,
                                                    aktifMi ? styles.sidebarItemLabelActive : null,
                                                ]}
                                                numberOfLines={1}
                                            >
                                                {item.label}
                                            </Text>

                                            {item.description ? (
                                                <Text
                                                    style={styles.sidebarItemDescription}
                                                    numberOfLines={1}
                                                >
                                                    {item.description}
                                                </Text>
                                            ) : null}
                                        </View>
                                    </Pressable>
                                );
                            })}
                        </View>

                        <Pressable
                            onPress={() => setMenuOpen(false)}
                            style={({ pressed }) => [
                                styles.sidebarCloseButton,
                                pressed ? styles.sidebarCloseButtonPressed : null,
                            ]}
                        >
                            <Text style={styles.sidebarCloseButtonText}>Menüyü Kapat</Text>
                        </Pressable>
                    </View>
                </View>
            </Modal>

            {/* Bildirim ön izleme modalı */}
            <Modal
                visible={bildirimModalAcik}
                transparent
                animationType="fade"
                onRequestClose={() => setBildirimModalAcik(false)}
            >
                <View style={styles.notificationPreviewRoot}>
                    <Pressable
                        style={styles.notificationPreviewBackdrop}
                        onPress={() => setBildirimModalAcik(false)}
                    />

                    <View style={styles.notificationPreviewCard}>
                        <View style={styles.notificationPreviewHeader}>
                            <View>
                                <Text style={styles.notificationPreviewTitle}>Bildirimler</Text>
                                <Text style={styles.notificationPreviewSubtitle}>
                                    Son bildirimlerin
                                </Text>
                            </View>

                            <Pressable
                                onPress={() => setBildirimModalAcik(false)}
                                style={({ pressed }) => [
                                    styles.notificationPreviewCloseButton,
                                    pressed ? styles.iconButtonPressed : null,
                                ]}
                            >
                                <Feather name="x" size={18} color="#334155" />
                            </Pressable>
                        </View>

                        {bildirimYukleniyor ? (
                            <View style={styles.notificationPreviewLoading}>
                                <ActivityIndicator size="small" color="#2563eb" />
                                <Text style={styles.notificationPreviewLoadingText}>
                                    Bildirimler yükleniyor...
                                </Text>
                            </View>
                        ) : bildirimHata ? (
                            <View style={styles.notificationPreviewEmpty}>
                                <Text style={styles.notificationPreviewEmptyTitle}>
                                    Bildirim alınamadı
                                </Text>
                                <Text style={styles.notificationPreviewEmptyText}>
                                    {bildirimHata}
                                </Text>

                                <Pressable
                                    onPress={sonBildirimleriGetir}
                                    style={({ pressed }) => [
                                        styles.notificationPreviewRetryButton,
                                        pressed ? styles.iconButtonPressed : null,
                                    ]}
                                >
                                    <Text style={styles.notificationPreviewRetryButtonText}>
                                        Tekrar Dene
                                    </Text>
                                </Pressable>
                            </View>
                        ) : sonBildirimler.length > 0 ? (
                            <View style={styles.notificationPreviewList}>
                                {sonBildirimler.map((bildirim) => (
                                    <NotificationPreviewItem
                                        key={bildirim.bildirimId}
                                        bildirim={bildirim}
                                    />
                                ))}
                            </View>
                        ) : (
                            <View style={styles.notificationPreviewEmpty}>
                                <Text style={styles.notificationPreviewEmptyTitle}>
                                    Bildirim yok
                                </Text>
                                <Text style={styles.notificationPreviewEmptyText}>
                                    Henüz görüntülenecek bildirimin bulunmuyor.
                                </Text>
                            </View>
                        )}

                        <Pressable
                            onPress={bildirimlerSayfasinaGit}
                            style={({ pressed }) => [
                                styles.notificationPreviewAllButton,
                                pressed ? styles.iconButtonPressed : null,
                            ]}
                        >
                            <Text style={styles.notificationPreviewAllButtonText}>
                                Tümünü Gör
                            </Text>
                        </Pressable>
                    </View>
                </View>
            </Modal>

            {/* Üst panel header alanı */}
            <View style={styles.header}>
                {menuItems.length > 0 ? (
                    <Pressable
                        accessibilityLabel="Menüyü aç"
                        accessibilityRole="button"
                        onPress={() => setMenuOpen(true)}
                        style={({ pressed }) => [
                            styles.menuButton,
                            pressed ? styles.iconButtonPressed : null,
                        ]}
                    >
                        <Feather name="menu" size={22} color="#0f172a" />
                    </Pressable>
                ) : null}

                <View style={styles.headerText}>
                    <Text style={styles.eyebrow} numberOfLines={1}>
                        {eyebrow}
                    </Text>

                    <Text style={styles.title} numberOfLines={1}>
                        {title}
                    </Text>

                    {subtitle ? (
                        <Text style={styles.subtitle} numberOfLines={2}>
                            {subtitle}
                        </Text>
                    ) : null}
                </View>

                {/* Header sağ aksiyonları: Profil değiştir + Bildirim + Logout */}
                <View style={styles.headerActions}>
                    {profilDegistirilebilirMi ? (
                        <Pressable
                            accessibilityLabel="Profil değiştir"
                            accessibilityRole="button"
                            onPress={profilDegistirSayfasinaGit}
                            style={({ pressed }) => [
                                styles.iconButton,
                                styles.switchPanelButton,
                                pressed ? styles.iconButtonPressed : null,
                            ]}
                        >
                            <Feather name="repeat" size={20} color="#2563eb" />
                        </Pressable>
                    ) : null}

                    <Pressable
                        accessibilityLabel="Bildirimler"
                        accessibilityRole="button"
                        onPress={bildirimOnizlemeAc}
                        style={({ pressed }) => [
                            styles.iconButton,
                            pressed ? styles.iconButtonPressed : null,
                        ]}
                    >
                        <Feather name="bell" size={21} color="#0f172a" />

                        {notificationCount > 0 ? (
                            <View style={styles.badge}>
                                <Text style={styles.badgeText}>{badgeText}</Text>
                            </View>
                        ) : null}
                    </Pressable>

                    <Pressable
                        accessibilityLabel="Çıkış yap"
                        accessibilityRole="button"
                        disabled={loggingOut}
                        onPress={cikisOnayiAl}
                        style={({ pressed }) => [
                            styles.iconButton,
                            styles.logoutButton,
                            pressed && !loggingOut ? styles.iconButtonPressed : null,
                            loggingOut ? styles.iconButtonDisabled : null,
                        ]}
                    >
                        {loggingOut ? (
                            <ActivityIndicator color="#ef4444" size="small" />
                        ) : (
                            <Feather name="log-out" size={21} color="#ef4444" />
                        )}
                    </Pressable>
                </View>
            </View>

            {/* Sayfa içeriği */}
            <ScrollView
                style={styles.scroll}
                contentContainerStyle={[styles.content, contentStyle]}
                showsVerticalScrollIndicator={false}
                refreshControl={
                    onRefresh ? (
                        <RefreshControl
                            refreshing={refreshing}
                            onRefresh={onRefresh}
                            tintColor="#2563eb"
                        />
                    ) : undefined
                }
            >
                {children}
            </ScrollView>
        </SafeAreaView>
    );
}

// Header bildirim ön izlemesindeki tek bildirim satırı.
function NotificationPreviewItem({
    bildirim,
}: {
    bildirim: MobileBildirimItem;
}) {
    return (
        <View
            style={[
                styles.notificationPreviewItem,
                !bildirim.okunduMu ? styles.notificationPreviewItemUnread : null,
            ]}
        >
            <View
                style={[
                    styles.notificationPreviewIcon,
                    !bildirim.okunduMu ? styles.notificationPreviewIconUnread : null,
                ]}
            >
                <Text
                    style={[
                        styles.notificationPreviewIconText,
                        !bildirim.okunduMu
                            ? styles.notificationPreviewIconTextUnread
                            : null,
                    ]}
                >
                    {bildirimTipIkonuGetir(bildirim.bildirimTipAdi)}
                </Text>
            </View>

            <View style={styles.notificationPreviewTextArea}>
                <View style={styles.notificationPreviewTitleRow}>
                    <Text style={styles.notificationPreviewItemTitle} numberOfLines={1}>
                        {bildirim.baslik}
                    </Text>

                    {!bildirim.okunduMu ? (
                        <View style={styles.notificationPreviewUnreadDot} />
                    ) : null}
                </View>

                <Text style={styles.notificationPreviewItemMessage} numberOfLines={2}>
                    {bildirim.mesaj}
                </Text>

                <Text style={styles.notificationPreviewDate} numberOfLines={1}>
                    {tarihSaatFormatla(bildirim.olusturmaTarihi)}
                </Text>
            </View>
        </View>
    );
}

// Bildirim tipi adına göre kısa ikon metni verir.
function bildirimTipIkonuGetir(tipAdi: string) {
    const tip = tipAdi.toLowerCase();

    if (tip.includes("uyarı")) {
        return "!";
    }

    if (tip.includes("hata")) {
        return "×";
    }

    return "i";
}

// ISO tarih değerini TR tarih/saat formatına çevirir.
function tarihSaatFormatla(value: string) {
    const tarih = new Date(value);

    if (Number.isNaN(tarih.getTime())) {
        return value;
    }

    return tarih.toLocaleString("tr-TR", {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit",
    });
}

const styles = StyleSheet.create({
    safeArea: {
        flex: 1,
        backgroundColor: "#f8fafc",
        paddingTop: Platform.OS === "android" ? StatusBar.currentHeight ?? 0 : 0,
    },
    header: {
        backgroundColor: "#ffffff",
        borderBottomColor: "#e5e7eb",
        borderBottomWidth: 1,
        flexDirection: "row",
        alignItems: "center",
        gap: 10,
        paddingHorizontal: 16,
        paddingBottom: 16,
        paddingTop: 10,
    },
    menuButton: {
        width: 44,
        height: 44,
        alignItems: "center",
        justifyContent: "center",
        borderColor: "#e5e7eb",
        borderRadius: 14,
        borderWidth: 1,
        backgroundColor: "#ffffff",
    },
    headerText: {
        flex: 1,
        minWidth: 0,
    },
    eyebrow: {
        color: "#2563eb",
        fontSize: 12,
        fontWeight: "900",
        letterSpacing: 0.8,
        textTransform: "uppercase",
    },
    title: {
        color: "#0f172a",
        fontSize: 23,
        fontWeight: "900",
        marginTop: 3,
    },
    subtitle: {
        color: "#64748b",
        fontSize: 13,
        lineHeight: 18,
        marginTop: 4,
    },
    headerActions: {
        flexDirection: "row",
        gap: 8,
    },
    iconButton: {
        width: 44,
        height: 44,
        alignItems: "center",
        justifyContent: "center",
        borderColor: "#e5e7eb",
        borderRadius: 14,
        borderWidth: 1,
        backgroundColor: "#ffffff",
        position: "relative",
    },
    iconButtonPressed: {
        opacity: 0.72,
    },
    iconButtonDisabled: {
        opacity: 0.6,
    },
    switchPanelButton: {
        backgroundColor: "#eff6ff",
        borderColor: "#bfdbfe",
    },
    logoutButton: {
        backgroundColor: "#fff1f2",
        borderColor: "#fecdd3",
    },
    badge: {
        minWidth: 18,
        height: 18,
        alignItems: "center",
        justifyContent: "center",
        borderRadius: 9,
        backgroundColor: "#ef4444",
        paddingHorizontal: 5,
        position: "absolute",
        right: -4,
        top: -4,
    },
    badgeText: {
        color: "#ffffff",
        fontSize: 10,
        fontWeight: "900",
    },
    scroll: {
        flex: 1,
    },
    content: {
        padding: 20,
        paddingBottom: 36,
    },

    notificationPreviewRoot: {
        flex: 1,
        justifyContent: "flex-start",
        alignItems: "flex-end",
        paddingHorizontal: 14,
        paddingTop: 82,
    },
    notificationPreviewBackdrop: {
        ...StyleSheet.absoluteFillObject,
        backgroundColor: "rgba(15, 23, 42, 0.25)",
    },
    notificationPreviewCard: {
        width: "100%",
        maxWidth: 360,
        backgroundColor: "#ffffff",
        borderRadius: 24,
        padding: 16,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    notificationPreviewHeader: {
        flexDirection: "row",
        alignItems: "center",
        justifyContent: "space-between",
        marginBottom: 12,
        gap: 12,
    },
    notificationPreviewTitle: {
        fontSize: 19,
        fontWeight: "900",
        color: "#0f172a",
    },
    notificationPreviewSubtitle: {
        marginTop: 3,
        fontSize: 12,
        fontWeight: "700",
        color: "#64748b",
    },
    notificationPreviewCloseButton: {
        width: 36,
        height: 36,
        borderRadius: 13,
        backgroundColor: "#f8fafc",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        alignItems: "center",
        justifyContent: "center",
    },
    notificationPreviewLoading: {
        minHeight: 120,
        alignItems: "center",
        justifyContent: "center",
    },
    notificationPreviewLoadingText: {
        marginTop: 8,
        fontSize: 13,
        fontWeight: "700",
        color: "#64748b",
    },
    notificationPreviewEmpty: {
        minHeight: 120,
        backgroundColor: "#f8fafc",
        borderRadius: 18,
        borderWidth: 1,
        borderColor: "#e5e7eb",
        alignItems: "center",
        justifyContent: "center",
        padding: 16,
    },
    notificationPreviewEmptyTitle: {
        fontSize: 15,
        fontWeight: "900",
        color: "#0f172a",
    },
    notificationPreviewEmptyText: {
        marginTop: 5,
        fontSize: 13,
        lineHeight: 18,
        color: "#64748b",
        textAlign: "center",
    },
    notificationPreviewRetryButton: {
        marginTop: 12,
        backgroundColor: "#eff6ff",
        borderWidth: 1,
        borderColor: "#bfdbfe",
        borderRadius: 14,
        paddingHorizontal: 14,
        paddingVertical: 9,
    },
    notificationPreviewRetryButtonText: {
        fontSize: 13,
        fontWeight: "900",
        color: "#2563eb",
    },
    notificationPreviewList: {
        gap: 10,
    },
    notificationPreviewItem: {
        flexDirection: "row",
        backgroundColor: "#ffffff",
        borderRadius: 18,
        borderWidth: 1,
        borderColor: "#e5e7eb",
        padding: 12,
    },
    notificationPreviewItemUnread: {
        backgroundColor: "#f8fbff",
        borderColor: "#bfdbfe",
    },
    notificationPreviewIcon: {
        width: 38,
        height: 38,
        borderRadius: 14,
        backgroundColor: "#f1f5f9",
        alignItems: "center",
        justifyContent: "center",
        marginRight: 10,
    },
    notificationPreviewIconUnread: {
        backgroundColor: "#eff6ff",
    },
    notificationPreviewIconText: {
        fontSize: 17,
        fontWeight: "900",
        color: "#64748b",
    },
    notificationPreviewIconTextUnread: {
        color: "#2563eb",
    },
    notificationPreviewTextArea: {
        flex: 1,
        minWidth: 0,
    },
    notificationPreviewTitleRow: {
        flexDirection: "row",
        alignItems: "center",
        gap: 7,
    },
    notificationPreviewItemTitle: {
        flex: 1,
        fontSize: 14,
        fontWeight: "900",
        color: "#0f172a",
    },
    notificationPreviewUnreadDot: {
        width: 8,
        height: 8,
        borderRadius: 999,
        backgroundColor: "#2563eb",
    },
    notificationPreviewItemMessage: {
        marginTop: 4,
        fontSize: 12,
        lineHeight: 17,
        color: "#475569",
    },
    notificationPreviewDate: {
        marginTop: 5,
        fontSize: 11,
        fontWeight: "700",
        color: "#94a3b8",
    },
    notificationPreviewAllButton: {
        marginTop: 14,
        minHeight: 44,
        backgroundColor: "#2563eb",
        borderRadius: 15,
        alignItems: "center",
        justifyContent: "center",
    },
    notificationPreviewAllButtonText: {
        color: "#ffffff",
        fontSize: 13,
        fontWeight: "900",
    },

    sidebarRoot: {
        flex: 1,
        flexDirection: "row",
    },
    sidebarBackdrop: {
        ...StyleSheet.absoluteFillObject,
        backgroundColor: "rgba(15, 23, 42, 0.38)",
    },
    sidebarPanel: {
        width: 300,
        maxWidth: "82%",
        backgroundColor: "#ffffff",
        paddingTop: 48,
        paddingHorizontal: 18,
        paddingBottom: 22,
        borderTopRightRadius: 28,
        borderBottomRightRadius: 28,
    },
    sidebarHeader: {
        marginBottom: 22,
    },
    sidebarBrand: {
        fontSize: 28,
        fontWeight: "900",
        color: "#2563eb",
    },
    sidebarSubtitle: {
        marginTop: 4,
        fontSize: 13,
        fontWeight: "700",
        color: "#64748b",
    },
    sidebarMenu: {
        gap: 8,
    },
    sidebarItem: {
        flexDirection: "row",
        alignItems: "center",
        padding: 10,
        borderRadius: 18,
        borderWidth: 1,
        borderColor: "transparent",
    },
    sidebarItemActive: {
        backgroundColor: "#eff6ff",
        borderColor: "#bfdbfe",
    },
    sidebarItemDisabled: {
        opacity: 0.45,
    },
    sidebarItemPressed: {
        opacity: 0.75,
    },
    sidebarIconBox: {
        width: 40,
        height: 40,
        borderRadius: 14,
        backgroundColor: "#eff6ff",
        alignItems: "center",
        justifyContent: "center",
        marginRight: 12,
    },
    sidebarIconBoxActive: {
        backgroundColor: "#2563eb",
    },
    sidebarItemTextArea: {
        flex: 1,
    },
    sidebarItemLabel: {
        fontSize: 14,
        fontWeight: "900",
        color: "#0f172a",
    },
    sidebarItemLabelActive: {
        color: "#1d4ed8",
    },
    sidebarItemDescription: {
        marginTop: 2,
        fontSize: 12,
        fontWeight: "600",
        color: "#64748b",
    },
    sidebarCloseButton: {
        marginTop: "auto",
        backgroundColor: "#f8fafc",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        borderRadius: 16,
        paddingVertical: 12,
        alignItems: "center",
    },
    sidebarCloseButtonPressed: {
        opacity: 0.75,
    },
    sidebarCloseButtonText: {
        fontSize: 13,
        fontWeight: "900",
        color: "#334155",
    },
});
