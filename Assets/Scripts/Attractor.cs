using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attractor : MonoBehaviour
{

    public float attractionRadius = 2;
    public float attractionForce = 1;
    public string tagToAttract;

    public void FixedUpdate()
    {
        foreach (Collider theCollider in Physics.OverlapSphere(transform.position, attractionRadius))
        {
            if (theCollider.tag == tagToAttract)
            {
                // calculate direction from target to me
                Vector3 forceDirection = transform.position - theCollider.transform.position;

                // apply force on target towards me
                var rb = theCollider.GetComponent<Rigidbody>();
                if (rb) rb.AddForce(forceDirection.normalized * attractionForce * Time.fixedDeltaTime);
            }
        }
    }
}
