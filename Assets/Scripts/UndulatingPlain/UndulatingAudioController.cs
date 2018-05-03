using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class UndulatingAudioController : MonoBehaviour
{
    public AudioMixer mixer;
    public RangeFloat cutoffRange = new RangeFloat(500f, 1200f);
    public RangeFloat resonanceRange = new RangeFloat(1f, 2.5f);

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
        float longSlowLerp = plainManager.GetIceLerp();

        float cutoffParam = GameUtils.Map(longSlowLerp, 0f, 1f, cutoffRange.Low(), cutoffRange.High());
        mixer.SetFloat("LowpassCutoff", cutoffParam);

        float resonanceParam = GameUtils.Map(longSlowLerp, 0f, 1f, resonanceRange.Low(), resonanceRange.High());
        mixer.SetFloat("LowpassResonance", resonanceParam);

        float metalLerp = plainManager.GetMetalLerp();
        // float cutoffParam = GameUtils.Map(iceLerp, 0f, 1f, 500f, 1200.0f);
        // float metalParam = GameUtils.Map(metalLerp, 0.5f, 1f, 0f, 127f);
        // float cutoffParam = GameUtils.Map(metalLerp, 0.5f, 1f, 0f, 1200.0f);
    }

    public void SetMetallicValue(float newMetallicValue)
    {

    }

    public void SetIceValue(float newIceValue)
    {

    }
}