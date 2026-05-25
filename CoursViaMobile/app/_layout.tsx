import { Stack } from "expo-router";

// Uygulamanın ana route düzeni.
// Expo Router, app klasörü içindeki dosyalara göre sayfa oluşturur.
// Stack yapısı sayfalar arası geçişi yönetir.
export default function RootLayout() {
  return (
    <Stack
      screenOptions={{
        // Şimdilik tüm ekranlarda üst header'ı kapatıyoruz.
        // Daha sonra rol panellerinde özel header tasarımı yapabiliriz.
        headerShown: false,
      }}
    />
  );
}