using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerOsc : MonoBehaviour
{
    public string tagToTrigger;
    public float octave = 0;

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == tagToTrigger)
        {
            List<float> floatParams = new List<float>();
            floatParams.Add(octave);

            float speed = other.gameObject.GetComponent<Rigidbody>().velocity.magnitude;
            floatParams.Add(speed);

            float size = other.transform.localScale.x;
            floatParams.Add(size);

            float rotation = other.transform.rotation.eulerAngles.z;
            floatParams.Add(rotation);

            floatParams.Add(other.transform.position.x);
            floatParams.Add(other.transform.position.y);
            floatParams.Add(other.transform.position.z);

            OSCHandler.Instance.SendMessageToClient<float>("Max", "/trigger", floatParams);
        }
    }


}
