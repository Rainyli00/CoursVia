import { router, useLocalSearchParams } from "expo-router";
import { useEffect, useMemo, useState } from "react";
import {
    ActivityIndicator,
    Alert,
    Keyboard,
    Pressable,
    StyleSheet,
    Text,
    TextInput,
    View,
} from "react-native";

import PanelLayout from "@/app/_shared/PanelLayout";
import { ADMIN_MENU_ITEMS } from "@/app/_shared/panelMenus";
import { api } from "@/src/api/client";
import type {
    MobileAdminKullaniciKursItem,
    MobileAdminKullaniciKurslarResponse,
} from "@/src/types/admin";

// Admin kullanıcı kayıtlı kursları ekranı.
// Kullanıcının hangi kurslara kayıtlı olduğunu sade şekilde gösterir.
export default function AdminKullaniciKurslariScreen() {
    const params = useLocalSearchParams<{
        kullaniciId?: string | string[];
    }>();

    // Kullanıcının kayıtlı kurs listesi ve başlıkta gösterilecek ad bilgisi.
    const [kurslar, setKurslar] = useState<MobileAdminKullaniciKursItem[]>([]);
    const [adSoyad, setAdSoyad] = useState("");

    // aramaInput ekrandaki yazı, arama ise API'ye uygulanmış aktif filtredir.
    const [aramaInput, setAramaInput] = useState("");
    const [arama, setArama] = useState<string | null>(null);

    // Backend sayfalama bilgileri.
    const [sayfa, setSayfa] = useState(1);
    const [sayfaBasinaKayit] = useState(10);

    const [toplamKayit, setToplamKayit] = useState(0);
    const [toplamSayfa, setToplamSayfa] = useState(1);

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

    // Geçerli kullanıcı id varsa kayıtlı kurslar yüklenir, yoksa hata ekranı gösterilir.
    useEffect(() => {
        if (!kullaniciId) {
            setYukleniyor(false);
            setHata("Geçersiz kullanıcı bilgisi.");
            return;
        }

        kurslariGetir(false, {
            arama: null,
            sayfa: 1,
        });
    }, [kullaniciId]);

    // Kullanıcının kayıtlı kurslarını API'den getirir; override arama/sayfa için kullanılır.
    async function kurslariGetir(
        refreshMi = false,
        override?: {
            arama?: string | null;
            sayfa?: number;
        }
    ) {
        if (!kullaniciId) {
            return;
        }

        const aktifArama =
            override && "arama" in override ? override.arama ?? null : arama;

        const aktifSayfa = override?.sayfa ?? sayfa;

        try {
            if (refreshMi) {
                setYenileniyor(true);
            } else {
                setYukleniyor(true);
            }

            setHata(null);

            const response = await api.get<MobileAdminKullaniciKurslarResponse>(
                `/api/mobile/admin/kullanicilar/${kullaniciId}/kurslar`,
                {
                    params: {
                        arama: aktifArama || undefined,
                        sayfa: aktifSayfa,
                        sayfaBasinaKayit,
                    },
                }
            );

            setKurslar(response.data.kurslar ?? []);
            setAdSoyad(response.data.adSoyad ?? "");

            setToplamKayit(response.data.toplamKayit ?? 0);
            setToplamSayfa(response.data.toplamSayfa ?? 1);
            setSayfa(response.data.sayfa ?? aktifSayfa);

            setArama(response.data.arama ?? aktifArama);
            setAramaInput(response.data.arama ?? aktifArama ?? "");
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Kullanıcının kayıtlı kursları alınırken hata oluştu.";

            setHata(mesaj);
            Alert.alert("Hata", mesaj);
        } finally {
            setYukleniyor(false);
            setYenileniyor(false);
        }
    }

    // Arama uygulandığında liste ilk sayfadan yeniden yüklenir.
    function aramaUygula() {
        const temizArama = aramaInput.trim() || null;

        Keyboard.dismiss();

        setArama(temizArama);
        setSayfa(1);

        kurslariGetir(false, {
            arama: temizArama,
            sayfa: 1,
        });
    }

    // Arama filtresini temizleyip listeyi varsayılana döndürür.
    function filtreleriTemizle() {
        Keyboard.dismiss();

        setAramaInput("");
        setArama(null);
        setSayfa(1);

        kurslariGetir(false, {
            arama: null,
            sayfa: 1,
        });
    }

    // Geçersiz veya mevcut sayfaya tekrar istek atılmaz.
    function sayfaDegistir(yeniSayfa: number) {
        if (yeniSayfa < 1 || yeniSayfa > toplamSayfa || yeniSayfa === sayfa) {
            return;
        }

        setSayfa(yeniSayfa);

        kurslariGetir(false, {
            arama,
            sayfa: yeniSayfa,
        });
    }

    if (yukleniyor) {
        return <LoadingState />;
    }

    if (hata) {
        return (
            <ErrorState
                mesaj={hata}
                tekrarDene={() => kurslariGetir(false)}
            />
        );
    }

    return (
        <PanelLayout
            title="Kayıtlı Kurslar"
            subtitle={adSoyad ? `${adSoyad} kullanıcısının kursları` : "Kullanıcının kursları"}
            refreshing={yenileniyor}
            onRefresh={() => kurslariGetir(true)}
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
                <Text style={styles.backButtonText}>← Kullanıcı Detayına Dön</Text>
            </Pressable>

            <View style={styles.filterCard}>
                <Text style={styles.filterTitle}>Arama</Text>

                <TextInput
                    value={aramaInput}
                    onChangeText={setAramaInput}
                    placeholder="Kurs veya eğitmen ara..."
                    style={styles.searchInput}
                    returnKeyType="search"
                    onSubmitEditing={aramaUygula}
                />

                <View style={styles.filterButtonRow}>
                    <Pressable
                        onPress={aramaUygula}
                        style={({ pressed }) => [
                            styles.applyButton,
                            pressed ? styles.buttonPressed : null,
                        ]}
                    >
                        <Text style={styles.applyButtonText}>Uygula</Text>
                    </Pressable>

                    <Pressable
                        onPress={filtreleriTemizle}
                        style={({ pressed }) => [
                            styles.resetButton,
                            pressed ? styles.buttonPressed : null,
                        ]}
                    >
                        <Text style={styles.resetButtonText}>Temizle</Text>
                    </Pressable>
                </View>
            </View>

            <View style={styles.listHeader}>
                <View>
                    <Text style={styles.listTitle}>Kurs Listesi</Text>
                    <Text style={styles.listSubText}>{toplamKayit} kayıt bulundu</Text>
                </View>

                <View style={styles.pageBadge}>
                    <Text style={styles.pageBadgeText}>
                        {sayfa}/{toplamSayfa}
                    </Text>
                </View>
            </View>

            {kurslar.length > 0 ? (
                <View style={styles.courseList}>
                    {kurslar.map((kurs) => (
                        <KursKart key={kurs.kursKayitId} kurs={kurs} />
                    ))}
                </View>
            ) : (
                <EmptyState />
            )}

            <PaginationControls
                sayfa={sayfa}
                toplamSayfa={toplamSayfa}
                oncekiSayfa={() => sayfaDegistir(sayfa - 1)}
                sonrakiSayfa={() => sayfaDegistir(sayfa + 1)}
            />
        </PanelLayout>
    );
}

