using UnityEngine;

//Unlocks the character
public class UnlockCharacterAction : CutsceneAction
{
    [SerializeField]
    CharacterData characterToUnlock;

    public override void Execute()
    {
        if (characterToUnlock == null)
        {
            Debug.LogError("Character not set");
            onExecutionComplete?.Invoke();
            return;
        }

        RelationshipStats.UnlockCharacter(characterToUnlock);
        onExecutionComplete?.Invoke(); 
    }
}
