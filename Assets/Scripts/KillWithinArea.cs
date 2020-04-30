using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillWithinArea : MonoBehaviour
{
    public string tagToKill;

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == tagToKill) Destroy(other.gameObject);
    }
}
