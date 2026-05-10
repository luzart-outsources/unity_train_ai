namespace Luzart
{
    /// <summary>
    /// Trạng thái vòng đời của một UI instance.
    /// Tham khảo: Lumber Inc dùng Main/Sub visibility, ở đây mình dùng state machine
    /// tuyến tính cho đơn giản + thêm Paused tách biệt.
    /// </summary>
    public enum UIState
    {
        /// <summary>Chưa được load.</summary>
        None = 0,

        /// <summary>Đang download/load asset (Addressables async).</summary>
        Loading = 1,

        /// <summary>Asset đã load, instance đã tạo nhưng chưa show.</summary>
        Loaded = 2,

        /// <summary>Đang chạy animation show / OnBeforeShow.</summary>
        Showing = 3,

        /// <summary>Đang hiển thị, nhận input.</summary>
        Visible = 4,

        /// <summary>Đang visible nhưng bị popup khác đè lên (không nhận input).</summary>
        Paused = 5,

        /// <summary>Đang chạy animation hide.</summary>
        Hiding = 6,

        /// <summary>Đã hide, instance có thể còn trong pool.</summary>
        Hidden = 7,

        /// <summary>Instance đã destroy, asset sắp release.</summary>
        Releasing = 8,

        /// <summary>Asset đã release.</summary>
        Released = 9,
    }

    public enum UIHideReason
    {
        /// <summary>User click close hoặc back.</summary>
        UserRequested = 0,

        /// <summary>Code chủ động hide.</summary>
        Programmatic = 1,

        /// <summary>Popup khác đóng mình do exclusive trong lane.</summary>
        ReplacedByOther = 2,

        /// <summary>Scene change / game reset.</summary>
        SceneChange = 3,

        /// <summary>Cancellation token cancel.</summary>
        Cancelled = 4,

        /// <summary>Lỗi trong quá trình load/show.</summary>
        Error = 5,
    }

    public enum UICachePolicy
    {
        /// <summary>Hide xong destroy instance + release asset. Dùng cho UI hiếm mở.</summary>
        ReleaseOnClose = 0,

        /// <summary>Hide xong destroy instance, giữ asset. Dùng cho UI thỉnh thoảng mở.</summary>
        PoolOnClose = 1,

        /// <summary>Hide xong SetActive(false), giữ instance + asset. Dùng cho UI mở thường xuyên.</summary>
        KeepLoaded = 2,
    }
}
