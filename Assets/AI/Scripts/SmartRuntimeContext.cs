// SmartRuntimeContext.cs
//
// Phase A v2 — IRuntimeContext implementation that prefers slot-extracted values
// over hardcoded game-state fallbacks.
//
// Workflow per user message:
//   1. Tester gọi `ctx.SetExtractedSlots(extractor.Extract(userText))` trước khi
//      gọi `brain.Respond(userText)`.
//   2. Brain.Substitute("{place}") → ctx.Get("place") → trả về slot extracted
//      (vd: "khu A") nếu có; ngược lại fall back về `_inner.Get("place")`
//      (DummyContext logic — chỉ dùng cho ô slot model không trích được, vd
//      {direction}, {distance}, {scheduled_today}, {meal_time}).
//
// Kết quả: user hỏi "khu A ở đâu" → response "khu A ở phía đông doanh trại,
// đi thẳng 100m là tới" thay vì "khu A nằm ở khu B5" (sai về mặt logic).

using System.Collections.Generic;

public class SmartRuntimeContext : IRuntimeContext
{
    private readonly IRuntimeContext _inner;
    private Dictionary<string, string> _slots;

    public SmartRuntimeContext(IRuntimeContext fallback = null)
    {
        _inner = fallback ?? new DummyContext();
        _slots = new Dictionary<string, string>();
    }

    /// <summary>Set the slot extraction for the next Respond() call.</summary>
    public void SetExtractedSlots(Dictionary<string, string> slots)
    {
        _slots = slots ?? new Dictionary<string, string>();
    }

    /// <summary>Resolve a placeholder key. Order: extracted slots → inner fallback.</summary>
    public string Get(string key)
    {
        if (_slots != null && _slots.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v))
            return v;
        return _inner?.Get(key);
    }
}
