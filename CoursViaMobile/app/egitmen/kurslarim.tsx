import { router } from "expo-router";
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
    MobileEgitmenIslemResponse,
    MobileEgitmenKategoriSecenek,
    MobileEgitmenKursItem,
    MobileEgitmenKurslarimResponse,
} from "@/src/types/egitmen";

// Eğitmen kurslarım ekranı.
// Arama, durum filtresi, kategori filtresi, sıralama, sayfalama ve kursu taslağa alma işlemi destekler.
export default function EgitmenKurslarimScreen() {
    const [kurslar, setKurslar] = useState<MobileEgitmenKursItem[]>([]);
    const [kategoriler, setKategoriler] = useState<MobileEgitmenKategoriSecenek[]>([]);

    const [aramaInput, setAramaInput] = useState("");
    const [arama, setArama] = useState<string | null>(null);

    const [durumId, setDurumId] = useState<number | null>(null);
    const [kategoriId, setKategoriId] = useState<number | null>(null);
    const [sirala, setSirala] = useState("guncel");

    const [sayfa, setSayfa] = useState(1);
    const [sayfaBasinaKayit] = useState(10);

    const [toplamKayit, setToplamKayit] = useState(0);
    const [toplamSayfa, setToplamSayfa] = useState(1);

    const [yukleniyor, setYukleniyor] = useState(true);
    const [yenileniyor, setYenileniyor] = useState(false);
    const [hata, setHata] = useState<string | null>(null);

    const [taslagaAlinanKursId, setTaslagaAlinanKursId] = useState<number | null>(
        null
    );

    useEffect(() => {
        kurslariGetir();
    }, []);

    const ozet = useMemo(() => {
        const yayindaki = kurslar.filter((x) => x.durumId === 5).length;
        const bekleyen = kurslar.filter((x) => x.durumId === 4).length;
        const taslak = kurslar.filter((x) => x.durumId === 3).length;

        return {
            toplam: toplamKayit,
            yayindaki,
            bekleyen,
            taslak,
        };
    }, [kurslar, toplamKayit]);

    async function kurslariGetir(
        refreshMi = false,
        override?: {
            arama?: string | null;
            durumId?: number | null;
            kategoriId?: number | null;
            sirala?: string;
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

            const response = await api.get<MobileEgitmenKurslarimResponse>(
                "/api/mobile/egitmen/kurslarim",
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
            setKategoriler(response.data.kategoriler ?? []);

            setToplamKayit(response.data.toplamKayit ?? 0);
            setToplamSayfa(response.data.toplamSayfa ?? 1);
            setSayfa(response.data.sayfa ?? aktifSayfa);

            setArama(response.data.arama ?? aktifArama);
            setDurumId(response.data.durumId ?? aktifDurumId);
            setKategoriId(response.data.kategoriId ?? aktifKategoriId);
            setSirala(response.data.sirala ?? aktifSirala);
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

    function siralamaSec(yeniSirala: string) {
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

    function kursDetayAc(kursId: number) {
        router.push(`/egitmen/kurs-detay/${kursId}` as any);
    }

    function taslagaAlOnayiAl(kurs: MobileEgitmenKursItem) {
        if (kurs.durumId === 3) {
            Alert.alert("Bilgi", "Bu kurs zaten taslak durumunda.");
            return;
        }

        Alert.alert(
            "Kurs Taslağa Alınsın mı?",
            `"${kurs.kursAdi}" taslak durumuna alınacak. Düzenleme yaptıktan sonra tekrar onaya/yayına göndermen gerekebilir.`,
            [
                {
                    text: "Vazgeç",
                    style: "cancel",
                },
                {
                    text: "Taslağa Al",
                    style: "destructive",
                    onPress: () => taslagaAl(kurs.kursId),
                },
            ]
        );
    }

    async function taslagaAl(kursId: number) {
        try {
            setTaslagaAlinanKursId(kursId);

            const response = await api.post<MobileEgitmenIslemResponse>(
                `/api/mobile/egitmen/kurslarim/${kursId}/taslaga-al`
            );

            Alert.alert("Başarılı", response.data.mesaj || "Kurs taslağa alındı.");

            await kurslariGetir(true, {
                arama,
                durumId,
                kategoriId,
                sirala,
                sayfa,
            });
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Kurs taslağa alınırken hata oluştu.";

            Alert.alert("Hata", mesaj);
        } finally {
            setTaslagaAlinanKursId(null);
        }
    }

    if (yukleniyor) {
        return <LoadingState />;
    }

    if (hata) {
        return <ErrorState mesaj={hata} tekrarDene={() => kurslariGetir()} />;
    }

    return (
        <PanelLayout
            title="Kurslarım"
            subtitle="Kurslarının yayın, taslak ve onay durumlarını buradan takip edebilirsin."
            refreshing={yenileniyor}
            onRefresh={() => kurslariGetir(true)}
            menuItems={EGITMEN_MENU_ITEMS}
            activeMenuKey="kurslarim"
        >
            <View style={styles.summaryGrid}>
                <SummaryCard title="Toplam" value={ozet.toplam} />
                <SummaryCard title="Yayında" value={ozet.yayindaki} />
                <SummaryCard title="Bekleyen" value={ozet.bekleyen} />
                <SummaryCard title="Taslak" value={ozet.taslak} />
            </View>

            <FilterPanel
                aramaInput={aramaInput}
                setAramaInput={setAramaInput}
                aramaUygula={aramaUygula}
                filtreleriTemizle={filtreleriTemizle}
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
                        <KursKart
                            key={kurs.kursId}
                            kurs={kurs}
                            kursDetayAc={kursDetayAc}
                            taslagaAlOnayiAl={taslagaAlOnayiAl}
                            taslagaAliniyor={taslagaAlinanKursId === kurs.kursId}
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
            <Text style={styles.loadingText}>Kursların yükleniyor...</Text>
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

function FilterPanel({
    aramaInput,
    setAramaInput,
    aramaUygula,
    filtreleriTemizle,
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
    durumId: number | null;
    durumSec: (durumId: number | null) => void;
    kategoriler: MobileEgitmenKategoriSecenek[];
    kategoriId: number | null;
    kategoriSec: (kategoriId: number | null) => void;
    sirala: string;
    siralamaSec: (sirala: string) => void;
}) {
    return (
        <View style={styles.filterCard}>
            <View style={styles.filterHeader}>
                <Text style={styles.filterTitle}>Filtrele</Text>
                <Text style={styles.filterSubtitle}>
                    Kurslarını hızlıca bul ve sırala
                </Text>
            </View>

            <View style={styles.formGroup}>
                <Text style={styles.inputLabel}>Arama</Text>

                <TextInput
                    value={aramaInput}
                    onChangeText={setAramaInput}
                    placeholder="Kurs adı, durum veya kategori ara..."
                    style={styles.searchInput}
                    returnKeyType="search"
                    onSubmitEditing={aramaUygula}
                />
            </View>

            <View style={styles.formGroup}>
                <Text style={styles.inputLabel}>Durum</Text>

                <DurumDropdown durumId={durumId} durumSec={durumSec} />
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
    durumId,
    durumSec,
}: {
    durumId: number | null;
    durumSec: (durumId: number | null) => void;
}) {
    const [modalAcik, setModalAcik] = useState(false);

    const durumlar = [
        { label: "Tüm Durumlar", value: null },
        { label: "Pasif", value: 2 },
        { label: "Taslak", value: 3 },
        { label: "Onay Bekliyor", value: 4 },
        { label: "Yayında", value: 5 },
        { label: "Reddedildi", value: 6 },
        { label: "Düzeltme İsteniyor", value: 7 },
    ];

    const seciliDurum = durumlar.find((x) => x.value === durumId);
    const seciliMetin = seciliDurum?.label ?? "Tüm Durumlar";

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
                {durumlar.map((item) => (
                    <DropdownItem
                        key={item.label}
                        label={item.label}
                        active={item.value === durumId}
                        onPress={() => sec(item.value)}
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
    kategoriler: MobileEgitmenKategoriSecenek[];
    kategoriId: number | null;
    kategoriSec: (kategoriId: number | null) => void;
}) {
    const [modalAcik, setModalAcik] = useState(false);

    const seciliKategori = kategoriler.find((x) => x.kategoriId === kategoriId);
    const seciliMetin = seciliKategori
        ? seciliKategori.kategoriAdi
        : "Tüm Kategoriler";

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
                        key={kategori.kategoriId}
                        label={kategori.kategoriAdi}
                        active={kategori.kategoriId === kategoriId}
                        onPress={() => sec(kategori.kategoriId)}
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
    sirala: string;
    siralamaSec: (sirala: string) => void;
}) {
    const [modalAcik, setModalAcik] = useState(false);

    const siralamalar = [
        { label: "Güncel", value: "guncel" },
        { label: "Eski", value: "eski" },
        { label: "A-Z", value: "ad-az" },
        { label: "Z-A", value: "ad-za" },

        { label: "Puan: Yüksekten Düşüğe", value: "puan-yuksek" },
        { label: "Puan: Düşükten Yükseğe", value: "puan-dusuk" },

        { label: "Öğrenci: Çoktan Aza", value: "ogrenci-cok" },
        { label: "Öğrenci: Azdan Çoğa", value: "ogrenci-az" },
    ];

    const seciliSiralama = siralamalar.find((x) => x.value === sirala);
    const seciliMetin = seciliSiralama?.label ?? "Güncel";

    function sec(value: string) {
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
                <Text style={styles.dropdownTextActive} numberOfLines={1}>
                    {seciliMetin}
                </Text>

                <Text style={styles.dropdownArrow}>⌄</Text>
            </Pressable>

            <SelectionModal
                visible={modalAcik}
                title="Sıralama Seç"
                close={() => setModalAcik(false)}
            >
                {siralamalar.map((item) => (
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

function KursKart({
    kurs,
    kursDetayAc,
    taslagaAlOnayiAl,
    taslagaAliniyor,
}: {
    kurs: MobileEgitmenKursItem;
    kursDetayAc: (kursId: number) => void;
    taslagaAlOnayiAl: (kurs: MobileEgitmenKursItem) => void;
    taslagaAliniyor: boolean;
}) {
    const taslakMi = kurs.durumId === 3;

    return (
        <View style={styles.courseCard}>
            <Pressable
                onPress={() => kursDetayAc(kurs.kursId)}
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

                        <Text style={styles.courseSubText} numberOfLines={1}>
                            {kurs.kategoriler && kurs.kategoriler.length > 0
                                ? kurs.kategoriler.join(", ")
                                : "Kategori yok"}
                        </Text>
                    </View>

                    <View style={styles.statusBadge}>
                        <Text style={styles.statusBadgeText}>{kurs.durumAdi}</Text>
                    </View>
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

                <View style={styles.metaGrid}>
                    <InfoPill label="Öğrenci" value={String(kurs.ogrenciSayisi)} />
                    <InfoPill
                        label="Tamamlayan"
                        value={String(kurs.tamamlayanOgrenciSayisi)}
                    />
                    <InfoPill label="Ders" value={String(kurs.dersSayisi)} />
                </View>

                <View style={styles.bottomRow}>
                    <Text style={styles.ratingText}>
                        Puan:{" "}
                        {kurs.degerlendirmeSayisi > 0
                            ? `${kurs.ortalamaPuan}/5 (${kurs.degerlendirmeSayisi})`
                            : "-"}
                    </Text>
                </View>
            </Pressable>

            <View style={styles.cardActionRow}>
                <Pressable
                    onPress={() => kursDetayAc(kurs.kursId)}
                    style={({ pressed }) => [
                        styles.detailButton,
                        pressed ? styles.buttonPressed : null,
                    ]}
                >
                    <Text style={styles.detailButtonText}>Detay</Text>
                </Pressable>

                {taslakMi ? (
                    <View style={styles.draftPassiveButton}>
                        <Text style={styles.draftPassiveButtonText}>Taslakta</Text>
                    </View>
                ) : (
                    <Pressable
                        disabled={taslagaAliniyor}
                        onPress={() => taslagaAlOnayiAl(kurs)}
                        style={({ pressed }) => [
                            styles.draftButton,
                            pressed && !taslagaAliniyor ? styles.buttonPressed : null,
                            taslagaAliniyor ? styles.disabledButton : null,
                        ]}
                    >
                        {taslagaAliniyor ? (
                            <ActivityIndicator size="small" color="#b45309" />
                        ) : (
                            <Text style={styles.draftButtonText}>Taslağa Al</Text>
                        )}
                    </Pressable>
                )}
            </View>
        </View>
    );
}

function InfoPill({ label, value }: { label: string; value: string }) {
    return (
        <View style={styles.infoPill}>
            <Text style={styles.infoValue}>{value}</Text>
            <Text style={styles.infoLabel}>{label}</Text>
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
        flex: 1,
        fontSize: 14,
        fontWeight: "800",
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
    disabledButton: {
        opacity: 0.55,
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
    courseSubText: {
        marginTop: 3,
        fontSize: 13,
        color: "#64748b",
    },
    statusBadge: {
        marginLeft: 10,
        backgroundColor: "#eff6ff",
        borderWidth: 1,
        borderColor: "#bfdbfe",
        borderRadius: 999,
        paddingHorizontal: 10,
        paddingVertical: 6,
    },
    statusBadgeText: {
        fontSize: 11,
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
    metaGrid: {
        marginTop: 14,
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
    bottomRow: {
        marginTop: 12,
        flexDirection: "row",
        alignItems: "center",
        justifyContent: "space-between",
    },
    ratingText: {
        fontSize: 12,
        fontWeight: "800",
        color: "#64748b",
    },
    cardActionRow: {
        marginTop: 14,
        flexDirection: "row",
        gap: 10,
    },
    detailButton: {
        flex: 1,
        backgroundColor: "#eff6ff",
        borderWidth: 1,
        borderColor: "#bfdbfe",
        borderRadius: 14,
        paddingVertical: 11,
        alignItems: "center",
    },
    detailButtonText: {
        fontSize: 13,
        fontWeight: "900",
        color: "#2563eb",
    },
    draftButton: {
        flex: 1,
        backgroundColor: "#fffbeb",
        borderWidth: 1,
        borderColor: "#fde68a",
        borderRadius: 14,
        paddingVertical: 11,
        alignItems: "center",
    },
    draftButtonText: {
        fontSize: 13,
        fontWeight: "900",
        color: "#b45309",
    },
    draftPassiveButton: {
        flex: 1,
        backgroundColor: "#f8fafc",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        borderRadius: 14,
        paddingVertical: 11,
        alignItems: "center",
    },
    draftPassiveButtonText: {
        fontSize: 13,
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