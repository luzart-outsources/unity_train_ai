using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterMovement), typeof(Animator))]
public class CharacterRenderer : MonoBehaviour
{
    CharacterMovement movement;
    Animator animator; 
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        movement = GetComponent<CharacterMovement>(); 
    }

    // Update is called once per frame
    void Update()
    {
        animator.SetBool("Walk", movement.IsMoving()); 
    }
}
