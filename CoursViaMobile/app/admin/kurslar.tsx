import { router } from "expo-router";
import type { ReactNode } from "react";
import { useEffect, useState } from "react";
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
import { ADMIN_MENU_ITEMS } from "@/app/_shared/panelMenus";
import { api } from "@/src/api/client";
import type {
    MobileAdminKursItem,
    MobileAdminKurslarResponse,
    MobileAdminSecenek,
} from "@/src/types/admin";

type SiralamaValue =
    | "guncel"
    | "eski"
    | "ad-az"
    | "ad-za"
    | "puan-yuksek"
    | "puan-dusuk"
    | "ogrenci-cok"
    | "ogrenci-az";

const SIRALAMA_SECENEKLERI: { label: string; value: SiralamaValue }[] = [
    { label: "Güncel", value: "guncel" },
    { label: "Eski", value: "eski" },
    { label: "A-Z", value: "ad-az" },
    { label: "Z-A", value: "ad-za" },
    { label: "Puan: Yüksekten Düşüğe", value: "puan-yuksek" },
    { label: "Puan: Düşükten Yükseğe", value: "puan-dusuk" },
    { label: "Öğrenci: Çoktan Aza", value: "ogrenci-cok" },
    { label: "Öğrenci: Azdan Çoğa", value: "ogrenci-az" },
];

