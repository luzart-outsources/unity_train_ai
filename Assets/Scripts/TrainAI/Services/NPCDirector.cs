using System;
using System.Collections.Generic;
using TrainAI.Core;
using TrainAI.Core.Messages;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.Services
{
    public class NPCDirector : INPCDirector, IDisposable
    {
        readonly NPCDB _npcDB;
        readonly IMovementService _movement;
        readonly GameClockRSO _clock;
        readonly Dictionary<string, Transform> _registered = new();
        readonly Dictionary<string, AreaSO> _lastTarget = new();
        // Reused scratch list so the per-minute schedule scan doesn't alloc.
        // Also guards against "Collection was modified" if an NPC strategy
        // Tick path ends up registering/unregistering an agent mid-iteration.
        readonly List<NPCSO> _scratch = new();

        public NPCDirector(NPCDB npcDB, IMovementService movement, GameClockRSO clock)
        {
            _npcDB = npcDB;
            _movement = movement;
            _clock = clock;
            BroadcastService.Subscribe<TimeTickMsg>(OnTimeTick);
        }

        public void Dispose() => BroadcastService.Unsubscribe<TimeTickMsg>(OnTimeTick);

        public void RegisterNpcTransform(string npcId, Transform t)
        {
            if (string.IsNullOrEmpty(npcId) || t == null) return;
            _registered[npcId] = t;
            var npc = _npcDB != null ? _npcDB.ById(npcId) : null;
            if (npc != null && npc.movement != null) _movement?.RegisterAgent(t, npc.movement);
        }

        void OnTimeTick(TimeTickMsg msg)
        {
            if (_npcDB == null || _npcDB.all == null) return;
            _scratch.Clear();
            // Snapshot reference list so SetTarget side-effects can't mutate
            // _npcDB.all during enumeration.
            for (int i = 0; i < _npcDB.all.Count; i++) _scratch.Add(_npcDB.all[i]);

            for (int i = 0; i < _scratch.Count; i++)
            {
                var npc = _scratch[i];
                if (npc == null || npc.schedule == null) continue;
                if (!_registered.TryGetValue(npc.id, out var t) || t == null) continue;
                try
                {
                    var entry = npc.schedule.GetEntryAt(_clock.day, msg.hour, msg.minute);
                    if (entry.target == null) continue;
                    if (_lastTarget.TryGetValue(npc.id, out var prev) && prev == entry.target) continue;
                    _lastTarget[npc.id] = entry.target;
                    _movement?.SetTarget(t, entry.target.worldPos);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[NPCDirector] schedule eval for '{npc.id}' threw: {e.Message}");
                }
            }
        }

        public void Tick(float dt) { }
    }
}
