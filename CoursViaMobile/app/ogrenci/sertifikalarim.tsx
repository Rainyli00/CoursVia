import { useEffect, useState } from "react";
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
import { OGRENCI_MENU_ITEMS } from "@/app/_shared/panelMenus";
import { api } from "@/src/api/client";
import type {
    MobileOgrenciSertifikaItem,
    MobileOgrenciSertifikalarimResponse,
} from "@/src/types/ogrenci";

// Öğrencinin kazandığı sertifikaları gösteren ekran.
// Bu ekranda arama ve sayfalama bulunur.
export default function OgrenciSertifikalarimScreen() {
    // Backend'den gelen sertifika listesini tutar.
    const [sertifikalar, setSertifikalar] = useState<
        MobileOgrenciSertifikaItem[]
    >([]);

    // Arama inputunda yazılan değeri tutar.
    const [aramaInput, setAramaInput] = useState("");

    // API'ye gönderilen aktif arama değerini tutar.
    const [arama, setArama] = useState<string | null>(null);

    // Aktif sayfa bilgisini tutar.
    const [sayfa, setSayfa] = useState(1);

    // Sayfa başına kaç kayıt çekileceğini belirler.
    const [sayfaBasinaKayit] = useState(10);

    // API'den gelen toplam kayıt bilgisini tutar.
    const [toplamKayit, setToplamKayit] = useState(0);

    // API'den gelen toplam sayfa bilgisini tutar.
    const [toplamSayfa, setToplamSayfa] = useState(1);

    // İlk yükleme durumunu tutar.
    const [yukleniyor, setYukleniyor] = useState(true);

    // Pull-to-refresh durumunu tutar.
    const [yenileniyor, setYenileniyor] = useState(false);

    // API hata mesajını tutar.
    const [hata, setHata] = useState<string | null>(null);

    // Ekran ilk açıldığında sertifikaları getiriyoruz.
    useEffect(() => {
        sertifikalariGetir();
    }, []);

    // GET /api/mobile/ogrenci/sertifikalarim
    async function sertifikalariGetir(
        refreshMi = false,
        override?: {
            arama?: string | null;
            sayfa?: number;
        }
    ) {
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

            const response = await api.get<MobileOgrenciSertifikalarimResponse>(
                "/api/mobile/ogrenci/sertifikalarim",
                {
                    params: {
                        arama: aktifArama || undefined,
                        sayfa: aktifSayfa,
                        sayfaBasinaKayit,
                    },
                }
            );

            setSertifikalar(response.data.sertifikalar ?? []);
            setToplamKayit(response.data.toplamKayit ?? 0);
            setToplamSayfa(response.data.toplamSayfa ?? 1);
            setSayfa(response.data.sayfa ?? aktifSayfa);
            setArama(response.data.arama ?? aktifArama);
            setAramaInput(response.data.arama ?? aktifArama ?? "");
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Sertifikalar alınırken hata oluştu.";

            setHata(mesaj);
            Alert.alert("Hata", mesaj);
        } finally {
            setYukleniyor(false);
            setYenileniyor(false);
        }
    }

    // Arama inputundaki değeri API'ye uygular.
    function aramaUygula() {
        const temizArama = aramaInput.trim() || null;

        Keyboard.dismiss();

        setArama(temizArama);
        setSayfa(1);

        sertifikalariGetir(false, {
            arama: temizArama,
            sayfa: 1,
        });
    }

    // Arama filtresini temizler.
    function filtreleriTemizle() {
        Keyboard.dismiss();

        setAramaInput("");
        setArama(null);
        setSayfa(1);

        sertifikalariGetir(false, {
            arama: null,
            sayfa: 1,
        });
    }

    // Sayfa değiştirir.
    function sayfaDegistir(yeniSayfa: number) {
        if (yeniSayfa < 1 || yeniSayfa > toplamSayfa || yeniSayfa === sayfa) {
            return;
        }

        setSayfa(yeniSayfa);

        sertifikalariGetir(false, {
            arama,
            sayfa: yeniSayfa,
        });
    }

    // İlk yükleme ekranı.
    if (yukleniyor) {
        return <LoadingState />;
    }

    // Hata ekranı.
    if (hata) {
        return <ErrorState mesaj={hata} tekrarDene={() => sertifikalariGetir()} />;
    }

    return (
        <PanelLayout
            title="Sertifikalarım"
            subtitle="Tamamladığın kurslardan kazandığın sertifikaları buradan görebilirsin."
            refreshing={yenileniyor}
            onRefresh={() => sertifikalariGetir(true)}
            menuItems={OGRENCI_MENU_ITEMS}
            activeMenuKey="sertifikalarim"
        >
            <SummaryCard value={toplamKayit} />

            <FilterPanel
                aramaInput={aramaInput}
                setAramaInput={setAramaInput}
                aramaUygula={aramaUygula}
                filtreleriTemizle={filtreleriTemizle}
            />

            <View style={styles.listHeader}>
                <View>
                    <Text style={styles.listTitle}>Sertifikalar</Text>
                    <Text style={styles.listSubText}>{toplamKayit} kayıt bulundu</Text>
                </View>

                <View style={styles.pageBadge}>
                    <Text style={styles.pageBadgeText}>
                        {sayfa}/{toplamSayfa}
                    </Text>
                </View>
            </View>

            {sertifikalar.length > 0 ? (
                <View style={styles.certificateList}>
                    {sertifikalar.map((sertifika) => (
                        <SertifikaKart
                            key={sertifika.sertifikaId}
                            sertifika={sertifika}
                        />
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

// İlk yükleme componenti.
function LoadingState() {
    return (
        <View style={styles.centerContainer}>
            <ActivityIndicator size="large" color="#2563eb" />
            <Text style={styles.loadingText}>Sertifikaların yükleniyor...</Text>
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

// Sertifikalarım üst özet kartı.
function SummaryCard({ value }: { value: number }) {
    return (
        <View style={styles.summaryCard}>
            <Text style={styles.summaryValue}>{value}</Text>
            <Text style={styles.summaryTitle}>Toplam Sertifika</Text>
        </View>
    );
}

// Sertifikalarım filtre alanı.
function FilterPanel({
    aramaInput,
    setAramaInput,
    aramaUygula,
    filtreleriTemizle,
}: {
    aramaInput: string;
    setAramaInput: (value: string) => void;
    aramaUygula: () => void;
    filtreleriTemizle: () => void;
}) {
    return (
        <View style={styles.filterCard}>
            <View style={styles.filterHeader}>
                <Text style={styles.filterTitle}>Filtrele</Text>
                <Text style={styles.filterSubtitle}>Sertifikalarını hızlıca bul</Text>
            </View>

            <View style={styles.formGroup}>
                <Text style={styles.inputLabel}>Arama</Text>

                <TextInput
                    value={aramaInput}
                    onChangeText={setAramaInput}
                    placeholder="Kurs, eğitmen veya sertifika kodu ara..."
                    style={styles.searchInput}
                    returnKeyType="search"
                    onSubmitEditing={aramaUygula}
                />
            </View>

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
    );
}

// Tek bir sertifika kartı.
function SertifikaKart({
    sertifika,
}: {
    sertifika: MobileOgrenciSertifikaItem;
}) {
    return (
        <View style={styles.certificateCard}>
            <View style={styles.certificateTop}>
                <View style={styles.certificateIcon}>
                    <Text style={styles.certificateIconText}>✓</Text>
                </View>

                <View style={styles.certificateInfo}>
                    <Text style={styles.certificateCourse} numberOfLines={1}>
                        {sertifika.kursAdi}
                    </Text>

                    <Text style={styles.certificateSubText} numberOfLines={1}>
                        {sertifika.egitmenAdSoyad || "CoursVia Sertifikası"}
                    </Text>
                </View>
            </View>

            <View style={styles.infoBox}>
                <Text style={styles.infoLabel}>Sertifika Kodu</Text>

                <Text style={styles.infoValue} numberOfLines={1}>
                    {sertifika.sertifikaKodu || "-"}
                </Text>
            </View>

            <View style={styles.infoBox}>
                <Text style={styles.infoLabel}>Verilme Tarihi</Text>

                <Text style={styles.infoValue}>
                    {sertifika.verilmeTarihi
                        ? tarihFormatla(sertifika.verilmeTarihi)
                        : "-"}
                </Text>
            </View>
        </View>
    );
}

// ISO tarih değerini basit TR formatına çevirir.
function tarihFormatla(value: string) {
    const tarih = new Date(value);

    if (Number.isNaN(tarih.getTime())) {
        return value;
    }

    return tarih.toLocaleDateString("tr-TR");
}

// Sayfalama butonları.
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

// Sertifika yokken gösterilecek boş durum.
function EmptyState() {
    return (
        <View style={styles.emptyCard}>
            <Text style={styles.emptyTitle}>Sertifika bulunamadı</Text>

            <Text style={styles.emptyText}>
                Aramana uygun sertifika bulunamadı.
            </Text>
        </View>
    );
}

// Sertifikalarım ekranı stilleri.
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
    summaryCard: {
        backgroundColor: "#ffffff",
        borderRadius: 18,
        padding: 16,
        borderWidth: 1,
        borderColor: "#e5e7eb",
        marginBottom: 16,
    },
    summaryValue: {
        fontSize: 30,
        fontWeight: "900",
        color: "#0f172a",
    },
    summaryTitle: {
        marginTop: 4,
        fontSize: 13,
        fontWeight: "700",
        color: "#64748b",
    },
    filterCard: {
        backgroundColor: "#ffffff",
        borderRadius: 22,
        padding: 16,
        borderWidth: 1,
        borderColor: "#e5e7eb",
        marginBottom: 20,
    },
    filterHeader: {
        marginBottom: 14,
    },
    filterTitle: {
        fontSize: 17,
        fontWeight: "900",
        color: "#0f172a",
    },
    filterSubtitle: {
        marginTop: 3,
        fontSize: 12,
        fontWeight: "700",
        color: "#64748b",
    },
    formGroup: {
        marginBottom: 12,
    },
    inputLabel: {
        marginBottom: 7,
        fontSize: 12,
        fontWeight: "900",
        color: "#475569",
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
        marginTop: 4,
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
    buttonPressed: {
        opacity: 0.75,
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
    certificateList: {
        gap: 12,
    },
    certificateCard: {
        backgroundColor: "#ffffff",
        borderRadius: 20,
        padding: 16,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    certificateTop: {
        flexDirection: "row",
        alignItems: "center",
        marginBottom: 14,
    },
    certificateIcon: {
        width: 48,
        height: 48,
        borderRadius: 16,
        backgroundColor: "#dcfce7",
        alignItems: "center",
        justifyContent: "center",
        marginRight: 12,
    },
    certificateIconText: {
        fontSize: 22,
        fontWeight: "900",
        color: "#16a34a",
    },
    certificateInfo: {
        flex: 1,
    },
    certificateCourse: {
        fontSize: 16,
        fontWeight: "900",
        color: "#0f172a",
    },
    certificateSubText: {
        marginTop: 3,
        fontSize: 13,
        color: "#64748b",
    },
    infoBox: {
        backgroundColor: "#f8fafc",
        borderRadius: 14,
        paddingHorizontal: 12,
        paddingVertical: 10,
        marginTop: 8,
    },
    infoLabel: {
        fontSize: 12,
        fontWeight: "800",
        color: "#64748b",
    },
    infoValue: {
        marginTop: 3,
        fontSize: 14,
        fontWeight: "900",
        color: "#0f172a",
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