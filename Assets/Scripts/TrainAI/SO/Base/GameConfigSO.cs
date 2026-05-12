using UnityEngine;
using TrainAI.Core;

namespace TrainAI.SO.Base
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "TrainAI/Config/Game")]
    public class GameConfigSO : ScriptableObject
    {
        public int totalDays = 30;
        public bool skipWeekend = true;
        public int startRenLuyen = 100;
        public int maxHocTap = 480;
        public int passHocTap = 288;
        public int passRenLuyen = 60;
        public BackendKind preferredBackend = BackendKind.GPUCompute;
    }
}
