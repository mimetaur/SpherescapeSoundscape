using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateAround : MonoBehaviour
{

    public Transform target;
    public float angle;

    void Update()
    {
        transform.RotateAround(target.position, Vector3.up, angle);
    }

}
