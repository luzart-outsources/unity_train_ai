using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AnimalRenderer : MonoBehaviour
{
    NavMeshAgent agent;
    [SerializeField]
    Animator childModel, adultModel;

    Animator animatorToWorkWith; 
    int age;
    AnimalData animalType;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>(); 
    }

    public void RenderAnimal(int age, string animalName)
    {
        animalType = AnimalStats.GetAnimalTypeFromString(animalName);
        this.age = age;

        animatorToWorkWith = (age >= animalType.daysToMature) ? adultModel : childModel;

        childModel.gameObject.SetActive(false);
        adultModel.gameObject.SetActive(false);

        //This is the model we will work with
        animatorToWorkWith.gameObject.SetActive(true); 
    }

    private void Update()
    {
        if(animatorToWorkWith != null)
        {
            animatorToWorkWith.SetBool("Walk", agent.velocity.sqrMagnitude > 0);
        }
    }
}
