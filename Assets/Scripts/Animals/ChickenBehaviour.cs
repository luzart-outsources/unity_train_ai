using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChickenBehaviour : AnimalBehaviour
{
    protected override void Start()
    {
        base.Start();
        LayEgg();
    }

    void LayEgg()
    {
        //Check the age 
        AnimalData animalType = AnimalStats.GetAnimalTypeFromString(relationship.animalType);

        //If not an adult, it cant lay eggs
        if(relationship.age < animalType.daysToMature)
        {
            return; 
        }

        //As long as the chicken is not sad
        if (relationship.Mood > 30 && !relationship.givenProduceToday)
        {
            //Lay an egg 
            ItemData egg = InventoryManager.Instance.GetItemFromString("Egg");
            GameStateManager.Instance.PersistentInstantiate(egg.gameModel, transform.position, Quaternion.identity);
            relationship.givenProduceToday = true; 
        }
    }
}
