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
import { ADMIN_MENU_ITEMS } from "@/app/_shared/panelMenus";
import { api } from "@/src/api/client";
import type {
    MobileAdminDashboardResponse,
    MobileAdminLogItem,
} from "@/src/types/admin";

// Admin dashboard ekranı.
// Mobil admin V1 için sade özet sayaçları ve son admin loglarını gösterir.
export default function AdminDashboardScreen() {
    const [dashboard, setDashboard] = useState<MobileAdminDashboardResponse | null>(
        null
    );

    const [yukleniyor, setYukleniyor] = useState(true);
    const [yenileniyor, setYenileniyor] = useState(false);
    const [hata, setHata] = useState<string | null>(null);

    useEffect(() => {
        dashboardGetir();
    }, []);

    async function dashboardGetir(refreshMi = false) {
        try {
            if (refreshMi) {
                setYenileniyor(true);
            } else {
                setYukleniyor(true);
            }

            setHata(null);

            const response = await api.get<MobileAdminDashboardResponse>(
                "/api/mobile/admin/dashboard"
            );

            setDashboard(response.data);
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Admin dashboard bilgileri alınırken hata oluştu.";

            setHata(mesaj);
            Alert.alert("Hata", mesaj);
        } finally {
            setYukleniyor(false);
            setYenileniyor(false);
        }
    }

    if (yukleniyor) {
        return <LoadingState />;
    }

    if (hata || !dashboard) {
        return (
            <ErrorState
                mesaj={hata || "Dashboard bilgileri bulunamadı."}
                tekrarDene={() => dashboardGetir()}
            />
        );
    }

    return (
        <PanelLayout
            title="Admin Paneli"
            subtitle="Genel sistem özeti"
            refreshing={yenileniyor}
            onRefresh={() => dashboardGetir(true)}
            menuItems={ADMIN_MENU_ITEMS}
            activeMenuKey="dashboard"
        >
            <View style={styles.summaryGrid}>
                <SummaryCard
                    title="Toplam Kullanıcı"
                    value={dashboard.toplamKullaniciSayisi}
                />

                <SummaryCard
                    title="Online Kullanıcı"
                    value={dashboard.onlineKullaniciSayisi}
                />

                <SummaryCard
                    title="Bekleyen Eğitmen"
                    value={dashboard.bekleyenEgitmenBasvuruSayisi}
                />

                <SummaryCard
                    title="Bekleyen Kurs"
                    value={dashboard.bekleyenKursOnaySayisi}
                />
            </View>

            <View style={styles.logSection}>
                <View style={styles.sectionHeader}>
                    <Text style={styles.sectionTitle}>Son Admin Logları</Text>
                    <Text style={styles.sectionSubText}>Son 3 işlem kaydı</Text>
                </View>

                {(dashboard.sonLoglar ?? []).length > 0 ? (
                    <View style={styles.logList}>
                        {dashboard.sonLoglar.map((log) => (
                            <LogKart key={log.adminLogId} log={log} />
                        ))}
                    </View>
                ) : (
                    <View style={styles.emptyLogCard}>
                        <Text style={styles.emptyLogText}>Henüz log kaydı bulunmuyor.</Text>
                    </View>
                )}
            </View>
        </PanelLayout>
    );
}

function LoadingState() {
    return (
        <View style={styles.centerContainer}>
            <ActivityIndicator size="large" color="#2563eb" />
            <Text style={styles.loadingText}>Admin dashboard yükleniyor...</Text>
        </View>
    );
}

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

function SummaryCard({ title, value }: { title: string; value: number }) {
    return (
        <View style={styles.summaryCard}>
            <Text style={styles.summaryValue}>{value}</Text>
            <Text style={styles.summaryTitle}>{title}</Text>
        </View>
    );
}

function LogKart({ log }: { log: MobileAdminLogItem }) {
    return (
        <View style={styles.logCard}>
            <View style={styles.logTop}>
                <View style={styles.logIcon}>
                    <Text style={styles.logIconText}>L</Text>
                </View>

                <View style={styles.logInfo}>
                    <Text style={styles.logTitle} numberOfLines={1}>
                        {log.islemTipi || "Bilinmeyen İşlem"}
                    </Text>

                    <Text style={styles.logAdmin} numberOfLines={1}>
                        {log.adminAdSoyad || "Bilinmeyen Admin"}
                    </Text>
                </View>

                <View style={styles.dateBadge}>
                    <Text style={styles.dateBadgeText}>
                        {tarihFormatla(log.islemTarihi)}
                    </Text>
                </View>
            </View>

            <Text style={styles.logDescription} numberOfLines={2}>
                {log.aciklama || "Açıklama yok"}
            </Text>
        </View>
    );
}

function tarihFormatla(value: string) {
    const tarih = new Date(value);

    if (Number.isNaN(tarih.getTime())) {
        return value;
    }

    return tarih.toLocaleDateString("tr-TR");
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
    },
    summaryCard: {
        flexBasis: "47%",
        flexGrow: 1,
        backgroundColor: "#ffffff",
        borderRadius: 20,
        padding: 18,
        borderWidth: 1,
        borderColor: "#e5e7eb",
        minHeight: 118,
        justifyContent: "center",
    },
    summaryValue: {
        fontSize: 32,
        fontWeight: "900",
        color: "#2563eb",
    },
    summaryTitle: {
        marginTop: 8,
        fontSize: 14,
        fontWeight: "900",
        color: "#0f172a",
    },
    logSection: {
        marginTop: 22,
    },
    sectionHeader: {
        marginBottom: 12,
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
    logList: {
        gap: 10,
    },
    logCard: {
        backgroundColor: "#ffffff",
        borderRadius: 18,
        padding: 14,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    logTop: {
        flexDirection: "row",
        alignItems: "center",
    },
    logIcon: {
        width: 42,
        height: 42,
        borderRadius: 15,
        backgroundColor: "#eff6ff",
        alignItems: "center",
        justifyContent: "center",
        marginRight: 11,
    },
    logIconText: {
        fontSize: 16,
        fontWeight: "900",
        color: "#2563eb",
    },
    logInfo: {
        flex: 1,
    },
    logTitle: {
        fontSize: 14,
        fontWeight: "900",
        color: "#0f172a",
    },
    logAdmin: {
        marginTop: 3,
        fontSize: 12,
        fontWeight: "700",
        color: "#64748b",
    },
    dateBadge: {
        marginLeft: 10,
        backgroundColor: "#f8fafc",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        borderRadius: 999,
        paddingHorizontal: 9,
        paddingVertical: 5,
    },
    dateBadgeText: {
        fontSize: 11,
        fontWeight: "900",
        color: "#64748b",
    },
    logDescription: {
        marginTop: 10,
        fontSize: 13,
        lineHeight: 18,
        fontWeight: "700",
        color: "#334155",
    },
    emptyLogCard: {
        backgroundColor: "#ffffff",
        borderRadius: 18,
        padding: 16,
        borderWidth: 1,
        borderColor: "#e5e7eb",
        alignItems: "center",
    },
    emptyLogText: {
        fontSize: 13,
        fontWeight: "700",
        color: "#64748b",
    },
});