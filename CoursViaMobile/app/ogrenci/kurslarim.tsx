import { router } from "expo-router";
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
import { OGRENCI_MENU_ITEMS } from "@/app/_shared/panelMenus";
import { api } from "@/src/api/client";
import type {
    MobileOgrenciIslemResponse,
    MobileOgrenciKategoriSecenek,
    MobileOgrenciKursItem,
    MobileOgrenciKurslarimResponse,
} from "@/src/types/ogrenci";

// Öğrencinin kayıtlı olduğu tüm kursları gösteren ekran.
// Bu ekranda arama, kategori dropdown, sayfalama, detay, yorum/puan ve kayıt iptal işlemleri bulunur.
export default function OgrenciKurslarimScreen() {
    const [kurslar, setKurslar] = useState<MobileOgrenciKursItem[]>([]);
    const [kategoriler, setKategoriler] = useState<MobileOgrenciKategoriSecenek[]>(
        []
    );

    const [aramaInput, setAramaInput] = useState("");
    const [arama, setArama] = useState<string | null>(null);
    const [kategoriId, setKategoriId] = useState<number | null>(null);

    const [sayfa, setSayfa] = useState(1);
    const [sayfaBasinaKayit] = useState(10);
    const [toplamKayit, setToplamKayit] = useState(0);
    const [toplamSayfa, setToplamSayfa] = useState(1);

    const [yukleniyor, setYukleniyor] = useState(true);
    const [yenileniyor, setYenileniyor] = useState(false);
    const [hata, setHata] = useState<string | null>(null);

    const [yorumModalAcik, setYorumModalAcik] = useState(false);
    const [seciliKurs, setSeciliKurs] = useState<MobileOgrenciKursItem | null>(
        null
    );

    const [puan, setPuan] = useState(5);
    const [yorumMetni, setYorumMetni] = useState("");
    const [yorumGonderiliyor, setYorumGonderiliyor] = useState(false);

    const [iptalEdilenKursKayitId, setIptalEdilenKursKayitId] = useState<
        number | null
    >(null);

    useEffect(() => {
        kurslariGetir();
    }, []);

    const ozet = useMemo(() => {
        const tamamlananKursSayisi = kurslar.filter(
            (x) => x.kursTamamlandiMi
        ).length;

        const devamEdenKursSayisi = kurslar.filter(
            (x) => !x.kursTamamlandiMi
        ).length;

        const ortalamaIlerlemeYuzdesi =
            kurslar.length === 0
                ? 0
                : Math.round(
                    kurslar.reduce(
                        (toplam, kurs) => toplam + kurs.ilerlemeYuzdesi,
                        0
                    ) / kurslar.length
                );

        return {
            toplamKursSayisi: toplamKayit,
            devamEdenKursSayisi,
            tamamlananKursSayisi,
            ortalamaIlerlemeYuzdesi,
        };
    }, [kurslar, toplamKayit]);

    async function kurslariGetir(
        refreshMi = false,
        override?: {
            arama?: string | null;
            kategoriId?: number | null;
            sayfa?: number;
        }
    ) {
        const aktifArama =
            override && "arama" in override ? override.arama ?? null : arama;

        const aktifKategoriId =
            override && "kategoriId" in override
                ? override.kategoriId ?? null
                : kategoriId;

        const aktifSayfa = override?.sayfa ?? sayfa;

        try {
            if (refreshMi) {
                setYenileniyor(true);
            } else {
                setYukleniyor(true);
            }

            setHata(null);

            const response = await api.get<MobileOgrenciKurslarimResponse>(
                "/api/mobile/ogrenci/kurslarim",
                {
                    params: {
                        arama: aktifArama || undefined,
                        kategoriId: aktifKategoriId || undefined,
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

    function kursDetayAc(kursKayitId: number) {
        router.push(`/ogrenci/kurs-detay/${kursKayitId}` as any);
    }

    function aramaUygula() {
        const temizArama = aramaInput.trim() || null;

        Keyboard.dismiss();

        setArama(temizArama);
        setSayfa(1);

        kurslariGetir(false, {
            arama: temizArama,
            kategoriId,
            sayfa: 1,
        });
    }

    function filtreleriTemizle() {
        Keyboard.dismiss();

        setAramaInput("");
        setArama(null);
        setKategoriId(null);
        setSayfa(1);

        kurslariGetir(false, {
            arama: null,
            kategoriId: null,
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
            kategoriId: yeniKategoriId,
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
            kategoriId,
            sayfa: yeniSayfa,
        });
    }

    function yorumModalAc(kurs: MobileOgrenciKursItem) {
        setSeciliKurs(kurs);
        setPuan(kurs.kendiPuan ?? 5);
        setYorumMetni(kurs.kendiYorumMetni ?? "");
        setYorumModalAcik(true);
    }

    function yorumModalKapat() {
        if (yorumGonderiliyor) {
            return;
        }

        setYorumModalAcik(false);
        setSeciliKurs(null);
        setPuan(5);
        setYorumMetni("");
    }

    async function yorumGonder() {
        if (!seciliKurs) {
            return;
        }

        if (puan < 1 || puan > 5) {
            Alert.alert("Geçersiz puan", "Puan 1 ile 5 arasında olmalıdır.");
            return;
        }

        try {
            setYorumGonderiliyor(true);

            const response = await api.post<MobileOgrenciIslemResponse>(
                `/api/mobile/ogrenci/kurslarim/${seciliKurs.kursKayitId}/degerlendir`,
                {
                    puan,
                    yorumMetni: yorumMetni.trim() ? yorumMetni.trim() : null,
                }
            );

            Alert.alert(
                "Başarılı",
                response.data.mesaj || "Değerlendirmen kaydedildi."
            );

            setYorumModalAcik(false);
            setSeciliKurs(null);
            setPuan(5);
            setYorumMetni("");

            await kurslariGetir(true);
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Değerlendirme kaydedilirken hata oluştu.";

            Alert.alert("Hata", mesaj);
        } finally {
            setYorumGonderiliyor(false);
        }
    }

    function kayitIptalOnayiAl(kurs: MobileOgrenciKursItem) {
        Alert.alert(
            "Kayıt İptal",
            `"${kurs.kursAdi}" kurs kaydını iptal etmek istiyor musun? Bu işlem kurs ilerlemeni etkileyebilir.`,
            [
                {
                    text: "Vazgeç",
                    style: "cancel",
                },
                {
                    text: "Kaydı İptal Et",
                    style: "destructive",
                    onPress: () => kayitIptalEt(kurs),
                },
            ]
        );
    }

    async function kayitIptalEt(kurs: MobileOgrenciKursItem) {
        try {
            setIptalEdilenKursKayitId(kurs.kursKayitId);

            const response = await api.post<MobileOgrenciIslemResponse>(
                `/api/mobile/ogrenci/kurslarim/${kurs.kursKayitId}/kayit-iptal`
            );

            Alert.alert(
                "Başarılı",
                response.data.mesaj || "Kurs kaydı iptal edildi."
            );

            await kurslariGetir(true);
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Kurs kaydı iptal edilirken hata oluştu.";

            Alert.alert("Hata", mesaj);
        } finally {
            setIptalEdilenKursKayitId(null);
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
            subtitle="Kayıtlı olduğun kursları ve ilerleme durumunu buradan takip edebilirsin."
            refreshing={yenileniyor}
            onRefresh={() => kurslariGetir(true)}
            menuItems={OGRENCI_MENU_ITEMS}
            activeMenuKey="kurslarim"
        >
            <View style={styles.summaryGrid}>
                <SummaryCard title="Toplam Kurs" value={ozet.toplamKursSayisi} />
                <SummaryCard title="Devam Eden" value={ozet.devamEdenKursSayisi} />
                <SummaryCard title="Tamamlanan" value={ozet.tamamlananKursSayisi} />
                <SummaryCard
                    title="Ort. İlerleme"
                    value={`%${ozet.ortalamaIlerlemeYuzdesi}`}
                />
            </View>

            <FilterPanel
                aramaInput={aramaInput}
                setAramaInput={setAramaInput}
                aramaUygula={aramaUygula}
                filtreleriTemizle={filtreleriTemizle}
                kategoriler={kategoriler}
                kategoriId={kategoriId}
                kategoriSec={kategoriSec}
            />

            <View style={styles.listHeader}>
                <View>
                    <Text style={styles.listTitle}>Kayıtlı Kurslar</Text>
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
                            key={kurs.kursKayitId}
                            kurs={kurs}
                            kursDetayAc={kursDetayAc}
                            yorumModalAc={yorumModalAc}
                            kayitIptalOnayiAl={kayitIptalOnayiAl}
                            iptalEdiliyor={iptalEdilenKursKayitId === kurs.kursKayitId}
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

            <YorumModal
                acik={yorumModalAcik}
                kurs={seciliKurs}
                puan={puan}
                yorumMetni={yorumMetni}
                gonderiliyor={yorumGonderiliyor}
                setPuan={setPuan}
                setYorumMetni={setYorumMetni}
                kapat={yorumModalKapat}
                gonder={yorumGonder}
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
    kategoriler,
    kategoriId,
    kategoriSec,
}: {
    aramaInput: string;
    setAramaInput: (value: string) => void;
    aramaUygula: () => void;
    filtreleriTemizle: () => void;
    kategoriler: MobileOgrenciKategoriSecenek[];
    kategoriId: number | null;
    kategoriSec: (kategoriId: number | null) => void;
}) {
    return (
        <View style={styles.filterCard}>
            <View style={styles.filterHeader}>
                <Text style={styles.filterTitle}>Filtrele</Text>
                <Text style={styles.filterSubtitle}>Kurslarını hızlıca bul</Text>
            </View>

            <View style={styles.formGroup}>
                <Text style={styles.inputLabel}>Arama</Text>

                <TextInput
                    value={aramaInput}
                    onChangeText={setAramaInput}
                    placeholder="Kurs, eğitmen, açıklama veya kategori ara..."
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

            <Modal
                visible={modalAcik}
                transparent
                animationType="fade"
                onRequestClose={() => setModalAcik(false)}
            >
                <View style={styles.dropdownModalRoot}>
                    <Pressable
                        style={styles.dropdownBackdrop}
                        onPress={() => setModalAcik(false)}
                    />

                    <View style={styles.dropdownModalCard}>
                        <View style={styles.dropdownModalHeader}>
                            <Text style={styles.dropdownModalTitle}>Kategori Seç</Text>

                            <Pressable
                                onPress={() => setModalAcik(false)}
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
                        </ScrollView>
                    </View>
                </View>
            </Modal>
        </>
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
            <View style={styles.dropdownItemTextArea}>
                <Text
                    style={[
                        styles.dropdownItemLabel,
                        active ? styles.dropdownItemLabelActive : null,
                    ]}
                    numberOfLines={1}
                >
                    {label}
                </Text>
            </View>

            {active ? <Text style={styles.dropdownCheck}>✓</Text> : null}
        </Pressable>
    );
}

function KursKart({
    kurs,
    kursDetayAc,
    yorumModalAc,
    kayitIptalOnayiAl,
    iptalEdiliyor,
}: {
    kurs: MobileOgrenciKursItem;
    kursDetayAc: (kursKayitId: number) => void;
    yorumModalAc: (kurs: MobileOgrenciKursItem) => void;
    kayitIptalOnayiAl: (kurs: MobileOgrenciKursItem) => void;
    iptalEdiliyor: boolean;
}) {
    const guncelleniyorMu = kurs.guncelleniyorMu;

    return (
        <View style={styles.courseCard}>
            <Pressable
                disabled={guncelleniyorMu}
                onPress={() => kursDetayAc(kurs.kursKayitId)}
                style={({ pressed }) => [
                    styles.courseTapArea,
                    guncelleniyorMu ? styles.courseTapAreaDisabled : null,
                    pressed && !guncelleniyorMu ? styles.buttonPressed : null,
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

                    <Text style={styles.coursePercent}>%{kurs.ilerlemeYuzdesi}</Text>
                </View>

                {guncelleniyorMu ? (
                    <View style={styles.updateBadge}>
                        <Text style={styles.updateBadgeText}>Güncelleniyor</Text>
                    </View>
                ) : null}

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

                <View style={styles.progressTrack}>
                    <View
                        style={[
                            styles.progressFill,
                            {
                                width: `${kurs.ilerlemeYuzdesi}%`,
                            },
                        ]}
                    />
                </View>

                <View style={styles.courseMeta}>
                    <Text style={styles.courseLessonText}>
                        {kurs.tamamlananDersSayisi}/{kurs.toplamDersSayisi} ders tamamlandı
                    </Text>

                    <Text
                        style={[
                            styles.courseStatus,
                            guncelleniyorMu
                                ? styles.courseStatusUpdating
                                : kurs.kursTamamlandiMi
                                ? styles.courseStatusDone
                                : styles.courseStatusActive,
                        ]}
                    >
                        {guncelleniyorMu
                            ? "Güncelleniyor"
                            : kurs.kursTamamlandiMi
                                ? "Tamamlandı"
                                : "Devam ediyor"}
                    </Text>
                </View>

                {guncelleniyorMu ? (
                    <View style={styles.updateInfoBox}>
                        <Text style={styles.updateInfoTitle}>
                            Kurs erişime geçici olarak kapalı
                        </Text>

                        <Text style={styles.updateInfoText}>
                            Güncelleme tamamlandığında devam edebilirsiniz.
                        </Text>
                    </View>
                ) : (
                    <Text style={styles.detailHint}>Detayı görüntülemek için dokun</Text>
                )}
            </Pressable>

            {kurs.degerlendirmeVarMi ? (
                <View style={styles.reviewBox}>
                    <Text style={styles.reviewText}>
                        Puanın: {kurs.kendiPuan ?? "-"} / 5
                    </Text>

                    {kurs.kendiYorumMetni ? (
                        <Text style={styles.reviewComment} numberOfLines={2}>
                            {kurs.kendiYorumMetni}
                        </Text>
                    ) : null}
                </View>
            ) : null}

            <View style={styles.actionRow}>
                <Pressable
                    onPress={() => yorumModalAc(kurs)}
                    style={({ pressed }) => [
                        styles.secondaryActionButton,
                        pressed ? styles.buttonPressed : null,
                    ]}
                >
                    <Text style={styles.secondaryActionText}>
                        {kurs.degerlendirmeVarMi ? "Yorumu Güncelle" : "Yorum ve Puan"}
                    </Text>
                </Pressable>

                <Pressable
                    disabled={iptalEdiliyor}
                    onPress={() => kayitIptalOnayiAl(kurs)}
                    style={({ pressed }) => [
                        styles.dangerActionButton,
                        pressed && !iptalEdiliyor ? styles.buttonPressed : null,
                        iptalEdiliyor ? styles.actionButtonDisabled : null,
                    ]}
                >
                    {iptalEdiliyor ? (
                        <ActivityIndicator size="small" color="#dc2626" />
                    ) : (
                        <Text style={styles.dangerActionText}>Kaydı İptal Et</Text>
                    )}
                </Pressable>
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

function YorumModal({
    acik,
    kurs,
    puan,
    yorumMetni,
    gonderiliyor,
    setPuan,
    setYorumMetni,
    kapat,
    gonder,
}: {
    acik: boolean;
    kurs: MobileOgrenciKursItem | null;
    puan: number;
    yorumMetni: string;
    gonderiliyor: boolean;
    setPuan: (puan: number) => void;
    setYorumMetni: (yorum: string) => void;
    kapat: () => void;
    gonder: () => void;
}) {
    return (
        <Modal visible={acik} transparent animationType="fade" onRequestClose={kapat}>
            <View style={styles.modalRoot}>
                <Pressable style={styles.modalBackdrop} onPress={kapat} />

                <View style={styles.modalCard}>
                    <Text style={styles.modalTitle}>Yorum ve Puan</Text>

                    <Text style={styles.modalSubtitle} numberOfLines={2}>
                        {kurs?.kursAdi}
                    </Text>

                    <View style={styles.starRow}>
                        {[1, 2, 3, 4, 5].map((item) => (
                            <Pressable
                                key={item}
                                onPress={() => setPuan(item)}
                                style={[
                                    styles.starButton,
                                    item <= puan ? styles.starButtonActive : null,
                                ]}
                            >
                                <Text
                                    style={[
                                        styles.starText,
                                        item <= puan ? styles.starTextActive : null,
                                    ]}
                                >
                                    ★
                                </Text>
                            </Pressable>
                        ))}
                    </View>

                    <TextInput
                        value={yorumMetni}
                        onChangeText={setYorumMetni}
                        multiline
                        placeholder="Kurs hakkında yorumunu yaz..."
                        style={styles.commentInput}
                        textAlignVertical="top"
                    />

                    <View style={styles.modalActions}>
                        <Pressable
                            disabled={gonderiliyor}
                            onPress={kapat}
                            style={({ pressed }) => [
                                styles.modalCancelButton,
                                pressed && !gonderiliyor ? styles.buttonPressed : null,
                            ]}
                        >
                            <Text style={styles.modalCancelText}>Vazgeç</Text>
                        </Pressable>

                        <Pressable
                            disabled={gonderiliyor}
                            onPress={gonder}
                            style={({ pressed }) => [
                                styles.modalSaveButton,
                                pressed && !gonderiliyor ? styles.buttonPressed : null,
                                gonderiliyor ? styles.actionButtonDisabled : null,
                            ]}
                        >
                            {gonderiliyor ? (
                                <ActivityIndicator color="#ffffff" size="small" />
                            ) : (
                                <Text style={styles.modalSaveText}>Kaydet</Text>
                            )}
                        </Pressable>
                    </View>
                </View>
            </View>
        </Modal>
    );
}

function EmptyState() {
    return (
        <View style={styles.emptyCard}>
            <Text style={styles.emptyTitle}>Kurs bulunamadı</Text>

            <Text style={styles.emptyText}>
                Aramana veya filtrelerine uygun kayıtlı kurs bulunamadı.
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
    dropdownItemTextArea: {
        flex: 1,
    },
    dropdownItemLabel: {
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
        padding: 16,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    courseTapArea: {
        borderRadius: 16,
    },
    courseTapAreaDisabled: {
        opacity: 0.92,
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
    coursePercent: {
        marginLeft: 10,
        fontSize: 16,
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
    updateBadge: {
        alignSelf: "flex-start",
        marginTop: 12,
        backgroundColor: "#fffbeb",
        borderWidth: 1,
        borderColor: "#fde68a",
        borderRadius: 999,
        paddingHorizontal: 10,
        paddingVertical: 6,
    },
    updateBadgeText: {
        fontSize: 11,
        fontWeight: "900",
        color: "#b45309",
    },
    progressTrack: {
        height: 8,
        backgroundColor: "#e2e8f0",
        borderRadius: 999,
        marginTop: 14,
        overflow: "hidden",
    },
    progressFill: {
        height: "100%",
        backgroundColor: "#22c55e",
        borderRadius: 999,
    },
    courseMeta: {
        marginTop: 12,
        flexDirection: "row",
        justifyContent: "space-between",
        gap: 12,
    },
    courseLessonText: {
        flex: 1,
        fontSize: 13,
        fontWeight: "700",
        color: "#475569",
    },
    courseStatus: {
        fontSize: 13,
        fontWeight: "900",
    },
    courseStatusActive: {
        color: "#2563eb",
    },
    courseStatusDone: {
        color: "#16a34a",
    },
    courseStatusUpdating: {
        color: "#b45309",
    },
    detailHint: {
        marginTop: 8,
        fontSize: 12,
        fontWeight: "800",
        color: "#2563eb",
    },
    updateInfoBox: {
        marginTop: 12,
        backgroundColor: "#fffbeb",
        borderWidth: 1,
        borderColor: "#fde68a",
        borderRadius: 14,
        paddingHorizontal: 12,
        paddingVertical: 10,
    },
    updateInfoTitle: {
        fontSize: 13,
        fontWeight: "900",
        color: "#92400e",
    },
    updateInfoText: {
        marginTop: 3,
        fontSize: 12,
        fontWeight: "700",
        lineHeight: 17,
        color: "#b45309",
    },
    reviewBox: {
        marginTop: 12,
        backgroundColor: "#f8fafc",
        borderRadius: 14,
        paddingHorizontal: 12,
        paddingVertical: 10,
    },
    reviewText: {
        fontSize: 13,
        fontWeight: "900",
        color: "#334155",
    },
    reviewComment: {
        marginTop: 4,
        fontSize: 13,
        color: "#64748b",
        lineHeight: 18,
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
    dangerActionButton: {
        flex: 1,
        backgroundColor: "#fff1f2",
        borderWidth: 1,
        borderColor: "#fecdd3",
        borderRadius: 14,
        paddingVertical: 11,
        alignItems: "center",
    },
    dangerActionText: {
        fontSize: 13,
        fontWeight: "900",
        color: "#dc2626",
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
    modalRoot: {
        flex: 1,
        justifyContent: "center",
        padding: 20,
    },
    modalBackdrop: {
        ...StyleSheet.absoluteFillObject,
        backgroundColor: "rgba(15, 23, 42, 0.45)",
    },
    modalCard: {
        backgroundColor: "#ffffff",
        borderRadius: 24,
        padding: 18,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    modalTitle: {
        fontSize: 20,
        fontWeight: "900",
        color: "#0f172a",
    },
    modalSubtitle: {
        marginTop: 5,
        fontSize: 13,
        fontWeight: "700",
        color: "#64748b",
    },
    starRow: {
        flexDirection: "row",
        gap: 8,
        marginTop: 16,
    },
    starButton: {
        flex: 1,
        minHeight: 42,
        borderRadius: 13,
        backgroundColor: "#f8fafc",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        alignItems: "center",
        justifyContent: "center",
    },
    starButtonActive: {
        backgroundColor: "#fffbeb",
        borderColor: "#fde68a",
    },
    starText: {
        fontSize: 18,
        fontWeight: "900",
        color: "#94a3b8",
    },
    starTextActive: {
        color: "#f59e0b",
    },
    commentInput: {
        minHeight: 110,
        marginTop: 14,
        backgroundColor: "#f8fafc",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        borderRadius: 16,
        paddingHorizontal: 13,
        paddingVertical: 11,
        fontSize: 14,
        color: "#0f172a",
    },
    modalActions: {
        flexDirection: "row",
        gap: 10,
        marginTop: 14,
    },
    modalCancelButton: {
        flex: 1,
        backgroundColor: "#f8fafc",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        borderRadius: 14,
        paddingVertical: 12,
        alignItems: "center",
    },
    modalCancelText: {
        fontSize: 13,
        fontWeight: "900",
        color: "#334155",
    },
    modalSaveButton: {
        flex: 1,
        backgroundColor: "#2563eb",
        borderRadius: 14,
        paddingVertical: 12,
        alignItems: "center",
    },
    modalSaveText: {
        fontSize: 13,
        fontWeight: "900",
        color: "#ffffff",
    },
});
