import { Feather } from "@expo/vector-icons";
import { useEffect, useMemo, useState } from "react";
import {
    ActivityIndicator,
    Alert,
    Pressable,
    StyleSheet,
    Text,
    TextInput,
    View,
} from "react-native";

import type { PanelMenuItem } from "@/app/_shared/PanelLayout";
import PanelLayout from "@/app/_shared/PanelLayout";
import { api } from "@/src/api/client";
import type {
    MobileAiOneriItem,
    MobileAiOnerilerResponse,
    MobileAiOneriSilResponse,
    MobileAiOneriSiralama,
} from "@/src/types/aiOneri";

type AiOnerilerScreenProps = {
    title: string;
    subtitle: string;
    menuItems: PanelMenuItem[];
    activeMenuKey: string;
};

const SAYFA_BOYUTU = 10;

export default function AiOnerilerScreen({
    title,
    subtitle,
    menuItems,
    activeMenuKey,
}: AiOnerilerScreenProps) {
    const [oneriler, setOneriler] = useState<MobileAiOneriItem[]>([]);

    const [arama, setArama] = useState("");
    const [siralama, setSiralama] = useState<MobileAiOneriSiralama>("yeni");

    const [sayfa, setSayfa] = useState(1);
    const [toplamKayit, setToplamKayit] = useState(0);
    const [toplamSayfa, setToplamSayfa] = useState(0);

    const [yukleniyor, setYukleniyor] = useState(true);
    const [yenileniyor, setYenileniyor] = useState(false);
    const [hata, setHata] = useState<string | null>(null);

    const [acikOneriId, setAcikOneriId] = useState<number | null>(null);
    const [silinenOneriId, setSilinenOneriId] = useState<number | null>(null);

    const siralamaSecenekleri = useMemo(
        () => [
            { key: "yeni" as MobileAiOneriSiralama, label: "Yeni" },
            { key: "eski" as MobileAiOneriSiralama, label: "Eski" },
            { key: "kurs-az" as MobileAiOneriSiralama, label: "Kurs A-Z" },
            { key: "kurs-za" as MobileAiOneriSiralama, label: "Kurs Z-A" },
        ],
        []
    );

    useEffect(() => {
        onerileriGetir(false, {
            sayfa: 1,
            arama,
            siralama,
        });
    }, []);

    async function onerileriGetir(
        refreshMi = false,
        override?: {
            sayfa?: number;
            arama?: string;
            siralama?: MobileAiOneriSiralama;
        }
    ) {
        const aktifSayfa = override?.sayfa ?? sayfa;
        const aktifArama = override?.arama ?? arama;
        const aktifSiralama = override?.siralama ?? siralama;

        try {
            if (refreshMi) {
                setYenileniyor(true);
            } else {
                setYukleniyor(true);
            }

            setHata(null);

            const response = await api.get<MobileAiOnerilerResponse>(
                "/api/mobile/ai-oneriler",
                {
                    params: {
                        arama: aktifArama.trim() || undefined,
                        siralama: aktifSiralama,
                        sayfa: aktifSayfa,
                        sayfaBoyutu: SAYFA_BOYUTU,
                    },
                }
            );

            const data = response.data;

            setOneriler(data.oneriler ?? []);
            setToplamKayit(data.toplamKayit ?? 0);
            setToplamSayfa(data.toplamSayfa ?? 0);
            setSayfa(data.sayfa ?? aktifSayfa);
            setSiralama(data.siralama ?? aktifSiralama);
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "AI öneriler alınırken hata oluştu.";

            setHata(mesaj);
            Alert.alert("Hata", mesaj);
        } finally {
            setYukleniyor(false);
            setYenileniyor(false);
        }
    }

    function aramaYap() {
        setSayfa(1);
        setAcikOneriId(null);

        onerileriGetir(false, {
            sayfa: 1,
            arama,
            siralama,
        });
    }

    function siralamaDegistir(yeniSiralama: MobileAiOneriSiralama) {
        if (yeniSiralama === siralama) {
            return;
        }

        setSiralama(yeniSiralama);
        setSayfa(1);
        setAcikOneriId(null);

        onerileriGetir(false, {
            sayfa: 1,
            arama,
            siralama: yeniSiralama,
        });
    }

    function sayfaDegistir(yeniSayfa: number) {
        if (yeniSayfa < 1 || yeniSayfa > toplamSayfa || yeniSayfa === sayfa) {
            return;
        }

        setSayfa(yeniSayfa);
        setAcikOneriId(null);

        onerileriGetir(false, {
            sayfa: yeniSayfa,
            arama,
            siralama,
        });
    }

    function oneriAcKapat(oneriId: number) {
        setAcikOneriId((prev) => (prev === oneriId ? null : oneriId));
    }

    function silOnayiAl(oneri: MobileAiOneriItem) {
        Alert.alert(
            "AI önerisini sil",
            `"${oneri.kursAdi ?? "Genel Öneri"}" önerisini silmek istiyor musun?`,
            [
                {
                    text: "Vazgeç",
                    style: "cancel",
                },
                {
                    text: "Sil",
                    style: "destructive",
                    onPress: () => oneriSil(oneri),
                },
            ]
        );
    }

    async function oneriSil(oneri: MobileAiOneriItem) {
        try {
            setSilinenOneriId(oneri.oneriId);

            await api.delete<MobileAiOneriSilResponse>(
                `/api/mobile/ai-oneriler/${oneri.oneriId}`
            );

            setAcikOneriId(null);

            await onerileriGetir(true, {
                sayfa,
                arama,
                siralama,
            });
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "AI önerisi silinirken hata oluştu.";

            Alert.alert("Hata", mesaj);
        } finally {
            setSilinenOneriId(null);
        }
    }

    const ozet = useMemo(() => {
        return {
            toplam: toplamKayit,
            sayfa,
            toplamSayfa,
        };
    }, [toplamKayit, sayfa, toplamSayfa]);

    if (yukleniyor) {
        return <LoadingState />;
    }

    if (hata) {
        return (
            <ErrorState
                mesaj={hata}
                tekrarDene={() =>
                    onerileriGetir(false, {
                        sayfa: 1,
                        arama,
                        siralama,
                    })
                }
            />
        );
    }

    return (
        <PanelLayout
            title={title}
            subtitle={subtitle}
            refreshing={yenileniyor}
            onRefresh={() =>
                onerileriGetir(true, {
                    sayfa,
                    arama,
                    siralama,
                })
            }
            menuItems={menuItems}
            activeMenuKey={activeMenuKey}
        >
            <View style={styles.summaryGrid}>
                <SummaryCard title="Toplam Öneri" value={ozet.toplam} />
                <SummaryCard title="Sayfa" value={`${ozet.sayfa}/${Math.max(ozet.toplamSayfa, 1)}`} />
            </View>

            <View style={styles.filterPanel}>
                <View style={styles.searchRow}>
                    <View style={styles.searchInputWrapper}>
                        <Feather name="search" size={18} color="#94a3b8" />
                        <TextInput
                            value={arama}
                            onChangeText={setArama}
                            onSubmitEditing={aramaYap}
                            placeholder="Önerilerde ara..."
                            placeholderTextColor="#94a3b8"
                            style={styles.searchInput}
                            returnKeyType="search"
                        />
                    </View>

                    <Pressable
                        onPress={aramaYap}
                        style={({ pressed }) => [
                            styles.searchButton,
                            pressed ? styles.buttonPressed : null,
                        ]}
                    >
                        <Text style={styles.searchButtonText}>Ara</Text>
                    </Pressable>
                </View>

                <View style={styles.sortRow}>
                    {siralamaSecenekleri.map((item) => (
                        <FilterButton
                            key={item.key}
                            title={item.label}
                            active={siralama === item.key}
                            onPress={() => siralamaDegistir(item.key)}
                        />
                    ))}
                </View>
            </View>

            {oneriler.length > 0 ? (
                <View style={styles.listArea}>
                    {oneriler.map((oneri) => (
                        <OneriCard
                            key={oneri.oneriId}
                            oneri={oneri}
                            acikMi={acikOneriId === oneri.oneriId}
                            siliniyorMu={silinenOneriId === oneri.oneriId}
                            onToggle={() => oneriAcKapat(oneri.oneriId)}
                            onDelete={() => silOnayiAl(oneri)}
                        />
                    ))}
                </View>
            ) : (
                <EmptyState />
            )}

            {toplamSayfa > 1 ? (
                <Pagination
                    sayfa={sayfa}
                    toplamSayfa={toplamSayfa}
                    onPrevious={() => sayfaDegistir(sayfa - 1)}
                    onNext={() => sayfaDegistir(sayfa + 1)}
                />
            ) : null}
        </PanelLayout>
    );
}

