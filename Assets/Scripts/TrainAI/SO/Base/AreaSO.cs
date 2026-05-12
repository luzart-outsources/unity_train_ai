using UnityEngine;
using TrainAI.Core;

namespace TrainAI.SO.Base
{
    [CreateAssetMenu(fileName = "Area_New", menuName = "TrainAI/Data/Area")]
    public class AreaSO : ScriptableObject
    {
        [Required] public string id;
        public Vector3 worldPos;
        public Vector3 size = new Vector3(2, 2, 2);
        public SceneRefSO scene;
        public SceneRefSO subScene;
    }
}