// Admin kurslar ekranı.
// Arama, durum filtresi, kategori filtresi, sıralama ve sayfalama destekler.
// Mobilde kurs onay/red yoktur, sadece görüntüleme vardır.
export default function AdminKurslarScreen() {
    const [kurslar, setKurslar] = useState<MobileAdminKursItem[]>([]);
    const [durumlar, setDurumlar] = useState<MobileAdminSecenek[]>([]);
    const [kategoriler, setKategoriler] = useState<MobileAdminSecenek[]>([]);

    const [aramaInput, setAramaInput] = useState("");
    const [arama, setArama] = useState<string | null>(null);

    const [durumId, setDurumId] = useState<number | null>(null);
    const [kategoriId, setKategoriId] = useState<number | null>(null);
    const [sirala, setSirala] = useState<SiralamaValue>("guncel");

    const [sayfa, setSayfa] = useState(1);
    const [sayfaBasinaKayit] = useState(10);

    const [toplamKayit, setToplamKayit] = useState(0);
    const [toplamSayfa, setToplamSayfa] = useState(1);

    const [yukleniyor, setYukleniyor] = useState(true);
    const [yenileniyor, setYenileniyor] = useState(false);
    const [hata, setHata] = useState<string | null>(null);

    useEffect(() => {
        kurslariGetir(false, {
            arama: null,
            durumId: null,
            kategoriId: null,
            sirala: "guncel",
            sayfa: 1,
        });
    }, []);

    async function kurslariGetir(
        refreshMi = false,
        override?: {
            arama?: string | null;
            durumId?: number | null;
            kategoriId?: number | null;
            sirala?: SiralamaValue;
            sayfa?: number;
        }
    ) {
        const aktifArama =
            override && "arama" in override ? override.arama ?? null : arama;

        const aktifDurumId =
            override && "durumId" in override ? override.durumId ?? null : durumId;

        const aktifKategoriId =
            override && "kategoriId" in override
                ? override.kategoriId ?? null
                : kategoriId;

        const aktifSirala = override?.sirala ?? sirala;
        const aktifSayfa = override?.sayfa ?? sayfa;

        try {
            if (refreshMi) {
                setYenileniyor(true);
            } else {
                setYukleniyor(true);
            }

            setHata(null);

            const response = await api.get<MobileAdminKurslarResponse>(
                "/api/mobile/admin/kurslar",
                {
                    params: {
                        arama: aktifArama || undefined,
                        durumId: aktifDurumId || undefined,
                        kategoriId: aktifKategoriId || undefined,
                        sirala: aktifSirala,
                        sayfa: aktifSayfa,
                        sayfaBasinaKayit,
                    },
                }
            );

            setKurslar(response.data.kurslar ?? []);
            setDurumlar(response.data.durumlar ?? []);
            setKategoriler(response.data.kategoriler ?? []);

            setToplamKayit(response.data.toplamKayit ?? 0);
            setToplamSayfa(response.data.toplamSayfa ?? 1);
            setSayfa(response.data.sayfa ?? aktifSayfa);

            setArama(response.data.arama ?? aktifArama);
            setDurumId(response.data.durumId ?? aktifDurumId);
            setKategoriId(response.data.kategoriId ?? aktifKategoriId);
            setSirala((response.data.sirala as SiralamaValue) ?? aktifSirala);
            setAramaInput(response.data.arama ?? aktifArama ?? "");
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Kurslar alınırken hata oluştu.";

            setHata(mesaj);
            Alert.alert("Hata", mesaj);
        } finally {
            setYukleniyor(false);
            setYenileniyor(false);
        }
    }

    function aramaUygula() {
        const temizArama = aramaInput.trim() || null;

        Keyboard.dismiss();

        setArama(temizArama);
        setSayfa(1);

        kurslariGetir(false, {
            arama: temizArama,
            durumId,
            kategoriId,
            sirala,
            sayfa: 1,
        });
    }

    function filtreleriTemizle() {
        Keyboard.dismiss();

        setAramaInput("");
        setArama(null);
        setDurumId(null);
        setKategoriId(null);
        setSirala("guncel");
        setSayfa(1);

        kurslariGetir(false, {
            arama: null,
            durumId: null,
            kategoriId: null,
            sirala: "guncel",
            sayfa: 1,
        });
    }

    function durumSec(yeniDurumId: number | null) {
        Keyboard.dismiss();

        const aktifArama = aramaInput.trim() || null;

        setArama(aktifArama);
        setDurumId(yeniDurumId);
        setSayfa(1);

        kurslariGetir(false, {
            arama: aktifArama,
            durumId: yeniDurumId,
            kategoriId,
            sirala,
            sayfa: 1,
        });
    }

    function kategoriSec(yeniKategoriId: number | null) {
        Keyboard.dismiss();

        const aktifArama = aramaInput.trim() || null;

        setArama(aktifArama);
        setKategoriId(yeniKategoriId);
        setSayfa(1);

        kurslariGetir(false, {
            arama: aktifArama,
            durumId,
            kategoriId: yeniKategoriId,
            sirala,
            sayfa: 1,
        });
    }

    function siralamaSec(yeniSirala: SiralamaValue) {
        Keyboard.dismiss();

        const aktifArama = aramaInput.trim() || null;

        setArama(aktifArama);
        setSirala(yeniSirala);
        setSayfa(1);

        kurslariGetir(false, {
            arama: aktifArama,
            durumId,
            kategoriId,
            sirala: yeniSirala,
            sayfa: 1,
        });
    }

    function sayfaDegistir(yeniSayfa: number) {
        if (yeniSayfa < 1 || yeniSayfa > toplamSayfa || yeniSayfa === sayfa) {
            return;
        }

        setSayfa(yeniSayfa);

        kurslariGetir(false, {
            arama,
            durumId,
            kategoriId,
            sirala,
            sayfa: yeniSayfa,
        });
    }

    if (yukleniyor) {
        return <LoadingState />;
    }

    if (hata) {
        return <ErrorState mesaj={hata} tekrarDene={() => kurslariGetir()} />;
    }

    return (
        <PanelLayout
            title="Kurslar"
            subtitle="Kursları durum, kategori, arama ve sıralama ile inceleyebilirsin."
            refreshing={yenileniyor}
            onRefresh={() => kurslariGetir(true)}
            menuItems={ADMIN_MENU_ITEMS}
            activeMenuKey="kurslar"
        >
            <FilterPanel
                aramaInput={aramaInput}
                setAramaInput={setAramaInput}
                aramaUygula={aramaUygula}
                filtreleriTemizle={filtreleriTemizle}
                durumlar={durumlar}
                durumId={durumId}
                durumSec={durumSec}
                kategoriler={kategoriler}
                kategoriId={kategoriId}
                kategoriSec={kategoriSec}
                sirala={sirala}
                siralamaSec={siralamaSec}
            />

            <View style={styles.listHeader}>
                <View>
                    <Text style={styles.listTitle}>Kurs Listesi</Text>
                    <Text style={styles.listSubText}>{toplamKayit} kurs bulundu</Text>
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
                        <KursKart key={kurs.kursId} kurs={kurs} />
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

function LoadingState() {
    return (
        <View style={styles.centerContainer}>
            <ActivityIndicator size="large" color="#2563eb" />
            <Text style={styles.loadingText}>Kurslar yükleniyor...</Text>
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

function FilterPanel({
    aramaInput,
    setAramaInput,
    aramaUygula,
    filtreleriTemizle,
    durumlar,
    durumId,
    durumSec,
    kategoriler,
    kategoriId,
    kategoriSec,
    sirala,
    siralamaSec,
}: {
    aramaInput: string;
    setAramaInput: (value: string) => void;
    aramaUygula: () => void;
    filtreleriTemizle: () => void;
    durumlar: MobileAdminSecenek[];
    durumId: number | null;
    durumSec: (durumId: number | null) => void;
    kategoriler: MobileAdminSecenek[];
    kategoriId: number | null;
    kategoriSec: (kategoriId: number | null) => void;
    sirala: SiralamaValue;
    siralamaSec: (sirala: SiralamaValue) => void;
}) {
    return (
        <View style={styles.filterCard}>
            <View style={styles.filterHeader}>
                <Text style={styles.filterTitle}>Filtrele</Text>
                <Text style={styles.filterSubtitle}>
                    Arama, durum, kategori ve sıralama seç
                </Text>
            </View>

            <View style={styles.formGroup}>
                <Text style={styles.inputLabel}>Arama</Text>

                <TextInput
                    value={aramaInput}
                    onChangeText={setAramaInput}
                    placeholder="Kurs, eğitmen, durum veya kategori ara..."
                    style={styles.searchInput}
                    returnKeyType="search"
                    onSubmitEditing={aramaUygula}
                />
            </View>

            <View style={styles.formGroup}>
                <Text style={styles.inputLabel}>Durum</Text>

                <DurumDropdown
                    durumlar={durumlar}
                    durumId={durumId}
                    durumSec={durumSec}
                />
            </View>

            <View style={styles.formGroup}>
                <Text style={styles.inputLabel}>Kategori</Text>

                <KategoriDropdown
                    kategoriler={kategoriler}
                    kategoriId={kategoriId}
                    kategoriSec={kategoriSec}
                />
            </View>

            <View style={styles.formGroup}>
                <Text style={styles.inputLabel}>Sıralama</Text>

                <SiralamaDropdown sirala={sirala} siralamaSec={siralamaSec} />
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

function DurumDropdown({
    durumlar,
    durumId,
    durumSec,
}: {
    durumlar: MobileAdminSecenek[];
    durumId: number | null;
    durumSec: (durumId: number | null) => void;
}) {
    const [modalAcik, setModalAcik] = useState(false);

    const seciliDurum = durumlar.find((x) => x.id === durumId);
    const seciliMetin = seciliDurum ? seciliDurum.ad : "Tüm Durumlar";

    function sec(value: number | null) {
        setModalAcik(false);
        durumSec(value);
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
                        durumId !== null ? styles.dropdownTextActive : null,
                    ]}
                    numberOfLines={1}
                >
                    {seciliMetin}
                </Text>

                <Text style={styles.dropdownArrow}>⌄</Text>
            </Pressable>

            <SelectionModal
                visible={modalAcik}
                title="Durum Seç"
                close={() => setModalAcik(false)}
            >
                <DropdownItem
                    label="Tüm Durumlar"
                    active={durumId === null}
                    onPress={() => sec(null)}
                />

                {durumlar.map((durum) => (
                    <DropdownItem
                        key={durum.id}
                        label={durum.ad}
                        active={durum.id === durumId}
                        onPress={() => sec(durum.id)}
                    />
                ))}
            </SelectionModal>
        </>
    );
}

function KategoriDropdown({
    kategoriler,
    kategoriId,
    kategoriSec,
}: {
    kategoriler: MobileAdminSecenek[];
    kategoriId: number | null;
    kategoriSec: (kategoriId: number | null) => void;
}) {
    const [modalAcik, setModalAcik] = useState(false);

    const seciliKategori = kategoriler.find((x) => x.id === kategoriId);
    const seciliMetin = seciliKategori ? seciliKategori.ad : "Tüm Kategoriler";

    function sec(value: number | null) {
        setModalAcik(false);
        kategoriSec(value);
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
                        kategoriId !== null ? styles.dropdownTextActive : null,
                    ]}
                    numberOfLines={1}
                >
                    {seciliMetin}
                </Text>

                <Text style={styles.dropdownArrow}>⌄</Text>
            </Pressable>

            <SelectionModal
                visible={modalAcik}
                title="Kategori Seç"
                close={() => setModalAcik(false)}
            >
                <DropdownItem
                    label="Tüm Kategoriler"
                    active={kategoriId === null}
                    onPress={() => sec(null)}
                />

                {kategoriler.map((kategori) => (
                    <DropdownItem
                        key={kategori.id}
                        label={kategori.ad}
                        active={kategori.id === kategoriId}
                        onPress={() => sec(kategori.id)}
                    />
                ))}
            </SelectionModal>
        </>
    );
}

function SiralamaDropdown({
    sirala,
    siralamaSec,
}: {
    sirala: SiralamaValue;
    siralamaSec: (sirala: SiralamaValue) => void;
}) {
    const [modalAcik, setModalAcik] = useState(false);

    const seciliSiralama =
        SIRALAMA_SECENEKLERI.find((x) => x.value === sirala) ??
        SIRALAMA_SECENEKLERI[0];

    function sec(value: SiralamaValue) {
        setModalAcik(false);
        siralamaSec(value);
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
                    style={[styles.dropdownText, styles.dropdownTextActive]}
                    numberOfLines={1}
                >
                    {seciliSiralama.label}
                </Text>

                <Text style={styles.dropdownArrow}>⌄</Text>
            </Pressable>

            <SelectionModal
                visible={modalAcik}
                title="Sıralama Seç"
                close={() => setModalAcik(false)}
            >
                {SIRALAMA_SECENEKLERI.map((item) => (
                    <DropdownItem
                        key={item.value}
                        label={item.label}
                        active={item.value === sirala}
                        onPress={() => sec(item.value)}
                    />
                ))}
            </SelectionModal>
        </>
    );
}

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

                    <ScrollView style={styles.dropdownList} showsVerticalScrollIndicator={false}>
                        {children}
                    </ScrollView>
                </View>
            </View>
        </Modal>
    );
}

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

