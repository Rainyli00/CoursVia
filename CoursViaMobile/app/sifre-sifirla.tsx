import { router, useLocalSearchParams } from "expo-router";
import { useMemo, useState } from "react";
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
import type { MobileSifreSifirlaResponse } from "@/src/types/auth";

export default function SifreSifirlaScreen() {
    const params = useLocalSearchParams<{ eposta?: string }>();

    const varsayilanEposta = useMemo(() => {
        if (Array.isArray(params.eposta)) {
            return params.eposta[0] || "";
        }

        return params.eposta || "";
    }, [params.eposta]);

    const [eposta, setEposta] = useState(varsayilanEposta);
    const [kod, setKod] = useState("");
    const [yeniSifre, setYeniSifre] = useState("");
    const [yeniSifreTekrar, setYeniSifreTekrar] = useState("");
    const [yukleniyor, setYukleniyor] = useState(false);

    async function sifreSifirla() {
        const temizEposta = eposta.trim().toLowerCase();
        const temizKod = kod.trim();

        if (!temizEposta || !temizKod || !yeniSifre.trim() || !yeniSifreTekrar.trim()) {
            Alert.alert("Eksik bilgi", "Lütfen tüm alanları doldurun.");
            return;
        }

        if (temizKod.length !== 6) {
            Alert.alert("Kod hatalı", "Kod 6 haneli olmalıdır.");
            return;
        }

        if (yeniSifre.length < 6) {
            Alert.alert("Şifre kısa", "Yeni şifre en az 6 karakter olmalıdır.");
            return;
        }

        if (yeniSifre !== yeniSifreTekrar) {
            Alert.alert("Şifreler eşleşmiyor", "Yeni şifre ve tekrar alanı aynı olmalıdır.");
            return;
        }

        try {
            setYukleniyor(true);

            const response = await api.post<MobileSifreSifirlaResponse>(
                "/api/mobile/auth/sifre-sifirla",
                {
                    eposta: temizEposta,
                    kod: temizKod,
                    yeniSifre,
                }
            );

            const data = response.data;

            if (!data.basarili) {
                Alert.alert("İşlem başarısız", data.mesaj || "Şifre sıfırlanamadı.");
                return;
            }

            Alert.alert(
                "Başarılı",
                data.mesaj || "Şifreniz başarıyla güncellendi.",
                [
                    {
                        text: "Giriş yap",
                        onPress: () => router.replace("/login" as any),
                    },
                ]
            );
        } catch (error: any) {
            const mesaj =
                error?.response?.data?.mesaj ||
                error?.message ||
                "Şifre sıfırlanırken hata oluştu.";

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

                <Text style={styles.title}>Yeni Şifre Belirle</Text>

                <Text style={styles.subtitle}>
                    E-posta adresine gelen doğrulama kodunu girerek yeni şifreni oluşturabilirsin.
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

                    <Text style={styles.label}>Doğrulama Kodu</Text>

                    <TextInput
                        value={kod}
                        onChangeText={(value) => setKod(value.replace(/\D/g, "").slice(0, 6))}
                        keyboardType="number-pad"
                        maxLength={6}
                        placeholder="000000"
                        placeholderTextColor="#94a3b8"
                        style={[styles.input, styles.codeInput]}
                    />

                    <Text style={styles.label}>Yeni Şifre</Text>

                    <TextInput
                        value={yeniSifre}
                        onChangeText={setYeniSifre}
                        secureTextEntry
                        placeholder="Yeni şifrenizi yazın"
                        placeholderTextColor="#94a3b8"
                        style={styles.input}
                    />

                    <Text style={styles.label}>Yeni Şifre Tekrar</Text>

                    <TextInput
                        value={yeniSifreTekrar}
                        onChangeText={setYeniSifreTekrar}
                        secureTextEntry
                        placeholder="Yeni şifrenizi tekrar yazın"
                        placeholderTextColor="#94a3b8"
                        style={styles.input}
                    />

                    <Pressable
                        onPress={sifreSifirla}
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
                            <Text style={styles.buttonText}>Şifreyi Güncelle</Text>
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
        marginTop: 24,
        gap: 9,
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
    codeInput: {
        textAlign: "center",
        letterSpacing: 6,
        fontSize: 18,
        fontWeight: "900",
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