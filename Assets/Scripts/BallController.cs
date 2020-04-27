using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallController : MonoBehaviour
{
    public float lifespan = 1.0f;
    private float age;

    // Use this for initialization
    void Start()
    {
        age = 0.0f;
    }

    // Update is called once per frame
    void Update()
    {
        age += Time.deltaTime;
        if (age > lifespan)
        {
            Destroy(this.gameObject);
        }
    }
}
