import BildirimlerScreen from "@/app/_shared/BildirimlerScreen";
import { EGITMEN_MENU_ITEMS } from "@/app/_shared/panelMenus";

// Eğitmen tarafı bildirimler sayfası ortak bildirim ekranını rol menüsüyle açar.
export default function EgitmenBildirimlerPage() {
    return (
        <BildirimlerScreen
            title="Bildirimler"
            subtitle="Kursların, öğrencilerin ve sistem mesajların burada görünür."
            menuItems={EGITMEN_MENU_ITEMS}
            activeMenuKey="bildirimler"
        />
    );
}
