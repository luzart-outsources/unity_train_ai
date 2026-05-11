using System.Collections.Generic;
using TrainAI.Configs;
using TrainAI.Core.Events;
using TrainAI.Core.Time;
using UnityEngine;

namespace TrainAI.World
{
    // GDD: NPC hoc sinh di chuyen tu do theo thoi gian bieu.
    // User: "agent tim duong gan cho con NPC".
    // Implementation don gian: Vector3.MoveTowards toi WorldAnchor co ten khop voi
    // NPCScheduleSO.entries[hour].locationKey. Khong dung ML.
    public class NPCWaypointAgent : MonoBehaviour
    {
        [Header("Profile")]
        [SerializeField] private NPCProfileSO profile;

        [Header("Movement")]
        [Tooltip("Khoang cach <= X coi nhu da toi.")]
        [SerializeField] private float arriveDistance = 0.5f;
        [SerializeField] private float turnSpeed = 6f;

        [Header("Anchor lookup")]
        [Tooltip("Prefix de tim Anchor trong scene. Vd 'Anchor_SanVanDong'.")]
        [SerializeField] private string anchorPrefix = "Anchor_";

        private Transform _currentTarget;
        private string _currentKey;
        private float _refreshTimer;

        public NPCProfileSO Profile => profile;
        public void SetProfile(NPCProfileSO p) => profile = p;
        public Transform CurrentTarget => _currentTarget;
        public string CurrentLocationKey => _currentKey;

        private void OnEnable()
        {
            GameEvents.TimeTick += OnTick;
            RefreshTargetFromSchedule();
        }

        private void OnDisable()
        {
            GameEvents.TimeTick -= OnTick;
        }

        private void OnTick(GameTime t)
        {
            // Refresh target moi gio (minute=0) hoac khi minute=30 (de bat kip thay doi giua gio).
            if (t.minute == 0 || t.minute == 30)
                RefreshTargetFromSchedule(t);
        }

        private void RefreshTargetFromSchedule(GameTime? now = null)
        {
            if (profile == null || profile.schedule == null) return;
            int hour;
            if (now.HasValue) hour = now.Value.hour;
            else if (TrainAI.Core.Bootstrap.GameServices.Time != null) hour = TrainAI.Core.Bootstrap.GameServices.Time.Now.hour;
            else return;

            string key = profile.schedule.GetLocationAtHour(hour);
            if (string.IsNullOrEmpty(key)) return;
            if (key == _currentKey && _currentTarget != null) return;

            var anchor = FindAnchor(key);
            if (anchor == null)
            {
                // Khong co anchor tuong ung trong scene -> dung im. Khong throw.
                _currentTarget = null;
                _currentKey = key;
                return;
            }
            _currentTarget = anchor;
            _currentKey = key;
        }

        private Transform FindAnchor(string key)
        {
            string fullName = anchorPrefix + key;
            // GameObject.Find chay 1 lan moi gio -> chi roi.
            var go = GameObject.Find(fullName);
            if (go != null) return go.transform;
            // Fallback: tim tag = key (it dung).
            return null;
        }

        private void Update()
        {
            // Periodic re-search neu target null (vd anchor spawn sau).
            _refreshTimer -= UnityEngine.Time.deltaTime;
            if (_refreshTimer <= 0f)
            {
                _refreshTimer = 2f;
                if (_currentTarget == null) RefreshTargetFromSchedule();
            }
            if (_currentTarget == null || profile == null) return;

            float dist = Vector3.Distance(transform.position, _currentTarget.position);
            if (dist <= arriveDistance) return;

            float speed = profile.moveSpeed;
            Vector3 dir = (_currentTarget.position - transform.position);
            dir.y = 0;
            if (dir.sqrMagnitude < 0.0001f) return;
            Vector3 dirN = dir.normalized;

            transform.position += dirN * speed * UnityEngine.Time.deltaTime;

            Quaternion targetRot = Quaternion.LookRotation(dirN);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * UnityEngine.Time.deltaTime);
        }
    }
}
