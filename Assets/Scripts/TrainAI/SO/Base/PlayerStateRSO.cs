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
        // Snapshot of player's world-scene position taken just before
        // transitioning into a sub-scene (NhaAn / LopHoc / KyTucXa). When
        // returning to 10_World the player is warped back here instead of
        // being left at the sub-scene's PlayerSpawn coord — so they end up
        // standing at the same door they used to enter.
        public Vector3 worldExitPos;

        public override void Reset()
        {
            playerName = "";
            hocTap = 0;
            renLuyen = 100;
            currentScene = "10_World";
            lastWorldPos = Vector3.zero;
            worldExitPos = Vector3.zero;
        }
    }
}
