using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepelAudioController : MonoBehaviour
{
    public int currentNote = 64;

    [Range(1, 20)]
    public float updateRateLow = 2.0f;
    [Range(1, 20)]
    public float updateRateHigh = 6.0f;

    private float updateRate;
    private BallMusicalNotes ballMusicalNotes;
    private Hv_spherescapeRepel_AudioLib heavyScript;
    private Repel repel;

    // Use this for initialization
    void Start()
    {
        updateRate = Random.Range(updateRateLow, updateRateHigh);

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
