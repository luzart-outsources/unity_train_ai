using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimalSpawnManager : MonoBehaviour
{
    [SerializeField] Collider floor; 
    // Start is called before the first frame update
    void Start()
    {
        RenderAnimals();
    }

   public void RenderAnimals()
   {
        //Loop through the animal relationships and check if they should be here
        foreach(AnimalRelationshipState animalRelation in AnimalStats.animalRelationships)
        {
            AnimalData animalType = animalRelation.AnimalType(); 
            
            if(animalType.locationToSpawn == SceneTransitionManager.Instance.currentLocation)
            {
                //Randomize the coordinates the animal will spawn in based on the bounds of the floor collider
                Bounds bounds = floor.bounds;
                float offsetX = Random.Range(-bounds.extents.x, bounds.extents.x);
                float offsetZ = Random.Range(-bounds.extents.z, bounds.extents.z);

                Vector3 spawnPt = new Vector3(offsetX, floor.transform.position.y, offsetZ);

                float randomYRotation = Random.Range(0f, 360f);
                Quaternion randomRotation = Quaternion.Euler(0f, randomYRotation, 0f);

                //Spawn the animal and get the AnimalBehaviour component
                AnimalBehaviour animal = Instantiate(animalType.animalObject, spawnPt, randomRotation);
                //Load in the relationship data
                animal.LoadRelationship(animalRelation); 
            }
        }
   }
}
