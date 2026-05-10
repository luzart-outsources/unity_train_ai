using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Animals/Animal")]
public class AnimalData : ScriptableObject
{
    public Sprite portrait;
    public AnimalBehaviour animalObject; 

    //The price the player purchases the animal for
    public int purchasePrice;

    //The days before the animal becomes an adult
    public int daysToMature;

    //The item the animal will produce
    public ItemData produce;

    //The location the animal should spawn in 
    public SceneTransitionManager.Location locationToSpawn; 
}
