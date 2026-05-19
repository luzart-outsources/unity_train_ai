using TrainAI.Core;
using TrainAI.Core.Messages;
using TrainAI.SO.Base;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrainAI.Presentation
{
    public class QuestArrowHUD : MonoBehaviour
    {
        [SerializeField] Transform arrow;
        [SerializeField] float headOffset = 2.5f;
        [SerializeField] float bobAmplitude = 0.15f;
        [SerializeField] float bobFrequency = 1.5f;
        [SerializeField] float pulseMin = 0.9f;
        [SerializeField] float pulseMax = 1.1f;
        // Arrow is a "you have a quest, go there" navigator — only meaningful in
        // the open-world scene. Inside sub-scenes (LopHoc / NhaAn / KTX) the
        // player is already AT the destination, so the arrow becomes visual
        // clutter and worse — points at the world-space area position from
        // inside a different scene, which is nonsensical. Default to 10_World
        // only; SceneManager.activeSceneChanged toggles arrow visibility.
        const string WorldSceneName = "10_World";
        bool _inWorldScene;

        Vector3 _baseScale;
        Vector3 _baseLocalPos;
        QuestSO _activeQuest;

        void Awake()
        {
            if (arrow == null && transform.childCount > 0)
                arrow = transform.GetChild(0);

            if (arrow != null)
            {
                // Force arrow to sit above the player's head so it's always visible.
                var lp = arrow.localPosition;
                if (lp.y < headOffset - 0.01f) lp.y = headOffset;
                arrow.localPosition = lp;

                _baseScale = arrow.localScale;
                _baseLocalPos = arrow.localPosition;
                arrow.gameObject.SetActive(false);
            }
        }

        void OnEnable()
        {
            BroadcastService.Subscribe<QuestActivatedMsg>(OnActivated);
            BroadcastService.Subscribe<QuestCompletedMsg>(OnEnded);
            BroadcastService.Subscribe<QuestMissedMsg>(OnEnded);
            SceneManager.activeSceneChanged += OnSceneSwap;
            SceneManager.sceneLoaded += OnSceneLoaded;
            _inWorldScene = SceneManager.GetActiveScene().name == WorldSceneName;
            ApplyVisibility();
        }

        void OnDisable()
        {
            BroadcastService.Unsubscribe<QuestActivatedMsg>(OnActivated);
            BroadcastService.Unsubscribe<QuestCompletedMsg>(OnEnded);
            BroadcastService.Unsubscribe<QuestMissedMsg>(OnEnded);
            SceneManager.activeSceneChanged -= OnSceneSwap;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneSwap(Scene _, Scene next)
        {
            _inWorldScene = next.name == WorldSceneName;
            InvalidateMarkerCache();
            ApplyVisibility();
        }

        // Belt-and-suspenders: LoadReplacing uses additive load then sets the
        // new scene active, but the activeSceneChanged event order depends on
        // whether Unity flips active before or after the additive completes.
        // sceneLoaded always fires; resync here as well.
        void OnSceneLoaded(Scene s, LoadSceneMode _)
        {
            _inWorldScene = SceneManager.GetActiveScene().name == WorldSceneName;
            InvalidateMarkerCache();
            ApplyVisibility();
        }

        void OnActivated(QuestActivatedMsg m)
        {
            _activeQuest = m.quest as QuestSO;
            ApplyVisibility();
        }

        void OnEnded<T>(T _)
        {
            _activeQuest = null;
            ApplyVisibility();
        }

        void ApplyVisibility()
        {
            if (arrow == null) return;
            bool hasArea = _activeQuest != null && _activeQuest.area != null;
            arrow.gameObject.SetActive(hasArea && _inWorldScene);
        }

        void Update()
        {
            if (arrow == null || _activeQuest == null || _activeQuest.area == null || !_inWorldScene) return;

            // Prefer the scene-side InteractableMarker position over the static
            // AreaSO.worldPos field. When designers move buildings in the scene
            // they don't always update the SO's worldPos, so the arrow ended
            // up pointing at empty grass. Reading the live marker keeps the
            // arrow accurate even after re-layouts.
            Vector3 targetPos = ResolveTargetPos(_activeQuest.area);
            Vector3 worldArrow = arrow.position;
            Vector3 look = new Vector3(targetPos.x, worldArrow.y, targetPos.z);
            if ((look - worldArrow).sqrMagnitude > 0.001f)
                arrow.LookAt(look);

            float t = Time.time;
            float bob = Mathf.Sin(t * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
            arrow.localPosition = _baseLocalPos + new Vector3(0f, bob, 0f);
            float pulse = Mathf.Lerp(pulseMin, pulseMax, (Mathf.Sin(t * 3f) + 1f) * 0.5f);
            arrow.localScale = _baseScale * pulse;
        }

        // Scene-side marker cache, invalidated on every sceneLoaded /
        // activeSceneChanged. Previous version called FindObjectsByType
        // every Update — fine in isolation but combined with NPC spawning
        // contributed to first-frame CPU pressure during play-mode entry.
        // Re-scan only when the scene composition could have changed.
        static readonly System.Collections.Generic.Dictionary<AreaSO, Transform> _markerCache = new();
        bool _cacheDirty = true;

        void InvalidateMarkerCache() { _cacheDirty = true; }

        void RebuildMarkerCache()
        {
            _markerCache.Clear();
            var all = UnityEngine.Object.FindObjectsByType<InteractableMarker>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                var m = all[i];
                if (m == null || m.interactable == null || m.interactable.area == null) continue;
                var area = m.interactable.area;
                if (!_markerCache.TryGetValue(area, out var existing) || existing == null)
                { _markerCache[area] = m.transform; continue; }
                // Prefer the marker whose XZ is closest to the AreaSO's
                // anchor (the "intended" interaction point when multiple
                // markers reference the same area).
                Vector3 anchor = area.worldPos;
                float dExisting = (existing.position - anchor).sqrMagnitude;
                float dCand = (m.transform.position - anchor).sqrMagnitude;
                if (dCand < dExisting) _markerCache[area] = m.transform;
            }
            _cacheDirty = false;
        }

        Vector3 ResolveTargetPos(AreaSO area)
        {
            if (_cacheDirty) RebuildMarkerCache();
            if (_markerCache.TryGetValue(area, out var t) && t != null) return t.position;
            return area.worldPos;
        }
    }
}
