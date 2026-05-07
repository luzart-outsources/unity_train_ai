
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

    public void SetExtractedSlots(Dictionary<string, string> slots)
    {
        _slots = slots ?? new Dictionary<string, string>();
    }

    public string Get(string key)
    {
        if (_slots != null && _slots.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v))
            return v;
        return _inner?.Get(key);
    }
}
