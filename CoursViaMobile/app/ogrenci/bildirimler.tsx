import BildirimlerScreen from "@/app/_shared/BildirimlerScreen";
import { OGRENCI_MENU_ITEMS } from "@/app/_shared/panelMenus";

// Öğrenci tarafı bildirimler sayfası ortak bildirim ekranını öğrenci menüsüyle açar.
export default function OgrenciBildirimlerPage() {
    return (
        <BildirimlerScreen
            title="Bildirimler"
            subtitle="Duyurularını, uyarılarını ve sistem mesajlarını buradan takip edebilirsin."
            menuItems={OGRENCI_MENU_ITEMS}
            activeMenuKey="bildirimler"
        />
    );
}
