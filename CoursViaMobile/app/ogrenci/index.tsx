import { router } from "expo-router";
import { useEffect, useState } from "react";
import {
    ActivityIndicator,
    Alert,
    Pressable,
    StyleSheet,
    Text,
    View,
} from "react-native";

import PanelLayout from "@/app/_shared/PanelLayout";
import { OGRENCI_MENU_ITEMS } from "@/app/_shared/panelMenus";
import { api } from "@/src/api/client";
import type {
    MobileOgrenciDashboardKurs,
    MobileOgrenciDashboardResponse,
} from "@/src/types/ogrenci";

// Öğrenci dashboard ekranı.
// Öğrencinin genel kurs ilerlemesini ve son kurslarını gösterir.
export default function OgrenciScreen() {
    // Backend'den gelen dashboard verisini tutar.
    const [dashboard, setDashboard] =
        useState<MobileOgrenciDashboardResponse | null>(null);

    // İlk yükleme durumunu tutar.
    const [yukleniyor, setYukleniyor] = useState(true);

    // Pull-to-refresh durumunu tutar.
    const [yenileniyor, setYenileniyor] = useState(false);

    // API hata mesajını tutar.
    const [hata, setHata] = useState<string | null>(null);

    // Sayfa ilk açıldığında dashboard verisini getiriyoruz.
    useEffect(() => {
        dashboardGetir();
    }, []);

    // Öğrenci dashboard API isteği.
    async function dashboardGetir(refreshMi = false) {
        try {
            if (refreshMi) {
                setYenileniyor(true);
            } else {
                setYukleniyor(true);
            }

            setHata(null);

            // Token otomatik olarak axios interceptor ile Authorization header'a eklenir.
            const response = await api.get<MobileOgrenciDashboardResponse>(
                "/api/mobile/ogrenci/dashboard"
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

    // İlk yükleme ekranı.
    if (yukleniyor) {
        return <LoadingState />;
    }

    // Hata ekranı.
    if (hata) {
        return <ErrorState mesaj={hata} tekrarDene={() => dashboardGetir()} />;
    }

    return (
        <PanelLayout
            title="Öğrenci Paneli"
            subtitle="Kurs ilerlemeni ve son durumunu buradan takip edebilirsin."
            notificationCount={dashboard?.okunmamisBildirimSayisi ?? 0}
            refreshing={yenileniyor}
            onRefresh={() => dashboardGetir(true)}
            menuItems={OGRENCI_MENU_ITEMS}
            activeMenuKey="dashboard"
        >
            {/* Üst özet kartları.
          Mobilde sade görünmesi için sadece en önemli iki bilgiyi gösteriyoruz. */}
            <View style={styles.metricsGrid}>
                <MetricCard
                    title="Kayıtlı Kurs"
                    value={dashboard?.kayitliKursSayisi ?? 0}
                />

                <MetricCard
                    title="Devam Eden"
                    value={dashboard?.devamEdenKursSayisi ?? 0}
                />
            </View>

            <ProgressSummary value={dashboard?.ortalamaIlerlemeYuzdesi ?? 0} />

            <View style={styles.section}>
                <View style={styles.sectionHeader}>
                    <Text style={styles.sectionTitle}>Son Kurslar</Text>

                    <Pressable
                        onPress={() => router.push("./kurslarim")}
                        style={({ pressed }) => [
                            styles.sectionActionButton,
                            pressed ? styles.sectionActionButtonPressed : null,
                        ]}
                    >
                        <Text style={styles.sectionAction}>Tümünü Gör</Text>
                    </Pressable>
                </View>

                {dashboard?.sonKurslar.length ? (
                    <View style={styles.courseList}>
                        {dashboard.sonKurslar.map((kurs) => (
                            <CourseCard key={kurs.kursKayitId} kurs={kurs} />
                        ))}
                    </View>
                ) : (
                    <EmptyState />
                )}
            </View>
        </PanelLayout>
    );
}

// İlk yükleme componenti.
function LoadingState() {
    return (
        <View style={styles.centerContainer}>
            <ActivityIndicator size="large" color="#2563eb" />
            <Text style={styles.loadingText}>Öğrenci paneli yükleniyor...</Text>
        </View>
    );
}

// Hata durumunda gösterilen component.
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

// Dashboard üstündeki küçük istatistik kartı.
function MetricCard({ title, value }: { title: string; value: number }) {
    return (
        <View style={styles.metricCard}>
            <Text style={styles.metricValue}>{value}</Text>
            <Text style={styles.metricTitle}>{title}</Text>
        </View>
    );
}

// Ortalama ilerleme özeti.
function ProgressSummary({ value }: { value: number }) {
    return (
        <View style={styles.summaryCard}>
            <Text style={styles.summaryLabel}>Ortalama İlerleme</Text>

            <Text style={styles.summaryValue}>%{value}</Text>

            <View style={styles.progressTrack}>
                <View style={[styles.progressFill, { width: `${value}%` }]} />
            </View>
        </View>
    );
}

// Dashboard altında görünen son kurs kartı.
function CourseCard({ kurs }: { kurs: MobileOgrenciDashboardKurs }) {
    const guncelleniyorMu = kurs.guncelleniyorMu;

    return (
        <View style={styles.courseCard}>
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

                    <Text style={styles.courseInstructor} numberOfLines={1}>
                        {kurs.egitmenAdSoyad}
                    </Text>
                </View>

                <Text style={styles.coursePercent}>%{kurs.ilerlemeYuzdesi}</Text>
            </View>

            <View style={styles.courseProgressTrack}>
                <View
                    style={[
                        styles.courseProgressFill,
                        { width: `${kurs.ilerlemeYuzdesi}%` },
                    ]}
                />
            </View>

            <View style={styles.courseBottom}>
                <Text style={styles.courseLessonText}>
                    {kurs.tamamlananDersSayisi}/{kurs.toplamDersSayisi} ders tamamlandı
                </Text>

                <Text
                    style={[
                        styles.courseStatus,
                        guncelleniyorMu
                            ? styles.courseStatusUpdating
                            : kurs.kursTamamlandiMi
                            ? styles.courseStatusDone
                            : styles.courseStatusActive,
                    ]}
                >
                    {guncelleniyorMu
                        ? "Güncelleniyor"
                        : kurs.kursTamamlandiMi
                            ? "Tamamlandı"
                            : "Devam ediyor"}
                </Text>
            </View>

            {guncelleniyorMu ? (
                <Text style={styles.updateNotice}>
                    Kurs erişime geçici olarak kapalı.
                </Text>
            ) : null}
        </View>
    );
}

// Kurs yokken gösterilecek boş durum componenti.
function EmptyState() {
    return (
        <View style={styles.emptyCard}>
            <Text style={styles.emptyTitle}>Henüz kayıtlı kurs yok</Text>

            <Text style={styles.emptyText}>
                Bir kursa kayıt olduğunda ilerleme bilgilerin burada görünecek.
            </Text>
        </View>
    );
}

// Öğrenci dashboard ekranı stilleri.
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
    },
    primaryButton: {
        backgroundColor: "#2563eb",
        paddingHorizontal: 18,
        paddingVertical: 12,
        borderRadius: 14,
    },
    primaryButtonText: {
        color: "#ffffff",
        fontWeight: "800",
    },
    metricsGrid: {
        flexDirection: "row",
        gap: 12,
    },
    metricCard: {
        flex: 1,
        backgroundColor: "#ffffff",
        borderRadius: 18,
        padding: 16,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    metricValue: {
        fontSize: 28,
        fontWeight: "900",
        color: "#0f172a",
    },
    metricTitle: {
        marginTop: 4,
        fontSize: 13,
        fontWeight: "700",
        color: "#64748b",
    },
    summaryCard: {
        marginTop: 16,
        backgroundColor: "#ffffff",
        borderRadius: 20,
        padding: 18,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    summaryLabel: {
        fontSize: 14,
        fontWeight: "800",
        color: "#64748b",
    },
    summaryValue: {
        marginTop: 4,
        fontSize: 34,
        fontWeight: "900",
        color: "#2563eb",
    },
    progressTrack: {
        height: 10,
        backgroundColor: "#e2e8f0",
        borderRadius: 999,
        marginTop: 16,
        overflow: "hidden",
    },
    progressFill: {
        height: "100%",
        backgroundColor: "#2563eb",
        borderRadius: 999,
    },
    section: {
        marginTop: 24,
    },
    sectionHeader: {
        marginBottom: 12,
        flexDirection: "row",
        alignItems: "center",
        justifyContent: "space-between",
    },
    sectionTitle: {
        fontSize: 20,
        fontWeight: "900",
        color: "#0f172a",
    },
    sectionActionButton: {
        paddingHorizontal: 10,
        paddingVertical: 6,
        borderRadius: 999,
        backgroundColor: "#eff6ff",
    },
    sectionActionButtonPressed: {
        opacity: 0.75,
    },
    sectionAction: {
        fontSize: 13,
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
        width: 48,
        height: 48,
        borderRadius: 16,
        backgroundColor: "#eff6ff",
        alignItems: "center",
        justifyContent: "center",
        marginRight: 12,
    },
    courseAvatarText: {
        fontSize: 20,
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
    courseInstructor: {
        marginTop: 3,
        fontSize: 13,
        color: "#64748b",
    },
    coursePercent: {
        marginLeft: 10,
        fontSize: 16,
        fontWeight: "900",
        color: "#2563eb",
    },
    courseProgressTrack: {
        height: 8,
        backgroundColor: "#e2e8f0",
        borderRadius: 999,
        marginTop: 14,
        overflow: "hidden",
    },
    courseProgressFill: {
        height: "100%",
        backgroundColor: "#22c55e",
        borderRadius: 999,
    },
    courseBottom: {
        marginTop: 12,
        flexDirection: "row",
        justifyContent: "space-between",
        gap: 12,
    },
    courseLessonText: {
        flex: 1,
        fontSize: 13,
        fontWeight: "700",
        color: "#475569",
    },
    courseStatus: {
        fontSize: 13,
        fontWeight: "900",
    },
    courseStatusActive: {
        color: "#2563eb",
    },
    courseStatusDone: {
        color: "#16a34a",
    },
    courseStatusUpdating: {
        color: "#b45309",
    },
    updateNotice: {
        marginTop: 10,
        backgroundColor: "#fffbeb",
        borderWidth: 1,
        borderColor: "#fde68a",
        borderRadius: 12,
        paddingHorizontal: 10,
        paddingVertical: 8,
        fontSize: 12,
        fontWeight: "800",
        color: "#92400e",
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
