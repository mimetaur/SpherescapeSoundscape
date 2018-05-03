using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateAround : MonoBehaviour
{

    public Transform target;
    public float angle;
    public float initialDelay = 5f;

    void Update()
    {
        if (Time.time < initialDelay) return;
        transform.RotateAround(target.position, Vector3.up, angle * Time.deltaTime);
    }

}
