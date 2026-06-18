import { useEffect, useMemo, useState } from "react";
import {
    ActivityIndicator,
    Alert,
    Pressable,
    StyleSheet,
    Text,
    View,
} from "react-native";

import type { PanelMenuItem } from "@/app/_shared/PanelLayout";
import PanelLayout from "@/app/_shared/PanelLayout";
import { api } from "@/src/api/client";
import type {
    MobileBildirimDurum,
    MobileBildirimIslemResponse,
    MobileBildirimItem,
    MobileBildirimlerResponse,
} from "@/src/types/bildirim";

type BildirimlerScreenProps = {
    title: string;
    subtitle: string;
    menuItems: PanelMenuItem[];
    activeMenuKey: string;
};

// Ortak bildirimler ekranı.
// Öğrenci, eğitmen ve ileride admin tarafında aynı component kullanılabilir.
export default function BildirimlerScreen({
    title,
    subtitle,
    menuItems,
    activeMenuKey,
}: BildirimlerScreenProps) {
    // Liste ve filtre state'leri backend'deki sayfalı bildirim cevabını yansıtır.
    const [bildirimler, setBildirimler] = useState<MobileBildirimItem[]>([]);
    const [durum, setDurum] = useState<MobileBildirimDurum>("tum");
    const [sayfa, setSayfa] = useState(1);
    const [sayfaBasinaKayit] = useState(10);
    const [toplamKayit, setToplamKayit] = useState(0);
    const [toplamSayfa, setToplamSayfa] = useState(1);
    const [okunmamisBildirimSayisi, setOkunmamisBildirimSayisi] = useState(0);

    // İlk yükleme, yenileme ve hata halleri ayrı UI göstergileri için tutulur.
    const [yukleniyor, setYukleniyor] = useState(true);
    const [yenileniyor, setYenileniyor] = useState(false);
    const [hata, setHata] = useState<string | null>(null);

    // Tekil bildirim işleminde sadece ilgili kartın butonları kilitlenir.
    const [islemdekiBildirimId, setIslemdekiBildirimId] = useState<number | null>(
        null
    );
    const [tumunuOkunduYapiliyor, setTumunuOkunduYapiliyor] = useState(false);

    // Ekran açıldığında ilk bildirim sayfası yüklenir.
    useEffect(() => {
        bildirimleriGetir();
    }, []);

    // Özet kartları için okunan sayı backend'den gelen toplam ve okunmamış sayıdan hesaplanır.
    const ozet = useMemo(() => {
        const okunanSayisi = Math.max(toplamKayit - okunmamisBildirimSayisi, 0);

        return {
            toplam: toplamKayit,
            okunmamis: okunmamisBildirimSayisi,
            okunan: okunanSayisi,
        };
    }, [toplamKayit, okunmamisBildirimSayisi]);

    // Listeyi API'den getirir; filtre veya sayfa değişimlerinde override ile çağrılır.
    async function bildirimleriGetir(
        refreshMi = false,
        override?: {
            durum?: MobileBildirimDurum;
            sayfa?: number;
        }
    ) {
        const aktifDurum = override?.durum ?? durum;
        const aktifSayfa = override?.sayfa ?? sayfa;

        try {
            if (refreshMi) {
                setYenileniyor(true);
            } else {
                setYukleniyor(true);
            }

            setHata(null);

            const response = await api.get<MobileBildirimlerResponse>(
                "/api/mobile/bildirimler",
                {
                    params: {
                        durum: aktifDurum,
                        sayfa: aktifSayfa,
                        sayfaBasinaKayit,
                    },
                }
            );

            setBildirimler(response.data.bildirimler ?? []);
            setDurum(response.data.durum ?? aktifDurum);
            setToplamKayit(response.data.toplamKayit ?? 0);
            setToplamSayfa(response.data.toplamSayfa ?? 1);
            setSayfa(response.data.sayfa ?? aktifSayfa);
            setOkunmamisBildirimSayisi(
                response.data.okunmamisBildirimSayisi ?? 0
            );
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Bildirimler alınırken hata oluştu.";

            setHata(mesaj);
            Alert.alert("Hata", mesaj);
        } finally {
            setYukleniyor(false);
            setYenileniyor(false);
        }
    }

    // Filtre değişince liste yeni filtreyle ilk sayfadan başlar.
    function durumDegistir(yeniDurum: MobileBildirimDurum) {
        if (yeniDurum === durum) {
            return;
        }

        setDurum(yeniDurum);
        setSayfa(1);

        bildirimleriGetir(false, {
            durum: yeniDurum,
            sayfa: 1,
        });
    }

    // Sayfa sınırları dışına çıkılmasını ve gereksiz tekrar isteklerini engeller.
    function sayfaDegistir(yeniSayfa: number) {
        if (yeniSayfa < 1 || yeniSayfa > toplamSayfa || yeniSayfa === sayfa) {
            return;
        }

        setSayfa(yeniSayfa);

        bildirimleriGetir(false, {
            durum,
            sayfa: yeniSayfa,
        });
    }

    // Zaten okundu olan bildirim için API çağrısı yapılmaz.
    async function okunduYap(bildirim: MobileBildirimItem) {
        if (bildirim.okunduMu) {
            return;
        }

        try {
            setIslemdekiBildirimId(bildirim.bildirimId);

            const response = await api.post<MobileBildirimIslemResponse>(
                `/api/mobile/bildirimler/${bildirim.bildirimId}/okundu`
            );

            setOkunmamisBildirimSayisi(
                response.data.okunmamisBildirimSayisi ?? 0
            );

            await bildirimleriGetir(true, {
                durum,
                sayfa,
            });
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Bildirim okundu yapılırken hata oluştu.";

            Alert.alert("Hata", mesaj);
        } finally {
            setIslemdekiBildirimId(null);
        }
    }

    // Okunmuş bildirimi tekrar okunmamış durumuna alır ve rozet sayısını günceller.
    async function okunmadiYap(bildirim: MobileBildirimItem) {
        if (!bildirim.okunduMu) {
            return;
        }

        try {
            setIslemdekiBildirimId(bildirim.bildirimId);

            const response = await api.post<MobileBildirimIslemResponse>(
                `/api/mobile/bildirimler/${bildirim.bildirimId}/okunmadi`
            );

            setOkunmamisBildirimSayisi(
                response.data.okunmamisBildirimSayisi ?? 0
            );

            await bildirimleriGetir(true, {
                durum,
                sayfa,
            });
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Bildirim okunmamış yapılırken hata oluştu.";

            Alert.alert("Hata", mesaj);
        } finally {
            setIslemdekiBildirimId(null);
        }
    }

    // Okunmamış bildirim yoksa toplu işlem yerine bilgilendirme gösterilir.
    async function tumunuOkunduYap() {
        if (okunmamisBildirimSayisi <= 0) {
            Alert.alert("Bilgi", "Okunmamış bildirimin bulunmuyor.");
            return;
        }

        try {
            setTumunuOkunduYapiliyor(true);

            const response = await api.post<MobileBildirimIslemResponse>(
                "/api/mobile/bildirimler/tumunu-okundu"
            );

            setOkunmamisBildirimSayisi(
                response.data.okunmamisBildirimSayisi ?? 0
            );

            await bildirimleriGetir(true, {
                durum,
                sayfa: 1,
            });
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Bildirimler okundu yapılırken hata oluştu.";

            Alert.alert("Hata", mesaj);
        } finally {
            setTumunuOkunduYapiliyor(false);
        }
    }

    // Silme işlemi kullanıcı onayı olmadan başlatılmaz.
    function silOnayiAl(bildirim: MobileBildirimItem) {
        Alert.alert(
            "Bildirim Sil",
            `"${bildirim.baslik}" bildirimini silmek istiyor musun?`,
            [
                {
                    text: "Vazgeç",
                    style: "cancel",
                },
                {
                    text: "Sil",
                    style: "destructive",
                    onPress: () => bildirimSil(bildirim),
                },
            ]
        );
    }

    // Silme sonrası mevcut filtre ve sayfayla liste yeniden çekilir.
    async function bildirimSil(bildirim: MobileBildirimItem) {
        try {
            setIslemdekiBildirimId(bildirim.bildirimId);

            const response = await api.delete<MobileBildirimIslemResponse>(
                `/api/mobile/bildirimler/${bildirim.bildirimId}`
            );

            setOkunmamisBildirimSayisi(
                response.data.okunmamisBildirimSayisi ?? 0
            );

            await bildirimleriGetir(true, {
                durum,
                sayfa,
            });
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Bildirim silinirken hata oluştu.";

            Alert.alert("Hata", mesaj);
        } finally {
            setIslemdekiBildirimId(null);
        }
    }

    if (yukleniyor) {
        return <LoadingState />;
    }

    if (hata) {
        return <ErrorState mesaj={hata} tekrarDene={() => bildirimleriGetir()} />;
    }

    return (
        <PanelLayout
            title={title}
            subtitle={subtitle}
            notificationCount={okunmamisBildirimSayisi}
            refreshing={yenileniyor}
            onRefresh={() => bildirimleriGetir(true)}
            menuItems={menuItems}
            activeMenuKey={activeMenuKey}
        >
            <View style={styles.summaryGrid}>
                <SummaryCard title="Toplam" value={ozet.toplam} />
                <SummaryCard title="Okunmamış" value={ozet.okunmamis} />
                <SummaryCard title="Okunmuş" value={ozet.okunan} />
            </View>

            <View style={styles.actionPanel}>
                <View style={styles.filterRow}>
                    <FilterButton
                        title="Tümü"
                        active={durum === "tum"}
                        onPress={() => durumDegistir("tum")}
                    />

                    <FilterButton
                        title="Okunmamış"
                        active={durum === "okunmamis"}
                        onPress={() => durumDegistir("okunmamis")}
                    />

                    <FilterButton
                        title="Okunmuş"
                        active={durum === "okunmus"}
                        onPress={() => durumDegistir("okunmus")}
                    />
                </View>

                <Pressable
                    disabled={tumunuOkunduYapiliyor || okunmamisBildirimSayisi <= 0}
                    onPress={tumunuOkunduYap}
                    style={({ pressed }) => [
                        styles.markAllButton,
                        pressed && !tumunuOkunduYapiliyor
                            ? styles.buttonPressed
                            : null,
                        tumunuOkunduYapiliyor || okunmamisBildirimSayisi <= 0
                            ? styles.disabledButton
                            : null,
                    ]}
                >
                    {tumunuOkunduYapiliyor ? (
                        <ActivityIndicator size="small" color="#2563eb" />
                    ) : (
                        <Text style={styles.markAllButtonText}>Tümünü Okundu Yap</Text>
                    )}
                </Pressable>
            </View>

            <View style={styles.listHeader}>
                <View>
                    <Text style={styles.listTitle}>Bildirim Listesi</Text>
                    <Text style={styles.listSubText}>{toplamKayit} kayıt bulundu</Text>
                </View>

                <View style={styles.pageBadge}>
                    <Text style={styles.pageBadgeText}>
                        {sayfa}/{toplamSayfa}
                    </Text>
                </View>
            </View>

            {bildirimler.length > 0 ? (
                <View style={styles.notificationList}>
                    {bildirimler.map((bildirim) => (
                        <BildirimKart
                            key={bildirim.bildirimId}
                            bildirim={bildirim}
                            islemde={islemdekiBildirimId === bildirim.bildirimId}
                            okunduYap={okunduYap}
                            okunmadiYap={okunmadiYap}
                            silOnayiAl={silOnayiAl}
                        />
                    ))}
                </View>
            ) : (
                <EmptyState durum={durum} />
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

// Ortak tam ekran yüklenme görünümü.
function LoadingState() {
    return (
        <View style={styles.centerContainer}>
            <ActivityIndicator size="large" color="#2563eb" />
            <Text style={styles.loadingText}>Bildirimlerin yükleniyor...</Text>
        </View>
    );
}

// İlk yükleme hatasında tekrar deneme aksiyonunu gösterir.
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

// Özet sayaç kartı.
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

// Bildirim durum filtrelerinde kullanılan pill buton.
function FilterButton({
    title,
    active,
    onPress,
}: {
    title: string;
    active: boolean;
    onPress: () => void;
}) {
    return (
        <Pressable
            onPress={onPress}
            style={({ pressed }) => [
                styles.filterButton,
                active ? styles.filterButtonActive : null,
                pressed ? styles.buttonPressed : null,
            ]}
        >
            <Text
                style={[
                    styles.filterButtonText,
                    active ? styles.filterButtonTextActive : null,
                ]}
            >
                {title}
            </Text>
        </Pressable>
    );
}

// Tek bildirim kartının okundu/okunmadı ve silme aksiyonlarını gösterir.
function BildirimKart({
    bildirim,
    islemde,
    okunduYap,
    okunmadiYap,
    silOnayiAl,
}: {
    bildirim: MobileBildirimItem;
    islemde: boolean;
    okunduYap: (bildirim: MobileBildirimItem) => void;
    okunmadiYap: (bildirim: MobileBildirimItem) => void;
    silOnayiAl: (bildirim: MobileBildirimItem) => void;
}) {
    return (
        <View
            style={[
                styles.notificationCard,
                !bildirim.okunduMu ? styles.notificationCardUnread : null,
            ]}
        >
            <View style={styles.notificationTop}>
                <View
                    style={[
                        styles.notificationIcon,
                        !bildirim.okunduMu ? styles.notificationIconUnread : null,
                    ]}
                >
                    <Text
                        style={[
                            styles.notificationIconText,
                            !bildirim.okunduMu ? styles.notificationIconTextUnread : null,
                        ]}
                    >
                        {bildirimTipIkonuGetir(bildirim.bildirimTipAdi)}
                    </Text>
                </View>

                <View style={styles.notificationInfo}>
                    <View style={styles.notificationTitleRow}>
                        <Text style={styles.notificationTitle} numberOfLines={1}>
                            {bildirim.baslik}
                        </Text>

                        {!bildirim.okunduMu ? <View style={styles.unreadDot} /> : null}
                    </View>

                    <Text style={styles.notificationType} numberOfLines={1}>
                        {bildirim.bildirimTipAdi || "Bildirim"} •{" "}
                        {tarihSaatFormatla(bildirim.olusturmaTarihi)}
                    </Text>
                </View>
            </View>

            <Text style={styles.notificationMessage}>{bildirim.mesaj}</Text>

            <View style={styles.notificationActions}>
                {!bildirim.okunduMu ? (
                    <Pressable
                        disabled={islemde}
                        onPress={() => okunduYap(bildirim)}
                        style={({ pressed }) => [
                            styles.readButton,
                            pressed && !islemde ? styles.buttonPressed : null,
                            islemde ? styles.disabledButton : null,
                        ]}
                    >
                        {islemde ? (
                            <ActivityIndicator size="small" color="#2563eb" />
                        ) : (
                            <Text style={styles.readButtonText}>Okundu Yap</Text>
                        )}
                    </Pressable>
                ) : (
                    <Pressable
                        disabled={islemde}
                        onPress={() => okunmadiYap(bildirim)}
                        style={({ pressed }) => [
                            styles.unreadButton,
                            pressed && !islemde ? styles.buttonPressed : null,
                            islemde ? styles.disabledButton : null,
                        ]}
                    >
                        {islemde ? (
                            <ActivityIndicator size="small" color="#475569" />
                        ) : (
                            <Text style={styles.unreadButtonText}>
                                Okunmamış Yap
                            </Text>
                        )}
                    </Pressable>
                )}

                <Pressable
                    disabled={islemde}
                    onPress={() => silOnayiAl(bildirim)}
                    style={({ pressed }) => [
                        styles.deleteButton,
                        pressed && !islemde ? styles.buttonPressed : null,
                        islemde ? styles.disabledButton : null,
                    ]}
                >
                    <Text style={styles.deleteButtonText}>Sil</Text>
                </Pressable>
            </View>
        </View>
    );
}

// Bildirim tipi adına göre kartta gösterilecek kısa sembolü seçer.
function bildirimTipIkonuGetir(tipAdi: string) {
    const tip = tipAdi.toLowerCase();

    if (tip.includes("uyarı")) {
        return "!";
    }

    if (tip.includes("hata")) {
        return "×";
    }

    return "i";
}

// Geçerli tarihse Türkçe kısa tarih/saat formatına çevirir.
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
// Tek sayfalama kontrolü; önceki/sonraki butonları ve sayfa bilgisini gösterir.
// Alt sayfalama kontrolü; sınırdaki butonları pasif hale getirir.
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

// Aktif filtreye göre boş liste mesajını özelleştirir.
function EmptyState({ durum }: { durum: MobileBildirimDurum }) {
    const mesaj =
        durum === "okunmamis"
            ? "Okunmamış bildirimin bulunmuyor."
            : durum === "okunmus"
                ? "Okunmuş bildirimin bulunmuyor."
                : "Henüz bildirimin bulunmuyor.";

    return (
        <View style={styles.emptyCard}>
            <Text style={styles.emptyTitle}>Bildirim bulunamadı</Text>
            <Text style={styles.emptyText}>{mesaj}</Text>
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
        flexBasis: "30%",
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
    actionPanel: {
        backgroundColor: "#ffffff",
        borderRadius: 22,
        padding: 16,
        borderWidth: 1,
        borderColor: "#e5e7eb",
        marginBottom: 20,
    },
    filterRow: {
        flexDirection: "row",
        gap: 8,
    },
    filterButton: {
        flex: 1,
        minHeight: 42,
        backgroundColor: "#f8fafc",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        borderRadius: 14,
        alignItems: "center",
        justifyContent: "center",
        paddingHorizontal: 8,
    },
    filterButtonActive: {
        backgroundColor: "#eff6ff",
        borderColor: "#bfdbfe",
    },
    filterButtonText: {
        fontSize: 12,
        fontWeight: "900",
        color: "#475569",
    },
    filterButtonTextActive: {
        color: "#2563eb",
    },
    markAllButton: {
        marginTop: 12,
        minHeight: 44,
        backgroundColor: "#eff6ff",
        borderWidth: 1,
        borderColor: "#bfdbfe",
        borderRadius: 15,
        alignItems: "center",
        justifyContent: "center",
    },
    markAllButtonText: {
        fontSize: 13,
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
    notificationList: {
        gap: 12,
    },
    notificationCard: {
        backgroundColor: "#ffffff",
        borderRadius: 20,
        padding: 16,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    notificationCardUnread: {
        borderColor: "#bfdbfe",
        backgroundColor: "#f8fbff",
    },
    notificationTop: {
        flexDirection: "row",
        alignItems: "center",
    },
    notificationIcon: {
        width: 46,
        height: 46,
        borderRadius: 16,
        backgroundColor: "#f1f5f9",
        alignItems: "center",
        justifyContent: "center",
        marginRight: 12,
    },
    notificationIconUnread: {
        backgroundColor: "#eff6ff",
    },
    notificationIconText: {
        fontSize: 20,
        fontWeight: "900",
        color: "#64748b",
    },
    notificationIconTextUnread: {
        color: "#2563eb",
    },
    notificationInfo: {
        flex: 1,
    },
    notificationTitleRow: {
        flexDirection: "row",
        alignItems: "center",
        gap: 8,
    },
    notificationTitle: {
        flex: 1,
        fontSize: 16,
        fontWeight: "900",
        color: "#0f172a",
    },
    unreadDot: {
        width: 9,
        height: 9,
        borderRadius: 999,
        backgroundColor: "#2563eb",
    },
    notificationType: {
        marginTop: 3,
        fontSize: 12,
        fontWeight: "700",
        color: "#64748b",
    },
    notificationMessage: {
        marginTop: 12,
        fontSize: 14,
        lineHeight: 20,
        color: "#334155",
    },
    notificationActions: {
        marginTop: 14,
        flexDirection: "row",
        gap: 10,
    },
    readButton: {
        flex: 1,
        backgroundColor: "#eff6ff",
        borderWidth: 1,
        borderColor: "#bfdbfe",
        borderRadius: 14,
        paddingVertical: 11,
        alignItems: "center",
    },
    readButtonText: {
        fontSize: 13,
        fontWeight: "900",
        color: "#2563eb",
    },
    unreadButton: {
        flex: 1,
        backgroundColor: "#f8fafc",
        borderWidth: 1,
        borderColor: "#cbd5e1",
        borderRadius: 14,
        paddingVertical: 11,
        alignItems: "center",
    },
    unreadButtonText: {
        fontSize: 13,
        fontWeight: "900",
        color: "#475569",
    },
    deleteButton: {
        flex: 1,
        backgroundColor: "#fff1f2",
        borderWidth: 1,
        borderColor: "#fecdd3",
        borderRadius: 14,
        paddingVertical: 11,
        alignItems: "center",
    },
    deleteButtonText: {
        fontSize: 13,
        fontWeight: "900",
        color: "#dc2626",
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
