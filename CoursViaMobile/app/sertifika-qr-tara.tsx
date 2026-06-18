import type { BarcodeScanningResult } from "expo-camera";
import { CameraView, useCameraPermissions } from "expo-camera";
import { router } from "expo-router";
import { useState } from "react";
import {
    ActivityIndicator,
    Alert,
    Pressable,
    StyleSheet,
    Text,
    View,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";

export default function SertifikaQrTaraScreen() {
    const [permission, requestPermission] = useCameraPermissions();

    // QR okuyucu aynı kodu peş peşe algılayabildiği için ilk başarılı okumadan sonra tarama kapatılır.
    const [tarandiMi, setTarandiMi] = useState(false);
    const [izinKontrolEdiliyor, setIzinKontrolEdiliyor] = useState(false);

    function sertifikaDogrulamayaDon() {
        router.replace("/sertifika-dogrula" as any);
    }

    // Kamera izni yoksa kullanıcıdan izin ister; sonuç olumsuzsa ekranda kalıp uyarı gösterir.
    async function izinIste() {
        try {
            setIzinKontrolEdiliyor(true);

            const sonuc = await requestPermission();

            if (!sonuc.granted) {
                Alert.alert(
                    "Kamera izni gerekli",
                    "QR kodu okutmak için kamera izni vermen gerekiyor."
                );
            }
        } finally {
            setIzinKontrolEdiliyor(false);
        }
    }

    // QR içinden sertifika kodu alınır ve doğrulama ekranına parametre olarak gönderilir.
    function qrOkundu(result: BarcodeScanningResult) {
        if (tarandiMi) {
            return;
        }

        const okunanKod = sertifikaKodunuAyikla(result.data);

        if (!okunanKod) {
            Alert.alert("QR okunamadı", "QR kod içinden sertifika kodu alınamadı.");
            return;
        }

        setTarandiMi(true);

        router.replace({
            pathname: "/sertifika-dogrula",
            params: {
                kod: okunanKod,
            },
        } as any);
    }

    // İzin bilgisi ilk render'da henüz hazır olmayabilir.
    if (!permission) {
        return (
            <SafeAreaView style={styles.centerContainer}>
                <ActivityIndicator size="large" color="#2563eb" />
                <Text style={styles.centerText}>Kamera hazırlanıyor...</Text>
            </SafeAreaView>
        );
    }

    // Kamera izni verilmediyse tarayıcı yerine izin isteme ekranı gösterilir.
    if (!permission.granted) {
        return (
            <SafeAreaView style={styles.centerContainer}>
                <Text style={styles.brand}>CoursVia</Text>

                <Text style={styles.permissionTitle}>Kamera İzni Gerekli</Text>

                <Text style={styles.permissionText}>
                    Sertifika QR kodunu okutmak için kamera erişimine izin vermelisin.
                </Text>

                <Pressable
                    onPress={izinIste}
                    disabled={izinKontrolEdiliyor}
                    style={({ pressed }) => [
                        styles.primaryButton,
                        pressed && !izinKontrolEdiliyor ? styles.buttonPressed : null,
                        izinKontrolEdiliyor ? styles.buttonDisabled : null,
                    ]}
                >
                    {izinKontrolEdiliyor ? (
                        <ActivityIndicator color="#ffffff" />
                    ) : (
                        <Text style={styles.primaryButtonText}>Kamera İzni Ver</Text>
                    )}
                </Pressable>

                <Pressable
                    onPress={sertifikaDogrulamayaDon}
                    style={({ pressed }) => [
                        styles.secondaryButton,
                        pressed ? styles.buttonPressed : null,
                    ]}
                >
                    <Text style={styles.secondaryButtonText}>Sertifika Doğrulamaya Dön</Text>
                </Pressable>
            </SafeAreaView>
        );
    }

    return (
        <View style={styles.container}>
            {/* Sadece QR okunur; diğer barkod tipleri bu ekran için gerekli değil. */}
            <CameraView
                style={styles.camera}
                facing="back"
                barcodeScannerSettings={{
                    barcodeTypes: ["qr"],
                }}
                onBarcodeScanned={tarandiMi ? undefined : qrOkundu}
            />

            <SafeAreaView style={styles.overlay}>
                <View style={styles.topBar}>
                    <Pressable
                        onPress={sertifikaDogrulamayaDon}
                        style={({ pressed }) => [
                            styles.backButton,
                            pressed ? styles.buttonPressed : null,
                        ]}
                    >
                        <Text style={styles.backButtonText}>← Geri</Text>
                    </Pressable>

                    <Text style={styles.topTitle}>QR Kod Tara</Text>

                    <View style={styles.topSpacer} />
                </View>

                <View style={styles.scanArea}>
                    <View style={styles.cornerTopLeft} />
                    <View style={styles.cornerTopRight} />
                    <View style={styles.cornerBottomLeft} />
                    <View style={styles.cornerBottomRight} />
                </View>

                <View style={styles.bottomCard}>
                    <Text style={styles.bottomTitle}>Sertifika QR kodunu okut</Text>

                    <Text style={styles.bottomText}>
                        QR kod algılandığında sertifika otomatik olarak doğrulanacak.
                    </Text>

                    {tarandiMi ? (
                        <View style={styles.detectedBox}>
                            <ActivityIndicator color="#2563eb" />
                            <Text style={styles.detectedText}>
                                Kod algılandı, doğrulanıyor...
                            </Text>
                        </View>
                    ) : null}
                </View>
            </SafeAreaView>
        </View>
    );
}

// QR içeriği direkt kod, doğrulama linki veya query parametreli link olabilir.
// Bu fonksiyon hepsinden ekrana gönderilecek sade sertifika kodunu üretir.
function sertifikaKodunuAyikla(value: string) {
    const temiz = (value || "").trim();

    if (!temiz) {
        return "";
    }

    try {
        const url = new URL(temiz);

        // Önce bilinen query parametrelerinde sertifika kodu aranır.
        const queryKod =
            url.searchParams.get("kod") ||
            url.searchParams.get("sertifikaKodu") ||
            url.searchParams.get("SertifikaKodu");

        if (queryKod && queryKod.trim()) {
            return decodeURIComponent(queryKod.trim());
        }

        // Query yoksa doğrulama linkinin son path parçası kod kabul edilir.
        const pathParcalari = url.pathname
            .split("/")
            .map((x) => x.trim())
            .filter(Boolean);

        const sonParca = pathParcalari[pathParcalari.length - 1];

        if (sonParca) {
            return decodeURIComponent(sonParca);
        }
    } catch {
        // Webdeki QR direkt sertifika kodu içeriyor.
    }

    // URL gibi parse edilemeyen ama "kod=..." içeren metinler için son kontrol.
    const parametreEslesme = temiz.match(/(?:kod|sertifikaKodu|SertifikaKodu)=([^&]+)/);

    if (parametreEslesme?.[1]) {
        return decodeURIComponent(parametreEslesme[1].trim());
    }

    return temiz;
}

const styles = StyleSheet.create({
    container: {
        flex: 1,
        backgroundColor: "#0f172a",
    },
    camera: {
        ...StyleSheet.absoluteFillObject,
    },
    overlay: {
        flex: 1,
        justifyContent: "space-between",
        paddingHorizontal: 20,
        paddingBottom: 24,
    },
    topBar: {
        marginTop: 8,
        flexDirection: "row",
        alignItems: "center",
        justifyContent: "space-between",
    },
    backButton: {
        backgroundColor: "rgba(15, 23, 42, 0.65)",
        borderWidth: 1,
        borderColor: "rgba(255, 255, 255, 0.24)",
        borderRadius: 999,
        paddingHorizontal: 13,
        paddingVertical: 9,
    },
    backButtonText: {
        color: "#ffffff",
        fontSize: 13,
        fontWeight: "900",
    },
    topTitle: {
        color: "#ffffff",
        fontSize: 17,
        fontWeight: "900",
    },
    topSpacer: {
        width: 58,
    },
    scanArea: {
        alignSelf: "center",
        width: 250,
        height: 250,
        position: "relative",
    },
    cornerTopLeft: {
        position: "absolute",
        top: 0,
        left: 0,
        width: 56,
        height: 56,
        borderTopWidth: 5,
        borderLeftWidth: 5,
        borderColor: "#ffffff",
        borderTopLeftRadius: 18,
    },
    cornerTopRight: {
        position: "absolute",
        top: 0,
        right: 0,
        width: 56,
        height: 56,
        borderTopWidth: 5,
        borderRightWidth: 5,
        borderColor: "#ffffff",
        borderTopRightRadius: 18,
    },
    cornerBottomLeft: {
        position: "absolute",
        bottom: 0,
        left: 0,
        width: 56,
        height: 56,
        borderBottomWidth: 5,
        borderLeftWidth: 5,
        borderColor: "#ffffff",
        borderBottomLeftRadius: 18,
    },
    cornerBottomRight: {
        position: "absolute",
        bottom: 0,
        right: 0,
        width: 56,
        height: 56,
        borderBottomWidth: 5,
        borderRightWidth: 5,
        borderColor: "#ffffff",
        borderBottomRightRadius: 18,
    },
    bottomCard: {
        backgroundColor: "rgba(255, 255, 255, 0.96)",
        borderRadius: 24,
        padding: 18,
        borderWidth: 1,
        borderColor: "rgba(255, 255, 255, 0.3)",
    },
    bottomTitle: {
        fontSize: 18,
        fontWeight: "900",
        color: "#0f172a",
        textAlign: "center",
    },
    bottomText: {
        marginTop: 6,
        fontSize: 14,
        lineHeight: 20,
        fontWeight: "700",
        color: "#64748b",
        textAlign: "center",
    },
    detectedBox: {
        marginTop: 14,
        borderRadius: 16,
        backgroundColor: "#eff6ff",
        padding: 12,
        flexDirection: "row",
        alignItems: "center",
        justifyContent: "center",
        gap: 8,
    },
    detectedText: {
        color: "#2563eb",
        fontSize: 13,
        fontWeight: "900",
    },
    centerContainer: {
        flex: 1,
        backgroundColor: "#f8fafc",
        justifyContent: "center",
        padding: 24,
    },
    centerText: {
        marginTop: 12,
        textAlign: "center",
        color: "#64748b",
        fontSize: 14,
        fontWeight: "800",
    },
    brand: {
        fontSize: 32,
        fontWeight: "900",
        color: "#2563eb",
        textAlign: "center",
        marginBottom: 22,
    },
    permissionTitle: {
        fontSize: 24,
        fontWeight: "900",
        color: "#0f172a",
        textAlign: "center",
    },
    permissionText: {
        marginTop: 8,
        marginBottom: 22,
        fontSize: 14,
        lineHeight: 21,
        color: "#64748b",
        textAlign: "center",
        fontWeight: "700",
    },
    primaryButton: {
        height: 50,
        borderRadius: 16,
        backgroundColor: "#2563eb",
        justifyContent: "center",
        alignItems: "center",
    },
    primaryButtonText: {
        color: "#ffffff",
        fontSize: 16,
        fontWeight: "900",
    },
    secondaryButton: {
        marginTop: 10,
        height: 48,
        borderRadius: 16,
        backgroundColor: "#ffffff",
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
});
