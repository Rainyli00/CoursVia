import { router, useLocalSearchParams } from "expo-router";
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
import { ADMIN_MENU_ITEMS } from "@/app/_shared/panelMenus";
import { api } from "@/src/api/client";
import type { MobileAdminKullaniciDetayResponse } from "@/src/types/admin";

// Admin kullanıcı detay ekranı.
// Mobil V1'de sadece görüntüleme vardır.
// Kullanıcı eğitmense eğitmen profil bilgileri de gösterilir.
export default function AdminKullaniciDetayScreen() {
    const params = useLocalSearchParams<{
        kullaniciId?: string | string[];
    }>();

    // Kullanıcı detay cevabı tek state'te tutulur; hesap ve eğitmen alanları buradan okunur.
    const [detay, setDetay] = useState<MobileAdminKullaniciDetayResponse | null>(
        null
    );

    // İlk yükleme, pull-to-refresh ve hata ekranı ayrı ayrı yönetilir.
    const [yukleniyor, setYukleniyor] = useState(true);
    const [yenileniyor, setYenileniyor] = useState(false);
    const [hata, setHata] = useState<string | null>(null);

    // Expo Router parametresi string gelebileceği için güvenli sayıya çevrilir.
    const kullaniciId = useMemo(() => {
        const rawValue = Array.isArray(params.kullaniciId)
            ? params.kullaniciId[0]
            : params.kullaniciId;

        const id = Number(rawValue);

        return Number.isFinite(id) && id > 0 ? id : null;
    }, [params.kullaniciId]);

    // Geçerli kullanıcı id varsa detay yüklenir, yoksa kullanıcıya hata ekranı gösterilir.
    useEffect(() => {
        if (!kullaniciId) {
            setYukleniyor(false);
            setHata("Geçersiz kullanıcı bilgisi.");
            return;
        }

        detayGetir();
    }, [kullaniciId]);

    // Kullanıcı detayını API'den getirir; refresh sırasında tam ekran loading göstermez.
    async function detayGetir(refreshMi = false) {
        if (!kullaniciId) {
            return;
        }

        try {
            if (refreshMi) {
                setYenileniyor(true);
            } else {
                setYukleniyor(true);
            }

            setHata(null);

            const response = await api.get<MobileAdminKullaniciDetayResponse>(
                `/api/mobile/admin/kullanicilar/${kullaniciId}`
            );

            setDetay(response.data);
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Kullanıcı detayı alınırken hata oluştu.";

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

    if (hata || !detay) {
        return (
            <ErrorState
                mesaj={hata || "Kullanıcı detayı bulunamadı."}
                tekrarDene={() => detayGetir()}
            />
        );
    }

    return (
        <PanelLayout
            title="Kullanıcı Detayı"
            subtitle="Kullanıcı rol, durum ve özet bilgilerini inceleyebilirsin."
            refreshing={yenileniyor}
            onRefresh={() => detayGetir(true)}
            menuItems={ADMIN_MENU_ITEMS}
            activeMenuKey="kullanicilar"
        >
            <Pressable
                onPress={() => router.back()}
                style={({ pressed }) => [
                    styles.backButton,
                    pressed ? styles.buttonPressed : null,
                ]}
            >
                <Text style={styles.backButtonText}>← Kullanıcılara Dön</Text>
            </Pressable>

            <View style={styles.heroCard}>
                <View style={styles.heroTop}>
                    <View style={styles.userAvatar}>
                        <Text style={styles.userAvatarText}>
                            {detay.adSoyad.substring(0, 1).toUpperCase()}
                        </Text>
                    </View>

                    <View style={styles.heroInfo}>
                        <Text style={styles.userName}>{detay.adSoyad}</Text>

                        <Text style={styles.userEmail} numberOfLines={1}>
                            {detay.eposta}
                        </Text>

                        <View style={styles.badgeRow}>
                            <View style={styles.statusBadge}>
                                <Text style={styles.statusBadgeText}>{detay.durumAdi}</Text>
                            </View>

                            <View
                                style={[
                                    styles.onlineBadge,
                                    detay.onlineMi ? styles.onlineBadgeActive : null,
                                ]}
                            >
                                <Text
                                    style={[
                                        styles.onlineBadgeText,
                                        detay.onlineMi ? styles.onlineBadgeTextActive : null,
                                    ]}
                                >
                                    {detay.onlineMi ? "Online" : "Offline"}
                                </Text>
                            </View>
                        </View>
                    </View>
                </View>

                <View style={styles.infoBlock}>
                    <Text style={styles.infoBlockLabel}>Roller</Text>
                    <Text style={styles.infoBlockValue}>
                        {detay.roller || "Rol bilgisi yok"}
                    </Text>
                </View>
            </View>

            <View style={styles.summaryGrid}>
                <SummaryCard title="Kayıtlı Kurs" value={detay.kayitliKursSayisi} />
                <SummaryCard title="Tamamlanan" value={detay.tamamlananKursSayisi} />
                <SummaryCard title="Sertifika" value={detay.sertifikaSayisi} />
                <SummaryCard title="Eğitmen Kursu" value={detay.egitmenKursSayisi} />
            </View>

            <Pressable
                onPress={() =>
                    router.push(`/admin/kullanici-kurslari/${detay.kullaniciId}` as any)
                }
                style={({ pressed }) => [
                    styles.courseButton,
                    pressed ? styles.buttonPressed : null,
                ]}
            >
                <View>
                    <Text style={styles.courseButtonTitle}>Kayıtlı Kursları Gör</Text>
                    <Text style={styles.courseButtonText}>
                        Kullanıcının kayıtlı olduğu kursları listele
                    </Text>
                </View>

                <Text style={styles.courseButtonArrow}>›</Text>
            </Pressable>

            <View style={styles.detailCard}>
                <Text style={styles.detailTitle}>Hesap Bilgileri</Text>

                <DetailRow label="E-posta" value={detay.eposta} />
                <DetailRow label="Telefon" value={detay.telefon || "Bilgi yok"} />
                <DetailRow label="Kayıt Tarihi" value={tarihFormatla(detay.kayitTarihi)} />
                <DetailRow
                    label="Son Giriş"
                    value={
                        detay.sonGirisTarihi
                            ? tarihFormatla(detay.sonGirisTarihi)
                            : "Bilgi yok"
                    }
                />
                <DetailRow label="Son IP Adresi" value={detay.sonIpAdresi || "Bilgi yok"} />
                <DetailRow label="Durum" value={detay.durumAdi} />
                <DetailRow label="Online Durumu" value={detay.onlineMi ? "Online" : "Offline"} />
            </View>

            {detay.egitmenProfiliVarMi ? (
                <View style={styles.instructorCard}>
                    <Text style={styles.detailTitle}>Eğitmen Profili</Text>

                    <View style={styles.instructorStatusBox}>
                        <Text style={styles.instructorStatusLabel}>Eğitmen Durumu</Text>
                        <Text style={styles.instructorStatusValue}>
                            {detay.egitmenDurumAdi || "Bilgi yok"}
                        </Text>
                    </View>

                    <DetailBlock
                        label="Uzmanlık Alanı"
                        value={detay.uzmanlikAlani || "Bilgi yok"}
                    />

                    <DetailBlock
                        label="Biyografi"
                        value={detay.biyografi || "Bilgi yok"}
                    />

                    <DetailBlock
                        label="Deneyim"
                        value={
                            detay.deneyimYili !== null
                                ? `${detay.deneyimYili} yıl`
                                : "Bilgi yok"
                        }
                    />

                    <DetailBlock
                        label="Website"
                        value={detay.websiteUrl || "Bilgi yok"}
                    />

                    <View style={styles.branchArea}>
                        <Text style={styles.branchTitle}>Branşlar</Text>

                        {detay.branslar.length > 0 ? (
                            <View style={styles.branchRow}>
                                {detay.branslar.map((brans) => (
                                    <View key={brans} style={styles.branchChip}>
                                        <Text style={styles.branchChipText}>{brans}</Text>
                                    </View>
                                ))}
                            </View>
                        ) : (
                            <Text style={styles.noBranchText}>Branş bilgisi yok</Text>
                        )}
                    </View>
                </View>
            ) : null}
        </PanelLayout>
    );
}

// Detay verisi gelene kadar gösterilen tam ekran loading.
function LoadingState() {
    return (
        <View style={styles.centerContainer}>
            <ActivityIndicator size="large" color="#2563eb" />
            <Text style={styles.loadingText}>Kullanıcı detayı yükleniyor...</Text>
        </View>
    );
}

// Detay alınamazsa tekrar deneme ve geri dönme aksiyonlarını gösterir.
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

            <Pressable style={styles.secondaryButton} onPress={() => router.back()}>
                <Text style={styles.secondaryButtonText}>Geri Dön</Text>
            </Pressable>
        </View>
    );
}

