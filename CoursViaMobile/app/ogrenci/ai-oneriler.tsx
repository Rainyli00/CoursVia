import AiOnerilerScreen from "@/app/_shared/AiOnerilerScreen";
import { OGRENCI_MENU_ITEMS } from "@/app/_shared/panelMenus";

// Öğrenci tarafı AI öneriler sayfası ortak liste ekranını öğrenci menüsüyle açar.
export default function OgrenciAiOnerilerPage() {
    return (
        <AiOnerilerScreen
            title="AI Öneriler"
            subtitle="Sana özel oluşturulan çalışma önerilerini buradan inceleyebilirsin."
            menuItems={OGRENCI_MENU_ITEMS}
            activeMenuKey="ai-oneriler"
        />
    );
}
