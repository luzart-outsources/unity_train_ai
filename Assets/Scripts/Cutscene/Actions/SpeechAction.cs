using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeechAction : CutsceneAction
{
    public List<DialogueLine> dialogueLines;

    public override void Execute()
    {
        Debug.Log("Cutscene: Executing Speech Action");
        DialogueManager.Instance.StartDialogue(dialogueLines, onExecutionComplete); 
    }
}
