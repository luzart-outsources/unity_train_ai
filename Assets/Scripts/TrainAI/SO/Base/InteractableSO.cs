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
        // When true, QuestRouter.IsInteractableAllowed returns true regardless
        // of the active quest's area. Set this on in-sub-scene interactables
        // (the dining table in NhaAn, the bunk in KTX, the desk in LopHoc)
        // so the player can ALWAYS eat / sleep / quiz once they've entered
        // the building, and can ALWAYS leave back to the world after — even
        // if AutoChain advanced the clock to a different quest while they
        // were inside. World-side door interactables stay gated so the player
        // can't open the dorm door during a quiz quest, etc.
        public bool bypassQuestGate;
    }
}
