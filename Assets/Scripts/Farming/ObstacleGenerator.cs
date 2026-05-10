using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleGenerator : MonoBehaviour
{
    //How much land do we want covered in obstacles
    [Range(1, 100)]
    public int percentageFilled;
    
    public void GenerateObstacles(List<Land> landPlots)
    {
        //Calculate how many plots to fill based on the percentage
        int plotsToFill = Mathf.RoundToInt((float)percentageFilled / 100 * landPlots.Count);

        //Get a list with the land IDs jumbled up
        List<int> shuffledList = ShuffleLandIndexes(landPlots.Count);
        
        for(int i = 0; i < plotsToFill; i++)
        {
            //Take the land id from the shuffled list
            int index = shuffledList[i];

            //Randomize what obstacle to spawn
            Land.FarmObstacleStatus status = (Land.FarmObstacleStatus) Random.Range(1, 4);

            //Set the land plot accordingly
            landPlots[index].SetObstacleStatus(status); 
        }

    }


    //Shuffle the indexes 
    List<int> ShuffleLandIndexes(int count)
    {
        List<int> listToReturn = new List<int>();
        for(int i = 0; i<count; i++)
        {
            int index = Random.Range(0, i + 1);
            listToReturn.Insert(index, i);
        }

        return listToReturn; 
    }
}