function SummaryCard({ title, value }: { title: string; value: number | string }) {
    return (
        <View style={styles.summaryCard}>
            <Text style={styles.summaryValue}>{value}</Text>
            <Text style={styles.summaryTitle}>{title}</Text>
        </View>
    );
}

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

function OneriCard({
    oneri,
    acikMi,
    siliniyorMu,
    onToggle,
    onDelete,
}: {
    oneri: MobileAiOneriItem;
    acikMi: boolean;
    siliniyorMu: boolean;
    onToggle: () => void;
    onDelete: () => void;
}) {
    return (
        <View style={styles.oneriCard}>
            <Pressable
                onPress={onToggle}
                style={({ pressed }) => [
                    styles.oneriHeader,
                    pressed ? styles.buttonPressed : null,
                ]}
            >
                <View style={styles.oneriIconBox}>
                    <Feather name="zap" size={18} color="#2563eb" />
                </View>

                <View style={styles.oneriHeaderText}>
                    <Text style={styles.oneriType} numberOfLines={1}>
                        {oneri.oneriTipAdi || "AI Önerisi"}
                    </Text>

                    <Text style={styles.oneriCourse} numberOfLines={2}>
                        {oneri.kursAdi || "Genel Öneri"}
                    </Text>

                    <Text style={styles.oneriDate} numberOfLines={1}>
                        {tarihFormatla(oneri.olusturmaTarihi)}
                    </Text>
                </View>

                <Feather
                    name={acikMi ? "chevron-up" : "chevron-down"}
                    size={22}
                    color="#64748b"
                />
            </Pressable>

            {acikMi ? (
                <View style={styles.oneriDetail}>
                    <Text style={styles.oneriText}>{oneri.oneriMetni}</Text>

                    <Pressable
                        disabled={siliniyorMu}
                        onPress={onDelete}
                        style={({ pressed }) => [
                            styles.deleteButton,
                            pressed && !siliniyorMu ? styles.buttonPressed : null,
                            siliniyorMu ? styles.disabledButton : null,
                        ]}
                    >
                        {siliniyorMu ? (
                            <ActivityIndicator size="small" color="#ef4444" />
                        ) : (
                            <>
                                <Feather name="trash-2" size={15} color="#ef4444" />
                                <Text style={styles.deleteButtonText}>Sil</Text>
                            </>
                        )}
                    </Pressable>
                </View>
            ) : (
                <Text style={styles.oneriPreview} numberOfLines={3}>
                    {oneri.oneriMetni}
                </Text>
            )}
        </View>
    );
}

