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
