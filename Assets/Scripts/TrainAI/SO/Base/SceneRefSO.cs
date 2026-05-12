using UnityEngine;
using TrainAI.Core;

namespace TrainAI.SO.Base
{
    [CreateAssetMenu(fileName = "SceneRef_New", menuName = "TrainAI/Data/Scene Ref")]
    public class SceneRefSO : ScriptableObject
    {
        public enum LoadMode { Single, Additive }

        [Required] public string sceneName;
        public LoadMode mode = LoadMode.Additive;
    }
}
