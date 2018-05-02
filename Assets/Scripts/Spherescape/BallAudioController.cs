using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallAudioController : MonoBehaviour
{
    private Hv_spherescape_AudioLib heavyScript;

    void Start()
    {
        heavyScript = GetComponent<Hv_spherescape_AudioLib>();
    }

    public void Trigger()
    {
        heavyScript.SendEvent(Hv_spherescape_AudioLib.Event.Bang);
    }
}