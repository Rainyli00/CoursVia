import { router } from "expo-router";
import { useEffect, useState } from "react";
import {
    Alert,
    Platform,
    Pressable,
    SafeAreaView,
    StatusBar,
    StyleSheet,
    Text,
    View,
} from "react-native";

import { getUser, saveSelectedRole } from "@/src/auth/authStorage";
import type { MobileKullanici } from "@/src/types/auth";

// Çok rollü kullanıcılar için profil seçim ekranı.
// Kullanıcı tekrar login olmadan profiller arasında geçiş yapabilir.
export default function RoleSelectScreen() {
    const [kullanici, setKullanici] = useState<MobileKullanici | null>(null);

    useEffect(() => {
        async function kullaniciGetir() {
            const storedUser = await getUser();

            if (!storedUser) {
                router.replace("/login" as any);
                return;
            }

            setKullanici(storedUser);
        }

        kullaniciGetir();
    }, []);

    async function rolSec(rol: string) {
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

        Alert.alert("Rol bulunamadı", "Bu rol için uygun profil bulunamadı.");
    }

    return (
        <SafeAreaView style={styles.safeArea}>
            <StatusBar barStyle="dark-content" backgroundColor="#f8fafc" />

            <View style={styles.container}>
                <View style={styles.header}>
                    <View>
                        <Text style={styles.brand}>CoursVia</Text>
                        <Text style={styles.headerText}>Profil değiştir</Text>
                    </View>

                    {router.canGoBack() && (
                        <Pressable
                            onPress={() => router.back()}
                            style={({ pressed }) => [
                                styles.backButton,
                                pressed ? styles.buttonPressed : null,
                            ]}
                        >
                            <Text style={styles.backButtonText}>Geri</Text>
                        </Pressable>
                    )}
                </View>

                <View style={styles.centerArea}>
                    <View style={styles.card}>
                        <Text style={styles.title}>
                            Hangi profilde devam etmek istiyorsun?
                        </Text>

                        <Text style={styles.subtitle}>
                            {kullanici?.adSoyad || "Kullanıcı"} hesabına bağlı profiller aşağıda listelenir.
                        </Text>

                        <View style={styles.roleList}>
                            {kullanici?.roller.map((rol) => (
                                <Pressable
                                    key={rol}
                                    onPress={() => rolSec(rol)}
                                    style={({ pressed }) => [
                                        styles.roleButton,
                                        pressed ? styles.buttonPressed : null,
                                    ]}
                                >
                                    <View style={styles.roleIcon}>
                                        <Text style={styles.roleIconText}>
                                            {rol.substring(0, 1).toUpperCase()}
                                        </Text>
                                    </View>

                                    <View style={styles.roleTextArea}>
                                        <Text style={styles.roleButtonText}>
                                            {rol} Profiline Geç
                                        </Text>

                                        <Text style={styles.roleDescription}>
                                            Bu profille mobil uygulamaya devam et
                                        </Text>
                                    </View>

                                    <Text style={styles.arrow}>›</Text>
                                </Pressable>
                            ))}
                        </View>
                    </View>
                </View>
            </View>
        </SafeAreaView>
    );
}

const styles = StyleSheet.create({
    safeArea: {
        flex: 1,
        backgroundColor: "#f8fafc",
        paddingTop: Platform.OS === "android" ? StatusBar.currentHeight ?? 0 : 0,
    },
    container: {
        flex: 1,
        paddingHorizontal: 20,
        paddingTop: 10,
        paddingBottom: 20,
    },
    header: {
        flexDirection: "row",
        alignItems: "center",
        justifyContent: "space-between",
        marginBottom: 22,
    },
    centerArea: {
        flex: 1,
        justifyContent: "flex-start",
        paddingTop: 26,
    },
    brand: {
        fontSize: 22,
        fontWeight: "900",
        color: "#2563eb",
    },
    headerText: {
        marginTop: 2,
        fontSize: 13,
        fontWeight: "700",
        color: "#64748b",
    },
    backButton: {
        backgroundColor: "#ffffff",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        borderRadius: 999,
        paddingHorizontal: 14,
        paddingVertical: 9,
    },
    backButtonText: {
        fontSize: 13,
        fontWeight: "900",
        color: "#334155",
    },
    card: {
        backgroundColor: "#ffffff",
        borderRadius: 26,
        padding: 20,
        borderWidth: 1,
        borderColor: "#e5e7eb",
    },
    title: {
        fontSize: 23,
        fontWeight: "900",
        color: "#0f172a",
        lineHeight: 30,
    },
    subtitle: {
        marginTop: 9,
        fontSize: 14,
        lineHeight: 21,
        color: "#64748b",
        fontWeight: "600",
    },
    roleList: {
        marginTop: 22,
        gap: 12,
    },
    roleButton: {
        minHeight: 72,
        borderRadius: 18,
        backgroundColor: "#f8fafc",
        borderWidth: 1,
        borderColor: "#e5e7eb",
        flexDirection: "row",
        alignItems: "center",
        padding: 12,
    },
    roleIcon: {
        width: 46,
        height: 46,
        borderRadius: 16,
        backgroundColor: "#eff6ff",
        borderWidth: 1,
        borderColor: "#bfdbfe",
        alignItems: "center",
        justifyContent: "center",
        marginRight: 12,
    },
    roleIconText: {
        fontSize: 17,
        fontWeight: "900",
        color: "#2563eb",
    },
    roleTextArea: {
        flex: 1,
    },
    roleButtonText: {
        color: "#0f172a",
        fontSize: 15,
        fontWeight: "900",
    },
    roleDescription: {
        marginTop: 3,
        color: "#64748b",
        fontSize: 12,
        fontWeight: "700",
    },
    arrow: {
        marginLeft: 8,
        fontSize: 26,
        fontWeight: "900",
        color: "#2563eb",
    },
    buttonPressed: {
        opacity: 0.75,
    },
});