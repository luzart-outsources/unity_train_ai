using UnityEngine;

namespace TrainAI.SO.Base
{
    [CreateAssetMenu(fileName = "ThirdPersonCameraConfig", menuName = "TrainAI/Config/Third Person Camera")]
    public class ThirdPersonCameraConfigSO : ScriptableObject
    {
        public float distance = 3.5f;
        public float height = 1.6f;
        public float yawSpeed = 120f;
        public float pitchMin = -20f;
        public float pitchMax = 50f;
        public float smoothTime = 0.1f;
        public bool invertY = false;
    }
}
