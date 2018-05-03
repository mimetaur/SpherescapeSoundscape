using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UndulatingAudioController : MonoBehaviour
{
    private Hv_prototype03_AudioLib heavyScript;
    private PlainManager plainManager;

    void Start()
    {
        plainManager = GameObject.Find("PlainManager").GetComponent<PlainManager>();
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

    private void Update()
    {
        float metalLerp = plainManager.GetMetalLerp();
        float metalParam = GameUtils.Map(metalLerp, 0.5f, 1f, 0f, 127f);
        // setfloatparameter for heavy

        float iceLerp = plainManager.GetIceLerp();
        float iceParam = GameUtils.Map(iceLerp, 0f, 1f, 0f, 127f);
        // setfloatparameter for heavy
    }

    public void SetMetallicValue(float newMetallicValue)
    {

    }

    public void SetIceValue(float newIceValue)
    {

    }
}