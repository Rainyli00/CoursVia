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
import { OGRENCI_MENU_ITEMS } from "@/app/_shared/panelMenus";
import { api } from "@/src/api/client";
import type {
    MobileOgrenciIslemResponse,
    MobileOgrenciKategoriSecenek,
    MobileOgrenciKesfetKursItem,
    MobileOgrenciKesfetResponse,
} from "@/src/types/ogrenci";

type KesfetSiralamaValue =
    | "guncel"
    | "puan-yuksek"
    | "populer"
    | "degerlendirme-cok"
    | "ad-az"
    | "ad-za";

// API'nin desteklediği keşfet sıralama değerleri kullanıcı etiketleriyle tutulur.
const SIRALAMA_SECENEKLERI: { label: string; value: KesfetSiralamaValue }[] = [
    { label: "Güncel", value: "guncel" },
    { label: "En İyi Puan", value: "puan-yuksek" },
    { label: "Popüler", value: "populer" },
    { label: "En Çok Değerlendirilen", value: "degerlendirme-cok" },
    { label: "A-Z", value: "ad-az" },
    { label: "Z-A", value: "ad-za" },
];

// Öğrenci keşfet ekranı.
// Yayındaki kursları listeler.
// Öğrencinin kayıtlı olduğu kurslar da görünür, kartta "Kayıtlısın" yazılır.
// Eğitmenin kendi kursunda "Kendi Kursun" yazılır.
// Arama, kategori filtreleme, sıralama, sayfalama, detay ve kayıt ol işlemleri bulunur.
export default function OgrenciKesfetScreen() {
    // Yayındaki kurs listesi ve kategori filtre seçenekleri.
    const [kurslar, setKurslar] = useState<MobileOgrenciKesfetKursItem[]>([]);
    const [kategoriler, setKategoriler] = useState<MobileOgrenciKategoriSecenek[]>(
        []
    );

    // aramaInput ekrandaki yazı, arama ise API'ye uygulanmış aktif filtredir.
    const [aramaInput, setAramaInput] = useState("");
    const [arama, setArama] = useState<string | null>(null);

    // Aktif kategori ve sıralama filtreleri.
    const [kategoriId, setKategoriId] = useState<number | null>(null);
    const [sirala, setSirala] = useState<KesfetSiralamaValue>("guncel");

    // Backend sayfalama bilgileri.
    const [sayfa, setSayfa] = useState(1);
    const [sayfaBasinaKayit] = useState(10);

    const [toplamKayit, setToplamKayit] = useState(0);
    const [toplamSayfa, setToplamSayfa] = useState(1);

    const [yukleniyor, setYukleniyor] = useState(true);
    const [yenileniyor, setYenileniyor] = useState(false);
    const [hata, setHata] = useState<string | null>(null);

    // Kayıt olma sırasında sadece ilgili kursun butonu loading olur.
    const [kayitOlunanKursId, setKayitOlunanKursId] = useState<number | null>(
        null
    );

    // Ekran ilk açıldığında varsayılan keşfet listesi çekilir.
    useEffect(() => {
        kesfetKurslariniGetir(false, {
            arama: null,
            kategoriId: null,
            sirala: "guncel",
            sayfa: 1,
        });
    }, []);

    // Keşfet listesini API'den çeker; override ile filtreler state'e yazılmadan kullanılabilir.
    async function kesfetKurslariniGetir(
        refreshMi = false,
        override?: {
            arama?: string | null;
            kategoriId?: number | null;
            sirala?: KesfetSiralamaValue;
            sayfa?: number;
        }
    ) {
        const aktifArama =
            override && "arama" in override ? override.arama ?? null : arama;

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

            const response = await api.get<MobileOgrenciKesfetResponse>(
                "/api/mobile/ogrenci/kesfet",
                {
                    params: {
                        arama: aktifArama || undefined,
                        kategoriId: aktifKategoriId || undefined,
                        sirala: aktifSirala,
                        sayfa: aktifSayfa,
                        sayfaBasinaKayit,
                    },
                }
            );

            setKurslar(response.data.kurslar ?? []);
            setKategoriler(response.data.kategoriler ?? []);
            setToplamKayit(response.data.toplamKayit ?? 0);
            setToplamSayfa(response.data.toplamSayfa ?? 1);
            setSayfa(response.data.sayfa ?? aktifSayfa);
            setArama(response.data.arama ?? aktifArama);
            setKategoriId(response.data.kategoriId ?? aktifKategoriId);
            setSirala((response.data.sirala as KesfetSiralamaValue) ?? aktifSirala);
            setAramaInput(response.data.arama ?? aktifArama ?? "");
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Keşfet kursları alınırken hata oluştu.";

            setHata(mesaj);
            Alert.alert("Hata", mesaj);
        } finally {
            setYukleniyor(false);
            setYenileniyor(false);
        }
    }

    // Arama yeni bir liste anlamına geldiği için sayfa bire döner.
    function aramaUygula() {
        const temizArama = aramaInput.trim() || null;

        Keyboard.dismiss();

        setArama(temizArama);
        setSayfa(1);

        kesfetKurslariniGetir(false, {
            arama: temizArama,
            kategoriId,
            sirala,
            sayfa: 1,
        });
    }

    // Tüm filtreleri varsayılana çekip ilk sayfayı yeniden yükler.
    function filtreleriTemizle() {
        Keyboard.dismiss();

        setAramaInput("");
        setArama(null);
        setKategoriId(null);
        setSirala("guncel");
        setSayfa(1);

        kesfetKurslariniGetir(false, {
            arama: null,
            kategoriId: null,
            sirala: "guncel",
            sayfa: 1,
        });
    }

    // Kategori değişince aktif arama korunur ve ilk sayfa yüklenir.
    function kategoriSec(yeniKategoriId: number | null) {
        Keyboard.dismiss();

        const aktifArama = aramaInput.trim() || null;

        setArama(aktifArama);
        setKategoriId(yeniKategoriId);
        setSayfa(1);

        kesfetKurslariniGetir(false, {
            arama: aktifArama,
            kategoriId: yeniKategoriId,
            sirala,
            sayfa: 1,
        });
    }

    // Sıralama değişince mevcut arama/kategori korunarak liste baştan çekilir.
    function siralamaSec(yeniSirala: KesfetSiralamaValue) {
        Keyboard.dismiss();

        const aktifArama = aramaInput.trim() || null;

        setArama(aktifArama);
        setSirala(yeniSirala);
        setSayfa(1);

        kesfetKurslariniGetir(false, {
            arama: aktifArama,
            kategoriId,
            sirala: yeniSirala,
            sayfa: 1,
        });
    }

    // Geçersiz veya mevcut sayfaya tekrar istek atılmaz.
    function sayfaDegistir(yeniSayfa: number) {
        if (yeniSayfa < 1 || yeniSayfa > toplamSayfa || yeniSayfa === sayfa) {
            return;
        }

        setSayfa(yeniSayfa);

        kesfetKurslariniGetir(false, {
            arama,
            kategoriId,
            sirala,
            sayfa: yeniSayfa,
        });
    }

    // Keşfet kartından detay ekranına geçer.
    function detayAc(kursId: number) {
        router.push(`/ogrenci/kesfet-detay/${kursId}` as any);
    }

    // Kayıt olmadan önce kursun uygunluk durumları tek tek kontrol edilir.
    function kayitOlOnayiAl(kurs: MobileOgrenciKesfetKursItem) {
        if (kurs.guncelleniyorMu) {
            Alert.alert("Bilgi", "Bu kurs şu anda güncelleniyor.");
            return;
        }

        if (kurs.kayitliMi) {
            Alert.alert("Bilgi", "Bu kursa zaten kayıtlısın.");
            return;
        }

        if (kurs.kendiKursuMu) {
            Alert.alert("Bilgi", "Kendi kursuna öğrenci olarak kayıt olamazsın.");
            return;
        }

        if (!kurs.kayitOlabilirMi) {
            Alert.alert("Bilgi", "Bu kursa şu anda kayıt olunamaz.");
            return;
        }

        Alert.alert(
            "Kursa Kayıt Ol",
            `"${kurs.kursAdi}" kursuna kayıt olmak istiyor musun?`,
            [
                {
                    text: "Vazgeç",
                    style: "cancel",
                },
                {
                    text: "Kayıt Ol",
                    onPress: () => kayitOl(kurs.kursId),
                },
            ]
        );
    }

    // Kayıt başarılı olursa kullanıcıya kurslarım sayfasına gitme seçeneği sunulur.
    async function kayitOl(kursId: number) {
        try {
            setKayitOlunanKursId(kursId);

            const response = await api.post<MobileOgrenciIslemResponse>(
                `/api/mobile/ogrenci/kesfet/${kursId}/kayit-ol`
            );

            Alert.alert(
                "Başarılı",
                response.data.mesaj || "Kursa başarıyla kayıt olundu.",
                [
                    {
                        text: "Kurslarıma Git",
                        onPress: () => router.push("/ogrenci/kurslarim" as any),
                    },
                    {
                        text: "Keşfette Kal",
                        style: "cancel",
                    },
                ]
            );

            await kesfetKurslariniGetir(true);
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Kursa kayıt olunurken hata oluştu.";

            Alert.alert("Hata", mesaj);
        } finally {
            setKayitOlunanKursId(null);
        }
    }

    if (yukleniyor) {
        return <LoadingState />;
    }

    if (hata) {
        return (
            <ErrorState
                mesaj={hata}
                tekrarDene={() => kesfetKurslariniGetir()}
            />
        );
    }

    return (
        <PanelLayout
            title="Keşfet"
            subtitle="Yayındaki kursları inceleyebilir, detaylarına bakabilir ve kayıt olabilirsin."
            refreshing={yenileniyor}
            onRefresh={() => kesfetKurslariniGetir(true)}
            menuItems={OGRENCI_MENU_ITEMS}
            activeMenuKey="kesfet"
        >
            <SummaryCard toplamKayit={toplamKayit} />

            <FilterPanel
                aramaInput={aramaInput}
                setAramaInput={setAramaInput}
                aramaUygula={aramaUygula}
                filtreleriTemizle={filtreleriTemizle}
                kategoriler={kategoriler}
                kategoriId={kategoriId}
                kategoriSec={kategoriSec}
                sirala={sirala}
                siralamaSec={siralamaSec}
            />

            <View style={styles.listHeader}>
                <View>
                    <Text style={styles.listTitle}>Yayındaki Kurslar</Text>
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
                        <KesfetKursKart
                            key={kurs.kursId}
                            kurs={kurs}
                            detayAc={detayAc}
                            kayitOlOnayiAl={kayitOlOnayiAl}
                            kayitOlunuyor={kayitOlunanKursId === kurs.kursId}
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

// İlk liste yüklenirken gösterilen tam ekran loading.
function LoadingState() {
    return (
        <View style={styles.centerContainer}>
            <ActivityIndicator size="large" color="#2563eb" />
            <Text style={styles.loadingText}>Keşfet kursları yükleniyor...</Text>
        </View>
    );
}

// Keşfet listesi alınamazsa tekrar deneme butonlu hata ekranı.
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

// Keşfet ekranındaki toplam yayın kursu özet kartı.
function SummaryCard({ toplamKayit }: { toplamKayit: number }) {
    return (
        <View style={styles.summaryCard}>
            <Text style={styles.summaryValue}>{toplamKayit}</Text>
            <Text style={styles.summaryTitle}>Yayındaki Kurs</Text>
        </View>
    );
}

// Arama, kategori ve sıralama kontrollerini tek panelde toplar.
function FilterPanel({
    aramaInput,
    setAramaInput,
    aramaUygula,
    filtreleriTemizle,
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
    kategoriler: MobileOgrenciKategoriSecenek[];
    kategoriId: number | null;
    kategoriSec: (kategoriId: number | null) => void;
    sirala: KesfetSiralamaValue;
    siralamaSec: (sirala: KesfetSiralamaValue) => void;
}) {
    return (
        <View style={styles.filterCard}>
            <View style={styles.filterHeader}>
                <Text style={styles.filterTitle}>Filtrele</Text>
                <Text style={styles.filterSubtitle}>Kursları hızlıca bul</Text>
            </View>

            <View style={styles.formGroup}>
                <Text style={styles.inputLabel}>Arama</Text>

                <TextInput
                    value={aramaInput}
                    onChangeText={setAramaInput}
                    placeholder="Kurs, eğitmen veya açıklama ara..."
                    style={styles.searchInput}
                    returnKeyType="search"
                    onSubmitEditing={aramaUygula}
                />
            </View>

            <View style={styles.formGroup}>
                <Text style={styles.inputLabel}>Kategori</Text>

                <CategoryDropdown
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

// Kategori seçeneklerini modal dropdown olarak gösterir.
function CategoryDropdown({
    kategoriler,
    kategoriId,
    kategoriSec,
}: {
    kategoriler: MobileOgrenciKategoriSecenek[];
    kategoriId: number | null;
    kategoriSec: (kategoriId: number | null) => void;
}) {
    const [modalAcik, setModalAcik] = useState(false);

    const seciliKategori = kategoriler.find((x) => x.kategoriId === kategoriId);

    const seciliMetin = seciliKategori
        ? seciliKategori.kategoriAdi
        : "Tüm Kategoriler";

    // null seçimi tüm kategoriler filtresine döner.
    function sec(id: number | null) {
        setModalAcik(false);
        kategoriSec(id);
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
                        key={kategori.kategoriId}
                        label={kategori.kategoriAdi}
                        active={kategoriId === kategori.kategoriId}
                        onPress={() => sec(kategori.kategoriId)}
                    />
                ))}
            </SelectionModal>
        </>
    );
}

