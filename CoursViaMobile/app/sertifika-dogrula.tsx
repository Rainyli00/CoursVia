import { router, useLocalSearchParams } from "expo-router";
import { useEffect, useMemo, useRef, useState } from "react";
import {
    ActivityIndicator,
    Alert,
    KeyboardAvoidingView,
    Platform,
    Pressable,
    ScrollView,
    StyleSheet,
    Text,
    TextInput,
    View,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";

import { api } from "@/src/api/client";
import type {
    MobileSertifikaDogrulamaDetay,
    MobileSertifikaDogrulamaResponse,
} from "@/src/types/sertifikaDogrulama";

export default function SertifikaDogrulaScreen() {
    const params = useLocalSearchParams<{ kod?: string }>();

    const otomatikDogrulananKodRef = useRef<string | null>(null);

    const gelenKod = useMemo(() => {
        if (Array.isArray(params.kod)) {
            return params.kod[0] || "";
        }

        return params.kod || "";
    }, [params.kod]);

    const [kod, setKod] = useState(gelenKod);
    const [yukleniyor, setYukleniyor] = useState(false);
    const [sonuc, setSonuc] = useState<MobileSertifikaDogrulamaDetay | null>(null);
    const [hata, setHata] = useState<string | null>(null);

    useEffect(() => {
        if (!gelenKod) {
            return;
        }

        if (otomatikDogrulananKodRef.current === gelenKod) {
            return;
        }

        otomatikDogrulananKodRef.current = gelenKod;
        setKod(gelenKod);
        dogrula(gelenKod);
    }, [gelenKod]);

    async function dogrula(disaridanKod?: string) {
        const temizKod = (disaridanKod ?? kod).trim();

        if (!temizKod) {
            Alert.alert("Eksik bilgi", "Lütfen sertifika kodunu girin.");
            return;
        }

        try {
            setYukleniyor(true);
            setHata(null);
            setSonuc(null);

            const response = await api.get<MobileSertifikaDogrulamaResponse>(
                "/api/mobile/sertifika-dogrulama",
                {
                    params: {
                        kod: temizKod,
                    },
                }
            );

            const data = response.data;

            if (!data.basarili || !data.gecerliMi || !data.sertifika) {
                setHata(data.mesaj || "Sertifika doğrulanamadı.");
                return;
            }

            setKod(data.sertifika.sertifikaKodu);
            setSonuc(data.sertifika);
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Sertifika doğrulanırken hata oluştu.";

            setHata(mesaj);
        } finally {
            setYukleniyor(false);
        }
    }

    function anaSayfayaDon() {
        router.replace("/" as any);
    }

    function qrTaraEkraninaGit() {
        router.push("/sertifika-qr-tara" as any);
    }

    return (
        <SafeAreaView style={styles.safeArea} edges={["top", "left", "right"]}>
            <KeyboardAvoidingView
                style={styles.container}
                behavior={Platform.OS === "ios" ? "padding" : undefined}
            >
                <ScrollView
                    contentContainerStyle={styles.scrollContent}
                    keyboardShouldPersistTaps="handled"
                    showsVerticalScrollIndicator={false}
                >
                    <Pressable
                        onPress={anaSayfayaDon}
                        style={({ pressed }) => [
                            styles.backButton,
                            pressed ? styles.buttonPressed : null,
                        ]}
                    >
                        <Text style={styles.backButtonText}>← Ana sayfaya dön</Text>
                    </Pressable>

                    <Text style={styles.brand}>CoursVia</Text>

                    <View style={styles.card}>
                        <Text style={styles.title}>Sertifika Doğrula</Text>

                        <Text style={styles.subtitle}>
                            Sertifika kodunu girerek veya QR kodu okutarak belgenin geçerliliğini kontrol edebilirsin.
                        </Text>

                        <View style={styles.form}>
                            <Text style={styles.label}>Sertifika Kodu</Text>

                            <TextInput
                                value={kod}
                                onChangeText={setKod}
                                autoCapitalize="characters"
                                placeholder="Sertifika kodunu yazın"
                                placeholderTextColor="#94a3b8"
                                style={styles.input}
                            />

                            <Pressable
                                onPress={() => dogrula()}
                                disabled={yukleniyor}
                                style={({ pressed }) => [
                                    styles.primaryButton,
                                    pressed && !yukleniyor ? styles.buttonPressed : null,
                                    yukleniyor ? styles.buttonDisabled : null,
                                ]}
                            >
                                {yukleniyor ? (
                                    <ActivityIndicator color="#ffffff" />
                                ) : (
                                    <Text style={styles.primaryButtonText}>Doğrula</Text>
                                )}
                            </Pressable>

                            <Pressable
                                onPress={qrTaraEkraninaGit}
                                style={({ pressed }) => [
                                    styles.secondaryButton,
                                    pressed ? styles.buttonPressed : null,
                                ]}
                            >
                                <Text style={styles.secondaryButtonText}>QR Kod Tara</Text>
                            </Pressable>
                        </View>
                    </View>

                    {hata ? (
                        <View style={styles.errorCard}>
                            <Text style={styles.errorTitle}>Doğrulanamadı</Text>
                            <Text style={styles.errorText}>{hata}</Text>
                        </View>
                    ) : null}

                    {sonuc ? <SertifikaSonuc sertifika={sonuc} /> : null}
                </ScrollView>
            </KeyboardAvoidingView>
        </SafeAreaView>
    );
}

function SertifikaSonuc({
    sertifika,
}: {
    sertifika: MobileSertifikaDogrulamaDetay;
}) {
    return (
        <View style={styles.successCard}>
            <View style={styles.successIcon}>
                <Text style={styles.successIconText}>✓</Text>
            </View>

            <Text style={styles.successTitle}>Sertifika Geçerli</Text>

            <InfoRow label="Sertifika Kodu" value={sertifika.sertifikaKodu} />
            <InfoRow label="Öğrenci" value={sertifika.ogrenciAdSoyad} />
            <InfoRow label="Kurs" value={sertifika.kursAdi} />
            <InfoRow label="Verilme Tarihi" value={tarihFormatla(sertifika.verilmeTarihi)} />
        </View>
    );
}

function InfoRow({ label, value }: { label: string; value: string }) {
    return (
        <View style={styles.infoRow}>
            <Text style={styles.infoLabel}>{label}</Text>
            <Text style={styles.infoValue}>{value || "-"}</Text>
        </View>
    );
}

function tarihFormatla(value: string) {
    const tarih = new Date(value);

    if (Number.isNaN(tarih.getTime())) {
        return value;
    }

    return tarih.toLocaleDateString("tr-TR", {
        day: "2-digit",
        month: "long",
        year: "numeric",
    });
}

const styles = StyleSheet.create({
    safeArea: {
        flex: 1,
        backgroundColor: "#f8fafc",
    },
    container: {
        flex: 1,
        backgroundColor: "#f8fafc",
    },
    scrollContent: {
        paddingHorizontal: 20,
        paddingTop: 18,
        paddingBottom: 40,
    },
    backButton: {
        alignSelf: "flex-start",
        backgroundColor: "#ffffff",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        borderRadius: 999,
        paddingHorizontal: 12,
        paddingVertical: 8,
        marginBottom: 22,
        shadowColor: "#000",
        shadowOpacity: 0.04,
        shadowRadius: 10,
        shadowOffset: { width: 0, height: 4 },
        elevation: 2,
    },
    backButtonText: {
        fontSize: 12,
        fontWeight: "900",
        color: "#334155",
    },
    brand: {
        fontSize: 30,
        fontWeight: "900",
        color: "#2563eb",
        textAlign: "center",
        marginBottom: 18,
    },
    card: {
        backgroundColor: "#ffffff",
        borderRadius: 24,
        padding: 20,
        borderWidth: 1,
        borderColor: "#e5e7eb",
        shadowColor: "#000",
        shadowOpacity: 0.05,
        shadowRadius: 16,
        shadowOffset: { width: 0, height: 8 },
        elevation: 3,
    },
    title: {
        fontSize: 24,
        fontWeight: "900",
        color: "#0f172a",
        textAlign: "center",
    },
    subtitle: {
        marginTop: 8,
        fontSize: 14,
        lineHeight: 21,
        color: "#64748b",
        textAlign: "center",
    },
    form: {
        marginTop: 22,
        gap: 10,
    },
    label: {
        fontSize: 13,
        fontWeight: "800",
        color: "#334155",
    },
    input: {
        height: 48,
        borderWidth: 1,
        borderColor: "#e2e8f0",
        borderRadius: 14,
        paddingHorizontal: 14,
        fontSize: 15,
        backgroundColor: "#ffffff",
        color: "#0f172a",
        fontWeight: "700",
    },
    primaryButton: {
        height: 50,
        borderRadius: 16,
        backgroundColor: "#2563eb",
        justifyContent: "center",
        alignItems: "center",
        marginTop: 8,
    },
    primaryButtonText: {
        color: "#ffffff",
        fontSize: 16,
        fontWeight: "900",
    },
    secondaryButton: {
        height: 48,
        borderRadius: 16,
        backgroundColor: "#f8fafc",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        justifyContent: "center",
        alignItems: "center",
    },
    secondaryButtonText: {
        color: "#334155",
        fontSize: 14,
        fontWeight: "900",
    },
    buttonPressed: {
        opacity: 0.8,
    },
    buttonDisabled: {
        opacity: 0.7,
    },
    errorCard: {
        marginTop: 16,
        backgroundColor: "#fef2f2",
        borderRadius: 20,
        padding: 16,
        borderWidth: 1,
        borderColor: "#fecaca",
    },
    errorTitle: {
        fontSize: 16,
        fontWeight: "900",
        color: "#991b1b",
    },
    errorText: {
        marginTop: 5,
        fontSize: 14,
        lineHeight: 20,
        color: "#b91c1c",
        fontWeight: "600",
    },
    successCard: {
        marginTop: 16,
        backgroundColor: "#ffffff",
        borderRadius: 24,
        padding: 18,
        borderWidth: 1,
        borderColor: "#bbf7d0",
        shadowColor: "#16a34a",
        shadowOpacity: 0.06,
        shadowRadius: 16,
        shadowOffset: { width: 0, height: 8 },
        elevation: 3,
    },
    successIcon: {
        width: 48,
        height: 48,
        borderRadius: 999,
        backgroundColor: "#dcfce7",
        alignItems: "center",
        justifyContent: "center",
        alignSelf: "center",
        marginBottom: 10,
    },
    successIconText: {
        fontSize: 24,
        fontWeight: "900",
        color: "#16a34a",
    },
    successTitle: {
        fontSize: 21,
        fontWeight: "900",
        color: "#166534",
        textAlign: "center",
        marginBottom: 16,
    },
    infoRow: {
        borderTopWidth: 1,
        borderTopColor: "#f1f5f9",
        paddingVertical: 11,
    },
    infoLabel: {
        fontSize: 12,
        fontWeight: "900",
        color: "#64748b",
        marginBottom: 4,
    },
    infoValue: {
        fontSize: 15,
        fontWeight: "800",
        color: "#0f172a",
        lineHeight: 21,
    },
});