import { router, useLocalSearchParams } from "expo-router";
import { useEffect, useMemo, useState } from "react";
import {
    ActivityIndicator,
    Alert,
    Pressable,
    StyleSheet,
    Text,
    View,
} from "react-native";

import PanelLayout from "@/app/_shared/PanelLayout";
import { OGRENCI_MENU_ITEMS } from "@/app/_shared/panelMenus";
import { api } from "@/src/api/client";
import type {
    MobileOgrenciIslemResponse,
    MobileOgrenciKesfetBolum,
    MobileOgrenciKesfetDers,
    MobileOgrenciKesfetDetayResponse,
} from "@/src/types/ogrenci";

// Keşfet kurs detay ekranı.
// Öğrenci kurs içeriğini inceleyebilir ve kayıt olabilir.
// Bu ekranda materyal adı gösterilmez, sadece materyal sayısı gösterilir.
export default function OgrenciKesfetDetayScreen() {
    const params = useLocalSearchParams<{
        kursId?: string | string[];
    }>();

    const [detay, setDetay] = useState<MobileOgrenciKesfetDetayResponse | null>(
        null
    );

    const [yukleniyor, setYukleniyor] = useState(true);
    const [yenileniyor, setYenileniyor] = useState(false);
    const [hata, setHata] = useState<string | null>(null);
    const [guncelleniyorHatasi, setGuncelleniyorHatasi] = useState(false);
    const [kayitOlunuyor, setKayitOlunuyor] = useState(false);

    const kursId = useMemo(() => {
        const rawValue = Array.isArray(params.kursId)
            ? params.kursId[0]
            : params.kursId;

        const id = Number(rawValue);

        return Number.isFinite(id) && id > 0 ? id : null;
    }, [params.kursId]);

    useEffect(() => {
        if (!kursId) {
            setYukleniyor(false);
            setHata("Geçersiz kurs bilgisi.");
            return;
        }

        detayGetir();
    }, [kursId]);

    async function detayGetir(refreshMi = false) {
        if (!kursId) {
            return;
        }

        try {
            if (refreshMi) {
                setYenileniyor(true);
            } else {
                setYukleniyor(true);
            }

            setHata(null);
            setGuncelleniyorHatasi(false);

            const response = await api.get<MobileOgrenciKesfetDetayResponse>(
                `/api/mobile/ogrenci/kesfet/${kursId}`
            );

            setDetay(response.data);
        } catch (error: any) {
            const kursGuncelleniyorMu = Boolean(
                error?.response?.data?.guncelleniyorMu
            );

            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Kurs detayı alınırken hata oluştu.";

            setHata(mesaj);
            setGuncelleniyorHatasi(kursGuncelleniyorMu);
            Alert.alert(kursGuncelleniyorMu ? "Kurs Güncelleniyor" : "Hata", mesaj);
        } finally {
            setYukleniyor(false);
            setYenileniyor(false);
        }
    }

    function kayitOlOnayiAl() {
        if (!detay || !detay.kayitOlabilirMi) {
            return;
        }

        Alert.alert(
            "Kursa Kayıt Ol",
            `"${detay.kursAdi}" kursuna kayıt olmak istiyor musun?`,
            [
                {
                    text: "Vazgeç",
                    style: "cancel",
                },
                {
                    text: "Kayıt Ol",
                    onPress: kayitOl,
                },
            ]
        );
    }

    async function kayitOl() {
        if (!kursId) {
            return;
        }

        try {
            setKayitOlunuyor(true);

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
                        text: "Burada Kal",
                        style: "cancel",
                    },
                ]
            );

            await detayGetir(true);
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Kursa kayıt olunurken hata oluştu.";

            Alert.alert("Hata", mesaj);
        } finally {
            setKayitOlunuyor(false);
        }
    }

    if (yukleniyor) {
        return <LoadingState />;
    }

    if (hata || !detay) {
        return (
            <ErrorState
                mesaj={hata || "Kurs detayı bulunamadı."}
                tekrarDene={() => detayGetir()}
                guncelleniyorMu={guncelleniyorHatasi}
            />
        );
    }

    return (
        <PanelLayout
            title="Kurs Detayı"
            subtitle="Kursa kayıt olmadan önce içerik ve genel bilgileri inceleyebilirsin."
            refreshing={yenileniyor}
            onRefresh={() => detayGetir(true)}
            menuItems={OGRENCI_MENU_ITEMS}
            activeMenuKey="kesfet"
        >
            <Pressable
                onPress={() => router.back()}
                style={({ pressed }) => [
                    styles.backButton,
                    pressed ? styles.buttonPressed : null,
                ]}
            >
                <Text style={styles.backButtonText}>← Keşfete Dön</Text>
            </Pressable>

            <View style={styles.heroCard}>
                <View style={styles.heroTop}>
                    <View style={styles.courseAvatar}>
                        <Text style={styles.courseAvatarText}>
                            {detay.kursAdi.substring(0, 1).toUpperCase()}
                        </Text>
                    </View>

                    <View style={styles.heroInfo}>
                        <Text style={styles.courseTitle}>{detay.kursAdi}</Text>

                        <Text style={styles.instructorText} numberOfLines={1}>
                            {detay.egitmenAdSoyad}
                        </Text>
                    </View>
                </View>

                {detay.aciklama ? (
                    <Text style={styles.descriptionText}>{detay.aciklama}</Text>
                ) : null}

                {detay.kategoriler.length > 0 ? (
                    <View style={styles.categoryRow}>
                        {detay.kategoriler.map((kategori) => (
                            <View key={kategori} style={styles.categoryChip}>
                                <Text style={styles.categoryChipText}>{kategori}</Text>
                            </View>
                        ))}
                    </View>
                ) : null}

                <View style={styles.metaGrid}>
                    <InfoCard title="Bölüm" value={String(detay.toplamBolumSayisi)} />
                    <InfoCard title="Ders" value={String(detay.toplamDersSayisi)} />
                    <InfoCard
                        title="Öğrenci"
                        value={String(detay.kayitliOgrenciSayisi)}
                    />
                    <InfoCard
                        title="Puan"
                        value={
                            detay.degerlendirmeSayisi > 0
                                ? `${detay.ortalamaPuan}/5`
                                : "-"
                        }
                    />
                </View>

                <View style={styles.examBox}>
                    <Text style={styles.examTitle}>Sınav Bilgisi</Text>

                    <Text style={styles.examText}>
                        {detay.sinavVarMi
                            ? `Bu kursta sınav bulunuyor. Geçme notu: ${detay.gecmeNotu ?? "-"
                            }`
                            : "Bu kurs için sınav tanımlanmamış."}
                    </Text>
                </View>

                <Pressable
                    disabled={kayitOlunuyor || !detay.kayitOlabilirMi}
                    onPress={kayitOlOnayiAl}
                    style={({ pressed }) => [
                        styles.enrollButton,
                        pressed && !kayitOlunuyor ? styles.buttonPressed : null,
                        kayitOlunuyor || !detay.kayitOlabilirMi
                            ? styles.enrollButtonDisabled
                            : null,
                    ]}
                >
                    {kayitOlunuyor ? (
                        <ActivityIndicator color="#ffffff" size="small" />
                    ) : (
                        <Text style={styles.enrollButtonText}>
                            {detay.guncelleniyorMu
                                ? "Geçici Olarak Kapalı"
                                : detay.kayitliMi
                                    ? "Zaten Kayıtlısın"
                                    : "Kursa Kayıt Ol"}
                        </Text>
                    )}
                </Pressable>
            </View>

            <View style={styles.sectionHeader}>
                <Text style={styles.sectionTitle}>Kurs İçeriği</Text>
                <Text style={styles.sectionSubText}>
                    {detay.bolumler.length} bölüm listeleniyor
                </Text>
            </View>

            {detay.bolumler.length > 0 ? (
                <View style={styles.sectionList}>
                    {detay.bolumler.map((bolum) => (
                        <BolumKart key={bolum.bolumId} bolum={bolum} />
                    ))}
                </View>
            ) : (
                <EmptyState />
            )}
        </PanelLayout>
    );
}