// İlk liste yüklenirken gösterilen tam ekran loading.
function LoadingState() {
    return (
        <View style={styles.centerContainer}>
            <ActivityIndicator size="large" color="#2563eb" />
            <Text style={styles.loadingText}>Kayıtlı kurslar yükleniyor...</Text>
        </View>
    );
}

// Liste alınamazsa tekrar deneme ve geri dönme aksiyonlarını gösterir.
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

// Kullanıcının kayıtlı olduğu kursu özetler; tıklandığında kurs detayına gider.
function KursKart({ kurs }: { kurs: MobileAdminKullaniciKursItem }) {
    return (
        <Pressable
            onPress={() => router.push(`/admin/kurs-detay/${kurs.kursId}` as any)}
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
                    <Text style={styles.courseTitle} numberOfLines={2}>
                        {kurs.kursAdi}
                    </Text>

                    <Text style={styles.instructorText} numberOfLines={1}>
                        {kurs.egitmenAdSoyad}
                    </Text>
                </View>

                <Text style={styles.cardArrow}>›</Text>
            </View>

            <View style={styles.badgeRow}>
                <View style={kurs.aktifMi ? styles.activeBadge : styles.passiveBadge}>
                    <Text
                        style={
                            kurs.aktifMi
                                ? styles.activeBadgeText
                                : styles.passiveBadgeText
                        }
                    >
                        {kurs.aktifMi ? "Aktif" : "Pasif"}
                    </Text>
                </View>

                <View style={kurs.tamamlandiMi ? styles.completedBadge : styles.progressBadge}>
                    <Text
                        style={
                            kurs.tamamlandiMi
                                ? styles.completedBadgeText
                                : styles.progressBadgeText
                        }
                    >
                        {kurs.tamamlandiMi ? "Tamamlandı" : "Devam Ediyor"}
                    </Text>
                </View>

                <View style={styles.dateBadge}>
                    <Text style={styles.dateBadgeText}>
                        {tarihFormatla(kurs.kayitTarihi)}
                    </Text>
                </View>
            </View>
        </Pressable>
    );
}

