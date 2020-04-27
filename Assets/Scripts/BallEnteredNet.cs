using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallEnteredNet : MonoBehaviour
{


    public GameObject ballType;
    public float octave = 0;
    private string triggerTag;


    // Use this for initialization
    void Start()
    {
        triggerTag = ballType.tag;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == triggerTag)
        {
            BallEnter ballScript = other.GetComponent<BallEnter>();
            ballScript.EnteredNet();
        }
    }
}