function KursKart({ kurs }: { kurs: MobileAdminKursItem }) {
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

                    <View style={styles.badgeRow}>
                        <View style={styles.statusBadge}>
                            <Text style={styles.statusBadgeText}>{kurs.durumAdi}</Text>
                        </View>

                        <View style={styles.infoBadge}>
                            <Text style={styles.infoBadgeText}>
                                {kurs.ogrenciSayisi} öğrenci
                            </Text>
                        </View>
                    </View>
                </View>

                <Text style={styles.cardArrow}>›</Text>
            </View>

            {kurs.kategoriler && kurs.kategoriler.length > 0 ? (
                <View style={styles.categoryRow}>
                    {kurs.kategoriler.slice(0, 4).map((kategori) => (
                        <View key={kategori} style={styles.categoryChip}>
                            <Text style={styles.categoryChipText} numberOfLines={1}>
                                {kategori}
                            </Text>
                        </View>
                    ))}
                </View>
            ) : null}

            <View style={styles.statsRow}>
                <MiniStat title="Ders" value={kurs.dersSayisi} />
                <MiniStat
                    title="Puan"
                    value={kurs.degerlendirmeSayisi > 0 ? kurs.ortalamaPuan : "-"}
                />
                <MiniStat title="Yorum" value={kurs.degerlendirmeSayisi} />
            </View>
        </Pressable>
    );
}

