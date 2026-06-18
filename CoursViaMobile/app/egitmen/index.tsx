import { router } from "expo-router";
import { useEffect, useMemo, useState } from "react";
import {
    ActivityIndicator,
    Alert,
    Pressable,
    StyleSheet,
    Text,
    View,
} from "react-native";

import PanelLayout from "@/app/_shared/PanelLayout";
import { EGITMEN_MENU_ITEMS } from "@/app/_shared/panelMenus";
import { api } from "@/src/api/client";
import type {
    MobileEgitmenDashboardKursItem,
    MobileEgitmenDashboardResponse,
} from "@/src/types/egitmen";

// Eğitmen dashboard ekranı.
// Sade tutuldu: toplam kurs, yayındaki kurs ve toplam öğrenci bilgisi gösterilir.
export default function EgitmenDashboardScreen() {
    // Dashboard API cevabı tek state'te tutulur; sayaçlar ve son kurslar buradan okunur.
    const [dashboard, setDashboard] =
        useState<MobileEgitmenDashboardResponse | null>(null);

    // İlk yükleme, pull-to-refresh ve hata ekranı birbirinden ayrı yönetilir.
    const [yukleniyor, setYukleniyor] = useState(true);
    const [yenileniyor, setYenileniyor] = useState(false);
    const [hata, setHata] = useState<string | null>(null);

    // Ekran açıldığında dashboard özeti bir kez yüklenir.
    useEffect(() => {
        dashboardGetir();
    }, []);

    // Üstteki özet kartları dashboard cevabından türetilir.
    const ozetKartlari = useMemo(() => {
        if (!dashboard) {
            return [];
        }

        return [
            {
                title: "Toplam Kurs",
                value: dashboard.toplamKursSayisi,
            },
            {
                title: "Yayındaki Kurs",
                value: dashboard.yayindakiKursSayisi,
            },
            {
                title: "Toplam Öğrenci",
                value: dashboard.toplamOgrenciSayisi,
            },
        ];
    }, [dashboard]);

    // Dashboard bilgisini getirir; refreshMi true ise tam ekran loading yerine yenileme göstergesi kullanılır.
    async function dashboardGetir(refreshMi = false) {
        try {
            if (refreshMi) {
                setYenileniyor(true);
            } else {
                setYukleniyor(true);
            }

            setHata(null);

            const response = await api.get<MobileEgitmenDashboardResponse>(
                "/api/mobile/egitmen/dashboard"
            );

            setDashboard(response.data);
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Dashboard bilgileri alınırken hata oluştu.";

            setHata(mesaj);
            Alert.alert("Hata", mesaj);
        } finally {
            setYukleniyor(false);
            setYenileniyor(false);
        }
    }

    // Son kurs kartından eğitmen kurs detay ekranına geçiş yapar.
    function kursDetayAc(kursId: number) {
        router.push(`/egitmen/kurs-detay/${kursId}` as any);
    }

    if (yukleniyor) {
        return <LoadingState />;
    }

    if (hata || !dashboard) {
        return (
            <ErrorState
                mesaj={hata || "Dashboard bilgisi bulunamadı."}
                tekrarDene={() => dashboardGetir()}
            />
        );
    }

    return (
        <PanelLayout
            title="Eğitmen Paneli"
            subtitle="Kurs ve öğrenci özetini buradan takip edebilirsin."
            notificationCount={dashboard.okunmamisBildirimSayisi}
            refreshing={yenileniyor}
            onRefresh={() => dashboardGetir(true)}
            menuItems={EGITMEN_MENU_ITEMS}
            activeMenuKey="dashboard"
        >
            <View style={styles.summaryGrid}>
                {ozetKartlari.map((kart) => (
                    <SummaryCard key={kart.title} title={kart.title} value={kart.value} />
                ))}
            </View>

            <View style={styles.sectionHeader}>
                <View>
                    <Text style={styles.sectionTitle}>Son Kurslar</Text>
                    <Text style={styles.sectionSubText}>Son güncellenen kursların</Text>
                </View>

                <Pressable
                    onPress={() => router.push("/egitmen/kurslarim" as any)}
                    style={({ pressed }) => [
                        styles.seeAllButton,
                        pressed ? styles.buttonPressed : null,
                    ]}
                >
                    <Text style={styles.seeAllButtonText}>Tümünü Gör</Text>
                </Pressable>
            </View>

            {dashboard.sonKurslar.length > 0 ? (
                <View style={styles.courseList}>
                    {dashboard.sonKurslar.map((kurs) => (
                        <DashboardKursKart
                            key={kurs.kursId}
                            kurs={kurs}
                            kursDetayAc={kursDetayAc}
                        />
                    ))}
                </View>
            ) : (
                <EmptyState />
            )}
        </PanelLayout>
    );
}

