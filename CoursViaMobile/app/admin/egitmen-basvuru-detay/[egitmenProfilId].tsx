import { router, useLocalSearchParams } from "expo-router";
import { useEffect, useMemo, useState } from "react";
import {
    ActivityIndicator,
    Alert,
    Modal,
    Pressable,
    StyleSheet,
    Text,
    TextInput,
    View,
} from "react-native";

import PanelLayout from "@/app/_shared/PanelLayout";
import { ADMIN_MENU_ITEMS } from "@/app/_shared/panelMenus";
import { api } from "@/src/api/client";
import type {
    MobileAdminEgitmenBasvuruDetayResponse,
    MobileAdminIslemResponse,
} from "@/src/types/admin";

// Admin eğitmen başvuru detay ekranı.
// Durum uygunsa onayla/reddet işlemi yapılabilir.
// Karar verilmiş başvurularda ekstra "Karar Verilmiş" kartı gösterilmez.
export default function AdminEgitmenBasvuruDetayScreen() {
    const params = useLocalSearchParams<{
        egitmenProfilId?: string | string[];
    }>();

    // Başvuru detay cevabı tek state'te tutulur; karar ve profil alanları buradan beslenir.
    const [detay, setDetay] =
        useState<MobileAdminEgitmenBasvuruDetayResponse | null>(null);

    // İlk yükleme, pull-to-refresh ve hata ekranı ayrı ayrı yönetilir.
    const [yukleniyor, setYukleniyor] = useState(true);
    const [yenileniyor, setYenileniyor] = useState(false);
    const [hata, setHata] = useState<string | null>(null);

    // Onay/red sırasında aksiyon butonları kilitlenir.
    const [islemYapiliyor, setIslemYapiliyor] = useState(false);

    // Red kararında açıklama zorunlu olduğu için modal form state'i ayrı tutulur.
    const [redModalAcik, setRedModalAcik] = useState(false);
    const [redAciklama, setRedAciklama] = useState("");

    // Expo Router parametresi string gelebileceği için güvenli sayıya çevrilir.
    const egitmenProfilId = useMemo(() => {
        const rawValue = Array.isArray(params.egitmenProfilId)
            ? params.egitmenProfilId[0]
            : params.egitmenProfilId;

        const id = Number(rawValue);

        return Number.isFinite(id) && id > 0 ? id : null;
    }, [params.egitmenProfilId]);

    // Sadece bekleyen veya düzeltme istenen başvurularda karar aksiyonları gösterilir.
    const kararVerilebilirMi = detay?.durumId === 4 || detay?.durumId === 7;

    // Geçerli başvuru id varsa detay yüklenir, yoksa hata ekranı gösterilir.
    useEffect(() => {
        if (!egitmenProfilId) {
            setYukleniyor(false);
            setHata("Geçersiz başvuru bilgisi.");
            return;
        }

        detayGetir();
    }, [egitmenProfilId]);

    // Başvuru detayını API'den getirir; refresh sırasında tam ekran loading göstermez.
    async function detayGetir(refreshMi = false) {
        if (!egitmenProfilId) {
            return;
        }

        try {
            if (refreshMi) {
                setYenileniyor(true);
            } else {
                setYukleniyor(true);
            }

            setHata(null);

            const response = await api.get<MobileAdminEgitmenBasvuruDetayResponse>(
                `/api/mobile/admin/egitmen-basvurulari/${egitmenProfilId}`
            );

            setDetay(response.data);
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Başvuru detayı alınırken hata oluştu.";

            setHata(mesaj);
            Alert.alert("Hata", mesaj);
        } finally {
            setYukleniyor(false);
            setYenileniyor(false);
        }
    }

    // Onay işlemi rol değişikliği yaratacağı için kullanıcıdan açık onay alınır.
    function onayOnayiAl() {
        if (!detay || !egitmenProfilId || !kararVerilebilirMi) {
            return;
        }

        Alert.alert(
            "Başvuruyu Onayla",
            `"${detay.adSoyad}" kullanıcısının eğitmen başvurusunu onaylamak istiyor musun?`,
            [
                {
                    text: "Vazgeç",
                    style: "cancel",
                },
                {
                    text: "Onayla",
                    onPress: basvuruOnayla,
                },
            ]
        );
    }

    // Başvuruyu onaylar ve güncel durumun ekrana yansıması için detayı yeniler.
    async function basvuruOnayla() {
        if (!egitmenProfilId) {
            return;
        }

        try {
            setIslemYapiliyor(true);

            const response = await api.post<MobileAdminIslemResponse>(
                `/api/mobile/admin/egitmen-basvurulari/${egitmenProfilId}/onayla`,
                {
                    aciklama: null,
                }
            );

            Alert.alert("Başarılı", response.data.mesaj || "Başvuru onaylandı.");

            await detayGetir(true);
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Başvuru onaylanırken hata oluştu.";

            Alert.alert("Hata", mesaj);
        } finally {
            setIslemYapiliyor(false);
        }
    }

    // Red modalı sadece karar verilebilir başvurularda açılır.
    function redModalAc() {
        if (!kararVerilebilirMi) {
            return;
        }

        setRedAciklama("");
        setRedModalAcik(true);
    }

    // Red açıklaması zorunludur; başarılı olursa detay yenilenir.
    async function basvuruReddet() {
        if (!egitmenProfilId) {
            return;
        }

        const temizAciklama = redAciklama.trim();

        if (!temizAciklama) {
            Alert.alert("Uyarı", "Red açıklaması zorunludur.");
            return;
        }

        try {
            setIslemYapiliyor(true);

            const response = await api.post<MobileAdminIslemResponse>(
                `/api/mobile/admin/egitmen-basvurulari/${egitmenProfilId}/reddet`,
                {
                    aciklama: temizAciklama,
                }
            );

            setRedModalAcik(false);
            setRedAciklama("");

            Alert.alert("Başarılı", response.data.mesaj || "Başvuru reddedildi.");

            await detayGetir(true);
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Başvuru reddedilirken hata oluştu.";

            Alert.alert("Hata", mesaj);
        } finally {
            setIslemYapiliyor(false);
        }
    }

    if (yukleniyor) {
        return <LoadingState />;
    }

    if (hata || !detay) {
        return (
            <ErrorState
                mesaj={hata || "Başvuru detayı bulunamadı."}
                tekrarDene={() => detayGetir()}
            />
        );
    }

    return (
        <>
            <PanelLayout
                title="Başvuru Detayı"
                subtitle="Eğitmen başvurusunu inceleyebilirsin."
                refreshing={yenileniyor}
                onRefresh={() => detayGetir(true)}
                menuItems={ADMIN_MENU_ITEMS}
                activeMenuKey="egitmen-basvurulari"
            >
                <Pressable
                    onPress={() => router.back()}
                    style={({ pressed }) => [
                        styles.backButton,
                        pressed ? styles.buttonPressed : null,
                    ]}
                >
                    <Text style={styles.backButtonText}>← Başvurulara Dön</Text>
                </Pressable>

                <View style={styles.heroCard}>
                    <View style={styles.heroTop}>
                        <View style={styles.avatar}>
                            <Text style={styles.avatarText}>
                                {detay.adSoyad.substring(0, 1).toUpperCase()}
                            </Text>
                        </View>

                        <View style={styles.heroInfo}>
                            <Text style={styles.name}>{detay.adSoyad}</Text>

                            <Text style={styles.email} numberOfLines={1}>
                                {detay.eposta}
                            </Text>

                            <View style={styles.statusBadge}>
                                <Text style={styles.statusBadgeText}>{detay.durumAdi}</Text>
                            </View>
                        </View>
                    </View>

                    <View style={styles.infoGrid}>
                        <InfoCard
                            title="Deneyim"
                            value={
                                detay.deneyimYili !== null
                                    ? `${detay.deneyimYili} yıl`
                                    : "-"
                            }
                        />

                        <InfoCard
                            title="Son İşlem"
                            value={
                                detay.sonIslemTarihi
                                    ? tarihFormatla(detay.sonIslemTarihi)
                                    : "-"
                            }
                        />
                    </View>

                    {detay.branslar.length > 0 ? (
                        <View style={styles.branchArea}>
                            <Text style={styles.blockTitle}>Branşlar</Text>

                            <View style={styles.branchRow}>
                                {detay.branslar.map((brans) => (
                                    <View key={brans} style={styles.branchChip}>
                                        <Text style={styles.branchChipText}>{brans}</Text>
                                    </View>
                                ))}
                            </View>
                        </View>
                    ) : null}
                </View>

                <View style={styles.detailCard}>
                    <Text style={styles.detailTitle}>Profil Bilgileri</Text>

                    <DetailBlock
                        label="Uzmanlık Alanı"
                        value={detay.uzmanlikAlani || "Bilgi yok"}
                    />

                    <DetailBlock
                        label="Biyografi"
                        value={detay.biyografi || "Bilgi yok"}
                    />

                    <DetailBlock
                        label="Website"
                        value={detay.websiteUrl || "Bilgi yok"}
                    />

                    <DetailBlock
                        label="Son Açıklama"
                        value={detay.aciklama || "Açıklama yok"}
                    />
                </View>

                {kararVerilebilirMi ? (
                    <View style={styles.actionCard}>
                        <Text style={styles.actionTitle}>Başvuru Kararı</Text>

                        <Text style={styles.actionText}>
                            Bu işlem kullanıcının eğitmen rolünü ve başvuru durumunu etkiler.
                        </Text>

                        <View style={styles.actionRow}>
                            <Pressable
                                disabled={islemYapiliyor}
                                onPress={onayOnayiAl}
                                style={({ pressed }) => [
                                    styles.approveButton,
                                    pressed && !islemYapiliyor
                                        ? styles.buttonPressed
                                        : null,
                                    islemYapiliyor ? styles.disabledButton : null,
                                ]}
                            >
                                {islemYapiliyor ? (
                                    <ActivityIndicator color="#ffffff" size="small" />
                                ) : (
                                    <Text style={styles.approveButtonText}>Onayla</Text>
                                )}
                            </Pressable>

                            <Pressable
                                disabled={islemYapiliyor}
                                onPress={redModalAc}
                                style={({ pressed }) => [
                                    styles.rejectButton,
                                    pressed && !islemYapiliyor
                                        ? styles.buttonPressed
                                        : null,
                                    islemYapiliyor ? styles.disabledButton : null,
                                ]}
                            >
                                <Text style={styles.rejectButtonText}>Reddet</Text>
                            </Pressable>
                        </View>
                    </View>
                ) : null}
            </PanelLayout>

            <RedModal
                visible={redModalAcik}
                value={redAciklama}
                setValue={setRedAciklama}
                close={() => setRedModalAcik(false)}
                submit={basvuruReddet}
                loading={islemYapiliyor}
            />
        </>
    );
}