function MiniStat({ title, value }: { title: string; value: number | string }) {
    return (
        <View style={styles.miniStat}>
            <Text style={styles.miniStatValue}>{value}</Text>
            <Text style={styles.miniStatTitle}>{title}</Text>
        </View>
    );
}

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

function EmptyState() {
    return (
        <View style={styles.emptyCard}>
            <Text style={styles.emptyTitle}>Kurs bulunamadı</Text>
            <Text style={styles.emptyText}>
                Aramana veya filtrelerine uygun kurs bulunamadı.
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
    buttonPressed: {
        opacity: 0.75,
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
    badgeRow: {
        marginTop: 8,
        flexDirection: "row",
        flexWrap: "wrap",
        gap: 7,
    },
    statusBadge: {
        backgroundColor: "#eff6ff",
        borderWidth: 1,
        borderColor: "#bfdbfe",
        borderRadius: 999,
        paddingHorizontal: 9,
        paddingVertical: 5,
    },
    statusBadgeText: {
        fontSize: 11,
        fontWeight: "900",
        color: "#2563eb",
    },
    infoBadge: {
        backgroundColor: "#f8fafc",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        borderRadius: 999,
        paddingHorizontal: 9,
        paddingVertical: 5,
    },
    infoBadgeText: {
        fontSize: 11,
        fontWeight: "900",
        color: "#64748b",
    },
    cardArrow: {
        marginLeft: 10,
        fontSize: 26,
        fontWeight: "900",
        color: "#2563eb",
    },
    categoryRow: {
        marginTop: 12,
        flexDirection: "row",
        flexWrap: "wrap",
        gap: 7,
    },
    categoryChip: {
        backgroundColor: "#f8fafc",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        borderRadius: 999,
        paddingHorizontal: 10,
        paddingVertical: 6,
    },
    categoryChipText: {
        fontSize: 11,
        fontWeight: "900",
        color: "#475569",
    },
    statsRow: {
        marginTop: 13,
        flexDirection: "row",
        gap: 9,
    },
    miniStat: {
        flex: 1,
        backgroundColor: "#f8fafc",
        borderRadius: 14,
        padding: 10,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    miniStatValue: {
        fontSize: 16,
        fontWeight: "900",
        color: "#0f172a",
    },
    miniStatTitle: {
        marginTop: 3,
        fontSize: 11,
        fontWeight: "800",
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