// Sayfa sınırlarında önceki/sonraki butonlarını pasifleştirir.
function PaginationControls({
    sayfa,
    toplamSayfa,
    oncekiSayfa,
    sonrakiSayfa,
}: {
    sayfa: number;
    toplamSayfa: number;
    oncekiSayfa: () => void;
    sonrakiSayfa: () => void;
}) {
    return (
        <View style={styles.pagination}>
            <Pressable
                disabled={sayfa <= 1}
                onPress={oncekiSayfa}
                style={({ pressed }) => [
                    styles.pageButton,
                    sayfa <= 1 ? styles.pageButtonDisabled : null,
                    pressed && sayfa > 1 ? styles.buttonPressed : null,
                ]}
            >
                <Text
                    style={[
                        styles.pageButtonText,
                        sayfa <= 1 ? styles.pageButtonTextDisabled : null,
                    ]}
                >
                    Önceki
                </Text>
            </Pressable>

            <Text style={styles.pageInfo}>
                Sayfa {sayfa} / {toplamSayfa}
            </Text>

            <Pressable
                disabled={sayfa >= toplamSayfa}
                onPress={sonrakiSayfa}
                style={({ pressed }) => [
                    styles.pageButton,
                    sayfa >= toplamSayfa ? styles.pageButtonDisabled : null,
                    pressed && sayfa < toplamSayfa ? styles.buttonPressed : null,
                ]}
            >
                <Text
                    style={[
                        styles.pageButtonText,
                        sayfa >= toplamSayfa ? styles.pageButtonTextDisabled : null,
                    ]}
                >
                    Sonraki
                </Text>
            </Pressable>
        </View>
    );
}