// Detay verisi gelene kadar gösterilen tam ekran loading.
function LoadingState() {
    return (
        <View style={styles.centerContainer}>
            <ActivityIndicator size="large" color="#2563eb" />
            <Text style={styles.loadingText}>Başvuru detayı yükleniyor...</Text>
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

// Hero alanındaki küçük başvuru metrik kartı.
function InfoCard({ title, value }: { title: string; value: string }) {
    return (
        <View style={styles.infoCard}>
            <Text style={styles.infoValue}>{value}</Text>
            <Text style={styles.infoTitle}>{title}</Text>
        </View>
    );
}

// Profil bilgisindeki uzun metinli alanlar için kullanılır.
function DetailBlock({ label, value }: { label: string; value: string }) {
    return (
        <View style={styles.detailBlock}>
            <Text style={styles.detailLabel}>{label}</Text>
            <Text style={styles.detailValue}>{value}</Text>
        </View>
    );
}

// Başvuruyu reddetmek için zorunlu açıklama isteyen modal.
function RedModal({
    visible,
    value,
    setValue,
    close,
    submit,
    loading,
}: {
    visible: boolean;
    value: string;
    setValue: (value: string) => void;
    close: () => void;
    submit: () => void;
    loading: boolean;
}) {
    return (
        <Modal visible={visible} transparent animationType="fade" onRequestClose={close}>
            <View style={styles.modalRoot}>
                <Pressable style={styles.modalBackdrop} onPress={close} />

                <View style={styles.modalCard}>
                    <Text style={styles.modalTitle}>Başvuruyu Reddet</Text>

                    <Text style={styles.modalText}>
                        Red açıklaması zorunludur. Bu açıklama işlem kaydında görünür.
                    </Text>

                    <TextInput
                        value={value}
                        onChangeText={setValue}
                        placeholder="Red açıklaması yaz..."
                        style={styles.redInput}
                        multiline
                        textAlignVertical="top"
                    />

                    <View style={styles.modalActionRow}>
                        <Pressable
                            disabled={loading}
                            onPress={close}
                            style={({ pressed }) => [
                                styles.modalCancelButton,
                                pressed && !loading ? styles.buttonPressed : null,
                            ]}
                        >
                            <Text style={styles.modalCancelText}>Vazgeç</Text>
                        </Pressable>

                        <Pressable
                            disabled={loading}
                            onPress={submit}
                            style={({ pressed }) => [
                                styles.modalRejectButton,
                                pressed && !loading ? styles.buttonPressed : null,
                                loading ? styles.disabledButton : null,
                            ]}
                        >
                            {loading ? (
                                <ActivityIndicator color="#ffffff" size="small" />
                            ) : (
                                <Text style={styles.modalRejectText}>Reddet</Text>
                            )}
                        </Pressable>
                    </View>
                </View>
            </View>
        </Modal>
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
    buttonPressed: {
        opacity: 0.75,
    },
    disabledButton: {
        opacity: 0.6,
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
    heroCard: {
        backgroundColor: "#ffffff",
        borderRadius: 22,
        padding: 16,
        borderWidth: 1,
        borderColor: "#e5e7eb",
        marginBottom: 16,
    },
    heroTop: {
        flexDirection: "row",
        alignItems: "center",
    },
    avatar: {
        width: 58,
        height: 58,
        borderRadius: 19,
        backgroundColor: "#eff6ff",
        alignItems: "center",
        justifyContent: "center",
        marginRight: 13,
    },
    avatarText: {
        fontSize: 24,
        fontWeight: "900",
        color: "#2563eb",
    },
    heroInfo: {
        flex: 1,
    },
    name: {
        fontSize: 20,
        fontWeight: "900",
        color: "#0f172a",
    },
    email: {
        marginTop: 4,
        fontSize: 13,
        fontWeight: "700",
        color: "#64748b",
    },
    statusBadge: {
        alignSelf: "flex-start",
        marginTop: 8,
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
    infoGrid: {
        marginTop: 16,
        flexDirection: "row",
        gap: 10,
    },
    infoCard: {
        flex: 1,
        backgroundColor: "#f8fafc",
        borderRadius: 16,
        padding: 12,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    infoValue: {
        fontSize: 16,
        fontWeight: "900",
        color: "#0f172a",
    },
    infoTitle: {
        marginTop: 4,
        fontSize: 12,
        fontWeight: "800",
        color: "#64748b",
    },
    branchArea: {
        marginTop: 16,
    },
    blockTitle: {
        fontSize: 13,
        fontWeight: "900",
        color: "#0f172a",
        marginBottom: 8,
    },
    branchRow: {
        flexDirection: "row",
        flexWrap: "wrap",
        gap: 7,
    },
    branchChip: {
        backgroundColor: "#f8fafc",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        borderRadius: 999,
        paddingHorizontal: 10,
        paddingVertical: 6,
    },
    branchChipText: {
        fontSize: 11,
        fontWeight: "900",
        color: "#475569",
    },
    detailCard: {
        backgroundColor: "#ffffff",
        borderRadius: 22,
        padding: 16,
        borderWidth: 1,
        borderColor: "#e5e7eb",
        marginBottom: 16,
    },
    detailTitle: {
        fontSize: 18,
        fontWeight: "900",
        color: "#0f172a",
        marginBottom: 10,
    },
    detailBlock: {
        paddingVertical: 11,
        borderBottomWidth: 1,
        borderBottomColor: "#f1f5f9",
    },
    detailLabel: {
        fontSize: 12,
        fontWeight: "900",
        color: "#64748b",
    },
    detailValue: {
        marginTop: 4,
        fontSize: 14,
        lineHeight: 20,
        fontWeight: "700",
        color: "#0f172a",
    },
    actionCard: {
        backgroundColor: "#ffffff",
        borderRadius: 22,
        padding: 16,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    actionTitle: {
        fontSize: 18,
        fontWeight: "900",
        color: "#0f172a",
    },
    actionText: {
        marginTop: 6,
        fontSize: 13,
        lineHeight: 19,
        color: "#64748b",
    },
    actionRow: {
        marginTop: 14,
        flexDirection: "row",
        gap: 10,
    },
    approveButton: {
        flex: 1,
        minHeight: 46,
        borderRadius: 15,
        backgroundColor: "#16a34a",
        alignItems: "center",
        justifyContent: "center",
    },
    approveButtonText: {
        color: "#ffffff",
        fontSize: 13,
        fontWeight: "900",
    },
    rejectButton: {
        flex: 1,
        minHeight: 46,
        borderRadius: 15,
        backgroundColor: "#fef2f2",
        borderWidth: 1,
        borderColor: "#fecaca",
        alignItems: "center",
        justifyContent: "center",
    },
    rejectButtonText: {
        color: "#dc2626",
        fontSize: 13,
        fontWeight: "900",
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
    modalText: {
        marginTop: 7,
        fontSize: 13,
        lineHeight: 19,
        color: "#64748b",
    },
    redInput: {
        marginTop: 14,
        minHeight: 110,
        backgroundColor: "#f8fafc",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        borderRadius: 16,
        padding: 12,
        fontSize: 14,
        color: "#0f172a",
    },
    modalActionRow: {
        marginTop: 14,
        flexDirection: "row",
        gap: 10,
    },
    modalCancelButton: {
        flex: 1,
        minHeight: 44,
        borderRadius: 15,
        backgroundColor: "#f8fafc",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        alignItems: "center",
        justifyContent: "center",
    },
    modalCancelText: {
        color: "#334155",
        fontSize: 13,
        fontWeight: "900",
    },
    modalRejectButton: {
        flex: 1,
        minHeight: 44,
        borderRadius: 15,
        backgroundColor: "#dc2626",
        alignItems: "center",
        justifyContent: "center",
    },
    modalRejectText: {
        color: "#ffffff",
        fontSize: 13,
        fontWeight: "900",
    },
});
