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
import { OGRENCI_MENU_ITEMS } from "@/app/_shared/panelMenus";
import { api } from "@/src/api/client";
import type {
    MobileOgrenciSinavItem,
    MobileOgrenciSinavlarimResponse,
} from "@/src/types/ogrenci";

// Öğrencinin kayıtlı kurslarındaki sınav durumlarını gösteren ekran.
// Mobilde sınava giriş yok; sadece durum, puan, giriş sayısı ve kalan hak bilgisi gösteriyoruz.
// Bu ekranda artık kategori filtresi yok; sadece arama ve sayfalama var.
export default function OgrenciSinavlarimScreen() {
    // Backend'den gelen sınav listesini tutar.
    const [sinavlar, setSinavlar] = useState<MobileOgrenciSinavItem[]>([]);

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

    // Ekran ilk açıldığında sınavları getiriyoruz.
    useEffect(() => {
        sinavlariGetir();
    }, []);

    // Sınav listesine göre üst özet kart değerlerini hesaplar.
    const ozet = useMemo(() => {
        const basariliSayisi = sinavlar.filter(
            (x) => x.durumMetni === "Başarılı"
        ).length;

        const hazirSayisi = sinavlar.filter(
            (x) =>
                x.durumMetni === "Sınava hazır" ||
                x.durumMetni === "Tekrar girilebilir"
        ).length;

        const hakDolduSayisi = sinavlar.filter(
            (x) => x.durumMetni === "Hak doldu"
        ).length;

        return {
            toplamKayit,
            basariliSayisi,
            hazirSayisi,
            hakDolduSayisi,
        };
    }, [sinavlar, toplamKayit]);

    // GET /api/mobile/ogrenci/sinavlarim
    async function sinavlariGetir(
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

            // Sınavlarım API'sinde artık kategori yok.
            // Sadece arama, sayfa ve sayfaBasinaKayit gönderiyoruz.
            const response = await api.get<MobileOgrenciSinavlarimResponse>(
                "/api/mobile/ogrenci/sinavlarim",
                {
                    params: {
                        arama: aktifArama || undefined,
                        sayfa: aktifSayfa,
                        sayfaBasinaKayit,
                    },
                }
            );

            setSinavlar(response.data.sinavlar ?? []);
            setToplamKayit(response.data.toplamKayit ?? 0);
            setToplamSayfa(response.data.toplamSayfa ?? 1);
            setSayfa(response.data.sayfa ?? aktifSayfa);
            setArama(response.data.arama ?? aktifArama);
            setAramaInput(response.data.arama ?? aktifArama ?? "");
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Sınav bilgileri alınırken hata oluştu.";

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

        sinavlariGetir(false, {
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

        sinavlariGetir(false, {
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

        sinavlariGetir(false, {
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
        return <ErrorState mesaj={hata} tekrarDene={() => sinavlariGetir()} />;
    }

    return (
        <PanelLayout
            title="Sınavlarım"
            subtitle="Kayıtlı kurslarındaki sınav durumlarını buradan takip edebilirsin."
            refreshing={yenileniyor}
            onRefresh={() => sinavlariGetir(true)}
            menuItems={OGRENCI_MENU_ITEMS}
            activeMenuKey="sinavlarim"
        >
            <View style={styles.summaryGrid}>
                <SummaryCard title="Toplam Kayıt" value={ozet.toplamKayit} />
                <SummaryCard title="Başarılı" value={ozet.basariliSayisi} />
                <SummaryCard title="Hazır" value={ozet.hazirSayisi} />
                <SummaryCard title="Hak Doldu" value={ozet.hakDolduSayisi} />
            </View>

            <FilterPanel
                aramaInput={aramaInput}
                setAramaInput={setAramaInput}
                aramaUygula={aramaUygula}
                filtreleriTemizle={filtreleriTemizle}
            />

            <View style={styles.listHeader}>
                <View>
                    <Text style={styles.listTitle}>Sınav Durumları</Text>
                    <Text style={styles.listSubText}>{toplamKayit} kayıt bulundu</Text>
                </View>

                <View style={styles.pageBadge}>
                    <Text style={styles.pageBadgeText}>
                        {sayfa}/{toplamSayfa}
                    </Text>
                </View>
            </View>

            {sinavlar.length > 0 ? (
                <View style={styles.examList}>
                    {sinavlar.map((sinav) => (
                        <SinavKart key={sinav.kursKayitId} sinav={sinav} />
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
            <Text style={styles.loadingText}>Sınavların yükleniyor...</Text>
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

// Üstteki küçük özet kartı.
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

// Sınavlarım filtre alanı.
// Kategori yok; sadece arama ve temizleme var.
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
                <Text style={styles.filterSubtitle}>Sınav durumlarını hızlıca bul</Text>
            </View>

            <View style={styles.formGroup}>
                <Text style={styles.inputLabel}>Arama</Text>

                <TextInput
                    value={aramaInput}
                    onChangeText={setAramaInput}
                    placeholder="Kurs, eğitmen veya sınav ara..."
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

// Tek bir sınav durum kartı.
function SinavKart({ sinav }: { sinav: MobileOgrenciSinavItem }) {
    return (
        <View style={styles.examCard}>
            <View style={styles.examTop}>
                <View style={styles.examIcon}>
                    <Text style={styles.examIconText}>S</Text>
                </View>

                <View style={styles.examInfo}>
                    <Text style={styles.examCourse} numberOfLines={1}>
                        {sinav.kursAdi}
                    </Text>

                    <Text style={styles.examName} numberOfLines={1}>
                        {sinav.sinavAdi || "Sınav bulunmuyor"}
                    </Text>
                </View>
            </View>

            <View style={styles.statusBox}>
                <View style={styles.statusHeader}>
                    <Text style={styles.statusLabel}>Durum</Text>

                    <Text style={[styles.statusBadge, durumRengiGetir(sinav.durumMetni)]}>
                        {sinav.durumMetni}
                    </Text>
                </View>

                <Text style={styles.statusDescription}>
                    {durumAciklamasiGetir(sinav)}
                </Text>
            </View>

            <View style={styles.examMetaGrid}>
                <InfoPill
                    label="Son Puan"
                    value={sinav.sonPuan === null ? "-" : String(sinav.sonPuan)}
                />

                <InfoPill
                    label="Geçme Notu"
                    value={sinav.gecmeNotu === null ? "-" : String(sinav.gecmeNotu)}
                />

                <InfoPill label="Giriş Sayısı" value={String(sinav.girisSayisi)} />

                <InfoPill label="Kalan Hak" value={String(sinav.kalanHak)} />
            </View>

            {sinav.sonSinavTarihi ? (
                <Text style={styles.lastExamText}>
                    Son sınav: {tarihFormatla(sinav.sonSinavTarihi)}
                </Text>
            ) : null}
        </View>
    );
}

// Küçük bilgi kutusu.
function InfoPill({ label, value }: { label: string; value: string }) {
    return (
        <View style={styles.infoPill}>
            <Text style={styles.infoLabel}>{label}</Text>
            <Text style={styles.infoValue}>{value}</Text>
        </View>
    );
}

// Sınav durumuna göre açıklama üretir.
function durumAciklamasiGetir(sinav: MobileOgrenciSinavItem) {
    if (sinav.guncelleniyorMu && sinav.sonucGectiMi !== true) {
        return "Kurs erişime geçici olarak kapalı. Güncelleme tamamlandığında devam edebilirsiniz.";
    }

    if (!sinav.sinavId) {
        return "Bu kurs için henüz sınav tanımlanmamış.";
    }

    if (!sinav.derslerTamamlandiMi) {
        return "Sınava hazır olmak için önce kurs derslerini tamamlamalısın.";
    }

    if (sinav.sonucGectiMi === true) {
        return "Bu kursun sınavını başarıyla tamamladın.";
    }

    if (sinav.kalanHak <= 0) {
        return "Sınav hakkın dolmuş görünüyor.";
    }

    if (sinav.durumMetni === "Devam ediyor") {
        return "Devam eden bir sınav girişin bulunuyor.";
    }

    return "Sınav durumunu ve kalan hakkını buradan takip edebilirsin.";
}

// Duruma göre rozet rengi verir.
function durumRengiGetir(durumMetni: string) {
    if (durumMetni === "Başarılı") {
        return styles.statusSuccess;
    }

    if (durumMetni === "Hak doldu") {
        return styles.statusDanger;
    }

    if (durumMetni === "Güncelleniyor") {
        return styles.statusWarning;
    }

    if (durumMetni === "Sınava hazır" || durumMetni === "Tekrar girilebilir") {
        return styles.statusPrimary;
    }

    return styles.statusNeutral;
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

// Sınav bilgisi yokken gösterilecek boş durum.
function EmptyState() {
    return (
        <View style={styles.emptyCard}>
            <Text style={styles.emptyTitle}>Sınav bilgisi bulunamadı</Text>

            <Text style={styles.emptyText}>
                Aramana uygun sınav bilgisi bulunamadı.
            </Text>
        </View>
    );
}

// Sınavlarım ekranı stilleri.
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
    examList: {
        gap: 12,
    },
    examCard: {
        backgroundColor: "#ffffff",
        borderRadius: 20,
        padding: 16,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    examTop: {
        flexDirection: "row",
        alignItems: "center",
    },
    examIcon: {
        width: 48,
        height: 48,
        borderRadius: 16,
        backgroundColor: "#eff6ff",
        alignItems: "center",
        justifyContent: "center",
        marginRight: 12,
    },
    examIconText: {
        fontSize: 20,
        fontWeight: "900",
        color: "#2563eb",
    },
    examInfo: {
        flex: 1,
    },
    examCourse: {
        fontSize: 16,
        fontWeight: "900",
        color: "#0f172a",
    },
    examName: {
        marginTop: 3,
        fontSize: 13,
        color: "#64748b",
    },
    statusBox: {
        marginTop: 14,
        backgroundColor: "#f8fafc",
        borderRadius: 14,
        paddingHorizontal: 12,
        paddingVertical: 10,
    },
    statusHeader: {
        flexDirection: "row",
        alignItems: "center",
        justifyContent: "space-between",
    },
    statusLabel: {
        fontSize: 12,
        fontWeight: "800",
        color: "#64748b",
    },
    statusBadge: {
        fontSize: 12,
        fontWeight: "900",
        paddingHorizontal: 10,
        paddingVertical: 5,
        borderRadius: 999,
        overflow: "hidden",
    },
    statusSuccess: {
        backgroundColor: "#dcfce7",
        color: "#16a34a",
    },
    statusDanger: {
        backgroundColor: "#fee2e2",
        color: "#dc2626",
    },
    statusWarning: {
        backgroundColor: "#fffbeb",
        color: "#b45309",
    },
    statusPrimary: {
        backgroundColor: "#eff6ff",
        color: "#2563eb",
    },
    statusNeutral: {
        backgroundColor: "#e2e8f0",
        color: "#475569",
    },
    statusDescription: {
        marginTop: 8,
        fontSize: 13,
        lineHeight: 18,
        color: "#64748b",
    },
    examMetaGrid: {
        marginTop: 12,
        flexDirection: "row",
        flexWrap: "wrap",
        gap: 10,
    },
    infoPill: {
        flexBasis: "47%",
        flexGrow: 1,
        backgroundColor: "#ffffff",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        borderRadius: 14,
        padding: 10,
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
    lastExamText: {
        marginTop: 12,
        fontSize: 12,
        fontWeight: "700",
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
