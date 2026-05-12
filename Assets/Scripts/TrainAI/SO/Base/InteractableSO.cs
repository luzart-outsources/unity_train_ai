using UnityEngine;
using TrainAI.Core;

namespace TrainAI.SO.Base
{
    [CreateAssetMenu(fileName = "Interactable_New", menuName = "TrainAI/Data/Interactable")]
    public class InteractableSO : ScriptableObject
    {
        [Required] public string id;
        [Required] public AreaSO area;
        [Required] public InteractionSO onInteract;
        public string conditionExpr;
    }
}
