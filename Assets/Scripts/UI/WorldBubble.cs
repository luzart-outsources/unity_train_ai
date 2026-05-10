using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldBubble : WorldUI
{
    
    [SerializeField]
    Animator speechAnimator; 

    public enum Emote
    {
        Happy, BadMood, Heart, Thinking, Sad
    }


    public void Display(Emote mood)
    {
        ResetAnimator();
        speechAnimator.SetBool(mood.ToString(), true); 
    }

    public void Display(Emote mood, float time)
    {
        Display(mood);
        StartCoroutine(Delay(time)); 
    }

    IEnumerator Delay(float time)
    {
        yield return new WaitForSeconds(time);
        ResetAnimator();
        gameObject.SetActive(false); 
    }

    void ResetAnimator()
    {
        foreach(AnimatorControllerParameter param in speechAnimator.parameters)
        {
            speechAnimator.SetBool(param.name, false); 
        }
    }

    private void OnDisable()
    {
        ResetAnimator();
    }





}
