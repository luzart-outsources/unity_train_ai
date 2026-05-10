using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// The location of the NPC at the point of time
/// </summary>
[System.Serializable]
public struct NPCLocationState
{
    public CharacterData character;
    public SceneTransitionManager.Location location;
    public Vector3 coord;
    public Vector3 facing; 

    public NPCLocationState(CharacterData character) : this()
    {
        this.character = character;
    }

    public NPCLocationState(CharacterData character, SceneTransitionManager.Location location, Vector3 coord, Vector3 facing)
    {
        this.character = character;
        this.location = location;
        this.coord = coord;
        this.facing = facing;
    }
}
