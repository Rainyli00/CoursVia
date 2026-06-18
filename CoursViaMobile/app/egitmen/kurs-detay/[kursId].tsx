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
import { EGITMEN_MENU_ITEMS } from "@/app/_shared/panelMenus";
import { api } from "@/src/api/client";
import type {
    MobileEgitmenBolumItem,
    MobileEgitmenDersItem,
    MobileEgitmenKursDetayResponse,
    MobileEgitmenKursYorumItem,
} from "@/src/types/egitmen";

// Eğitmen kurs detay ekranı.
// Kursun özetini, kategorilerini, sınav bilgisini, son yorumlarını,
// bölümlerini, derslerini ve ders materyallerini gösterir.
export default function EgitmenKursDetayScreen() {
    const params = useLocalSearchParams<{
        kursId?: string | string[];
    }>();

    // Kurs detay cevabı tek state'te tutulur; üst özet, sınav, yorum ve dersler buradan okunur.
    const [detay, setDetay] = useState<MobileEgitmenKursDetayResponse | null>(
        null
    );

    // İlk yükleme, pull-to-refresh ve hata ekranı ayrı ayrı yönetilir.
    const [yukleniyor, setYukleniyor] = useState(true);
    const [yenileniyor, setYenileniyor] = useState(false);
    const [hata, setHata] = useState<string | null>(null);

    // Expo Router parametresi string gelebileceği için güvenli sayıya çevrilir.
    const kursId = useMemo(() => {
        const rawValue = Array.isArray(params.kursId)
            ? params.kursId[0]
            : params.kursId;

        const id = Number(rawValue);

        return Number.isFinite(id) && id > 0 ? id : null;
    }, [params.kursId]);

    // Geçerli kursId varsa detay yüklenir, yoksa kullanıcıya hata ekranı gösterilir.
    useEffect(() => {
        if (!kursId) {
            setYukleniyor(false);
            setHata("Geçersiz kurs bilgisi.");
            return;
        }

        kursDetayGetir();
    }, [kursId]);

    // Kurs detayını API'den alır; refresh sırasında tam ekran loading göstermez.
    async function kursDetayGetir(refreshMi = false) {
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

            const response = await api.get<MobileEgitmenKursDetayResponse>(
                `/api/mobile/egitmen/kurslarim/${kursId}`
            );

            setDetay(response.data);
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Kurs detayı alınırken hata oluştu.";

            setHata(mesaj);
            Alert.alert("Hata", mesaj);
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
            />
        );
    }

    return (
        <PanelLayout
            title="Kurs Detayı"
            subtitle="Kursun durumunu, sınavını, yorumlarını ve içeriğini buradan inceleyebilirsin."
            refreshing={yenileniyor}
            onRefresh={() => kursDetayGetir(true)}
            menuItems={EGITMEN_MENU_ITEMS}
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

                        <View style={styles.statusBadge}>
                            <Text style={styles.statusBadgeText}>{detay.durumAdi}</Text>
                        </View>
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

                <View style={styles.metaGrid}>
                    <InfoCard title="Öğrenci" value={String(detay.ogrenciSayisi)} />

                    <InfoCard
                        title="Tamamlayan"
                        value={String(detay.tamamlayanOgrenciSayisi)}
                    />

                    <InfoCard title="Bölüm" value={String(detay.bolumSayisi)} />

                    <InfoCard title="Ders" value={String(detay.dersSayisi)} />

                    <InfoCard
                        title="Puan"
                        value={
                            detay.degerlendirmeSayisi > 0
                                ? String(detay.ortalamaPuan)
                                : "-"
                        }
                    />
                </View>

                <View style={styles.examBox}>
                    <Text style={styles.examTitle}>Sınav Bilgisi</Text>

                    {detay.sinavVarMi ? (
                        <>
                            <Text style={styles.examName}>
                                {detay.sinavAdi || "Kurs Sınavı"}
                            </Text>

                            <View style={styles.examInfoGrid}>
                                <ExamInfoCard
                                    title="Soru Sayısı"
                                    value={detay.sinavSoruSayisi?.toString() ?? "-"}
                                />

                                <ExamInfoCard
                                    title="Süre"
                                    value={
                                        detay.sinavSureDakika
                                            ? `${detay.sinavSureDakika} dk`
                                            : "-"
                                    }
                                />

                                <ExamInfoCard
                                    title="Geçme Notu"
                                    value={detay.sinavGecmeNotu?.toString() ?? "-"}
                                />
                            </View>
                        </>
                    ) : (
                        <Text style={styles.examText}>
                            Bu kurs için sınav tanımlanmamış.
                        </Text>
                    )}
                </View>
            </View>

            <View style={styles.sectionHeader}>
                <Text style={styles.sectionTitle}>Son Yorumlar</Text>
                <Text style={styles.sectionSubText}>
                    Kursa yapılan son değerlendirmeler
                </Text>
            </View>

            {(detay.sonYorumlar ?? []).length > 0 ? (
                <View style={styles.commentList}>
                    {detay.sonYorumlar.map((yorum) => (
                        <YorumKart key={yorum.degerlendirmeId} yorum={yorum} />
                    ))}
                </View>
            ) : (
                <SmallEmptyState text="Bu kurs için henüz yorum bulunmuyor." />
            )}

            <View style={styles.sectionHeaderAlt}>
                <Text style={styles.sectionTitle}>Bölümler ve Dersler</Text>
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
                <SmallEmptyState text="Bu kurs için bölüm ve ders bulunmuyor." />
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

// Detay alınamazsa tekrar deneme ve geri dönme aksiyonlarını gösterir.
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

            <Pressable style={styles.secondaryButton} onPress={() => router.back()}>
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

// Sınav alanındaki soru, süre ve geçme notu kartları.
function ExamInfoCard({ title, value }: { title: string; value: string }) {
    return (
        <View style={styles.examInfoCard}>
            <Text style={styles.examInfoValue}>{value}</Text>
            <Text style={styles.examInfoTitle}>{title}</Text>
        </View>
    );
}

// Kursa gelen son değerlendirme yorumunu gösterir.
function YorumKart({ yorum }: { yorum: MobileEgitmenKursYorumItem }) {
    return (
        <View style={styles.commentCard}>
            <View style={styles.commentTop}>
                <View style={styles.commentAvatar}>
                    <Text style={styles.commentAvatarText}>
                        {yorum.ogrenciAdSoyad.substring(0, 1).toUpperCase()}
                    </Text>
                </View>

                <View style={styles.commentInfo}>
                    <Text style={styles.commentName} numberOfLines={1}>
                        {yorum.ogrenciAdSoyad}
                    </Text>

                    <Text style={styles.commentDate}>
                        {tarihFormatla(yorum.degerlendirmeTarihi)}
                    </Text>
                </View>

                <View style={styles.commentRateBadge}>
                    <Text style={styles.commentRateText}>{yorum.puan}/5</Text>
                </View>
            </View>

            <Text style={styles.commentText}>{yorum.yorumMetni}</Text>
        </View>
    );
}

// Kurs içeriğindeki tek bölüm ve altındaki ders listesini gösterir.
function BolumKart({ bolum }: { bolum: MobileEgitmenBolumItem }) {
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

// Dersin aktiflik durumunu ve materyal özetini gösteren satır.
function DersSatiri({ ders }: { ders: MobileEgitmenDersItem }) {
    return (
        <View style={styles.lessonCard}>
            <View style={styles.lessonRow}>
                <View style={styles.lessonNumber}>
                    <Text style={styles.lessonNumberText}>{ders.siraNo}</Text>
                </View>

                <View style={styles.lessonInfo}>
                    <Text style={styles.lessonTitle} numberOfLines={2}>
                        {ders.dersAdi}
                    </Text>

                    <View style={styles.lessonMetaRow}>
                        <Text
                            style={[
                                styles.lessonStatus,
                                ders.aktifMi
                                    ? styles.lessonStatusActive
                                    : styles.lessonStatusPassive,
                            ]}
                        >
                            {ders.aktifMi ? "Aktif" : "Pasif"}
                        </Text>

                        <Text style={styles.lessonMaterialText}>
                            {ders.materyalVarMi
                                ? `${ders.materyalSayisi} materyal`
                                : "Materyal yok"}
                        </Text>
                    </View>
                </View>
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

// Yorum veya bölüm bulunmadığında küçük boş durum kartı.
function SmallEmptyState({ text }: { text: string }) {
    return (
        <View style={styles.smallEmptyCard}>
            <Text style={styles.smallEmptyText}>{text}</Text>
        </View>
    );
}

// Geçerli tarihse Türkçe kısa tarih formatına çevirir.
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
    statusBadge: {
        alignSelf: "flex-start",
        marginTop: 7,
        backgroundColor: "#eff6ff",
        borderWidth: 1,
        borderColor: "#bfdbfe",
        borderRadius: 999,
        paddingHorizontal: 10,
        paddingVertical: 5,
    },
    statusBadgeText: {
        fontSize: 11,
        fontWeight: "900",
        color: "#2563eb",
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
    examName: {
        marginTop: 5,
        fontSize: 14,
        fontWeight: "800",
        color: "#334155",
    },
    examText: {
        marginTop: 5,
        fontSize: 13,
        lineHeight: 19,
        color: "#64748b",
    },
    examInfoGrid: {
        marginTop: 12,
        flexDirection: "row",
        gap: 8,
    },
    examInfoCard: {
        flex: 1,
        backgroundColor: "#ffffff",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        borderRadius: 14,
        padding: 10,
    },
    examInfoValue: {
        fontSize: 15,
        fontWeight: "900",
        color: "#2563eb",
    },
    examInfoTitle: {
        marginTop: 3,
        fontSize: 11,
        fontWeight: "800",
        color: "#64748b",
    },
    sectionHeader: {
        marginBottom: 14,
    },
    sectionHeaderAlt: {
        marginTop: 22,
        marginBottom: 14,
    },
    sectionTitle: {
        fontSize: 20,
        fontWeight: "900",
        color: "#0f172a",
    },
    sectionSubText: {
        marginTop: 3,
        fontSize: 12,
        fontWeight: "700",
        color: "#64748b",
    },
    commentList: {
        gap: 10,
        marginBottom: 22,
    },
    commentCard: {
        backgroundColor: "#ffffff",
        borderRadius: 18,
        padding: 14,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    commentTop: {
        flexDirection: "row",
        alignItems: "center",
    },
    commentAvatar: {
        width: 42,
        height: 42,
        borderRadius: 15,
        backgroundColor: "#eff6ff",
        alignItems: "center",
        justifyContent: "center",
        marginRight: 11,
    },
    commentAvatarText: {
        fontSize: 17,
        fontWeight: "900",
        color: "#2563eb",
    },
    commentInfo: {
        flex: 1,
    },
    commentName: {
        fontSize: 15,
        fontWeight: "900",
        color: "#0f172a",
    },
    commentDate: {
        marginTop: 3,
        fontSize: 12,
        fontWeight: "700",
        color: "#64748b",
    },
    commentRateBadge: {
        marginLeft: 10,
        backgroundColor: "#eff6ff",
        borderWidth: 1,
        borderColor: "#bfdbfe",
        borderRadius: 999,
        paddingHorizontal: 10,
        paddingVertical: 6,
    },
    commentRateText: {
        fontSize: 12,
        fontWeight: "900",
        color: "#2563eb",
    },
    commentText: {
        marginTop: 10,
        fontSize: 13,
        lineHeight: 19,
        color: "#475569",
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
    lessonMetaRow: {
        marginTop: 5,
        flexDirection: "row",
        alignItems: "center",
        flexWrap: "wrap",
        gap: 8,
    },
    lessonStatus: {
        fontSize: 11,
        fontWeight: "900",
    },
    lessonStatusActive: {
        color: "#16a34a",
    },
    lessonStatusPassive: {
        color: "#dc2626",
    },
    lessonMaterialText: {
        fontSize: 11,
        fontWeight: "800",
        color: "#64748b",
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
    smallEmptyCard: {
        backgroundColor: "#ffffff",
        borderRadius: 18,
        padding: 16,
        borderWidth: 1,
        borderColor: "#e5e7eb",
        alignItems: "center",
        marginBottom: 22,
    },
    smallEmptyText: {
        fontSize: 13,
        fontWeight: "700",
        color: "#64748b",
        textAlign: "center",
        lineHeight: 19,
    },
});