function LoadingState() {
    return (
        <View style={styles.centerContainer}>
            <ActivityIndicator size="large" color="#2563eb" />
            <Text style={styles.loadingText}>Kurs detayı yükleniyor...</Text>
        </View>
    );
}

function ErrorState({
    mesaj,
    tekrarDene,
    guncelleniyorMu,
}: {
    mesaj: string;
    tekrarDene: () => void;
    guncelleniyorMu: boolean;
}) {
    return (
        <View style={styles.centerContainer}>
            <Text style={styles.errorTitle}>
                {guncelleniyorMu ? "Kurs Güncelleniyor" : "Bir sorun oluştu"}
            </Text>
            <Text style={styles.errorText}>{mesaj}</Text>

            <Pressable
                style={styles.primaryButton}
                onPress={
                    guncelleniyorMu
                        ? () => router.replace("/ogrenci/kesfet" as any)
                        : tekrarDene
                }
            >
                <Text style={styles.primaryButtonText}>
                    {guncelleniyorMu ? "Keşfete Dön" : "Tekrar Dene"}
                </Text>
            </Pressable>

            <Pressable
                style={styles.secondaryButton}
                onPress={() => router.back()}
            >
                <Text style={styles.secondaryButtonText}>Geri Dön</Text>
            </Pressable>
        </View>
    );
}