// Dashboard ilk açılış yüklenme ekranı.
function LoadingState() {
    return (
        <View style={styles.centerContainer}>
            <ActivityIndicator size="large" color="#2563eb" />
            <Text style={styles.loadingText}>Eğitmen paneli yükleniyor...</Text>
        </View>
    );
}

// Dashboard alınamazsa tekrar deneme butonlu hata ekranı.
function ErrorState({
    mesaj,
    tekrarDene,
}: {
    mesaj: string;
    tekrarDene: () => void;
}) {
    return (
        <View style={styles.centerContainer}>
            <Text style={styles.errorTitle}>Bir sorun oluştu</Text>
            <Text style={styles.errorText}>{mesaj}</Text>

            <Pressable style={styles.primaryButton} onPress={tekrarDene}>
                <Text style={styles.primaryButtonText}>Tekrar Dene</Text>
            </Pressable>
        </View>
    );
}

// Dashboard sayaçlarını göstermek için kullanılan küçük kart.
function SummaryCard({
    title,
    value,
}: {
    title: string;
    value: number | string;
}) {
    return (
        <View style={styles.summaryCard}>
            <Text style={styles.summaryValue}>{value}</Text>
            <Text style={styles.summaryTitle}>{title}</Text>
        </View>
    );
}

// Dashboard son kurs kartı.
// Burada MobileEgitmenKursItem değil, sade dashboard modeli kullanılır.
function DashboardKursKart({
    kurs,
    kursDetayAc,
}: {
    kurs: MobileEgitmenDashboardKursItem;
    kursDetayAc: (kursId: number) => void;
}) {
    return (
        <Pressable
            onPress={() => kursDetayAc(kurs.kursId)}
            style={({ pressed }) => [
                styles.courseCard,
                pressed ? styles.buttonPressed : null,
            ]}
        >
            <View style={styles.courseTop}>
                <View style={styles.courseAvatar}>
                    <Text style={styles.courseAvatarText}>
                        {kurs.kursAdi.substring(0, 1).toUpperCase()}
                    </Text>
                </View>

                <View style={styles.courseInfo}>
                    <Text style={styles.courseTitle} numberOfLines={1}>
                        {kurs.kursAdi}
                    </Text>

                    <Text style={styles.courseSubText} numberOfLines={1}>
                        {kurs.durumAdi}
                    </Text>
                </View>

                <Text style={styles.detailText}>Detay →</Text>
            </View>

            <View style={styles.courseMetaRow}>
                <Text style={styles.courseMetaText}>{kurs.ogrenciSayisi} öğrenci</Text>
                <Text style={styles.courseMetaDot}>•</Text>
                <Text style={styles.courseMetaText}>{kurs.dersSayisi} ders</Text>
            </View>
        </Pressable>
    );
}

// Eğitmene bağlı kurs yoksa gösterilen boş durum.
function EmptyState() {
    return (
        <View style={styles.emptyCard}>
            <Text style={styles.emptyTitle}>Kurs bulunamadı</Text>
            <Text style={styles.emptyText}>
                Henüz eğitmen hesabına bağlı kurs görünmüyor.
            </Text>
        </View>
    );
}

