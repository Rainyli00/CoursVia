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
    MobileAdminEgitmenBasvuruItem,
    MobileAdminEgitmenBasvurulariResponse,
    MobileAdminSecenek,
} from "@/src/types/admin";

// Admin eğitmen başvuruları ekranı.
// Arama, durum filtresi ve sayfalama destekler.
export default function AdminEgitmenBasvurulariScreen() {
    const [basvurular, setBasvurular] = useState<MobileAdminEgitmenBasvuruItem[]>([]);
    const [durumlar, setDurumlar] = useState<MobileAdminSecenek[]>([]);

    const [aramaInput, setAramaInput] = useState("");
    const [arama, setArama] = useState<string | null>(null);

    const [durumId, setDurumId] = useState<number | null>(null);

    const [sayfa, setSayfa] = useState(1);
    const [sayfaBasinaKayit] = useState(10);

    const [toplamKayit, setToplamKayit] = useState(0);
    const [toplamSayfa, setToplamSayfa] = useState(1);

    const [yukleniyor, setYukleniyor] = useState(true);
    const [yenileniyor, setYenileniyor] = useState(false);
    const [hata, setHata] = useState<string | null>(null);

    useEffect(() => {
        basvurulariGetir(false, {
            arama: null,
            durumId: null,
            sayfa: 1,
        });
    }, []);

    async function basvurulariGetir(
        refreshMi = false,
        override?: {
            arama?: string | null;
            durumId?: number | null;
            sayfa?: number;
        }
    ) {
        const aktifArama =
            override && "arama" in override ? override.arama ?? null : arama;

        const aktifDurumId =
            override && "durumId" in override ? override.durumId ?? null : durumId;

        const aktifSayfa = override?.sayfa ?? sayfa;

        try {
            if (refreshMi) {
                setYenileniyor(true);
            } else {
                setYukleniyor(true);
            }

            setHata(null);

            const response = await api.get<MobileAdminEgitmenBasvurulariResponse>(
                "/api/mobile/admin/egitmen-basvurulari",
                {
                    params: {
                        arama: aktifArama || undefined,
                        durumId: aktifDurumId || undefined,
                        sayfa: aktifSayfa,
                        sayfaBasinaKayit,
                    },
                }
            );

            setBasvurular(response.data.basvurular ?? []);
            setDurumlar((response.data.durumlar ?? []).filter((x) => x.id !== 7));

            setToplamKayit(response.data.toplamKayit ?? 0);
            setToplamSayfa(response.data.toplamSayfa ?? 1);
            setSayfa(response.data.sayfa ?? aktifSayfa);

            setArama(response.data.arama ?? aktifArama);
            setDurumId(response.data.durumId ?? aktifDurumId);
            setAramaInput(response.data.arama ?? aktifArama ?? "");
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Eğitmen başvuruları alınırken hata oluştu.";

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

        basvurulariGetir(false, {
            arama: temizArama,
            durumId,
            sayfa: 1,
        });
    }

    function filtreleriTemizle() {
        Keyboard.dismiss();

        setAramaInput("");
        setArama(null);
        setDurumId(null);
        setSayfa(1);

        basvurulariGetir(false, {
            arama: null,
            durumId: null,
            sayfa: 1,
        });
    }

    function durumSec(yeniDurumId: number | null) {
        Keyboard.dismiss();

        const aktifArama = aramaInput.trim() || null;

        setArama(aktifArama);
        setDurumId(yeniDurumId);
        setSayfa(1);

        basvurulariGetir(false, {
            arama: aktifArama,
            durumId: yeniDurumId,
            sayfa: 1,
        });
    }

    function sayfaDegistir(yeniSayfa: number) {
        if (yeniSayfa < 1 || yeniSayfa > toplamSayfa || yeniSayfa === sayfa) {
            return;
        }

        setSayfa(yeniSayfa);

        basvurulariGetir(false, {
            arama,
            durumId,
            sayfa: yeniSayfa,
        });
    }

    if (yukleniyor) {
        return <LoadingState />;
    }

    if (hata) {
        return <ErrorState mesaj={hata} tekrarDene={() => basvurulariGetir()} />;
    }

    return (
        <PanelLayout
            title="Eğitmen Başvuruları"
            subtitle="Eğitmen olmak isteyen kullanıcıların başvurularını inceleyebilirsin."
            refreshing={yenileniyor}
            onRefresh={() => basvurulariGetir(true)}
            menuItems={ADMIN_MENU_ITEMS}
            activeMenuKey="egitmen-basvurulari"
        >
            <FilterPanel
                aramaInput={aramaInput}
                setAramaInput={setAramaInput}
                aramaUygula={aramaUygula}
                filtreleriTemizle={filtreleriTemizle}
                durumlar={durumlar}
                durumId={durumId}
                durumSec={durumSec}
            />

            <View style={styles.listHeader}>
                <View>
                    <Text style={styles.listTitle}>Başvuru Listesi</Text>
                    <Text style={styles.listSubText}>{toplamKayit} başvuru bulundu</Text>
                </View>

                <View style={styles.pageBadge}>
                    <Text style={styles.pageBadgeText}>
                        {sayfa}/{toplamSayfa}
                    </Text>
                </View>
            </View>

            {basvurular.length > 0 ? (
                <View style={styles.applicationList}>
                    {basvurular.map((basvuru) => (
                        <BasvuruKart
                            key={basvuru.egitmenProfilId}
                            basvuru={basvuru}
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

function LoadingState() {
    return (
        <View style={styles.centerContainer}>
            <ActivityIndicator size="large" color="#2563eb" />
            <Text style={styles.loadingText}>Başvurular yükleniyor...</Text>
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
}: {
    aramaInput: string;
    setAramaInput: (value: string) => void;
    aramaUygula: () => void;
    filtreleriTemizle: () => void;
    durumlar: MobileAdminSecenek[];
    durumId: number | null;
    durumSec: (durumId: number | null) => void;
}) {
    return (
        <View style={styles.filterCard}>
            <View style={styles.filterHeader}>
                <Text style={styles.filterTitle}>Filtrele</Text>
                <Text style={styles.filterSubtitle}>Arama ve durum seç</Text>
            </View>

            <View style={styles.formGroup}>
                <Text style={styles.inputLabel}>Arama</Text>

                <TextInput
                    value={aramaInput}
                    onChangeText={setAramaInput}
                    placeholder="Ad, soyad, e-posta veya uzmanlık ara..."
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

function BasvuruKart({ basvuru }: { basvuru: MobileAdminEgitmenBasvuruItem }) {
    return (
        <Pressable
            onPress={() =>
                router.push(
                    `/admin/egitmen-basvuru-detay/${basvuru.egitmenProfilId}` as any
                )
            }
            style={({ pressed }) => [
                styles.applicationCard,
                pressed ? styles.buttonPressed : null,
            ]}
        >
            <View style={styles.avatar}>
                <Text style={styles.avatarText}>
                    {basvuru.adSoyad.substring(0, 1).toUpperCase()}
                </Text>
            </View>

            <View style={styles.applicationInfo}>
                <Text style={styles.applicationName} numberOfLines={1}>
                    {basvuru.adSoyad}
                </Text>

                <Text style={styles.applicationEmail} numberOfLines={1}>
                    {basvuru.eposta}
                </Text>

                <View style={styles.badgeRow}>
                    <View style={styles.statusBadge}>
                        <Text style={styles.statusBadgeText}>{basvuru.durumAdi}</Text>
                    </View>

                    {basvuru.sonIslemTarihi ? (
                        <View style={styles.dateBadge}>
                            <Text style={styles.dateBadgeText}>
                                {tarihFormatla(basvuru.sonIslemTarihi)}
                            </Text>
                        </View>
                    ) : null}
                </View>
            </View>

            <Text style={styles.cardArrow}>›</Text>
        </Pressable>
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
            <Text style={styles.emptyTitle}>Başvuru bulunamadı</Text>
            <Text style={styles.emptyText}>
                Aramana veya filtrelerine uygun eğitmen başvurusu bulunamadı.
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
    applicationList: {
        gap: 12,
    },
    applicationCard: {
        backgroundColor: "#ffffff",
        borderRadius: 20,
        padding: 15,
        borderWidth: 1,
        borderColor: "#e5e7eb",
        flexDirection: "row",
        alignItems: "center",
    },
    avatar: {
        width: 48,
        height: 48,
        borderRadius: 16,
        backgroundColor: "#eff6ff",
        alignItems: "center",
        justifyContent: "center",
        marginRight: 12,
    },
    avatarText: {
        fontSize: 20,
        fontWeight: "900",
        color: "#2563eb",
    },
    applicationInfo: {
        flex: 1,
    },
    applicationName: {
        fontSize: 16,
        fontWeight: "900",
        color: "#0f172a",
    },
    applicationEmail: {
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
    cardArrow: {
        marginLeft: 10,
        fontSize: 26,
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
