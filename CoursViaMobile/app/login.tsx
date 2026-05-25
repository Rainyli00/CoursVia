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
import { saveAuth, saveSelectedRole } from "@/src/auth/authStorage";
import type { MobileLoginResponse } from "@/src/types/auth";

export default function LoginScreen() {
    const [eposta, setEposta] = useState("");
    const [sifre, setSifre] = useState("");
    const [yukleniyor, setYukleniyor] = useState(false);

    async function girisYap() {
        if (!eposta.trim() || !sifre.trim()) {
            Alert.alert("Eksik bilgi", "E-posta ve şifre zorunludur.");
            return;
        }

        try {
            setYukleniyor(true);

            const response = await api.post<MobileLoginResponse>("/api/mobile/auth/login", {
                eposta: eposta.trim(),
                sifre: sifre.trim(),
            });

            const data = response.data;

            if (
                !data.basarili ||
                !data.accessToken ||
                !data.refreshToken ||
                !data.kullanici
            ) {
                Alert.alert("Giriş başarısız", data.mesaj || "Giriş yapılamadı.");
                return;
            }

            await saveAuth(
                data.accessToken,
                data.accessTokenBitisTarihi,
                data.refreshToken,
                data.refreshTokenBitisTarihi,
                data.kullanici
            );

            if (data.kullanici.roller.length > 1) {
                router.replace("/role-select" as any);
                return;
            }

            const rol = data.kullanici.roller[0];

            await saveSelectedRole(rol);

            if (rol === "Öğrenci") {
                router.replace("/ogrenci" as any);
                return;
            }

            if (rol === "Eğitmen") {
                router.replace("/egitmen" as any);
                return;
            }

            if (rol === "Admin") {
                router.replace("/admin" as any);
                return;
            }

            Alert.alert("Rol bulunamadı", "Bu kullanıcı için uygun panel bulunamadı.");
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Sunucuya bağlanırken hata oluştu.";

            Alert.alert("Giriş hatası", mesaj);
        } finally {
            setYukleniyor(false);
        }
    }

    function anaEkranaDon() {
        router.replace("/" as any);
    }

    function sifremiUnuttumGit() {
        router.push("/sifremi-unuttum" as any);
    }

    return (
        <KeyboardAvoidingView
            style={styles.container}
            behavior={Platform.OS === "ios" ? "padding" : undefined}
        >
            <View style={styles.card}>
                <Pressable
                    onPress={anaEkranaDon}
                    style={({ pressed }) => [
                        styles.backButton,
                        pressed ? styles.buttonPressed : null,
                    ]}
                >
                    <Text style={styles.backButtonText}>← Ana ekrana dön</Text>
                </Pressable>

                <View style={styles.logoBox}>
                    <Text style={styles.logoText}>CV</Text>
                </View>

                <Text style={styles.logo}>CoursVia</Text>

                <Text style={styles.title}>Mobil Panele Giriş</Text>

                <Text style={styles.subtitle}>
                    Öğrenci, eğitmen veya admin hesabınızla devam edin.
                </Text>

                <View style={styles.form}>
                    <Text style={styles.label}>E-posta</Text>

                    <TextInput
                        value={eposta}
                        onChangeText={setEposta}
                        autoCapitalize="none"
                        keyboardType="email-address"
                        placeholder="ornek@mail.com"
                        placeholderTextColor="#94a3b8"
                        style={styles.input}
                    />

                    <Text style={styles.label}>Şifre</Text>

                    <TextInput
                        value={sifre}
                        onChangeText={setSifre}
                        secureTextEntry
                        placeholder="Şifreniz"
                        placeholderTextColor="#94a3b8"
                        style={styles.input}
                    />

                    <Pressable
                        onPress={sifremiUnuttumGit}
                        style={({ pressed }) => [
                            styles.forgotButton,
                            pressed ? styles.buttonPressed : null,
                        ]}
                    >
                        <Text style={styles.forgotButtonText}>Şifremi unuttum</Text>
                    </Pressable>

                    <Pressable
                        onPress={girisYap}
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
                            <Text style={styles.buttonText}>Giriş Yap</Text>
                        )}
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
        marginBottom: 18,
    },
    backButtonText: {
        fontSize: 12,
        fontWeight: "900",
        color: "#334155",
    },
    logoBox: {
        width: 68,
        height: 68,
        borderRadius: 23,
        backgroundColor: "#eff6ff",
        borderWidth: 1,
        borderColor: "#bfdbfe",
        alignItems: "center",
        justifyContent: "center",
        alignSelf: "center",
        marginBottom: 12,
    },
    logoText: {
        fontSize: 24,
        fontWeight: "900",
        color: "#2563eb",
    },
    logo: {
        fontSize: 30,
        fontWeight: "900",
        color: "#2563eb",
        textAlign: "center",
        marginBottom: 8,
    },
    title: {
        fontSize: 22,
        fontWeight: "900",
        color: "#0f172a",
        textAlign: "center",
    },
    subtitle: {
        marginTop: 8,
        fontSize: 14,
        lineHeight: 20,
        color: "#64748b",
        textAlign: "center",
    },
    form: {
        marginTop: 28,
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
        marginBottom: 8,
        color: "#0f172a",
    },
    forgotButton: {
        alignSelf: "flex-end",
        paddingVertical: 4,
        paddingHorizontal: 4,
        marginTop: -4,
    },
    forgotButtonText: {
        color: "#2563eb",
        fontSize: 13,
        fontWeight: "900",
    },
    button: {
        height: 50,
        borderRadius: 16,
        backgroundColor: "#2563eb",
        justifyContent: "center",
        alignItems: "center",
        marginTop: 8,
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
});