const styles = StyleSheet.create({
    centerContainer: {
        flex: 1,
        backgroundColor: "#f8fafc",
        justifyContent: "center",
        alignItems: "center",
        padding: 24,
    },
    loadingText: {
        marginTop: 12,
        fontSize: 14,
        color: "#64748b",
    },
    errorTitle: {
        fontSize: 22,
        fontWeight: "900",
        color: "#0f172a",
        marginBottom: 8,
    },
    errorText: {
        fontSize: 14,
        color: "#64748b",
        textAlign: "center",
        marginBottom: 18,
        lineHeight: 20,
    },
    primaryButton: {
        backgroundColor: "#2563eb",
        paddingHorizontal: 18,
        paddingVertical: 12,
        borderRadius: 14,
    },
    primaryButtonText: {
        color: "#ffffff",
        fontWeight: "900",
    },
    summaryGrid: {
        flexDirection: "row",
        flexWrap: "wrap",
        gap: 12,
        marginBottom: 20,
    },
    summaryCard: {
        flexBasis: "30%",
        flexGrow: 1,
        backgroundColor: "#ffffff",
        borderRadius: 18,
        padding: 16,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    summaryValue: {
        fontSize: 26,
        fontWeight: "900",
        color: "#0f172a",
    },
    summaryTitle: {
        marginTop: 4,
        fontSize: 12,
        fontWeight: "700",
        color: "#64748b",
    },
    sectionHeader: {
        marginBottom: 14,
        flexDirection: "row",
        alignItems: "center",
        justifyContent: "space-between",
        gap: 12,
    },
    sectionTitle: {
        fontSize: 20,
        fontWeight: "900",
        color: "#0f172a",
    },
    sectionSubText: {
        marginTop: 3,
        fontSize: 12,
        fontWeight: "700",
        color: "#64748b",
    },
    seeAllButton: {
        backgroundColor: "#eff6ff",
        borderWidth: 1,
        borderColor: "#bfdbfe",
        borderRadius: 999,
        paddingHorizontal: 12,
        paddingVertical: 8,
    },
    seeAllButtonText: {
        fontSize: 12,
        fontWeight: "900",
        color: "#2563eb",
    },
    courseList: {
        gap: 12,
    },
    courseCard: {
        backgroundColor: "#ffffff",
        borderRadius: 20,
        padding: 16,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    courseTop: {
        flexDirection: "row",
        alignItems: "center",
    },
    courseAvatar: {
        width: 46,
        height: 46,
        borderRadius: 16,
        backgroundColor: "#eff6ff",
        alignItems: "center",
        justifyContent: "center",
        marginRight: 12,
    },
    courseAvatarText: {
        fontSize: 19,
        fontWeight: "900",
        color: "#2563eb",
    },
    courseInfo: {
        flex: 1,
    },
    courseTitle: {
        fontSize: 16,
        fontWeight: "900",
        color: "#0f172a",
    },
    courseSubText: {
        marginTop: 3,
        fontSize: 13,
        color: "#64748b",
    },
    detailText: {
        marginLeft: 10,
        fontSize: 12,
        fontWeight: "900",
        color: "#2563eb",
    },
    courseMetaRow: {
        marginTop: 12,
        flexDirection: "row",
        alignItems: "center",
        gap: 7,
    },
    courseMetaText: {
        fontSize: 13,
        fontWeight: "800",
        color: "#64748b",
    },
    courseMetaDot: {
        fontSize: 13,
        fontWeight: "900",
        color: "#94a3b8",
    },
    buttonPressed: {
        opacity: 0.75,
    },
    emptyCard: {
        backgroundColor: "#ffffff",
        borderRadius: 20,
        padding: 20,
        alignItems: "center",
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    emptyTitle: {
        fontSize: 16,
        fontWeight: "900",
        color: "#0f172a",
    },
    emptyText: {
        marginTop: 6,
        fontSize: 14,
        color: "#64748b",
        textAlign: "center",
        lineHeight: 20,
    },
});