// Kullanıcı özet sayılarını göstermek için kullanılan küçük kart.
function SummaryCard({ title, value }: { title: string; value: number }) {
    return (
        <View style={styles.summaryCard}>
            <Text style={styles.summaryValue}>{value}</Text>
            <Text style={styles.summaryTitle}>{title}</Text>
        </View>
    );
}

// Hesap bilgilerindeki kısa label/value satırı.
function DetailRow({ label, value }: { label: string; value: string }) {
    return (
        <View style={styles.detailRow}>
            <Text style={styles.detailLabel}>{label}</Text>
            <Text style={styles.detailValue}>{value}</Text>
        </View>
    );
}

// Uzun metinli eğitmen profil alanları için kullanılır.
function DetailBlock({ label, value }: { label: string; value: string }) {
    return (
        <View style={styles.detailBlock}>
            <Text style={styles.detailLabel}>{label}</Text>
            <Text style={styles.detailBlockValue}>{value}</Text>
        </View>
    );
}

// Geçerli tarihse Türkçe kısa tarih formatına çevirir.
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
        marginBottom: 10,
    },
    primaryButtonText: {
        color: "#ffffff",
        fontWeight: "900",
    },
    secondaryButton: {
        backgroundColor: "#ffffff",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        paddingHorizontal: 18,
        paddingVertical: 12,
        borderRadius: 14,
    },
    secondaryButtonText: {
        color: "#334155",
        fontWeight: "900",
    },
    buttonPressed: {
        opacity: 0.75,
    },
    backButton: {
        alignSelf: "flex-start",
        backgroundColor: "#ffffff",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        borderRadius: 999,
        paddingHorizontal: 14,
        paddingVertical: 9,
        marginBottom: 14,
    },
    backButtonText: {
        fontSize: 13,
        fontWeight: "900",
        color: "#334155",
    },
    heroCard: {
        backgroundColor: "#ffffff",
        borderRadius: 22,
        padding: 16,
        borderWidth: 1,
        borderColor: "#e5e7eb",
        marginBottom: 16,
    },
    heroTop: {
        flexDirection: "row",
        alignItems: "center",
    },
    userAvatar: {
        width: 58,
        height: 58,
        borderRadius: 19,
        backgroundColor: "#eff6ff",
        alignItems: "center",
        justifyContent: "center",
        marginRight: 13,
    },
    userAvatarText: {
        fontSize: 24,
        fontWeight: "900",
        color: "#2563eb",
    },
    heroInfo: {
        flex: 1,
    },
    userName: {
        fontSize: 20,
        fontWeight: "900",
        color: "#0f172a",
    },
    userEmail: {
        marginTop: 4,
        fontSize: 13,
        fontWeight: "700",
        color: "#64748b",
    },
    badgeRow: {
        marginTop: 9,
        flexDirection: "row",
        flexWrap: "wrap",
        gap: 7,
    },
    statusBadge: {
        backgroundColor: "#f8fafc",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        borderRadius: 999,
        paddingHorizontal: 9,
        paddingVertical: 5,
    },
    statusBadgeText: {
        fontSize: 11,
        fontWeight: "900",
        color: "#475569",
    },
    onlineBadge: {
        backgroundColor: "#f8fafc",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        borderRadius: 999,
        paddingHorizontal: 9,
        paddingVertical: 5,
    },
    onlineBadgeActive: {
        backgroundColor: "#dcfce7",
        borderColor: "#bbf7d0",
    },
    onlineBadgeText: {
        fontSize: 11,
        fontWeight: "900",
        color: "#64748b",
    },
    onlineBadgeTextActive: {
        color: "#16a34a",
    },
    infoBlock: {
        marginTop: 16,
        backgroundColor: "#f8fafc",
        borderRadius: 16,
        padding: 12,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    infoBlockLabel: {
        fontSize: 12,
        fontWeight: "900",
        color: "#64748b",
    },
    infoBlockValue: {
        marginTop: 4,
        fontSize: 14,
        fontWeight: "900",
        color: "#0f172a",
    },
    summaryGrid: {
        flexDirection: "row",
        flexWrap: "wrap",
        gap: 12,
        marginBottom: 16,
    },
    summaryCard: {
        flexBasis: "47%",
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
        color: "#2563eb",
    },
    summaryTitle: {
        marginTop: 4,
        fontSize: 12,
        fontWeight: "800",
        color: "#64748b",
    },
    courseButton: {
        backgroundColor: "#eff6ff",
        borderWidth: 1,
        borderColor: "#bfdbfe",
        borderRadius: 18,
        padding: 15,
        marginBottom: 16,
        flexDirection: "row",
        alignItems: "center",
        justifyContent: "space-between",
        gap: 12,
    },
    courseButtonTitle: {
        fontSize: 15,
        fontWeight: "900",
        color: "#1d4ed8",
    },
    courseButtonText: {
        marginTop: 3,
        fontSize: 12,
        fontWeight: "700",
        color: "#64748b",
    },
    courseButtonArrow: {
        fontSize: 26,
        fontWeight: "900",
        color: "#2563eb",
    },
    detailCard: {
        backgroundColor: "#ffffff",
        borderRadius: 22,
        padding: 16,
        borderWidth: 1,
        borderColor: "#e5e7eb",
        marginBottom: 16,
    },
    instructorCard: {
        backgroundColor: "#ffffff",
        borderRadius: 22,
        padding: 16,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    detailTitle: {
        fontSize: 18,
        fontWeight: "900",
        color: "#0f172a",
        marginBottom: 10,
    },
    detailRow: {
        paddingVertical: 11,
        borderBottomWidth: 1,
        borderBottomColor: "#f1f5f9",
    },
    detailLabel: {
        fontSize: 12,
        fontWeight: "900",
        color: "#64748b",
    },
    detailValue: {
        marginTop: 3,
        fontSize: 14,
        fontWeight: "800",
        color: "#0f172a",
    },
    detailBlock: {
        paddingVertical: 11,
        borderBottomWidth: 1,
        borderBottomColor: "#f1f5f9",
    },
    detailBlockValue: {
        marginTop: 4,
        fontSize: 14,
        lineHeight: 20,
        fontWeight: "700",
        color: "#0f172a",
    },
    instructorStatusBox: {
        backgroundColor: "#eff6ff",
        borderWidth: 1,
        borderColor: "#bfdbfe",
        borderRadius: 16,
        padding: 12,
        marginBottom: 8,
    },
    instructorStatusLabel: {
        fontSize: 12,
        fontWeight: "900",
        color: "#2563eb",
    },
    instructorStatusValue: {
        marginTop: 4,
        fontSize: 15,
        fontWeight: "900",
        color: "#0f172a",
    },
    branchArea: {
        marginTop: 12,
    },
    branchTitle: {
        fontSize: 12,
        fontWeight: "900",
        color: "#64748b",
        marginBottom: 8,
    },
    branchRow: {
        flexDirection: "row",
        flexWrap: "wrap",
        gap: 7,
    },
    branchChip: {
        backgroundColor: "#f8fafc",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        borderRadius: 999,
        paddingHorizontal: 10,
        paddingVertical: 6,
    },
    branchChipText: {
        fontSize: 11,
        fontWeight: "900",
        color: "#475569",
    },
    noBranchText: {
        fontSize: 13,
        fontWeight: "700",
        color: "#64748b",
    },
});
