using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepelAudioController : MonoBehaviour
{

    private Hv_spherescapeRepel_AudioLib heavyScript;
    private Repel repel;

    // Use this for initialization
    void Start()
    {
        repel = GetComponent<Repel>();
        heavyScript = GetComponent<Hv_spherescapeRepel_AudioLib>();

        InvokeRepeating("UpdateCount", 1.0f, 1.0f);
    }

    void UpdateCount()
    {
        float intensityAmount = GameUtils.Map((float)repel.numberOfBallsRepelling, 0, 25, 0, 127);
        heavyScript.SetFloatParameter(Hv_spherescapeRepel_AudioLib.Parameter.Intensity, intensityAmount);
    }
}
