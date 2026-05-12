using UnityEngine;

namespace TrainAI.SO.Base
{
    [CreateAssetMenu(fileName = "ResponseTemplates_New", menuName = "TrainAI/Data/Response Templates")]
    public class ResponseTemplatesSO : ScriptableObject
    {
        public TextAsset responsesJson;
    }
}
