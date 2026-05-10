using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldUI : MonoBehaviour
{
    Transform cameraPos;
    // Start is called before the first frame update
    void Start()
    {
        cameraPos = FindFirstObjectByType<CameraController>().transform;

    }

    // Update is called once per frame
    private void Update()
    {

        //Look at camera
        transform.rotation = cameraPos.rotation;


    }
}
