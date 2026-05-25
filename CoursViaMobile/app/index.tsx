import { Ionicons } from "@expo/vector-icons";
import { router } from "expo-router";
import { StatusBar } from "expo-status-bar";
import {
    ImageBackground,
    Pressable,
    ScrollView,
    StyleSheet,
    Text,
    View,
} from "react-native";
import { SafeAreaView } from "react-native-safe-area-context";

type FeatureItem = {
    title: string;
    text: string;
    icon: keyof typeof Ionicons.glyphMap;
    color: string;
    backgroundColor: string;
};

const features: FeatureItem[] = [
    {
        title: "Öğrenci deneyimi",
        text: "İlerleme, sınav ve sertifika durumunu tek yerden takip et.",
        icon: "stats-chart-outline",
        color: "#2563eb",
        backgroundColor: "#eff6ff",
    },
    {
        title: "Eğitmen takibi",
        text: "Öğrenci durumlarını, yayın süreçlerini ve bildirimleri gör.",
        icon: "people-outline",
        color: "#0f766e",
        backgroundColor: "#ecfdf5",
    },
    {
        title: "Sertifika doğrulama",
        text: "Sertifika kodunu girerek veya QR kodu okutarak belgeyi doğrula.",
        icon: "qr-code-outline",
        color: "#16a34a",
        backgroundColor: "#f0fdf4",
    },
    {
        title: "Admin yönetimi",
        text: "Kullanıcıları, başvuruları ve sistem kayıtlarını kontrol et.",
        icon: "shield-checkmark-outline",
        color: "#b45309",
        backgroundColor: "#fffbeb",
    },
    {
        title: "Bildirimler",
        text: "Yeni duyuruları ve sistem mesajlarını takip et.",
        icon: "notifications-outline",
        color: "#7c3aed",
        backgroundColor: "#f5f3ff",
    },
];

const heroImage = {
    uri: "https://images.unsplash.com/photo-1516321318423-f06f85e504b3?auto=format&fit=crop&w=1200&q=80",
};

export default function HomeScreen() {
    function giriseGit() {
        router.push("/login" as any);
    }

    function sertifikaDogrulamayaGit() {
        router.push("/sertifika-dogrula" as any);
    }

    return (
        <SafeAreaView style={styles.safeArea}>
            <StatusBar style="dark" />

            <ScrollView
                style={styles.container}
                contentContainerStyle={styles.content}
                showsVerticalScrollIndicator={false}
            >
                <View style={styles.header}>
                    <View style={styles.brandRow}>
                        <View style={styles.logoMark}>
                            <Text style={styles.logoText}>CV</Text>
                        </View>

                        <View style={styles.brandCopy}>
                            <Text style={styles.brandName}>CoursVia</Text>
                            <Text style={styles.brandSub}>Online Eğitim Platformu</Text>
                        </View>
                    </View>

                    <Pressable
                        onPress={giriseGit}
                        style={({ pressed }) => [
                            styles.loginButton,
                            pressed ? styles.pressed : null,
                        ]}
                    >
                        <Text style={styles.loginButtonText}>Giriş</Text>
                    </Pressable>
                </View>

                <ImageBackground
                    source={heroImage}
                    style={styles.hero}
                    imageStyle={styles.heroImage}
                    resizeMode="cover"
                >
                    <View style={styles.heroOverlay}>
                        <Text style={styles.heroKicker}>Online eğitim platformu</Text>

                        <Text style={styles.title}>
                            Eğitim sürecini cebinden takip et.
                        </Text>

                        <Text style={styles.subtitle}>
                            Öğrenci, eğitmen ve admin için gerekli bilgileri sade bir
                            mobil deneyimde görüntüle.
                        </Text>

                        <Pressable
                            onPress={giriseGit}
                            style={({ pressed }) => [
                                styles.primaryButton,
                                pressed ? styles.primaryButtonPressed : null,
                            ]}
                        >
                            <Text style={styles.primaryButtonText}>Giriş Yap</Text>
                            <Ionicons name="arrow-forward" size={20} color="#2563eb" />
                        </Pressable>

                        <Pressable
                            onPress={sertifikaDogrulamayaGit}
                            style={({ pressed }) => [
                                styles.secondaryHeroButton,
                                pressed ? styles.secondaryHeroButtonPressed : null,
                            ]}
                        >
                            <Ionicons name="qr-code-outline" size={20} color="#ffffff" />
                            <Text style={styles.secondaryHeroButtonText}>
                                Sertifika Doğrula
                            </Text>
                        </Pressable>
                    </View>
                </ImageBackground>

                <View style={styles.featuresHeader}>
                    <Text style={styles.featuresTitle}>
                        CoursVia Mobil ile neler yapılır?
                    </Text>
                </View>

                <View style={styles.featureList}>
                    {features.map((item) => (
                        <View key={item.title} style={styles.featureCard}>
                            <View
                                style={[
                                    styles.featureIcon,
                                    { backgroundColor: item.backgroundColor },
                                ]}
                            >
                                <Ionicons name={item.icon} size={22} color={item.color} />
                            </View>

                            <View style={styles.featureCopy}>
                                <Text style={styles.featureTitle}>{item.title}</Text>
                                <Text style={styles.featureText}>{item.text}</Text>
                            </View>
                        </View>
                    ))}
                </View>
            </ScrollView>
        </SafeAreaView>
    );
}

