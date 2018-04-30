using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UndulatingAudioController : MonoBehaviour
{
    Hv_prototype03_AudioLib heavyScript;

    void Start()
    {
        heavyScript = GetComponent<Hv_prototype03_AudioLib>();
    }

    public void SetBurst(float newBurst)
    {
        heavyScript.SetFloatParameter(Hv_prototype03_AudioLib.Parameter.Burst, newBurst);
    }

    public void SetBuild(float newBuild)
    {
        heavyScript.SetFloatParameter(Hv_prototype03_AudioLib.Parameter.Build, newBuild);
    }
}