namespace Luzart
{
    /// <summary>
    /// Phân loại UI theo lane. Thứ tự enum = thứ tự sort order mặc định.
    /// Cao hơn = render trên.
    /// </summary>
    public enum UILayer
    {
        /// <summary>UI gắn 3D (name tag, damage number, floating text).</summary>
        WorldOverlay = 0,

        /// <summary>Màn hình chính: Lobby, MapView, CharacterSelect. Exclusive trong lane.</summary>
        Screen = 1,

        /// <summary>HUD gameplay: HP/MP, minimap, chat, quick slot. Persistent.</summary>
        Hud = 2,

        /// <summary>Popup modal: Inventory, Shop, Quest, Mail. Stack LIFO + pause/resume.</summary>
        Popup = 3,

        /// <summary>Hệ thống: Loading, disconnect, maintenance, force-update. Topmost.</summary>
        System = 4,

        /// <summary>Thông báo nhanh, queue riêng, auto-fade.</summary>
        Toast = 5,
    }
}
