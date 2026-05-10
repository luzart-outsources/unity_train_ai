/*
 * Deprecated
 * 
 * using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class RelationshipSaveState 
{
    public List<NPCRelationshipState> relationships;
    public List<AnimalRelationshipState> animals;

    public RelationshipSaveState(
        List<NPCRelationshipState> relationships,
        List<AnimalRelationshipState> animals)
    {
        this.relationships = relationships;
        this.animals = animals;
    }

    public static RelationshipSaveState Export()
    {
        return new RelationshipSaveState(RelationshipStats.relationships, AnimalStats.animalRelationships);
    }

    public void LoadData()
    {
        //RelationshipStats.LoadStats(relationships);
        AnimalStats.LoadStats();
    }
}
*/