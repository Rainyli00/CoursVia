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
    MobileOgrenciBolumItem,
    MobileOgrenciDersItem,
    MobileOgrenciKursDetayResponse,
} from "@/src/types/ogrenci";

// Öğrenci kurs detay ekranı.
// Kurslarım listesinden gelen kursKayitId ile detay API'sini çağırır.
// Bu ekranda kategori, materyal adı ve tipi ders altında gösterilir.
export default function OgrenciKursDetayScreen() {
    const params = useLocalSearchParams<{
        kursKayitId?: string | string[];
    }>();

    // Kurslarım detay cevabı tek state'te tutulur; ilerleme, yorum ve dersler buradan okunur.
    const [detay, setDetay] = useState<MobileOgrenciKursDetayResponse | null>(
        null
    );

    // Güncelleniyor hatası ayrı tutulur; hata ekranında farklı yönlendirme gösterilir.
    const [yukleniyor, setYukleniyor] = useState(true);
    const [yenileniyor, setYenileniyor] = useState(false);
    const [hata, setHata] = useState<string | null>(null);
    const [guncelleniyorHatasi, setGuncelleniyorHatasi] = useState(false);

    // Expo Router parametresi string gelebileceği için güvenli sayıya çevrilir.
    const kursKayitId = useMemo(() => {
        const rawValue = Array.isArray(params.kursKayitId)
            ? params.kursKayitId[0]
            : params.kursKayitId;

        const id = Number(rawValue);

        return Number.isFinite(id) && id > 0 ? id : null;
    }, [params.kursKayitId]);

    // Geçerli kursKayitId varsa detay yüklenir, yoksa kullanıcıya hata ekranı gösterilir.
    useEffect(() => {
        if (!kursKayitId) {
            setYukleniyor(false);
            setHata("Geçersiz kurs kaydı.");
            return;
        }

        kursDetayGetir();
    }, [kursKayitId]);

    // Kayıtlı kurs detayını API'den getirir; refresh sırasında tam ekran loading göstermez.
    async function kursDetayGetir(refreshMi = false) {
        if (!kursKayitId) {
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

            const response = await api.get<MobileOgrenciKursDetayResponse>(
                `/api/mobile/ogrenci/kurslarim/${kursKayitId}`
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

    if (yukleniyor) {
        return <LoadingState />;
    }

    if (hata || !detay) {
        return (
            <ErrorState
                mesaj={hata || "Kurs detayı bulunamadı."}
                tekrarDene={() => kursDetayGetir()}
                guncelleniyorMu={guncelleniyorHatasi}
            />
        );
    }

    return (
        <PanelLayout
            title="Kurs Detayı"
            subtitle="Kurs ilerlemeni, bölümleri ve ders durumlarını buradan takip edebilirsin."
            refreshing={yenileniyor}
            onRefresh={() => kursDetayGetir(true)}
            menuItems={OGRENCI_MENU_ITEMS}
            activeMenuKey="kurslarim"
        >
            <Pressable
                onPress={() => router.back()}
                style={({ pressed }) => [
                    styles.backButton,
                    pressed ? styles.buttonPressed : null,
                ]}
            >
                <Text style={styles.backButtonText}>← Kurslarıma Dön</Text>
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

                {detay.kategoriler && detay.kategoriler.length > 0 ? (
                    <View style={styles.categoryRow}>
                        {detay.kategoriler.map((kategori) => (
                            <View key={kategori} style={styles.categoryChip}>
                                <Text style={styles.categoryChipText} numberOfLines={1}>
                                    {kategori}
                                </Text>
                            </View>
                        ))}
                    </View>
                ) : null}

                <View style={styles.progressBox}>
                    <View style={styles.progressTop}>
                        <Text style={styles.progressLabel}>Genel İlerleme</Text>
                        <Text style={styles.progressPercent}>%{detay.ilerlemeYuzdesi}</Text>
                    </View>

                    <View style={styles.progressTrack}>
                        <View
                            style={[
                                styles.progressFill,
                                {
                                    width: `${detay.ilerlemeYuzdesi}%`,
                                },
                            ]}
                        />
                    </View>
                </View>

                <View style={styles.metaGrid}>
                    <InfoCard
                        title="Toplam Ders"
                        value={String(detay.toplamDersSayisi)}
                    />

                    <InfoCard
                        title="Tamamlanan"
                        value={String(detay.tamamlananDersSayisi)}
                    />

                    <InfoCard
                        title="Durum"
                        value={detay.kursTamamlandiMi ? "Tamamlandı" : "Devam Ediyor"}
                    />

                    <InfoCard
                        title="Puan"
                        value={detay.kendiPuan ? `${detay.kendiPuan}/5` : "-"}
                    />
                </View>

                {detay.degerlendirmeVarMi ? (
                    <View style={styles.reviewBox}>
                        <Text style={styles.reviewTitle}>Senin Değerlendirmen</Text>

                        <Text style={styles.reviewText}>
                            Puan: {detay.kendiPuan ?? "-"} / 5
                        </Text>

                        {detay.kendiYorumMetni ? (
                            <Text style={styles.reviewComment}>{detay.kendiYorumMetni}</Text>
                        ) : null}
                    </View>
                ) : null}
            </View>

            <View style={styles.sectionHeader}>
                <Text style={styles.sectionTitle}>Bölümler ve Dersler</Text>
                <Text style={styles.sectionSubText}>
                    {detay.bolumler.length} bölüm listeleniyor
                </Text>
            </View>

            {detay.bolumler.length > 0 ? (
                <View style={styles.bolumList}>
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

// Detay verisi gelene kadar gösterilen tam ekran loading.
function LoadingState() {
    return (
        <View style={styles.centerContainer}>
            <ActivityIndicator size="large" color="#2563eb" />
            <Text style={styles.loadingText}>Kurs detayı yükleniyor...</Text>
        </View>
    );
}

// Detay alınamazsa tekrar deneme ya da kurslarım sayfasına dönüş aksiyonu gösterir.
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
                        ? () => router.replace("/ogrenci/kurslarim" as any)
                        : tekrarDene
                }
            >
                <Text style={styles.primaryButtonText}>
                    {guncelleniyorMu ? "Kurslarıma Dön" : "Tekrar Dene"}
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

// Hero alanındaki küçük kurs metrik kartı.
function InfoCard({ title, value }: { title: string; value: string }) {
    return (
        <View style={styles.infoCard}>
            <Text style={styles.infoValue}>{value}</Text>
            <Text style={styles.infoTitle}>{title}</Text>
        </View>
    );
}

// Kurs içeriğindeki tek bölüm ve altındaki ders listesini gösterir.
function BolumKart({ bolum }: { bolum: MobileOgrenciBolumItem }) {
    return (
        <View style={styles.bolumCard}>
            <View style={styles.bolumTop}>
                <View style={styles.bolumNumber}>
                    <Text style={styles.bolumNumberText}>{bolum.siraNo}</Text>
                </View>

                <View style={styles.bolumInfo}>
                    <Text style={styles.bolumTitle} numberOfLines={1}>
                        {bolum.bolumAdi}
                    </Text>

                    <Text style={styles.bolumSubText}>
                        {bolum.tamamlananDersSayisi}/{bolum.toplamDersSayisi} ders tamamlandı
                    </Text>
                </View>

                <Text style={styles.bolumPercent}>%{bolum.ilerlemeYuzdesi}</Text>
            </View>

            <View style={styles.bolumProgressTrack}>
                <View
                    style={[
                        styles.bolumProgressFill,
                        {
                            width: `${bolum.ilerlemeYuzdesi}%`,
                        },
                    ]}
                />
            </View>

            <View style={styles.dersList}>
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

// Dersin tamamlanma durumunu ve materyal listesini gösteren satır.
function DersSatiri({ ders }: { ders: MobileOgrenciDersItem }) {
    return (
        <View style={styles.lessonCard}>
            <View style={styles.lessonRow}>
                <View
                    style={[
                        styles.lessonStatusIcon,
                        ders.tamamlandiMi ? styles.lessonStatusIconDone : null,
                    ]}
                >
                    <Text
                        style={[
                            styles.lessonStatusIconText,
                            ders.tamamlandiMi ? styles.lessonStatusIconTextDone : null,
                        ]}
                    >
                        {ders.tamamlandiMi ? "✓" : ders.siraNo}
                    </Text>
                </View>

                <View style={styles.lessonInfo}>
                    <Text
                        style={[
                            styles.lessonTitle,
                            ders.tamamlandiMi ? styles.lessonTitleDone : null,
                        ]}
                        numberOfLines={2}
                    >
                        {ders.dersAdi}
                    </Text>

                    <Text style={styles.lessonMaterialText}>
                        {ders.materyalVarMi
                            ? `${ders.materyalSayisi} materyal`
                            : "Materyal yok"}
                    </Text>
                </View>

                <Text
                    style={[
                        styles.lessonStatusText,
                        ders.tamamlandiMi ? styles.lessonStatusTextDone : null,
                    ]}
                >
                    {ders.tamamlandiMi ? "Tamamlandı" : "Bekliyor"}
                </Text>
            </View>

            {ders.materyaller && ders.materyaller.length > 0 ? (
                <View style={styles.materialList}>
                    {ders.materyaller.map((materyal) => (
                        <View key={materyal.materyalId} style={styles.materialItem}>
                            <Text style={styles.materialTitle} numberOfLines={1}>
                                {materyal.baslik}
                            </Text>

                            <Text style={styles.materialType} numberOfLines={1}>
                                {materyal.materyalTipAdi}
                            </Text>
                        </View>
                    ))}
                </View>
            ) : null}
        </View>
    );
}

// Kurs içinde bölüm/ders yoksa gösterilen boş durum.
function EmptyState() {
    return (
        <View style={styles.emptyCard}>
            <Text style={styles.emptyTitle}>Bölüm bulunamadı</Text>

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
    progressBox: {
        marginTop: 16,
    },
    progressTop: {
        flexDirection: "row",
        alignItems: "center",
        justifyContent: "space-between",
    },
    progressLabel: {
        fontSize: 13,
        fontWeight: "900",
        color: "#475569",
    },
    progressPercent: {
        fontSize: 15,
        fontWeight: "900",
        color: "#2563eb",
    },
    progressTrack: {
        height: 9,
        backgroundColor: "#e2e8f0",
        borderRadius: 999,
        marginTop: 9,
        overflow: "hidden",
    },
    progressFill: {
        height: "100%",
        backgroundColor: "#22c55e",
        borderRadius: 999,
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
    reviewBox: {
        marginTop: 14,
        backgroundColor: "#f8fafc",
        borderRadius: 16,
        padding: 12,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    reviewTitle: {
        fontSize: 13,
        fontWeight: "900",
        color: "#0f172a",
    },
    reviewText: {
        marginTop: 6,
        fontSize: 13,
        fontWeight: "800",
        color: "#334155",
    },
    reviewComment: {
        marginTop: 6,
        fontSize: 13,
        lineHeight: 19,
        color: "#64748b",
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
    bolumList: {
        gap: 12,
    },
    bolumCard: {
        backgroundColor: "#ffffff",
        borderRadius: 20,
        padding: 15,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    bolumTop: {
        flexDirection: "row",
        alignItems: "center",
    },
    bolumNumber: {
        width: 42,
        height: 42,
        borderRadius: 15,
        backgroundColor: "#eff6ff",
        alignItems: "center",
        justifyContent: "center",
        marginRight: 11,
    },
    bolumNumberText: {
        fontSize: 15,
        fontWeight: "900",
        color: "#2563eb",
    },
    bolumInfo: {
        flex: 1,
    },
    bolumTitle: {
        fontSize: 16,
        fontWeight: "900",
        color: "#0f172a",
    },
    bolumSubText: {
        marginTop: 3,
        fontSize: 12,
        fontWeight: "700",
        color: "#64748b",
    },
    bolumPercent: {
        marginLeft: 10,
        fontSize: 15,
        fontWeight: "900",
        color: "#2563eb",
    },
    bolumProgressTrack: {
        height: 8,
        backgroundColor: "#e2e8f0",
        borderRadius: 999,
        marginTop: 12,
        overflow: "hidden",
    },
    bolumProgressFill: {
        height: "100%",
        backgroundColor: "#2563eb",
        borderRadius: 999,
    },
    dersList: {
        marginTop: 12,
        gap: 8,
    },
    lessonCard: {
        backgroundColor: "#f8fafc",
        borderRadius: 15,
        borderWidth: 1,
        borderColor: "#e5e7eb",
        overflow: "hidden",
    },
    lessonRow: {
        flexDirection: "row",
        alignItems: "center",
        padding: 10,
    },
    lessonStatusIcon: {
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
    lessonStatusIconDone: {
        backgroundColor: "#dcfce7",
        borderColor: "#bbf7d0",
    },
    lessonStatusIconText: {
        fontSize: 12,
        fontWeight: "900",
        color: "#64748b",
    },
    lessonStatusIconTextDone: {
        color: "#16a34a",
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
    lessonTitleDone: {
        color: "#0f172a",
    },
    lessonMaterialText: {
        marginTop: 3,
        fontSize: 11,
        fontWeight: "800",
        color: "#64748b",
    },
    lessonStatusText: {
        marginLeft: 8,
        fontSize: 11,
        fontWeight: "900",
        color: "#94a3b8",
    },
    lessonStatusTextDone: {
        color: "#16a34a",
    },
    materialList: {
        borderTopWidth: 1,
        borderTopColor: "#e5e7eb",
        paddingHorizontal: 10,
        paddingBottom: 10,
        gap: 7,
    },
    materialItem: {
        backgroundColor: "#ffffff",
        borderRadius: 12,
        borderWidth: 1,
        borderColor: "#e5e7eb",
        paddingHorizontal: 10,
        paddingVertical: 8,
    },
    materialTitle: {
        fontSize: 12,
        fontWeight: "900",
        color: "#0f172a",
    },
    materialType: {
        marginTop: 2,
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
