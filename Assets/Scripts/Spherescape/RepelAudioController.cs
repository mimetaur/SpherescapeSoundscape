using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepelAudioController : MonoBehaviour
{
    public int currentNote = 64;
    public float updateRate = 5.0f;

    private BallMusicalNotes ballMusicalNotes;
    private Hv_spherescapeRepel_AudioLib heavyScript;
    private Repel repel;

    // Use this for initialization
    void Start()
    {
        ballMusicalNotes = GameObject.Find("GameManager").GetComponent<BallMusicalNotes>();
        repel = GetComponent<Repel>();
        heavyScript = GetComponent<Hv_spherescapeRepel_AudioLib>();
        heavyScript.SetFloatParameter(Hv_spherescapeRepel_AudioLib.Parameter.Noteasnumber, (float)currentNote);

        // this parameter and the the rate of update are in sync
        heavyScript.SetFloatParameter(Hv_spherescapeRepel_AudioLib.Parameter.Intensityfadetime, updateRate);

        InvokeRepeating("UpdateCount", updateRate, updateRate);
    }

    void UpdateCount()
    {
        float intensityAmount = GameUtils.Map((float)repel.numberOfBallsRepelling, 0, repel.expectedMaxNumberOfBallsRepelling, 0, 127);
        heavyScript.SetFloatParameter(Hv_spherescapeRepel_AudioLib.Parameter.Intensity, intensityAmount);
    }
}
