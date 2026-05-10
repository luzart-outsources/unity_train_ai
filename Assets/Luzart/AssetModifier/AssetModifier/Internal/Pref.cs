#if UNITY_EDITOR
using UnityEditor;
using Object = UnityEngine.Object;

namespace AssetModifier.Internal
{
    /// <summary>
    /// Lightweight wrappers around EditorPrefs for the window's persistent per-user settings.
    /// Pure Unity APIs only.
    /// </summary>
    internal abstract class PrefBase<T>
    {
        public readonly string Key;
        protected T _cached;

        public System.Action OnValueChanged;

        protected PrefBase(string key) { Key = key; }

        public T Value
        {
            get => _cached;
            set => TrySetValue(value);
        }

        public void TrySetValue(T v)
        {
            if (Equals(v, _cached)) return;
            _cached = v;
            Save();
            OnValueChanged?.Invoke();
        }

        protected abstract void Save();

        public static implicit operator T(PrefBase<T> p) => p._cached;
    }

    internal sealed class PrefBool : PrefBase<bool>
    {
        public PrefBool(string key, bool def = false) : base(key) { _cached = EditorPrefs.GetBool(key, def); }
        protected override void Save() => EditorPrefs.SetBool(Key, _cached);
    }

    internal sealed class PrefInt : PrefBase<int>
    {
        public PrefInt(string key, int def = 0) : base(key) { _cached = EditorPrefs.GetInt(key, def); }
        protected override void Save() => EditorPrefs.SetInt(Key, _cached);
    }

    internal sealed class PrefFloat : PrefBase<float>
    {
        public PrefFloat(string key, float def = 0f) : base(key) { _cached = EditorPrefs.GetFloat(key, def); }
        protected override void Save() => EditorPrefs.SetFloat(Key, _cached);
    }

    internal sealed class PrefString : PrefBase<string>
    {
        public PrefString(string key, string def = "") : base(key) { _cached = EditorPrefs.GetString(key, def); }
        protected override void Save() => EditorPrefs.SetString(Key, _cached ?? "");
    }

    /// <summary>
    /// Persists an Object reference via GlobalObjectId (slow but robust across reloads).
    /// </summary>
    internal sealed class PrefObject
    {
        public readonly string Key;
        Object _cached;
        bool _loaded;

        public PrefObject(string key) { Key = key; }

        public Object Value
        {
            get { EnsureLoaded(); return _cached; }
            set => TrySetValue(value);
        }

        public void TrySetValue(Object v)
        {
            EnsureLoaded();
            if (v == _cached) return;
            _cached = v;
            if (v == null) EditorPrefs.DeleteKey(Key);
            else
            {
                var gid = GlobalObjectId.GetGlobalObjectIdSlow(v);
                EditorPrefs.SetString(Key, gid.ToString());
            }
        }

        void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            var s = EditorPrefs.GetString(Key, "");
            if (!string.IsNullOrEmpty(s) && GlobalObjectId.TryParse(s, out var gid))
            {
                _cached = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
            }
        }

        public static implicit operator Object(PrefObject p) => p.Value;
    }
}
#endif
