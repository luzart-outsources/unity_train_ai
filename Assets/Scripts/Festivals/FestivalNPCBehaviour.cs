using SoCollection;
using System;
using UnityEngine;


public class FestivalNPCBehaviour:ScriptableObject
{
    public CharacterData character;
    //The conditions needed for this festival behaviour to trigger for the NPC
    [SerializeField] BlackboardCondition[] blackboardConditions;

    public SoCollection<CutsceneAction> onInteract;

    public Vector3 position;
    public Vector3 rotation;
}
