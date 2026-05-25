import BildirimlerScreen from "@/app/_shared/BildirimlerScreen";
import { OGRENCI_MENU_ITEMS } from "@/app/_shared/panelMenus";

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