// Kullanıcının kayıtlı kursu yoksa gösterilir.
function EmptyState() {
    return (
        <View style={styles.emptyCard}>
            <Text style={styles.emptyTitle}>Kayıtlı kurs bulunamadı</Text>
            <Text style={styles.emptyText}>
                Bu kullanıcı için kayıtlı kurs bulunamadı.
            </Text>
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
    filterCard: {
        backgroundColor: "#ffffff",
        borderRadius: 22,
        padding: 16,
        borderWidth: 1,
        borderColor: "#e5e7eb",
        marginBottom: 20,
    },
    filterTitle: {
        fontSize: 17,
        fontWeight: "900",
        color: "#0f172a",
        marginBottom: 10,
    },
    searchInput: {
        minHeight: 46,
        backgroundColor: "#f8fafc",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        borderRadius: 15,
        paddingHorizontal: 13,
        fontSize: 14,
        color: "#0f172a",
    },
    filterButtonRow: {
        flexDirection: "row",
        gap: 10,
        marginTop: 12,
    },
    applyButton: {
        flex: 1,
        minHeight: 44,
        backgroundColor: "#2563eb",
        borderRadius: 15,
        alignItems: "center",
        justifyContent: "center",
    },
    applyButtonText: {
        color: "#ffffff",
        fontSize: 13,
        fontWeight: "900",
    },
    resetButton: {
        flex: 1,
        minHeight: 44,
        backgroundColor: "#f8fafc",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        borderRadius: 15,
        alignItems: "center",
        justifyContent: "center",
    },
    resetButtonText: {
        color: "#334155",
        fontSize: 13,
        fontWeight: "900",
    },
    listHeader: {
        marginBottom: 14,
        flexDirection: "row",
        alignItems: "center",
        justifyContent: "space-between",
        gap: 12,
    },
    listTitle: {
        fontSize: 20,
        fontWeight: "900",
        color: "#0f172a",
    },
    listSubText: {
        marginTop: 3,
        fontSize: 12,
        fontWeight: "700",
        color: "#64748b",
    },
    pageBadge: {
        backgroundColor: "#eff6ff",
        borderWidth: 1,
        borderColor: "#bfdbfe",
        borderRadius: 999,
        paddingHorizontal: 12,
        paddingVertical: 7,
    },
    pageBadgeText: {
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
        padding: 15,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    courseTop: {
        flexDirection: "row",
        alignItems: "center",
    },
    courseAvatar: {
        width: 50,
        height: 50,
        borderRadius: 17,
        backgroundColor: "#eff6ff",
        alignItems: "center",
        justifyContent: "center",
        marginRight: 12,
    },
    courseAvatarText: {
        fontSize: 21,
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
        lineHeight: 21,
    },
    instructorText: {
        marginTop: 3,
        fontSize: 12,
        fontWeight: "700",
        color: "#64748b",
    },
    cardArrow: {
        marginLeft: 10,
        fontSize: 26,
        fontWeight: "900",
        color: "#2563eb",
    },
    badgeRow: {
        marginTop: 13,
        flexDirection: "row",
        flexWrap: "wrap",
        gap: 7,
    },
    activeBadge: {
        backgroundColor: "#dcfce7",
        borderWidth: 1,
        borderColor: "#bbf7d0",
        borderRadius: 999,
        paddingHorizontal: 9,
        paddingVertical: 5,
    },
    activeBadgeText: {
        fontSize: 11,
        fontWeight: "900",
        color: "#16a34a",
    },
    passiveBadge: {
        backgroundColor: "#fef2f2",
        borderWidth: 1,
        borderColor: "#fecaca",
        borderRadius: 999,
        paddingHorizontal: 9,
        paddingVertical: 5,
    },
    passiveBadgeText: {
        fontSize: 11,
        fontWeight: "900",
        color: "#dc2626",
    },
    completedBadge: {
        backgroundColor: "#eff6ff",
        borderWidth: 1,
        borderColor: "#bfdbfe",
        borderRadius: 999,
        paddingHorizontal: 9,
        paddingVertical: 5,
    },
    completedBadgeText: {
        fontSize: 11,
        fontWeight: "900",
        color: "#2563eb",
    },
    progressBadge: {
        backgroundColor: "#f8fafc",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        borderRadius: 999,
        paddingHorizontal: 9,
        paddingVertical: 5,
    },
    progressBadgeText: {
        fontSize: 11,
        fontWeight: "900",
        color: "#64748b",
    },
    dateBadge: {
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
    pagination: {
        marginTop: 18,
        flexDirection: "row",
        alignItems: "center",
        justifyContent: "space-between",
        gap: 12,
    },
    pageButton: {
        minWidth: 92,
        backgroundColor: "#eff6ff",
        borderWidth: 1,
        borderColor: "#bfdbfe",
        borderRadius: 14,
        paddingHorizontal: 14,
        paddingVertical: 11,
        alignItems: "center",
    },
    pageButtonDisabled: {
        backgroundColor: "#f8fafc",
        borderColor: "#e5e7eb",
    },
    pageButtonText: {
        fontSize: 13,
        fontWeight: "900",
        color: "#2563eb",
    },
    pageButtonTextDisabled: {
        color: "#94a3b8",
    },
    pageInfo: {
        flex: 1,
        textAlign: "center",
        fontSize: 13,
        fontWeight: "900",
        color: "#334155",
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
