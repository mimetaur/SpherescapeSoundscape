using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallEnter : MonoBehaviour
{
    private BallAudioController ballAudioController;

    // Use this for initialization
    void Start()
    {
        ballAudioController = GetComponent<BallAudioController>();
    }

    public void EnteredNet()
    {
        ballAudioController.Trigger();

        // once I get the PD script wired up to
        // respond to these params, I'll send this data to
        // the Audio Controller as well

        // List<float> floatParams = new List<float>();
        // floatParams.Add(octave);

        // float speed = other.gameObject.GetComponent<Rigidbody>().velocity.magnitude;
        // floatParams.Add(speed);

        // float size = other.transform.localScale.x;
        // floatParams.Add(size);

        // float rotation = other.transform.rotation.eulerAngles.z;
        // floatParams.Add(rotation);

        // floatParams.Add(other.transform.position.x);
        // floatParams.Add(other.transform.position.y);
        // floatParams.Add(other.transform.position.z);
    }
}
