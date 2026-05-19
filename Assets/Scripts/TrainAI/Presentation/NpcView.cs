using TrainAI.Services;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.Presentation
{
    public class NpcView : MonoBehaviour
    {
        [SerializeField] NPCSO npcDef;
        [SerializeField] ServiceLocatorSO services;

        public NPCSO Definition => npcDef;

        // Runtime-only inject: NpcStagedSpawner instantiates the NPC prefab
        // a few frames after scene load and needs to wire NPCSO + locator
        // refs that the inspector can't pre-set on a freshly created instance.
        // Must run BEFORE Start() — call right after Instantiate().
        public void Configure(NPCSO def, ServiceLocatorSO svc)
        {
            npcDef = def;
            services = svc;
        }

        void Start()
        {
            if (services == null || !services.IsBootstrapped || npcDef == null) return;
            services.NPCs?.RegisterNpcTransform(npcDef.id, transform);
        }

        void OnDestroy()
        {
            if (services != null && services.Movement != null)
                services.Movement.Unregister(transform);
        }
    }
}