function Pagination({
    sayfa,
    toplamSayfa,
    onPrevious,
    onNext,
}: {
    sayfa: number;
    toplamSayfa: number;
    onPrevious: () => void;
    onNext: () => void;
}) {
    return (
        <View style={styles.pagination}>
            <Pressable
                disabled={sayfa <= 1}
                onPress={onPrevious}
                style={({ pressed }) => [
                    styles.pageButton,
                    sayfa <= 1 ? styles.disabledButton : null,
                    pressed && sayfa > 1 ? styles.buttonPressed : null,
                ]}
            >
                <Text style={styles.pageButtonText}>Önceki</Text>
            </Pressable>

            <Text style={styles.pageInfo}>
                {sayfa} / {toplamSayfa}
            </Text>

            <Pressable
                disabled={sayfa >= toplamSayfa}
                onPress={onNext}
                style={({ pressed }) => [
                    styles.pageButton,
                    sayfa >= toplamSayfa ? styles.disabledButton : null,
                    pressed && sayfa < toplamSayfa ? styles.buttonPressed : null,
                ]}
            >
                <Text style={styles.pageButtonText}>Sonraki</Text>
            </Pressable>
        </View>
    );
}

function EmptyState() {
    return (
        <View style={styles.emptyCard}>
            <Feather name="inbox" size={32} color="#94a3b8" />

            <Text style={styles.emptyTitle}>AI önerisi bulunamadı</Text>

            <Text style={styles.emptyText}>
                Web panelinde oluşturulan AI önerileri burada listelenir.
            </Text>
        </View>
    );
}

function LoadingState() {
    return (
        <View style={styles.centerContainer}>
            <ActivityIndicator size="large" color="#2563eb" />
            <Text style={styles.loadingText}>AI öneriler yükleniyor...</Text>
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

            <Pressable
                onPress={tekrarDene}
                style={({ pressed }) => [
                    styles.primaryButton,
                    pressed ? styles.buttonPressed : null,
                ]}
            >
                <Text style={styles.primaryButtonText}>Tekrar Dene</Text>
            </Pressable>
        </View>
    );
}

