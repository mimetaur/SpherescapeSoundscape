using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Repel : MonoBehaviour
{
    public float repelRadius = 2;
    public float repelForce = 1;
    public int expectedMaxNumberOfBallsRepelling = 10;
    public int numberOfBallsRepelling;
    public string tagToRepel;

    public void FixedUpdate()
    {
        var overlappingColliders = Physics.OverlapSphere(transform.position, repelRadius);
        numberOfBallsRepelling = overlappingColliders.Length;

        foreach (Collider theCollider in overlappingColliders)
        {
            if (theCollider.tag == tagToRepel)
            {
                // calculate direction from target to me
                Vector3 forceDirection = transform.position - theCollider.transform.position;

                // apply force on target towards me
                var rb = theCollider.GetComponent<Rigidbody>();
                if (rb) rb.AddForce((forceDirection.normalized * repelForce * Time.fixedDeltaTime) * -1);
            }
        }
    }
}
