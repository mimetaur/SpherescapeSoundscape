using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepelAudioController : MonoBehaviour
{
    public int[] allNotes;
    public int currentNote;

    private Hv_spherescapeRepel_AudioLib heavyScript;
    private Repel repel;

    // Use this for initialization
    void Start()
    {
        repel = GetComponent<Repel>();
        heavyScript = GetComponent<Hv_spherescapeRepel_AudioLib>();
        currentNote = allNotes[Random.Range(0, allNotes.Length)];
        heavyScript.SetFloatParameter(Hv_spherescapeRepel_AudioLib.Parameter.Noteasnumber, (float)currentNote);

        InvokeRepeating("UpdateCount", 2.0f, 2.0f);
    }

    void UpdateCount()
    {
        float intensityAmount = GameUtils.Map((float)repel.numberOfBallsRepelling, 0, repel.expectedMaxNumberOfBallsRepelling, 0, 127);
        heavyScript.SetFloatParameter(Hv_spherescapeRepel_AudioLib.Parameter.Intensity, intensityAmount);
    }
}
