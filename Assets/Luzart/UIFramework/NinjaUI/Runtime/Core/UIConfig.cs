using System;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Metadata cho 1 UI entry. Giu MUC TOI THIEU can cho framework dieu phoi —
    /// visual/layout/raycast/backdrop thuoc ve prefab, khong thuoc config.
    ///
    /// Quy tac them field: framework phai DOC field do o runtime. Neu chi UI tu
    /// doc config cua chinh no -> khong can o day, dat trong UI script hoac prefab.
    /// </summary>
    [Serializable]
    public class UIConfig
    {
        // Identity (required)
        [Header("Identity")]
        [Tooltip("Enum ID dev goi trong code: UIManager.ShowAsync(UIId.Inventory, ...).")]
        public UIId Id;

        [Tooltip("String ID cho server-driven popup. De trong neu client-only.\n" +
                 "Vi du: server gui {\"type\":\"quest_reward\"} -> map \"quest_reward\".")]
        public string StringId;

        // Asset (required) - direct prefab reference, khong qua Addressable.
        [Header("Asset")]
        [Tooltip("Prefab UI. Root phai co component ke thua UIBase. Drag-drop tu Project window.")]
        public GameObject AssetRef;

        // Routing (required)
        [Header("Routing")]
        [Tooltip("Lane UI se thuoc ve - quyet dinh stack behavior (LIFO pause, flat list, topmost...).")]
        public UILayer Lane = UILayer.Popup;

        [Tooltip("Hide xong lam gi voi instance.\n" +
                 "- ReleaseOnClose: destroy instance, GC asset (asset reload lan sau).\n" +
                 "- PoolOnClose: destroy instance, KHONG release asset (instantiate lan sau nhanh).\n" +
                 "- KeepLoaded: SetActive(false), giu instance trong pool.")]
        public UICachePolicy CachePolicy = UICachePolicy.PoolOnClose;

        // Optional overrides
        [Header("Optional")]
        [Tooltip("Preload luc boot. BAT BUOC true cho Loading/Disconnect/Toast " +
                 "(UI phai chay duoc khi mat mang).")]
        public bool PreloadOnBoot = false;

        [Tooltip("Cho phep nhieu instance dong thoi.\n" +
                 "true: Toast, DamageNumber, FloatingText.\n" +
                 "false (da so): Inventory, Shop, MainMenu...")]
        public bool AllowMultiInstance = false;

        [Tooltip("ESC/Android back co dong UI nay khong.\n" +
                 "false: popup Reward bat buoc xem, quang cao, force-update.\n" +
                 "true (da so): popup thuong.")]
        public bool DismissByEscape = true;

        [Tooltip("Khi bi popup khac de len -> pause giu state (thay vi hide).\n" +
                 "Chi co hieu luc voi lane Popup/Screen. Cac lane khac bo qua.")]
        public bool PausableWhenOverlaid = true;

        public bool IsValid =>
            Id != UIId.None && AssetRef != null;
    }
}
