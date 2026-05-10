using TrainAI.Core.Bootstrap;
using UnityEngine;

namespace TrainAI.Core.Time
{
    // MonoBehaviour adapter - call moi frame de drive TimeManager.
    // Gan vao GameBootstrap GO trong scene _Boot.
    public class TimeTickDriver : MonoBehaviour
    {
        private void Update()
        {
            var t = GameServices.Time;
            if (t == null) return;
            t.Tick(UnityEngine.Time.deltaTime);
        }
    }
}
