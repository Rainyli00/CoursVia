import type { PanelMenuItem } from "@/app/_shared/PanelLayout";

// Öğrenci panelinde kullanılacak sol sidebar menüsü.
export const OGRENCI_MENU_ITEMS: PanelMenuItem[] = [
    {
        key: "dashboard",
        label: "Dashboard",
        description: "Genel öğrenci özeti",
        icon: "home",
        href: "/ogrenci",
    },
    {
        key: "kesfet",
        label: "Keşfet",
        description: "Yeni kurslara göz at",
        icon: "search",
        href: "/ogrenci/kesfet",
    },
    {
        key: "kurslarim",
        label: "Kurslarım",
        description: "Kayıtlı kursların",
        icon: "book-open",
        href: "/ogrenci/kurslarim",
    },
    {
        key: "sinavlarim",
        label: "Sınavlarım",
        description: "Sınav durumların",
        icon: "edit-3",
        href: "/ogrenci/sinavlarim",
    },
    {
        key: "ai-oneriler",
        label: "AI Öneriler",
        description: "Kişisel çalışma önerilerin",
        icon: "zap",
        href: "/ogrenci/ai-oneriler",
    },
    {
        key: "sertifikalarim",
        label: "Sertifikalarım",
        description: "Kazandığın sertifikalar",
        icon: "award",
        href: "/ogrenci/sertifikalarim",
    },
    {
        key: "bildirimler",
        label: "Bildirimler",
        description: "Duyurular ve uyarılar",
        icon: "bell",
        href: "/ogrenci/bildirimler",
    },
];

// Eğitmen panelinde kullanılacak sol sidebar menüsü.
export const EGITMEN_MENU_ITEMS: PanelMenuItem[] = [
    {
        key: "dashboard",
        label: "Dashboard",
        description: "Genel eğitmen özeti",
        icon: "home",
        href: "/egitmen",
    },
    {
        key: "kurslarim",
        label: "Kurslarım",
        description: "Yayın, taslak ve onay durumları",
        icon: "book-open",
        href: "/egitmen/kurslarim",
    },
    {
        key: "ogrencilerim",
        label: "Öğrencilerim",
        description: "Kurslarına kayıtlı öğrenciler",
        icon: "users",
        href: "/egitmen/ogrencilerim",
    },
    {
        key: "ai-oneriler",
        label: "AI Öneriler",
        description: "Kurs geliştirme önerileri",
        icon: "zap",
        href: "/egitmen/ai-oneriler",
    },
    {
        key: "bildirimler",
        label: "Bildirimler",
        description: "Duyurular ve uyarılar",
        icon: "bell",
        href: "/egitmen/bildirimler",
    },
];

export const ADMIN_MENU_ITEMS: PanelMenuItem[] = [
    {
        key: "dashboard",
        label: "Dashboard",
        description: "Genel admin özeti",
        icon: "home",
        href: "/admin",
    },
    {
        key: "kullanicilar",
        label: "Kullanıcılar",
        description: "Kullanıcı özetleri",
        icon: "users",
        href: "/admin/kullanicilar",
    },
    {
        key: "kurslar",
        label: "Kurslar",
        description: "Kurs durumları",
        icon: "book-open",
        href: "/admin/kurslar",
    },
    {
        key: "egitmen-basvurulari",
        label: "Eğitmen Başvuruları",
        description: "Onay bekleyen başvurular",
        icon: "user-check",
        href: "/admin/egitmen-basvurulari",
    },
    {
        key: "loglar",
        label: "Admin Logları",
        description: "Sistem işlem kayıtları",
        icon: "file-text",
        href: "/admin/loglar",
    },
    {
        key: "bildirimler",
        label: "Bildirimler",
        description: "Duyurular ve uyarılar",
        icon: "bell",
        href: "/admin/bildirimler",
    },
];