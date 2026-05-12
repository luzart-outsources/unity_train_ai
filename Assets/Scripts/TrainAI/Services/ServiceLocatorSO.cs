using UnityEngine;

namespace TrainAI.Services
{
    [CreateAssetMenu(fileName = "ServiceLocator", menuName = "TrainAI/Service Locator")]
    public class ServiceLocatorSO : ScriptableObject
    {
        public void Bootstrap()
        {
            Debug.Log("[ServiceLocator] Bootstrap stub - no services wired yet.");
        }
    }
}