// Keşfet sıralama seçeneklerini modal dropdown olarak gösterir.
function SiralamaDropdown({
    sirala,
    siralamaSec,
}: {
    sirala: KesfetSiralamaValue;
    siralamaSec: (sirala: KesfetSiralamaValue) => void;
}) {
    const [modalAcik, setModalAcik] = useState(false);

    const seciliSiralama =
        SIRALAMA_SECENEKLERI.find((x) => x.value === sirala) ??
        SIRALAMA_SECENEKLERI[0];

    // Seçilen value doğrudan API'nin beklediği sirala parametresidir.
    function sec(value: KesfetSiralamaValue) {
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

// Keşfet kurs kartı; detay ve kayıt ol aksiyonlarını birlikte sunar.
function KesfetKursKart({
    kurs,
    detayAc,
    kayitOlOnayiAl,
    kayitOlunuyor,
}: {
    kurs: MobileOgrenciKesfetKursItem;
    detayAc: (kursId: number) => void;
    kayitOlOnayiAl: (kurs: MobileOgrenciKesfetKursItem) => void;
    kayitOlunuyor: boolean;
}) {
    return (
        <View style={styles.courseCard}>
            <Pressable
                onPress={() => detayAc(kurs.kursId)}
                style={({ pressed }) => [
                    styles.courseTapArea,
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
                        <Text style={styles.courseTitle} numberOfLines={1}>
                            {kurs.kursAdi}
                        </Text>

                        <Text style={styles.courseInstructor} numberOfLines={1}>
                            {kurs.egitmenAdSoyad}
                        </Text>
                    </View>
                </View>

                {kurs.aciklama ? (
                    <Text style={styles.courseDescription} numberOfLines={2}>
                        {kurs.aciklama}
                    </Text>
                ) : null}

                {kurs.kategoriler.length > 0 ? (
                    <View style={styles.categoryRow}>
                        {kurs.kategoriler.slice(0, 3).map((kategori) => (
                            <View key={kategori} style={styles.categoryChip}>
                                <Text style={styles.categoryChipText} numberOfLines={1}>
                                    {kategori}
                                </Text>
                            </View>
                        ))}
                    </View>
                ) : null}

                <View style={styles.metaGrid}>
                    <InfoPill label="Ders" value={String(kurs.toplamDersSayisi)} />
                    <InfoPill
                        label="Öğrenci"
                        value={String(kurs.kayitliOgrenciSayisi)}
                    />
                    <InfoPill
                        label="Puan"
                        value={
                            kurs.degerlendirmeSayisi > 0
                                ? `${kurs.ortalamaPuan}/5`
                                : "-"
                        }
                    />
                </View>
            </Pressable>

            <View style={styles.actionRow}>
                <Pressable
                    onPress={() => detayAc(kurs.kursId)}
                    style={({ pressed }) => [
                        styles.secondaryActionButton,
                        pressed ? styles.buttonPressed : null,
                    ]}
                >
                    <Text style={styles.secondaryActionText}>Detay</Text>
                </Pressable>

                {kurs.kayitliMi ? (
                    <View style={styles.enrolledBadge}>
                        <Text style={styles.enrolledBadgeText}>Kayıtlısın</Text>
                    </View>
                ) : kurs.kendiKursuMu ? (
                    <View style={styles.ownCourseBadge}>
                        <Text style={styles.ownCourseBadgeText}>Kendi Kursun</Text>
                    </View>
                ) : kurs.guncelleniyorMu ? (
                    <View style={styles.closedBadge}>
                        <Text style={styles.closedBadgeText}>Güncelleniyor</Text>
                    </View>
                ) : !kurs.kayitOlabilirMi ? (
                    <View style={styles.closedBadge}>
                        <Text style={styles.closedBadgeText}>Kayıt Kapalı</Text>
                    </View>
                ) : (
                    <Pressable
                        disabled={kayitOlunuyor}
                        onPress={() => kayitOlOnayiAl(kurs)}
                        style={({ pressed }) => [
                            styles.primaryActionButton,
                            pressed && !kayitOlunuyor ? styles.buttonPressed : null,
                            kayitOlunuyor ? styles.actionButtonDisabled : null,
                        ]}
                    >
                        {kayitOlunuyor ? (
                            <ActivityIndicator size="small" color="#ffffff" />
                        ) : (
                            <Text style={styles.primaryActionText}>Kayıt Ol</Text>
                        )}
                    </Pressable>
                )}
            </View>
        </View>
    );
}

// Kurs kartındaki küçük metrik kutusu.
function InfoPill({ label, value }: { label: string; value: string }) {
    return (
        <View style={styles.infoPill}>
            <Text style={styles.infoValue}>{value}</Text>
            <Text style={styles.infoLabel}>{label}</Text>
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

// Arama veya filtrelere uygun keşfet kursu yoksa gösterilir.
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
    courseList: {
        gap: 12,
    },
    courseCard: {
        backgroundColor: "#ffffff",
        borderRadius: 20,
        padding: 16,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    courseTapArea: {
        borderRadius: 16,
    },
    courseTop: {
        flexDirection: "row",
        alignItems: "center",
    },
    courseAvatar: {
        width: 48,
        height: 48,
        borderRadius: 16,
        backgroundColor: "#eff6ff",
        alignItems: "center",
        justifyContent: "center",
        marginRight: 12,
    },
    courseAvatarText: {
        fontSize: 20,
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
    },
    courseInstructor: {
        marginTop: 3,
        fontSize: 13,
        color: "#64748b",
    },
    courseDescription: {
        marginTop: 12,
        fontSize: 13,
        lineHeight: 19,
        color: "#475569",
    },
    categoryRow: {
        marginTop: 12,
        flexDirection: "row",
        flexWrap: "wrap",
        gap: 7,
    },
    categoryChip: {
        backgroundColor: "#eff6ff",
        borderWidth: 1,
        borderColor: "#bfdbfe",
        borderRadius: 999,
        paddingHorizontal: 10,
        paddingVertical: 6,
    },
    categoryChipText: {
        fontSize: 11,
        fontWeight: "900",
        color: "#2563eb",
    },
    metaGrid: {
        marginTop: 13,
        flexDirection: "row",
        gap: 8,
    },
    infoPill: {
        flex: 1,
        backgroundColor: "#f8fafc",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        borderRadius: 14,
        padding: 10,
    },
    infoValue: {
        fontSize: 15,
        fontWeight: "900",
        color: "#0f172a",
    },
    infoLabel: {
        marginTop: 2,
        fontSize: 11,
        fontWeight: "800",
        color: "#64748b",
    },
    actionRow: {
        marginTop: 14,
        flexDirection: "row",
        gap: 10,
    },
    secondaryActionButton: {
        flex: 1,
        backgroundColor: "#eff6ff",
        borderWidth: 1,
        borderColor: "#bfdbfe",
        borderRadius: 14,
        paddingVertical: 11,
        alignItems: "center",
    },
    secondaryActionText: {
        fontSize: 13,
        fontWeight: "900",
        color: "#2563eb",
    },
    primaryActionButton: {
        flex: 1,
        backgroundColor: "#2563eb",
        borderRadius: 14,
        paddingVertical: 11,
        alignItems: "center",
    },
    primaryActionText: {
        fontSize: 13,
        fontWeight: "900",
        color: "#ffffff",
    },
    enrolledBadge: {
        flex: 1,
        backgroundColor: "#dcfce7",
        borderWidth: 1,
        borderColor: "#bbf7d0",
        borderRadius: 14,
        paddingVertical: 11,
        alignItems: "center",
    },
    enrolledBadgeText: {
        fontSize: 13,
        fontWeight: "900",
        color: "#16a34a",
    },
    ownCourseBadge: {
        flex: 1,
        backgroundColor: "#fef3c7",
        borderWidth: 1,
        borderColor: "#fde68a",
        borderRadius: 14,
        paddingVertical: 11,
        alignItems: "center",
    },
    ownCourseBadgeText: {
        fontSize: 13,
        fontWeight: "900",
        color: "#b45309",
    },
    closedBadge: {
        flex: 1,
        backgroundColor: "#f8fafc",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        borderRadius: 14,
        paddingVertical: 11,
        alignItems: "center",
    },
    closedBadgeText: {
        fontSize: 13,
        fontWeight: "900",
        color: "#64748b",
    },
    actionButtonDisabled: {
        opacity: 0.6,
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
