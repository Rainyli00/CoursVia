import AiOnerilerScreen from "@/app/_shared/AiOnerilerScreen";
import { EGITMEN_MENU_ITEMS } from "@/app/_shared/panelMenus";

// Eğitmen tarafı AI öneriler sayfası ortak liste ekranını rol menüsüyle açar.
export default function EgitmenAiOnerilerPage() {
    return (
        <AiOnerilerScreen
            title="AI Öneriler"
            subtitle="Kursların için oluşturulan AI içerik geliştirme önerilerini buradan inceleyebilirsin."
            menuItems={EGITMEN_MENU_ITEMS}
            activeMenuKey="ai-oneriler"
        />
    );
}
