import AiOnerilerScreen from "@/app/_shared/AiOnerilerScreen";
import { EGITMEN_MENU_ITEMS } from "@/app/_shared/panelMenus";

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