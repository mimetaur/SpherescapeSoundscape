using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepelAudioController : MonoBehaviour
{
    public int currentNote = 64;
    public RangeFloat updateRateRange = new RangeFloat(2f, 6);
    public RangeFloat intensityToFilterAmountRange = new RangeFloat(70f, 80f); // originally 76.5
    public RangeFloat intensityToLfoSpeedAmountRange = new RangeFloat(30f, 50f); // originally 40

    private BallMusicalNotes ballMusicalNotes;
    private Hv_spherescapeRepel_AudioLib heavyScript;
    private Repel repel;

    private float updateRate;
    private float intensityToFilterAmount;
    private float intensityToLfoSpeedAmount;

    // Use this for initialization
    void Start()
    {
        updateRate = updateRateRange.GetRandomValueFromRange();
        intensityToFilterAmount = intensityToFilterAmountRange.GetRandomValueFromRange();
        intensityToLfoSpeedAmount = intensityToLfoSpeedAmountRange.GetRandomValueFromRange();

        ballMusicalNotes = GameObject.Find("GameManager").GetComponent<BallMusicalNotes>();
        repel = GetComponent<Repel>();

        heavyScript = GetComponent<Hv_spherescapeRepel_AudioLib>();

        // each sphere has a different note, set at startup in the spawn script via BallMusicalNotes class
        heavyScript.SetFloatParameter(Hv_spherescapeRepel_AudioLib.Parameter.Noteasnumber, (float)currentNote);

        // each repel sphere should have slightly different characteristics
        // effected differently by intensity parameter
        heavyScript.SetFloatParameter(Hv_spherescapeRepel_AudioLib.Parameter.Intensitytofilteramount, intensityToFilterAmount);

        heavyScript.SetFloatParameter(Hv_spherescapeRepel_AudioLib.Parameter.Intensitytolfospeed, intensityToLfoSpeedAmount);

        // this parameter and the the rate of update are in sync
        // makes for smoother parameter changes - using hv.line~ in PD
        heavyScript.SetFloatParameter(Hv_spherescapeRepel_AudioLib.Parameter.Intensityfadetime, updateRate);

        InvokeRepeating("UpdateCount", updateRate, updateRate);
    }

    void UpdateCount()
    {
        float intensityAmount = GameUtils.Map((float)repel.numberOfBallsRepelling, 0, repel.expectedMaxNumberOfBallsRepelling, SpherescapeConstants.MinIntensity, SpherescapeConstants.MaxIntensity);
        heavyScript.SetFloatParameter(Hv_spherescapeRepel_AudioLib.Parameter.Intensity, intensityAmount);
    }
}