function InfoCard({ title, value }: { title: string; value: string }) {
    return (
        <View style={styles.infoCard}>
            <Text style={styles.infoValue}>{value}</Text>
            <Text style={styles.infoTitle}>{title}</Text>
        </View>
    );
}

function BolumKart({ bolum }: { bolum: MobileOgrenciKesfetBolum }) {
    return (
        <View style={styles.sectionCard}>
            <View style={styles.sectionTop}>
                <View style={styles.sectionNumber}>
                    <Text style={styles.sectionNumberText}>{bolum.siraNo}</Text>
                </View>

                <View style={styles.sectionInfo}>
                    <Text style={styles.sectionCardTitle} numberOfLines={1}>
                        {bolum.bolumAdi}
                    </Text>

                    <Text style={styles.sectionCardSubText}>
                        {bolum.dersSayisi} ders
                    </Text>
                </View>
            </View>

            <View style={styles.lessonList}>
                {bolum.dersler.length > 0 ? (
                    bolum.dersler.map((ders) => (
                        <DersSatiri key={ders.dersId} ders={ders} />
                    ))
                ) : (
                    <Text style={styles.noLessonText}>Bu bölümde ders bulunmuyor.</Text>
                )}
            </View>
        </View>
    );
}

function DersSatiri({ ders }: { ders: MobileOgrenciKesfetDers }) {
    return (
        <View style={styles.lessonRow}>
            <View style={styles.lessonNumber}>
                <Text style={styles.lessonNumberText}>{ders.siraNo}</Text>
            </View>

            <View style={styles.lessonInfo}>
                <Text style={styles.lessonTitle} numberOfLines={2}>
                    {ders.dersAdi}
                </Text>

                <Text style={styles.lessonMaterialText}>
                    {ders.materyalVarMi
                        ? `${ders.materyalSayisi} materyal`
                        : "Materyal yok"}
                </Text>
            </View>
        </View>
    );
}

