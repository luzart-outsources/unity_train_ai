using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    //Movement Components
    private CharacterController controller; 
    private Animator animator;

    private float moveSpeed = 4f;

    [Header("Movement System")]
    public float walkSpeed = 4f;
    public float runSpeed = 8f;

    private float gravity = 9.81f;


    //Interaction components
    PlayerInteraction playerInteraction; 

    // Start is called before the first frame update
    void Start()
    {
        //Get movement components
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        //Get interaction component
        playerInteraction = GetComponentInChildren<PlayerInteraction>(); 

    }

    // Update is called once per frame
    void Update()
    {
        //Runs the function that handles all movement
        Move();

        //Runs the function that handles all interaction
        Interact();


        //Debugging purposes only
        //Skip the time when the right square bracket is pressed
        if (Input.GetKey(KeyCode.RightBracket))
        {
            if (Input.GetKey(KeyCode.LeftShift)) { 
                //Advance the entire day
                for(int i =0; i< 60*24;  i++)
                {
                    TimeManager.Instance.Tick();
                }
            } else
            {
                TimeManager.Instance.Tick();
            }

            
        }

        //Toggle relationship panel
        if (Input.GetKeyDown(KeyCode.R))
        {
            UIManager.Instance.ToggleRelationshipPanel();
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            SceneTransitionManager.Location location = LocationManager.GetNextLocation(SceneTransitionManager.Location.PlayerHome, SceneTransitionManager.Location.Farm);
            Debug.Log(location);
        }
    }

    public void Interact()
    {
        //Tool interaction
        if (Input.GetButtonDown("Fire1"))
        {
            //Interact
            playerInteraction.Interact(); 
        }

        //Item interaction
        if (Input.GetButtonDown("Fire2"))
        {
            playerInteraction.ItemInteract();
        }

        //Keep items 
        if (Input.GetButtonDown("Fire3"))
        {
            playerInteraction.ItemKeep();
        }
    }

    
    public void Move()
    {
        //Get the horizontal and vertical inputs as a number
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        //Direction in a normalised vector
        Vector3 dir = new Vector3(horizontal, 0f, vertical).normalized;
        Vector3 velocity = moveSpeed * Time.deltaTime * dir;

        if (controller.isGrounded)
        {
            velocity.y = 0; 
        }
        velocity.y -= Time.deltaTime * gravity; 

        //Is the sprint key pressed down?
        if (Input.GetButton("Sprint"))
        {
            //Set the animation to run and increase our movespeed
            moveSpeed = runSpeed;
            animator.SetBool("Running", true);
        } else
        {
            //Set the animation to walk and decrease our movespeed
            moveSpeed = walkSpeed;
            animator.SetBool("Running", false);
        }


        //Check if there is movement
        if (dir.magnitude >= 0.1f)
        {
            //Look towards that direction
            transform.rotation = Quaternion.LookRotation(dir);

            //Move if allowed
            if (controller.enabled)
            {
                controller.Move(velocity);
            }
            
            
        }

        //Animation speed parameter
        animator.SetFloat("Speed", dir.magnitude); 



    }
}
