using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float offsetZ = 5f;
    public float smoothing =2f; 
    //player transform component
    Transform playerPos;
    // Start is called before the first frame update
    void Start()
    {
        //Find the player gameobject in the scene and get its transform component
        playerPos = FindObjectOfType<PlayerController>().transform; 
    }

    // Update is called once per frame
    void Update()
    {
        FollowPlayer();
    }

    //Following the player
    void FollowPlayer()
    {
        //Position the camera should be in
        Vector3 targetPosition = new Vector3(playerPos.position.x, transform.position.y, playerPos.position.z - offsetZ); 

        //Set the position accordingly
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothing * Time.deltaTime);
    }
}
