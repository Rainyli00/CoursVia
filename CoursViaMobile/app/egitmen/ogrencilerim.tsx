import { useLocalSearchParams } from "expo-router";
import type { ReactNode } from "react";
import { useEffect, useMemo, useState } from "react";
import {
    ActivityIndicator,
    Alert,
    Keyboard,
    Modal,
    Pressable,
    ScrollView,
    StyleSheet,
    Text,
    TextInput,
    View,
} from "react-native";

import PanelLayout from "@/app/_shared/PanelLayout";
import { EGITMEN_MENU_ITEMS } from "@/app/_shared/panelMenus";
import { api } from "@/src/api/client";
import type {
    MobileEgitmenKursItem,
    MobileEgitmenKurslarimResponse,
    MobileEgitmenOgrenciItem,
    MobileEgitmenOgrencilerimResponse,
} from "@/src/types/egitmen";

// Eğitmen öğrencilerim ekranı.
// Aynı öğrenci birden fazla kursa kayıtlı olsa bile tek kart olarak görünür.
// Sıralama filtresi kaldırıldı.
export default function EgitmenOgrencilerimScreen() {
    const params = useLocalSearchParams<{
        kursId?: string | string[];
    }>();

    // Öğrenci listesi ve kurs filtresinde kullanılacak eğitmen kursları.
    const [ogrenciler, setOgrenciler] = useState<MobileEgitmenOgrenciItem[]>([]);
    const [kurslar, setKurslar] = useState<MobileEgitmenKursItem[]>([]);

    // aramaInput ekrandaki yazı, arama ise API'ye uygulanmış değerdir.
    const [aramaInput, setAramaInput] = useState("");
    const [arama, setArama] = useState<string | null>(null);

    // Seçili kurs filtresi; null tüm kursları ifade eder.
    const [kursId, setKursId] = useState<number | null>(null);

    // Backend sayfalama bilgileri.
    const [sayfa, setSayfa] = useState(1);
    const [sayfaBasinaKayit] = useState(10);

    const [toplamKayit, setToplamKayit] = useState(0);
    const [toplamSayfa, setToplamSayfa] = useState(1);

    const [yukleniyor, setYukleniyor] = useState(true);
    const [yenileniyor, setYenileniyor] = useState(false);
    const [hata, setHata] = useState<string | null>(null);

    // Kurs detayından gelindiyse query'deki kursId ilk filtre olarak kullanılır.
    const routeKursId = useMemo(() => {
        const rawValue = Array.isArray(params.kursId)
            ? params.kursId[0]
            : params.kursId;

        const id = Number(rawValue);

        return Number.isFinite(id) && id > 0 ? id : null;
    }, [params.kursId]);

    // Route filtresi değişirse hem dropdown kursları hem öğrenci listesi yenilenir.
    useEffect(() => {
        const ilkKursId = routeKursId;

        setKursId(ilkKursId);

        kurslariGetir();

        ogrencileriGetir(false, {
            arama: null,
            kursId: ilkKursId,
            sayfa: 1,
        });
    }, [routeKursId]);

    // Üst özet kartları toplam kayıt ve mevcut sayfadaki öğrenci sayısından oluşur.
    const ozet = useMemo(() => {
        return {
            toplamOgrenci: toplamKayit,
            sayfadakiOgrenci: ogrenciler.length,
        };
    }, [ogrenciler.length, toplamKayit]);

    // Kurs dropdown'ı için eğitmenin kurslarından kısa bir liste alınır.
    async function kurslariGetir() {
        try {
            const response = await api.get<MobileEgitmenKurslarimResponse>(
                "/api/mobile/egitmen/kurslarim",
                {
                    params: {
                        sirala: "ad-az",
                        sayfa: 1,
                        sayfaBasinaKayit: 50,
                    },
                }
            );

            setKurslar(response.data.kurslar ?? []);
        } catch {
            setKurslar([]);
        }
    }

    // Öğrencileri aktif arama/kurs filtresi ve sayfaya göre getirir.
    async function ogrencileriGetir(
        refreshMi = false,
        override?: {
            arama?: string | null;
            kursId?: number | null;
            sayfa?: number;
        }
    ) {
        const aktifArama =
            override && "arama" in override ? override.arama ?? null : arama;

        const aktifKursId =
            override && "kursId" in override ? override.kursId ?? null : kursId;

        const aktifSayfa = override?.sayfa ?? sayfa;

        try {
            if (refreshMi) {
                setYenileniyor(true);
            } else {
                setYukleniyor(true);
            }

            setHata(null);

            const response = await api.get<MobileEgitmenOgrencilerimResponse>(
                "/api/mobile/egitmen/ogrencilerim",
                {
                    params: {
                        arama: aktifArama || undefined,
                        kursId: aktifKursId || undefined,
                        sayfa: aktifSayfa,
                        sayfaBasinaKayit,
                    },
                }
            );

            setOgrenciler(response.data.ogrenciler ?? []);
            setToplamKayit(response.data.toplamKayit ?? 0);
            setToplamSayfa(response.data.toplamSayfa ?? 1);
            setSayfa(response.data.sayfa ?? aktifSayfa);
            setArama(response.data.arama ?? aktifArama);
            setKursId(response.data.kursId ?? aktifKursId);
            setAramaInput(response.data.arama ?? aktifArama ?? "");
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Öğrenciler alınırken hata oluştu.";

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

        ogrencileriGetir(false, {
            arama: temizArama,
            kursId,
            sayfa: 1,
        });
    }

    // Arama ve kurs filtresini kaldırıp listeyi varsayılana döndürür.
    function filtreleriTemizle() {
        Keyboard.dismiss();

        setAramaInput("");
        setArama(null);
        setKursId(null);
        setSayfa(1);

        ogrencileriGetir(false, {
            arama: null,
            kursId: null,
            sayfa: 1,
        });
    }

    // Kurs seçimi değişince aktif arama korunur ve sayfa bire çekilir.
    function kursSec(yeniKursId: number | null) {
        Keyboard.dismiss();

        const aktifArama = aramaInput.trim() || null;

        setArama(aktifArama);
        setKursId(yeniKursId);
        setSayfa(1);

        ogrencileriGetir(false, {
            arama: aktifArama,
            kursId: yeniKursId,
            sayfa: 1,
        });
    }

    // Geçersiz veya mevcut sayfaya tekrar istek atılmaz.
    function sayfaDegistir(yeniSayfa: number) {
        if (yeniSayfa < 1 || yeniSayfa > toplamSayfa || yeniSayfa === sayfa) {
            return;
        }

        setSayfa(yeniSayfa);

        ogrencileriGetir(false, {
            arama,
            kursId,
            sayfa: yeniSayfa,
        });
    }

    if (yukleniyor) {
        return <LoadingState />;
    }

    if (hata) {
        return <ErrorState mesaj={hata} tekrarDene={() => ogrencileriGetir()} />;
    }

    return (
        <PanelLayout
            title="Öğrencilerim"
            subtitle="Kurslarına kayıtlı öğrencileri buradan takip edebilirsin."
            refreshing={yenileniyor}
            onRefresh={() => ogrencileriGetir(true)}
            menuItems={EGITMEN_MENU_ITEMS}
            activeMenuKey="ogrencilerim"
        >
            <View style={styles.summaryGrid}>
                <SummaryCard title="Toplam Öğrenci" value={ozet.toplamOgrenci} />
                <SummaryCard title="Sayfadaki" value={ozet.sayfadakiOgrenci} />
            </View>

            <FilterPanel
                aramaInput={aramaInput}
                setAramaInput={setAramaInput}
                aramaUygula={aramaUygula}
                filtreleriTemizle={filtreleriTemizle}
                kurslar={kurslar}
                kursId={kursId}
                kursSec={kursSec}
            />

            <View style={styles.listHeader}>
                <View>
                    <Text style={styles.listTitle}>Öğrenci Listesi</Text>
                    <Text style={styles.listSubText}>{toplamKayit} öğrenci bulundu</Text>
                </View>

                <View style={styles.pageBadge}>
                    <Text style={styles.pageBadgeText}>
                        {sayfa}/{toplamSayfa}
                    </Text>
                </View>
            </View>

            {ogrenciler.length > 0 ? (
                <View style={styles.studentList}>
                    {ogrenciler.map((ogrenci) => (
                        <OgrenciKart key={ogrenci.kullaniciId} ogrenci={ogrenci} />
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
            <Text style={styles.loadingText}>Öğrenciler yükleniyor...</Text>
        </View>
    );
}

// Öğrenci listesi alınamazsa tekrar deneme butonlu hata ekranı.
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

// Arama ve kurs filtresini tek panelde toplar.
function FilterPanel({
    aramaInput,
    setAramaInput,
    aramaUygula,
    filtreleriTemizle,
    kurslar,
    kursId,
    kursSec,
}: {
    aramaInput: string;
    setAramaInput: (value: string) => void;
    aramaUygula: () => void;
    filtreleriTemizle: () => void;
    kurslar: MobileEgitmenKursItem[];
    kursId: number | null;
    kursSec: (kursId: number | null) => void;
}) {
    return (
        <View style={styles.filterCard}>
            <View style={styles.filterHeader}>
                <Text style={styles.filterTitle}>Filtrele</Text>
                <Text style={styles.filterSubtitle}>Öğrenci adı ve kurs seç</Text>
            </View>

            <View style={styles.formGroup}>
                <Text style={styles.inputLabel}>Arama</Text>

                <TextInput
                    value={aramaInput}
                    onChangeText={setAramaInput}
                    placeholder="Öğrenci adı veya kurs adı ara..."
                    style={styles.searchInput}
                    returnKeyType="search"
                    onSubmitEditing={aramaUygula}
                />
            </View>

            <View style={styles.formGroup}>
                <Text style={styles.inputLabel}>Kurs</Text>

                <KursDropdown kurslar={kurslar} kursId={kursId} kursSec={kursSec} />
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

// Eğitmenin kurslarını modal dropdown olarak gösterir.
function KursDropdown({
    kurslar,
    kursId,
    kursSec,
}: {
    kurslar: MobileEgitmenKursItem[];
    kursId: number | null;
    kursSec: (kursId: number | null) => void;
}) {
    const [modalAcik, setModalAcik] = useState(false);

    const seciliKurs = kurslar.find((x) => x.kursId === kursId);
    const seciliMetin = seciliKurs ? seciliKurs.kursAdi : "Tüm Kurslar";

    // null seçimi tüm kurslar filtresine döner.
    function sec(value: number | null) {
        setModalAcik(false);
        kursSec(value);
    }

    return (
        <>
            <Pressable
                onPress={() => {
                    Keyboard.dismiss();
                    setModalAcik(true);
                }}
                style={({ pressed }) => [
                    styles.dropdownButton,
                    pressed ? styles.buttonPressed : null,
                ]}
            >
                <Text
                    style={[
                        styles.dropdownText,
                        kursId !== null ? styles.dropdownTextActive : null,
                    ]}
                    numberOfLines={1}
                >
                    {seciliMetin}
                </Text>

                <Text style={styles.dropdownArrow}>⌄</Text>
            </Pressable>

            <SelectionModal
                visible={modalAcik}
                title="Kurs Seç"
                close={() => setModalAcik(false)}
            >
                <DropdownItem
                    label="Tüm Kurslar"
                    active={kursId === null}
                    onPress={() => sec(null)}
                />

                {kurslar.map((kurs) => (
                    <DropdownItem
                        key={kurs.kursId}
                        label={kurs.kursAdi}
                        active={kurs.kursId === kursId}
                        onPress={() => sec(kurs.kursId)}
                    />
                ))}
            </SelectionModal>
        </>
    );
}

// Dropdown seçimlerini ortak modal içinde gösterir.
function SelectionModal({
    visible,
    title,
    close,
    children,
}: {
    visible: boolean;
    title: string;
    close: () => void;
    children: ReactNode;
}) {
    return (
        <Modal visible={visible} transparent animationType="fade" onRequestClose={close}>
            <View style={styles.dropdownModalRoot}>
                <Pressable style={styles.dropdownBackdrop} onPress={close} />

                <View style={styles.dropdownModalCard}>
                    <View style={styles.dropdownModalHeader}>
                        <Text style={styles.dropdownModalTitle}>{title}</Text>

                        <Pressable
                            onPress={close}
                            style={({ pressed }) => [
                                styles.dropdownCloseButton,
                                pressed ? styles.buttonPressed : null,
                            ]}
                        >
                            <Text style={styles.dropdownCloseText}>Kapat</Text>
                        </Pressable>
                    </View>

                    <ScrollView
                        style={styles.dropdownList}
                        showsVerticalScrollIndicator={false}
                    >
                        {children}
                    </ScrollView>
                </View>
            </View>
        </Modal>
    );
}

// Modal içindeki tek seçim satırı.
function DropdownItem({
    label,
    active,
    onPress,
}: {
    label: string;
    active: boolean;
    onPress: () => void;
}) {
    return (
        <Pressable
            onPress={onPress}
            style={({ pressed }) => [
                styles.dropdownItem,
                active ? styles.dropdownItemActive : null,
                pressed ? styles.buttonPressed : null,
            ]}
        >
            <Text
                style={[
                    styles.dropdownItemLabel,
                    active ? styles.dropdownItemLabelActive : null,
                ]}
                numberOfLines={1}
            >
                {label}
            </Text>

            {active ? <Text style={styles.dropdownCheck}>✓</Text> : null}
        </Pressable>
    );
}

// Öğrenci adını ve kaç kursa kayıtlı olduğunu gösteren liste kartı.
function OgrenciKart({ ogrenci }: { ogrenci: MobileEgitmenOgrenciItem }) {
    return (
        <View style={styles.studentCard}>
            <View style={styles.studentTop}>
                <View style={styles.studentAvatar}>
                    <Text style={styles.studentAvatarText}>
                        {ogrenci.ogrenciAdSoyad.substring(0, 1).toUpperCase()}
                    </Text>
                </View>

                <View style={styles.studentInfo}>
                    <Text style={styles.studentName} numberOfLines={1}>
                        {ogrenci.ogrenciAdSoyad}
                    </Text>

                    <Text style={styles.studentSubText} numberOfLines={1}>
                        {ogrenci.kayitliKursSayisi} kursa kayıtlı
                    </Text>
                </View>

                <View style={styles.courseCountBadge}>
                    <Text style={styles.courseCountText}>
                        {ogrenci.kayitliKursSayisi}
                    </Text>
                </View>
            </View>
        </View>
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

// Arama veya kurs filtresine uygun öğrenci yoksa gösterilir.
function EmptyState() {
    return (
        <View style={styles.emptyCard}>
            <Text style={styles.emptyTitle}>Öğrenci bulunamadı</Text>
            <Text style={styles.emptyText}>
                Aramana veya filtrelerine uygun öğrenci bulunamadı.
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
        fontSize: 12,
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
    dropdownButton: {
        minHeight: 46,
        backgroundColor: "#f8fafc",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        borderRadius: 15,
        paddingHorizontal: 13,
        flexDirection: "row",
        alignItems: "center",
        justifyContent: "space-between",
        gap: 10,
    },
    dropdownText: {
        flex: 1,
        fontSize: 14,
        fontWeight: "800",
        color: "#64748b",
    },
    dropdownTextActive: {
        color: "#0f172a",
    },
    dropdownArrow: {
        fontSize: 18,
        fontWeight: "900",
        color: "#2563eb",
    },
    dropdownModalRoot: {
        flex: 1,
        justifyContent: "center",
        padding: 20,
    },
    dropdownBackdrop: {
        ...StyleSheet.absoluteFillObject,
        backgroundColor: "rgba(15, 23, 42, 0.45)",
    },
    dropdownModalCard: {
        maxHeight: "72%",
        backgroundColor: "#ffffff",
        borderRadius: 24,
        padding: 16,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    dropdownModalHeader: {
        flexDirection: "row",
        alignItems: "center",
        justifyContent: "space-between",
        gap: 12,
        marginBottom: 12,
    },
    dropdownModalTitle: {
        fontSize: 19,
        fontWeight: "900",
        color: "#0f172a",
    },
    dropdownCloseButton: {
        backgroundColor: "#f8fafc",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        borderRadius: 999,
        paddingHorizontal: 12,
        paddingVertical: 8,
    },
    dropdownCloseText: {
        fontSize: 12,
        fontWeight: "900",
        color: "#334155",
    },
    dropdownList: {
        maxHeight: 420,
    },
    dropdownItem: {
        minHeight: 52,
        borderRadius: 16,
        paddingHorizontal: 13,
        paddingVertical: 10,
        flexDirection: "row",
        alignItems: "center",
        borderWidth: 1,
        borderColor: "transparent",
        backgroundColor: "#ffffff",
    },
    dropdownItemActive: {
        backgroundColor: "#eff6ff",
        borderColor: "#bfdbfe",
    },
    dropdownItemLabel: {
        flex: 1,
        fontSize: 14,
        fontWeight: "900",
        color: "#0f172a",
    },
    dropdownItemLabelActive: {
        color: "#2563eb",
    },
    dropdownCheck: {
        marginLeft: 12,
        fontSize: 18,
        fontWeight: "900",
        color: "#2563eb",
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
    studentList: {
        gap: 12,
    },
    studentCard: {
        backgroundColor: "#ffffff",
        borderRadius: 20,
        padding: 16,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    studentTop: {
        flexDirection: "row",
        alignItems: "center",
    },
    studentAvatar: {
        width: 48,
        height: 48,
        borderRadius: 16,
        backgroundColor: "#eff6ff",
        alignItems: "center",
        justifyContent: "center",
        marginRight: 12,
    },
    studentAvatarText: {
        fontSize: 20,
        fontWeight: "900",
        color: "#2563eb",
    },
    studentInfo: {
        flex: 1,
    },
    studentName: {
        fontSize: 16,
        fontWeight: "900",
        color: "#0f172a",
    },
    studentSubText: {
        marginTop: 3,
        fontSize: 13,
        fontWeight: "700",
        color: "#64748b",
    },
    courseCountBadge: {
        marginLeft: 10,
        minWidth: 42,
        height: 42,
        borderRadius: 15,
        backgroundColor: "#eff6ff",
        borderWidth: 1,
        borderColor: "#bfdbfe",
        alignItems: "center",
        justifyContent: "center",
    },
    courseCountText: {
        fontSize: 16,
        fontWeight: "900",
        color: "#2563eb",
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
