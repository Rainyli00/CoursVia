import { router } from "expo-router";
import { useState } from "react";
import {
    ActivityIndicator,
    Alert,
    KeyboardAvoidingView,
    Platform,
    Pressable,
    StyleSheet,
    Text,
    TextInput,
    View,
} from "react-native";

import { api } from "@/src/api/client";
import type { MobileSifremiUnuttumResponse } from "@/src/types/auth";

export default function SifremiUnuttumScreen() {
    const [eposta, setEposta] = useState("");
    const [yukleniyor, setYukleniyor] = useState(false);

    async function kodGonder() {
        const temizEposta = eposta.trim().toLowerCase();

        if (!temizEposta) {
            Alert.alert("Eksik bilgi", "E-posta adresi zorunludur.");
            return;
        }

        try {
            setYukleniyor(true);

            const response = await api.post<MobileSifremiUnuttumResponse>(
                "/api/mobile/auth/sifremi-unuttum",
                {
                    eposta: temizEposta,
                }
            );

            const data = response.data;

            if (!data.basarili || !data.kodGonderildiMi) {
                Alert.alert("İşlem başarısız", data.mesaj || "Kod gönderilemedi.");
                return;
            }

            Alert.alert(
                "Kod gönderildi",
                data.mesaj || "Şifre sıfırlama kodu e-posta adresinize gönderildi."
            );

            router.push({
                pathname: "/sifre-sifirla",
                params: {
                    eposta: data.eposta || temizEposta,
                },
            } as any);
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Kod gönderilirken hata oluştu.";

            Alert.alert("Hata", mesaj);
        } finally {
            setYukleniyor(false);
        }
    }

    return (
        <KeyboardAvoidingView
            style={styles.container}
            behavior={Platform.OS === "ios" ? "padding" : undefined}
        >
            <View style={styles.card}>
                <Pressable
                    onPress={() => router.back()}
                    style={({ pressed }) => [
                        styles.backButton,
                        pressed ? styles.buttonPressed : null,
                    ]}
                >
                    <Text style={styles.backButtonText}>← Geri dön</Text>
                </Pressable>

                <Text style={styles.brand}>CoursVia</Text>

                <Text style={styles.title}>Şifremi Unuttum</Text>

                <Text style={styles.subtitle}>
                    Hesabına ait e-posta adresini girerek şifre sıfırlama işlemine devam edebilirsin.
                </Text>

                <View style={styles.form}>
                    <Text style={styles.label}>E-posta Adresi</Text>

                    <TextInput
                        value={eposta}
                        onChangeText={setEposta}
                        autoCapitalize="none"
                        keyboardType="email-address"
                        placeholder="E-posta adresinizi yazın"
                        placeholderTextColor="#94a3b8"
                        style={styles.input}
                    />

                    <Pressable
                        onPress={kodGonder}
                        disabled={yukleniyor}
                        style={({ pressed }) => [
                            styles.button,
                            pressed && !yukleniyor ? styles.buttonPressed : null,
                            yukleniyor ? styles.buttonDisabled : null,
                        ]}
                    >
                        {yukleniyor ? (
                            <ActivityIndicator color="#ffffff" />
                        ) : (
                            <Text style={styles.buttonText}>Devam Et</Text>
                        )}
                    </Pressable>

                    <Pressable
                        onPress={() => router.replace("/login" as any)}
                        style={({ pressed }) => [
                            styles.secondaryButton,
                            pressed ? styles.buttonPressed : null,
                        ]}
                    >
                        <Text style={styles.secondaryButtonText}>Giriş ekranına dön</Text>
                    </Pressable>
                </View>
            </View>
        </KeyboardAvoidingView>
    );
}

const styles = StyleSheet.create({
    container: {
        flex: 1,
        backgroundColor: "#f8fafc",
        justifyContent: "center",
        padding: 20,
    },
    card: {
        backgroundColor: "#ffffff",
        borderRadius: 26,
        padding: 24,
        borderWidth: 1,
        borderColor: "#e5e7eb",
        shadowColor: "#000",
        shadowOpacity: 0.08,
        shadowRadius: 20,
        shadowOffset: { width: 0, height: 10 },
        elevation: 5,
    },
    backButton: {
        alignSelf: "flex-start",
        backgroundColor: "#f8fafc",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        borderRadius: 999,
        paddingHorizontal: 12,
        paddingVertical: 8,
        marginBottom: 22,
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
        marginTop: 26,
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
    },
    button: {
        height: 50,
        borderRadius: 16,
        backgroundColor: "#2563eb",
        justifyContent: "center",
        alignItems: "center",
        marginTop: 10,
    },
    buttonPressed: {
        opacity: 0.8,
    },
    buttonDisabled: {
        opacity: 0.7,
    },
    buttonText: {
        color: "#ffffff",
        fontSize: 16,
        fontWeight: "900",
    },
    secondaryButton: {
        height: 46,
        borderRadius: 16,
        backgroundColor: "#f8fafc",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        justifyContent: "center",
        alignItems: "center",
        marginTop: 2,
    },
    secondaryButtonText: {
        color: "#334155",
        fontSize: 14,
        fontWeight: "900",
    },
});