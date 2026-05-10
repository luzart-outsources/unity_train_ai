
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager Instance { get; private set; }

    //The list of all the actors in the scene
    Dictionary<string, NavMeshAgent> actors;

    //The player prefab
    [SerializeField] PlayerController player;
    //Whether default behaviour should be triggered on Location load
    bool pause = false;

    private void Awake()
    {
        //If there is more than one instance, destroy the extra
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            //Set the static instance to this instance
            Instance = this;
        }
    }

    const string CUTSCENE_PREFIX = "Cutscene";

    Queue<CutsceneAction> actionsToExecute;
    
    //Things to do once cutscene ends
    public Action onCutsceneStop;

    public void Pause(bool toggle)
    {
        pause = toggle; 
    }

    public void OnLocationLoad()
    {

        if (pause) return; 
        //Get current scene
        string location = SceneTransitionManager.Instance.currentLocation.ToString();

        //Load in all the candidate cutscenes
        Cutscene[] candidates = Resources.LoadAll<Cutscene>("Cutscenes/" + location);
        Cutscene cutsceneToPlay = GetCutsceneToPlay(candidates);
        
        //Check if there is a cutscene to play
        if (!cutsceneToPlay) return;
        Debug.Log($"The cutscene to play is {cutsceneToPlay.name}");

        StartCutsceneSequence(cutsceneToPlay);

    }

    public void StartCutsceneSequence(Cutscene cutsceneToPlay)
    {
        //Disable time,player, npcs
        TimeManager.Instance.TimeTicking = false;
        player = FindAnyObjectByType<PlayerController>();
        player.enabled = false;
        player.GetComponent<CharacterController>().enabled = false; 


        NPCManager.Instance.Pause();

        //Reset the actors
        actors = new();

        //Save to the blackboard
        GameBlackboard blackboard = GameStateManager.Instance.GetBlackboard();
        blackboard.SetValue((CUTSCENE_PREFIX + cutsceneToPlay.name), true);

        //Convert into a queue
        actionsToExecute = new Queue<CutsceneAction>(cutsceneToPlay.action);


        UpdateCutscene(); 
    }

    public void UpdateCutscene()
    {
        //Check if there are any more actions in the queue
        if(actionsToExecute.Count == 0)
        {
            EndCutscene(); 
            return; 
        }
        //Dequeue
        CutsceneAction actionToExecute = actionsToExecute.Dequeue();
        //Start the action sequence and have it call this function once done
        actionToExecute.Init(() => { UpdateCutscene();  }); 
        
    }

    public void EndCutscene()
    {
        //Clean up the actors
        ClearActors();

        //Enable time and player
        TimeManager.Instance.TimeTicking = true;
        player.gameObject.SetActive(true);
        player.enabled = true;
        player.GetComponent<CharacterController>().enabled = true;


        NPCManager.Instance.Continue();

        onCutsceneStop?.Invoke(); 
        
    }

    public void RemoveActor(string actor)
    {
        if (actors.ContainsKey(actor))
        {
            GameObject actorGameObject = actors[actor].gameObject;
            actors.Remove(actor);
            if(actorGameObject != null)
            {
                Destroy(actorGameObject);
            }
            
        }
        
        
    }

    void ClearActors()
    {
        foreach(var actor in actors)
        {
            if (actor.Key == "Player")
            {
                //Destroy only the navmesh agent
                Destroy(actor.Value);
                continue;
            }
            Destroy(actor.Value.gameObject); 
        }
        actors.Clear();
    }



    

    public static Cutscene GetCutsceneToPlay(Cutscene[] candidates)
    {
        Cutscene cutsceneToPlay = null;
        //Replace the cutscene set with the highest condition score
        int highestConditionScore = -1;
        foreach(Cutscene candidate in candidates)
        {
            //Check if candidate is recurring
            if (!candidate.recurring)
            {
                //Get the blackboard key 
                GameBlackboard blackboard = GameStateManager.Instance.GetBlackboard();
                //Check if the event has played already
                if(blackboard.ContainsKey(CUTSCENE_PREFIX + candidate.name)) continue;
            }

            //Check if conditions met first
            if (candidate.CheckConditions(out int score))
            {
                if (score > highestConditionScore)
                {
                    highestConditionScore = score;
                    cutsceneToPlay = candidate;
                    Debug.Log("Will play " + candidate.name);
                }
            }


        }
        return cutsceneToPlay; 
    }


    public void AddOrMoveActor(string actor, Vector3 position, Action onExecutionComplete)
    {

        //Convert the position to a place on the navmesh
        NavMesh.SamplePosition(position, out NavMeshHit hit, 10f, NavMesh.AllAreas);
        position = hit.position;

        Debug.Log($"CUTSCENE: Trying to add/move {actor} on {position}");
        //The movement component of the actor
        NavMeshAgent actorMovement;
        bool actorExists = actors.TryGetValue(actor, out actorMovement);
        
        //Actor exists, create actor 
        if (actorExists)
        {
            Debug.Log($"CUTSCENE: {actor} exists. Moving actor to {position}");
            actorMovement.SetDestination(position);
            StartCoroutine(WaitForDestination(actorMovement, position, onExecutionComplete));
            return;
        }


        //If actor is player 
        if (actor == "Player")
        {
            //Give it a navmesh agent
            actorMovement = player.gameObject.AddComponent<NavMeshAgent>();
            actors.Add("Player", actorMovement);
            actorMovement.SetDestination(position);
            StartCoroutine(WaitForDestination(actorMovement, position, onExecutionComplete));
            return; 
        }
        Debug.Log($"CUTSCENE: {actor} doesnt exist. Creating actor at {position}");
        //Get NPC 
        CharacterData characterData = NPCManager.Instance.Characters().Find(x => x.name == actor);
        GameObject npcObj = Instantiate(characterData.prefab, position, Quaternion.identity);
        actors.Add(actor, npcObj.GetComponent<NavMeshAgent>());
        onExecutionComplete?.Invoke(); 
    }

    //Coroutine that waits for the actor to reach the destination
    IEnumerator WaitForDestination(NavMeshAgent actorAgent, Vector3 destination, Action onExecutionComplete)
    {
        
        while(Vector3.SqrMagnitude(actorAgent.transform.position - destination) > 0.25f)
        {
            if (actorAgent == null) break; 
            yield return new WaitForEndOfFrame();
        }
        //Mark execution as complete
        onExecutionComplete?.Invoke(); 

    }

    public void KillActor(string actor)
    {
        
        GameObject objToDestroy = actors[actor].gameObject;
        actors.Remove(actor);
        //Prevent killing the player
        if (actor == "Player")
        {
            player.gameObject.SetActive(false); 
            return; 
        }

        Destroy(objToDestroy);
        
    }
}
