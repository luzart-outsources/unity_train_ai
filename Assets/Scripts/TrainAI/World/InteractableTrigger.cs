using TrainAI.Configs;
using TrainAI.Core.Bootstrap;
using UnityEngine;

namespace TrainAI.World
{
    // Gan vao GameObject co SphereCollider trigger trong scene. Khi player buoc vao,
    // raise InteractionManager.Enter; ra ngoai -> Exit.
    [RequireComponent(typeof(SphereCollider))]
    public class InteractableTrigger : MonoBehaviour
    {
        [SerializeField] private InteractableSO data;
        [SerializeField] private string playerTag = "Player";

        public InteractableSO Data => data;

        public void SetData(InteractableSO d) { data = d; ApplyRadius(); }

        private void OnValidate() => ApplyRadius();
        private void Awake() => ApplyRadius();

        private void ApplyRadius()
        {
            var col = GetComponent<SphereCollider>();
            if (col == null) return;
            col.isTrigger = true;
            if (data != null) col.radius = Mathf.Max(0.5f, data.interactRadius);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (data == null || GameServices.Interaction == null) return;
            if (!other.CompareTag(playerTag)) return;
            GameServices.Interaction.Enter(data, transform);
        }

        private void OnTriggerExit(Collider other)
        {
            if (data == null || GameServices.Interaction == null) return;
            if (!other.CompareTag(playerTag)) return;
            GameServices.Interaction.Exit(data);
        }
    }
}
