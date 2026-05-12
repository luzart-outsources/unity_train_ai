using UnityEngine;
using TrainAI.Core;

namespace TrainAI.SO.Base
{
    [CreateAssetMenu(fileName = "PlayerStateRSO", menuName = "TrainAI/Runtime/Player State")]
    public class PlayerStateRSO : RuntimeSO
    {
        public string playerName;
        public int hocTap;
        public int renLuyen;
        public string currentScene;
        public Vector3 lastWorldPos;

        public override void Reset()
        {
            playerName = "";
            hocTap = 0;
            renLuyen = 100;
            currentScene = "10_World";
            lastWorldPos = Vector3.zero;
        }
    }
}
