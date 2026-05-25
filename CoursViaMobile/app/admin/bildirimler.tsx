import BildirimlerScreen from "@/app/_shared/BildirimlerScreen";
import { ADMIN_MENU_ITEMS } from "@/app/_shared/panelMenus";

// Admin bildirimler ekranı.
// Ortak mobil bildirim ekranını kullanır.
export default function AdminBildirimlerScreen() {
    return (
        <BildirimlerScreen
            title="Bildirimler"
            subtitle="Admin bildirimlerini buradan takip edebilirsin."
            menuItems={ADMIN_MENU_ITEMS}
            activeMenuKey="bildirimler"
        />
    );
}