const styles = StyleSheet.create({
    safeArea: {
        flex: 1,
        backgroundColor: "#f8fafc",
    },
    container: {
        flex: 1,
    },
    content: {
        paddingHorizontal: 20,
        paddingTop: 14,
        paddingBottom: 30,
    },
    header: {
        flexDirection: "row",
        alignItems: "center",
        justifyContent: "space-between",
        gap: 14,
    },
    brandRow: {
        flex: 1,
        flexDirection: "row",
        alignItems: "center",
        gap: 12,
    },
    logoMark: {
        width: 46,
        height: 46,
        borderRadius: 8,
        backgroundColor: "#2563eb",
        alignItems: "center",
        justifyContent: "center",
        shadowColor: "#2563eb",
        shadowOpacity: 0.22,
        shadowRadius: 14,
        shadowOffset: { width: 0, height: 8 },
        elevation: 4,
    },
    logoText: {
        color: "#ffffff",
        fontSize: 16,
        fontWeight: "900",
    },
    brandCopy: {
        flex: 1,
    },
    brandName: {
        color: "#0f172a",
        fontSize: 21,
        fontWeight: "900",
    },
    brandSub: {
        marginTop: 1,
        color: "#64748b",
        fontSize: 12,
        fontWeight: "800",
    },
    loginButton: {
        height: 42,
        paddingHorizontal: 15,
        borderRadius: 8,
        backgroundColor: "#ffffff",
        borderWidth: 1,
        borderColor: "#dbeafe",
        alignItems: "center",
        justifyContent: "center",
    },
    loginButtonText: {
        color: "#2563eb",
        fontSize: 13,
        fontWeight: "900",
    },
    pressed: {
        opacity: 0.72,
    },
    hero: {
        marginTop: 34,
        borderRadius: 8,
        overflow: "hidden",
        backgroundColor: "#1d4ed8",
        borderWidth: 1,
        borderColor: "#bfdbfe",
        shadowColor: "#2563eb",
        shadowOpacity: 0.22,
        shadowRadius: 20,
        shadowOffset: { width: 0, height: 12 },
        elevation: 6,
    },
    heroImage: {
        borderRadius: 8,
    },
    heroOverlay: {
        padding: 20,
        minHeight: 360,
        justifyContent: "flex-end",
        backgroundColor: "rgba(29, 78, 216, 0.78)",
    },
    heroKicker: {
        alignSelf: "flex-start",
        color: "#dbeafe",
        fontSize: 12,
        fontWeight: "900",
        backgroundColor: "rgba(255, 255, 255, 0.14)",
        borderWidth: 1,
        borderColor: "rgba(255, 255, 255, 0.28)",
        borderRadius: 8,
        paddingHorizontal: 10,
        paddingVertical: 7,
        overflow: "hidden",
    },
    title: {
        marginTop: 18,
        color: "#ffffff",
        fontSize: 40,
        lineHeight: 44,
        fontWeight: "900",
    },
    subtitle: {
        marginTop: 12,
        color: "#dbeafe",
        fontSize: 16,
        lineHeight: 24,
        fontWeight: "600",
    },
    primaryButton: {
        marginTop: 24,
        minHeight: 54,
        borderRadius: 8,
        backgroundColor: "#ffffff",
        paddingHorizontal: 18,
        flexDirection: "row",
        alignItems: "center",
        justifyContent: "center",
        gap: 9,
        shadowColor: "#0f172a",
        shadowOpacity: 0.16,
        shadowRadius: 16,
        shadowOffset: { width: 0, height: 9 },
        elevation: 4,
    },
    primaryButtonPressed: {
        opacity: 0.86,
        transform: [{ scale: 0.99 }],
    },
    primaryButtonText: {
        color: "#2563eb",
        fontSize: 16,
        fontWeight: "900",
    },
    secondaryHeroButton: {
        marginTop: 12,
        minHeight: 52,
        borderRadius: 8,
        backgroundColor: "rgba(255, 255, 255, 0.14)",
        borderWidth: 1,
        borderColor: "rgba(255, 255, 255, 0.38)",
        paddingHorizontal: 18,
        flexDirection: "row",
        alignItems: "center",
        justifyContent: "center",
        gap: 9,
    },
    secondaryHeroButtonPressed: {
        opacity: 0.82,
        transform: [{ scale: 0.99 }],
    },
    secondaryHeroButtonText: {
        color: "#ffffff",
        fontSize: 15,
        fontWeight: "900",
    },
    featuresHeader: {
        marginTop: 24,
        marginBottom: 10,
    },
    featuresTitle: {
        color: "#0f172a",
        fontSize: 20,
        fontWeight: "900",
    },
    featureList: {
        gap: 10,
    },
    featureCard: {
        minHeight: 78,
        borderRadius: 8,
        backgroundColor: "#ffffff",
        borderWidth: 1,
        borderColor: "#e2e8f0",
        padding: 14,
        flexDirection: "row",
        alignItems: "center",
        gap: 12,
    },
    featureIcon: {
        width: 44,
        height: 44,
        borderRadius: 8,
        alignItems: "center",
        justifyContent: "center",
    },
    featureCopy: {
        flex: 1,
    },
    featureTitle: {
        color: "#0f172a",
        fontSize: 15,
        fontWeight: "900",
    },
    featureText: {
        marginTop: 3,
        color: "#64748b",
        fontSize: 13,
        lineHeight: 18,
        fontWeight: "700",
    },
});