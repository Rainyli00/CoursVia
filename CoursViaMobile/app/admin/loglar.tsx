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
    MobileAdminLogItem,
    MobileAdminLogKategori,
    MobileAdminLoglarResponse,
} from "@/src/types/admin";

type SiralamaValue = "yeni" | "eski";

const SIRALAMA_SECENEKLERI: { label: string; value: SiralamaValue }[] = [
    {
        label: "Yeni",
        value: "yeni",
    },
    {
        label: "Eski",
        value: "eski",
    },
];

// Admin logları ekranı.
// Arama, kategori filtresi, yeni/eski sıralama ve sayfalama destekler.
export default function AdminLoglarScreen() {
    const [loglar, setLoglar] = useState<MobileAdminLogItem[]>([]);
    const [kategoriler, setKategoriler] = useState<MobileAdminLogKategori[]>([]);

    const [aramaInput, setAramaInput] = useState("");
    const [arama, setArama] = useState<string | null>(null);

    const [kategori, setKategori] = useState("tum");
    const [sirala, setSirala] = useState<SiralamaValue>("yeni");

    const [sayfa, setSayfa] = useState(1);
    const [sayfaBasinaKayit] = useState(10);

    const [toplamKayit, setToplamKayit] = useState(0);
    const [toplamSayfa, setToplamSayfa] = useState(1);

    const [yukleniyor, setYukleniyor] = useState(true);
    const [yenileniyor, setYenileniyor] = useState(false);
    const [hata, setHata] = useState<string | null>(null);

    useEffect(() => {
        loglariGetir(false, {
            arama: null,
            kategori: "tum",
            sirala: "yeni",
            sayfa: 1,
        });
    }, []);

    async function loglariGetir(
        refreshMi = false,
        override?: {
            arama?: string | null;
            kategori?: string;
            sirala?: SiralamaValue;
            sayfa?: number;
        }
    ) {
        const aktifArama =
            override && "arama" in override ? override.arama ?? null : arama;

        const aktifKategori = override?.kategori ?? kategori;
        const aktifSirala = override?.sirala ?? sirala;
        const aktifSayfa = override?.sayfa ?? sayfa;

        try {
            if (refreshMi) {
                setYenileniyor(true);
            } else {
                setYukleniyor(true);
            }

            setHata(null);

            const response = await api.get<MobileAdminLoglarResponse>(
                "/api/mobile/admin/loglar",
                {
                    params: {
                        arama: aktifArama || undefined,
                        kategori: aktifKategori,
                        sirala: aktifSirala,
                        sayfa: aktifSayfa,
                        sayfaBasinaKayit,
                    },
                }
            );

            setLoglar(response.data.loglar ?? []);
            setKategoriler(response.data.kategoriler ?? []);

            setToplamKayit(response.data.toplamKayit ?? 0);
            setToplamSayfa(response.data.toplamSayfa ?? 1);
            setSayfa(response.data.sayfa ?? aktifSayfa);

            setArama(response.data.arama ?? aktifArama);
            setKategori(response.data.kategori ?? aktifKategori);
            setSirala((response.data.sirala as SiralamaValue) ?? aktifSirala);
            setAramaInput(response.data.arama ?? aktifArama ?? "");
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Admin logları alınırken hata oluştu.";

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

        loglariGetir(false, {
            arama: temizArama,
            kategori,
            sirala,
            sayfa: 1,
        });
    }

    function filtreleriTemizle() {
        Keyboard.dismiss();

        setAramaInput("");
        setArama(null);
        setKategori("tum");
        setSirala("yeni");
        setSayfa(1);

        loglariGetir(false, {
            arama: null,
            kategori: "tum",
            sirala: "yeni",
            sayfa: 1,
        });
    }

    function kategoriSec(yeniKategori: string) {
        Keyboard.dismiss();

        const aktifArama = aramaInput.trim() || null;

        setArama(aktifArama);
        setKategori(yeniKategori);
        setSayfa(1);

        loglariGetir(false, {
            arama: aktifArama,
            kategori: yeniKategori,
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

        loglariGetir(false, {
            arama: aktifArama,
            kategori,
            sirala: yeniSirala,
            sayfa: 1,
        });
    }

    function sayfaDegistir(yeniSayfa: number) {
        if (yeniSayfa < 1 || yeniSayfa > toplamSayfa || yeniSayfa === sayfa) {
            return;
        }

        setSayfa(yeniSayfa);

        loglariGetir(false, {
            arama,
            kategori,
            sirala,
            sayfa: yeniSayfa,
        });
    }

    if (yukleniyor) {
        return <LoadingState />;
    }

    if (hata) {
        return <ErrorState mesaj={hata} tekrarDene={() => loglariGetir()} />;
    }

    return (
        <PanelLayout
            title="Admin Logları"
            subtitle="Sistem işlem kayıtlarını kategori ve tarihe göre inceleyebilirsin."
            refreshing={yenileniyor}
            onRefresh={() => loglariGetir(true)}
            menuItems={ADMIN_MENU_ITEMS}
            activeMenuKey="loglar"
        >
            <FilterPanel
                aramaInput={aramaInput}
                setAramaInput={setAramaInput}
                aramaUygula={aramaUygula}
                filtreleriTemizle={filtreleriTemizle}
                kategoriler={kategoriler}
                kategori={kategori}
                kategoriSec={kategoriSec}
                sirala={sirala}
                siralamaSec={siralamaSec}
            />

            <View style={styles.listHeader}>
                <View>
                    <Text style={styles.listTitle}>Log Listesi</Text>
                    <Text style={styles.listSubText}>{toplamKayit} kayıt bulundu</Text>
                </View>

                <View style={styles.pageBadge}>
                    <Text style={styles.pageBadgeText}>
                        {sayfa}/{toplamSayfa}
                    </Text>
                </View>
            </View>

            {loglar.length > 0 ? (
                <View style={styles.logList}>
                    {loglar.map((log) => (
                        <LogKart key={log.adminLogId} log={log} />
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
            <Text style={styles.loadingText}>Admin logları yükleniyor...</Text>
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
    kategoriler,
    kategori,
    kategoriSec,
    sirala,
    siralamaSec,
}: {
    aramaInput: string;
    setAramaInput: (value: string) => void;
    aramaUygula: () => void;
    filtreleriTemizle: () => void;
    kategoriler: MobileAdminLogKategori[];
    kategori: string;
    kategoriSec: (kategori: string) => void;
    sirala: SiralamaValue;
    siralamaSec: (sirala: SiralamaValue) => void;
}) {
    return (
        <View style={styles.filterCard}>
            <View style={styles.filterHeader}>
                <Text style={styles.filterTitle}>Filtrele</Text>
                <Text style={styles.filterSubtitle}>
                    Arama, kategori ve sıralama seç
                </Text>
            </View>

            <View style={styles.formGroup}>
                <Text style={styles.inputLabel}>Arama</Text>

                <TextInput
                    value={aramaInput}
                    onChangeText={setAramaInput}
                    placeholder="Admin, işlem, açıklama veya IP ara..."
                    style={styles.searchInput}
                    returnKeyType="search"
                    onSubmitEditing={aramaUygula}
                />
            </View>

            <View style={styles.formGroup}>
                <Text style={styles.inputLabel}>Kategori</Text>

                <KategoriDropdown
                    kategoriler={kategoriler}
                    kategori={kategori}
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

function KategoriDropdown({
    kategoriler,
    kategori,
    kategoriSec,
}: {
    kategoriler: MobileAdminLogKategori[];
    kategori: string;
    kategoriSec: (kategori: string) => void;
}) {
    const [modalAcik, setModalAcik] = useState(false);

    const seciliKategori =
        kategoriler.find((x) => x.kategori === kategori) ??
        kategoriler.find((x) => x.kategori === "tum");

    const seciliMetin = seciliKategori?.kategoriAdi ?? "Tüm Kategoriler";

    function sec(value: string) {
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
                <Text style={[styles.dropdownText, styles.dropdownTextActive]} numberOfLines={1}>
                    {seciliMetin}
                </Text>

                <Text style={styles.dropdownArrow}>⌄</Text>
            </Pressable>

            <SelectionModal
                visible={modalAcik}
                title="Kategori Seç"
                close={() => setModalAcik(false)}
            >
                {kategoriler.map((item) => (
                    <DropdownItem
                        key={item.kategori}
                        label={item.kategoriAdi}
                        active={item.kategori === kategori}
                        onPress={() => sec(item.kategori)}
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
                <Text style={[styles.dropdownText, styles.dropdownTextActive]} numberOfLines={1}>
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

            <Text style={styles.logDescription}>{log.aciklama}</Text>

            <View style={styles.logFooter}>
                <Text style={styles.logDateTime}>{tarihSaatFormatla(log.islemTarihi)}</Text>

                {log.ipAdresi ? (
                    <Text style={styles.ipText} numberOfLines={1}>
                        IP: {log.ipAdresi}
                    </Text>
                ) : (
                    <Text style={styles.ipText}>IP yok</Text>
                )}
            </View>
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
            <Text style={styles.emptyTitle}>Log bulunamadı</Text>
            <Text style={styles.emptyText}>
                Aramana veya filtrelerine uygun admin log kaydı bulunamadı.
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
    logList: {
        gap: 12,
    },
    logCard: {
        backgroundColor: "#ffffff",
        borderRadius: 20,
        padding: 15,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    logTop: {
        flexDirection: "row",
        alignItems: "center",
    },
    logIcon: {
        width: 44,
        height: 44,
        borderRadius: 15,
        backgroundColor: "#eff6ff",
        alignItems: "center",
        justifyContent: "center",
        marginRight: 11,
    },
    logIconText: {
        fontSize: 17,
        fontWeight: "900",
        color: "#2563eb",
    },
    logInfo: {
        flex: 1,
    },
    logTitle: {
        fontSize: 15,
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
        marginTop: 12,
        fontSize: 13,
        lineHeight: 19,
        fontWeight: "700",
        color: "#334155",
    },
    logFooter: {
        marginTop: 12,
        flexDirection: "row",
        justifyContent: "space-between",
        gap: 10,
    },
    logDateTime: {
        flex: 1,
        fontSize: 11,
        fontWeight: "800",
        color: "#64748b",
    },
    ipText: {
        flex: 1,
        textAlign: "right",
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