function tarihFormatla(value: string) {
    const tarih = new Date(value);

    if (Number.isNaN(tarih.getTime())) {
        return value;
    }

    return tarih.toLocaleString("tr-TR", {
        day: "2-digit",
        month: "long",
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
        fontWeight: "700",
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
    disabledButton: {
        opacity: 0.45,
    },
    summaryGrid: {
        flexDirection: "row",
        gap: 12,
    },
    summaryCard: {
        flex: 1,
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
        fontWeight: "800",
        color: "#64748b",
    },
    filterPanel: {
        marginTop: 16,
        backgroundColor: "#ffffff",
        borderRadius: 20,
        padding: 14,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    searchRow: {
        flexDirection: "row",
        gap: 10,
    },
    searchInputWrapper: {
        flex: 1,
        minHeight: 46,
        borderRadius: 14,
        borderWidth: 1,
        borderColor: "#e2e8f0",
        backgroundColor: "#f8fafc",
        paddingHorizontal: 12,
        flexDirection: "row",
        alignItems: "center",
        gap: 8,
    },
    searchInput: {
        flex: 1,
        fontSize: 14,
        color: "#0f172a",
        fontWeight: "700",
        paddingVertical: 8,
    },
    searchButton: {
        minHeight: 46,
        paddingHorizontal: 16,
        borderRadius: 14,
        backgroundColor: "#2563eb",
        alignItems: "center",
        justifyContent: "center",
    },
    searchButtonText: {
        color: "#ffffff",
        fontWeight: "900",
        fontSize: 14,
    },
    sortRow: {
        marginTop: 12,
        flexDirection: "row",
        flexWrap: "wrap",
        gap: 8,
    },
    filterButton: {
        paddingHorizontal: 12,
        paddingVertical: 8,
        borderRadius: 999,
        backgroundColor: "#f1f5f9",
    },
    filterButtonActive: {
        backgroundColor: "#0f172a",
    },
    filterButtonText: {
        fontSize: 12,
        fontWeight: "900",
        color: "#334155",
    },
    filterButtonTextActive: {
        color: "#ffffff",
    },
    listArea: {
        marginTop: 16,
        gap: 12,
    },
    oneriCard: {
        backgroundColor: "#ffffff",
        borderRadius: 20,
        borderWidth: 1,
        borderColor: "#e5e7eb",
        overflow: "hidden",
    },
    oneriHeader: {
        flexDirection: "row",
        alignItems: "center",
        padding: 16,
        gap: 12,
    },
    oneriIconBox: {
        width: 42,
        height: 42,
        borderRadius: 14,
        backgroundColor: "#eff6ff",
        alignItems: "center",
        justifyContent: "center",
    },
    oneriHeaderText: {
        flex: 1,
        minWidth: 0,
    },
    oneriType: {
        fontSize: 12,
        fontWeight: "900",
        color: "#2563eb",
    },
    oneriCourse: {
        marginTop: 3,
        fontSize: 16,
        fontWeight: "900",
        color: "#0f172a",
    },
    oneriDate: {
        marginTop: 4,
        fontSize: 12,
        color: "#94a3b8",
        fontWeight: "700",
    },
    oneriPreview: {
        paddingHorizontal: 16,
        paddingBottom: 16,
        marginTop: -4,
        fontSize: 14,
        lineHeight: 21,
        color: "#475569",
    },
    oneriDetail: {
        borderTopWidth: 1,
        borderTopColor: "#f1f5f9",
        backgroundColor: "#f8fafc",
        padding: 16,
    },
    oneriText: {
        fontSize: 14,
        lineHeight: 23,
        color: "#334155",
        fontWeight: "500",
    },
    deleteButton: {
        marginTop: 14,
        alignSelf: "flex-start",
        flexDirection: "row",
        alignItems: "center",
        gap: 6,
        backgroundColor: "#fef2f2",
        borderRadius: 12,
        paddingHorizontal: 12,
        paddingVertical: 9,
    },
    deleteButtonText: {
        color: "#ef4444",
        fontWeight: "900",
        fontSize: 13,
    },
    pagination: {
        marginTop: 16,
        marginBottom: 8,
        flexDirection: "row",
        alignItems: "center",
        justifyContent: "space-between",
        gap: 12,
    },
    pageButton: {
        flex: 1,
        backgroundColor: "#ffffff",
        borderRadius: 14,
        borderWidth: 1,
        borderColor: "#e5e7eb",
        paddingVertical: 12,
        alignItems: "center",
    },
    pageButtonText: {
        fontSize: 13,
        fontWeight: "900",
        color: "#2563eb",
    },
    pageInfo: {
        minWidth: 64,
        textAlign: "center",
        fontSize: 13,
        fontWeight: "900",
        color: "#475569",
    },
    emptyCard: {
        marginTop: 16,
        backgroundColor: "#ffffff",
        borderRadius: 20,
        padding: 24,
        alignItems: "center",
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    emptyTitle: {
        marginTop: 10,
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