function EmptyState() {
    return (
        <View style={styles.emptyCard}>
            <Text style={styles.emptyTitle}>İçerik bulunamadı</Text>
            <Text style={styles.emptyText}>
                Bu kurs için bölüm ve ders bilgisi henüz oluşturulmamış.
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
        marginBottom: 10,
    },
    primaryButtonText: {
        color: "#ffffff",
        fontWeight: "900",
    },
    secondaryButton: {
        backgroundColor: "#ffffff",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        paddingHorizontal: 18,
        paddingVertical: 12,
        borderRadius: 14,
    },
    secondaryButtonText: {
        color: "#334155",
        fontWeight: "900",
    },
    backButton: {
        alignSelf: "flex-start",
        backgroundColor: "#ffffff",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        borderRadius: 999,
        paddingHorizontal: 14,
        paddingVertical: 9,
        marginBottom: 14,
    },
    backButtonText: {
        fontSize: 13,
        fontWeight: "900",
        color: "#334155",
    },
    buttonPressed: {
        opacity: 0.75,
    },
    heroCard: {
        backgroundColor: "#ffffff",
        borderRadius: 22,
        padding: 16,
        borderWidth: 1,
        borderColor: "#e5e7eb",
        marginBottom: 22,
    },
    heroTop: {
        flexDirection: "row",
        alignItems: "center",
    },
    courseAvatar: {
        width: 54,
        height: 54,
        borderRadius: 18,
        backgroundColor: "#eff6ff",
        alignItems: "center",
        justifyContent: "center",
        marginRight: 12,
    },
    courseAvatarText: {
        fontSize: 22,
        fontWeight: "900",
        color: "#2563eb",
    },
    heroInfo: {
        flex: 1,
    },
    courseTitle: {
        fontSize: 20,
        fontWeight: "900",
        color: "#0f172a",
        lineHeight: 25,
    },
    instructorText: {
        marginTop: 4,
        fontSize: 13,
        fontWeight: "700",
        color: "#64748b",
    },
    descriptionText: {
        marginTop: 14,
        fontSize: 14,
        lineHeight: 21,
        color: "#475569",
    },
    categoryRow: {
        marginTop: 14,
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
        marginTop: 16,
        flexDirection: "row",
        flexWrap: "wrap",
        gap: 10,
    },
    infoCard: {
        flexBasis: "47%",
        flexGrow: 1,
        backgroundColor: "#f8fafc",
        borderRadius: 16,
        padding: 12,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    infoValue: {
        fontSize: 18,
        fontWeight: "900",
        color: "#0f172a",
    },
    infoTitle: {
        marginTop: 4,
        fontSize: 12,
        fontWeight: "800",
        color: "#64748b",
    },
    examBox: {
        marginTop: 14,
        backgroundColor: "#f8fafc",
        borderRadius: 16,
        padding: 12,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    examTitle: {
        fontSize: 13,
        fontWeight: "900",
        color: "#0f172a",
    },
    examText: {
        marginTop: 5,
        fontSize: 13,
        lineHeight: 19,
        color: "#64748b",
    },
    enrollButton: {
        marginTop: 16,
        minHeight: 48,
        backgroundColor: "#2563eb",
        borderRadius: 16,
        alignItems: "center",
        justifyContent: "center",
    },
    enrollButtonDisabled: {
        opacity: 0.6,
    },
    enrollButtonText: {
        color: "#ffffff",
        fontSize: 14,
        fontWeight: "900",
    },
    sectionHeader: {
        marginBottom: 14,
    },
    sectionTitle: {
        fontSize: 20,
        fontWeight: "900",
        color: "#0f172a",
    },
    sectionSubText: {
        marginTop: 4,
        fontSize: 13,
        fontWeight: "700",
        color: "#64748b",
    },
    sectionList: {
        gap: 12,
    },
    sectionCard: {
        backgroundColor: "#ffffff",
        borderRadius: 20,
        padding: 15,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    sectionTop: {
        flexDirection: "row",
        alignItems: "center",
    },
    sectionNumber: {
        width: 42,
        height: 42,
        borderRadius: 15,
        backgroundColor: "#eff6ff",
        alignItems: "center",
        justifyContent: "center",
        marginRight: 11,
    },
    sectionNumberText: {
        fontSize: 15,
        fontWeight: "900",
        color: "#2563eb",
    },
    sectionInfo: {
        flex: 1,
    },
    sectionCardTitle: {
        fontSize: 16,
        fontWeight: "900",
        color: "#0f172a",
    },
    sectionCardSubText: {
        marginTop: 3,
        fontSize: 12,
        fontWeight: "700",
        color: "#64748b",
    },
    lessonList: {
        marginTop: 12,
        gap: 8,
    },
    lessonRow: {
        flexDirection: "row",
        alignItems: "center",
        backgroundColor: "#f8fafc",
        borderRadius: 15,
        padding: 10,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    lessonNumber: {
        width: 32,
        height: 32,
        borderRadius: 12,
        backgroundColor: "#ffffff",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        alignItems: "center",
        justifyContent: "center",
        marginRight: 10,
    },
    lessonNumberText: {
        fontSize: 12,
        fontWeight: "900",
        color: "#64748b",
    },
    lessonInfo: {
        flex: 1,
    },
    lessonTitle: {
        fontSize: 13,
        fontWeight: "800",
        color: "#334155",
        lineHeight: 18,
    },
    lessonMaterialText: {
        marginTop: 3,
        fontSize: 11,
        fontWeight: "800",
        color: "#64748b",
    },
    noLessonText: {
        fontSize: 13,
        fontWeight: "700",
        color: "#64748b",
        textAlign: "center",
        paddingVertical: 10,
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
