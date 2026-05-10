namespace Luzart
{
    /// <summary>
    /// Compile-time ID cho UI. Dev dùng khi gọi trong code.
    /// Thêm UI mới: (1) thêm entry ở đây, (2) thêm entry vào UIRegistrySO asset.
    ///
    /// Quy ước đánh số:
    ///   0xxx: System (Loading, Disconnect, Alert...)
    ///   1xxx: Screen (MainMenu, Lobby, Map, CharacterSelect...)
    ///   2xxx: Popup (Inventory, Shop, Quest, Mail...)
    ///   3xxx: Hud (HealthBar, Minimap, Chat...)
    ///   4xxx: Toast
    ///   5xxx: WorldOverlay
    ///
    /// Nếu cần string ID cho server-driven popup, config ở UIConfig.StringId.
    /// </summary>
    public enum UIId
    {
        None = 0,

        // --- System (0xxx) ---
        Loading = 1,
        Disconnect = 2,
        Alert = 3,
        ForceUpdate = 4,
        Notice = 5,

        // --- Screen (1xxx) ---
        Splash = 1000,
        Login = 1001,
        CharacterSelect = 1002,
        MainMenu = 1003,
        MapView = 1004,
        Lobby = 1005,
        Register = 1006,
        ForgotPass = 1007,
        SelectLogin = 1008,
        PanelSelectServer = 1009,
        CreateCharacter = 1010,

        // --- Popup (2xxx) ---
        Inventory = 2000,
        Shop = 2001,
        QuestBoard = 2002,
        QuestReward = 2003,
        Mail = 2004,
        Settings = 2005,
        Friend = 2006,
        Clan = 2007,

        // --- Hud (3xxx) ---
        GameplayHud = 3000,
        ChatBox = 3001,
        Minimap = 3002,

        // --- Toast (4xxx) ---
        Toast = 4000,

        // --- WorldOverlay (5xxx) ---
        DamageNumber = 5000,
        NameTag = 5001,
    }
}
