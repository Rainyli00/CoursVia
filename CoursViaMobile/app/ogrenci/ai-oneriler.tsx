import AiOnerilerScreen from "@/app/_shared/AiOnerilerScreen";
import { OGRENCI_MENU_ITEMS } from "@/app/_shared/panelMenus";

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