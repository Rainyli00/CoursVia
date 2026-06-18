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
    MobileAdminKullaniciItem,
    MobileAdminKullanicilarResponse,
    MobileAdminSecenek,
} from "@/src/types/admin";

// Admin kullanıcılar ekranı.
// Arama, rol filtresi, durum filtresi ve sayfalama destekler.
export default function AdminKullanicilarScreen() {
    // Liste verisi ve filtre dropdownlarında kullanılacak rol/durum seçenekleri.
    const [kullanicilar, setKullanicilar] = useState<MobileAdminKullaniciItem[]>([]);
    const [roller, setRoller] = useState<MobileAdminSecenek[]>([]);
    const [durumlar, setDurumlar] = useState<MobileAdminSecenek[]>([]);

    // aramaInput ekrandaki yazı, arama ise API'ye uygulanmış aktif filtredir.
    const [aramaInput, setAramaInput] = useState("");
    const [arama, setArama] = useState<string | null>(null);

    // Aktif rol ve kullanıcı durum filtreleri.
    const [rolId, setRolId] = useState<number | null>(null);
    const [durumId, setDurumId] = useState<number | null>(null);

    // Backend sayfalama bilgileri.
    const [sayfa, setSayfa] = useState(1);
    const [sayfaBasinaKayit] = useState(10);

    const [toplamKayit, setToplamKayit] = useState(0);
    const [toplamSayfa, setToplamSayfa] = useState(1);

    const [yukleniyor, setYukleniyor] = useState(true);
    const [yenileniyor, setYenileniyor] = useState(false);
    const [hata, setHata] = useState<string | null>(null);

    // Ekran açıldığında varsayılan filtrelerle ilk kullanıcı listesi çekilir.
    useEffect(() => {
        kullanicilariGetir(false, {
            arama: null,
            rolId: null,
            durumId: null,
            sayfa: 1,
        });
    }, []);

    // Kullanıcı listesini API'den çeker; override ile state güncellenmeden yeni filtrelerle istek yapılabilir.
    async function kullanicilariGetir(
        refreshMi = false,
        override?: {
            arama?: string | null;
            rolId?: number | null;
            durumId?: number | null;
            sayfa?: number;
        }
    ) {
        const aktifArama =
            override && "arama" in override ? override.arama ?? null : arama;

        const aktifRolId =
            override && "rolId" in override ? override.rolId ?? null : rolId;

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

            const response = await api.get<MobileAdminKullanicilarResponse>(
                "/api/mobile/admin/kullanicilar",
                {
                    params: {
                        arama: aktifArama || undefined,
                        rolId: aktifRolId || undefined,
                        durumId: aktifDurumId || undefined,
                        sayfa: aktifSayfa,
                        sayfaBasinaKayit,
                    },
                }
            );

            setKullanicilar(response.data.kullanicilar ?? []);
            setRoller(response.data.roller ?? []);
            setDurumlar(response.data.durumlar ?? []);

            setToplamKayit(response.data.toplamKayit ?? 0);
            setToplamSayfa(response.data.toplamSayfa ?? 1);
            setSayfa(response.data.sayfa ?? aktifSayfa);

            setArama(response.data.arama ?? aktifArama);
            setRolId(response.data.rolId ?? aktifRolId);
            setDurumId(response.data.durumId ?? aktifDurumId);
            setAramaInput(response.data.arama ?? aktifArama ?? "");
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Kullanıcılar alınırken hata oluştu.";

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

        kullanicilariGetir(false, {
            arama: temizArama,
            rolId,
            durumId,
            sayfa: 1,
        });
    }

    // Tüm filtreleri temizleyip kullanıcı listesini varsayılana döndürür.
    function filtreleriTemizle() {
        Keyboard.dismiss();

        setAramaInput("");
        setArama(null);
        setRolId(null);
        setDurumId(null);
        setSayfa(1);

        kullanicilariGetir(false, {
            arama: null,
            rolId: null,
            durumId: null,
            sayfa: 1,
        });
    }

    // Rol filtresi değişince aktif arama korunur ve sayfa bire döner.
    function rolSec(yeniRolId: number | null) {
        Keyboard.dismiss();

        const aktifArama = aramaInput.trim() || null;

        setArama(aktifArama);
        setRolId(yeniRolId);
        setSayfa(1);

        kullanicilariGetir(false, {
            arama: aktifArama,
            rolId: yeniRolId,
            durumId,
            sayfa: 1,
        });
    }

    // Durum filtresi değişince aktif arama/rol korunur ve liste baştan çekilir.
    function durumSec(yeniDurumId: number | null) {
        Keyboard.dismiss();

        const aktifArama = aramaInput.trim() || null;

        setArama(aktifArama);
        setDurumId(yeniDurumId);
        setSayfa(1);

        kullanicilariGetir(false, {
            arama: aktifArama,
            rolId,
            durumId: yeniDurumId,
            sayfa: 1,
        });
    }

    // Geçersiz veya mevcut sayfaya tekrar istek atılmaz.
    function sayfaDegistir(yeniSayfa: number) {
        if (yeniSayfa < 1 || yeniSayfa > toplamSayfa || yeniSayfa === sayfa) {
            return;
        }

        setSayfa(yeniSayfa);

        kullanicilariGetir(false, {
            arama,
            rolId,
            durumId,
            sayfa: yeniSayfa,
        });
    }

    if (yukleniyor) {
        return <LoadingState />;
    }

    if (hata) {
        return <ErrorState mesaj={hata} tekrarDene={() => kullanicilariGetir()} />;
    }

    return (
        <PanelLayout
            title="Kullanıcılar"
            subtitle="Kullanıcıları rol ve durum bilgisine göre inceleyebilirsin."
            refreshing={yenileniyor}
            onRefresh={() => kullanicilariGetir(true)}
            menuItems={ADMIN_MENU_ITEMS}
            activeMenuKey="kullanicilar"
        >
            <FilterPanel
                aramaInput={aramaInput}
                setAramaInput={setAramaInput}
                aramaUygula={aramaUygula}
                filtreleriTemizle={filtreleriTemizle}
                roller={roller}
                rolId={rolId}
                rolSec={rolSec}
                durumlar={durumlar}
                durumId={durumId}
                durumSec={durumSec}
            />

            <View style={styles.listHeader}>
                <View>
                    <Text style={styles.listTitle}>Kullanıcı Listesi</Text>
                    <Text style={styles.listSubText}>{toplamKayit} kullanıcı bulundu</Text>
                </View>

                <View style={styles.pageBadge}>
                    <Text style={styles.pageBadgeText}>
                        {sayfa}/{toplamSayfa}
                    </Text>
                </View>
            </View>

            {kullanicilar.length > 0 ? (
                <View style={styles.userList}>
                    {kullanicilar.map((kullanici) => (
                        <KullaniciKart key={kullanici.kullaniciId} kullanici={kullanici} />
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
            <Text style={styles.loadingText}>Kullanıcılar yükleniyor...</Text>
        </View>
    );
}

// Liste alınamazsa tekrar deneme butonlu hata ekranı.
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

// Arama, rol ve durum filtrelerini tek panelde toplar.
function FilterPanel({
    aramaInput,
    setAramaInput,
    aramaUygula,
    filtreleriTemizle,
    roller,
    rolId,
    rolSec,
    durumlar,
    durumId,
    durumSec,
}: {
    aramaInput: string;
    setAramaInput: (value: string) => void;
    aramaUygula: () => void;
    filtreleriTemizle: () => void;
    roller: MobileAdminSecenek[];
    rolId: number | null;
    rolSec: (rolId: number | null) => void;
    durumlar: MobileAdminSecenek[];
    durumId: number | null;
    durumSec: (durumId: number | null) => void;
}) {
    return (
        <View style={styles.filterCard}>
            <View style={styles.filterHeader}>
                <Text style={styles.filterTitle}>Filtrele</Text>
                <Text style={styles.filterSubtitle}>Arama, rol ve durum seç</Text>
            </View>

            <View style={styles.formGroup}>
                <Text style={styles.inputLabel}>Arama</Text>

                <TextInput
                    value={aramaInput}
                    onChangeText={setAramaInput}
                    placeholder="Ad, soyad veya e-posta ara..."
                    style={styles.searchInput}
                    returnKeyType="search"
                    onSubmitEditing={aramaUygula}
                />
            </View>

            <View style={styles.formGroup}>
                <Text style={styles.inputLabel}>Rol</Text>

                <SecenekDropdown
                    title="Rol Seç"
                    tumLabel="Tüm Roller"
                    secenekler={roller}
                    seciliId={rolId}
                    sec={rolSec}
                />
            </View>

            <View style={styles.formGroup}>
                <Text style={styles.inputLabel}>Durum</Text>

                <SecenekDropdown
                    title="Durum Seç"
                    tumLabel="Tüm Durumlar"
                    secenekler={durumlar}
                    seciliId={durumId}
                    sec={durumSec}
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

// Rol ve durum gibi id-ad seçeneklerini ortak modal dropdown olarak gösterir.
function SecenekDropdown({
    title,
    tumLabel,
    secenekler,
    seciliId,
    sec,
}: {
    title: string;
    tumLabel: string;
    secenekler: MobileAdminSecenek[];
    seciliId: number | null;
    sec: (id: number | null) => void;
}) {
    const [modalAcik, setModalAcik] = useState(false);

    const seciliSecenek = secenekler.find((x) => x.id === seciliId);
    const seciliMetin = seciliSecenek ? seciliSecenek.ad : tumLabel;

    // null seçimi ilgili filtrede "tümü" anlamına gelir.
    function secimYap(value: number | null) {
        setModalAcik(false);
        sec(value);
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
                        seciliId !== null ? styles.dropdownTextActive : null,
                    ]}
                    numberOfLines={1}
                >
                    {seciliMetin}
                </Text>

                <Text style={styles.dropdownArrow}>⌄</Text>
            </Pressable>

            <SelectionModal
                visible={modalAcik}
                title={title}
                close={() => setModalAcik(false)}
            >
                <DropdownItem
                    label={tumLabel}
                    active={seciliId === null}
                    onPress={() => secimYap(null)}
                />

                {secenekler.map((item) => (
                    <DropdownItem
                        key={item.id}
                        label={item.ad}
                        active={item.id === seciliId}
                        onPress={() => secimYap(item.id)}
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

                    <ScrollView style={styles.dropdownList} showsVerticalScrollIndicator={false}>
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

// Kullanıcı özet kartı; tıklandığında kullanıcı detayına gider.
function KullaniciKart({ kullanici }: { kullanici: MobileAdminKullaniciItem }) {
    return (
        <Pressable
            onPress={() =>
                router.push(`/admin/kullanici-detay/${kullanici.kullaniciId}` as any)
            }
            style={({ pressed }) => [
                styles.userCard,
                pressed ? styles.buttonPressed : null,
            ]}
        >
            <View style={styles.userAvatar}>
                <Text style={styles.userAvatarText}>
                    {kullanici.adSoyad.substring(0, 1).toUpperCase()}
                </Text>
            </View>

            <View style={styles.userInfo}>
                <Text style={styles.userName} numberOfLines={1}>
                    {kullanici.adSoyad}
                </Text>

                <Text style={styles.userRole} numberOfLines={1}>
                    {kullanici.roller || "Rol yok"}
                </Text>

                <View style={styles.badgeRow}>
                    <View style={styles.statusBadge}>
                        <Text style={styles.statusBadgeText}>{kullanici.durumAdi}</Text>
                    </View>

                    <View
                        style={[
                            styles.onlineBadge,
                            kullanici.onlineMi ? styles.onlineBadgeActive : null,
                        ]}
                    >
                        <Text
                            style={[
                                styles.onlineBadgeText,
                                kullanici.onlineMi ? styles.onlineBadgeTextActive : null,
                            ]}
                        >
                            {kullanici.onlineMi ? "Online" : "Offline"}
                        </Text>
                    </View>
                </View>
            </View>

            <Text style={styles.cardArrow}>›</Text>
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

// Arama veya filtrelere uygun kullanıcı yoksa gösterilir.
function EmptyState() {
    return (
        <View style={styles.emptyCard}>
            <Text style={styles.emptyTitle}>Kullanıcı bulunamadı</Text>
            <Text style={styles.emptyText}>
                Aramana veya filtrelerine uygun kullanıcı bulunamadı.
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
    userList: {
        gap: 12,
    },
    userCard: {
        backgroundColor: "#ffffff",
        borderRadius: 20,
        padding: 15,
        borderWidth: 1,
        borderColor: "#e5e7eb",
        flexDirection: "row",
        alignItems: "center",
    },
    userAvatar: {
        width: 48,
        height: 48,
        borderRadius: 16,
        backgroundColor: "#eff6ff",
        alignItems: "center",
        justifyContent: "center",
        marginRight: 12,
    },
    userAvatarText: {
        fontSize: 20,
        fontWeight: "900",
        color: "#2563eb",
    },
    userInfo: {
        flex: 1,
    },
    userName: {
        fontSize: 16,
        fontWeight: "900",
        color: "#0f172a",
    },
    userRole: {
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
