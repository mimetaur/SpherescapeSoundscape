using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepelBrightnessController : MonoBehaviour
{
    [ColorUsageAttribute(false, true)] public Color emissiveColor;
    public float updateRate = 1.0f;

    private float t = 0.0f;
    private Repel repel;
    private Renderer rend;
    private Color plainColor;

    void Start()
    {
        repel = GetComponent<Repel>();
        rend = GetComponent<Renderer>();

        rend.material.shader = Shader.Find("HDRP/Lit");
        plainColor = rend.material.GetColor("_EmissiveColor");

        InvokeRepeating("UpdateBrightness", updateRate, updateRate);
    }

    void UpdateBrightness()
    {
        t = GameUtils.Map(repel.numberOfBallsRepelling, 0, repel.expectedMaxNumberOfBallsRepelling, 0.0f, 1.0f);
    }

    void Update()
    {
        Color currentColor = Color.Lerp(plainColor, emissiveColor, t);
        rend.material.SetColor("_EmissiveColor", currentColor);
        // float emissiveIntensity = Mathf.Lerp(darkEmissionIntensity, brightEmissionIntensity, t);
        // rend.material.SetFloat("_EmissiveIntensity", emissiveIntensity);
        // myMaterial.color = Color.Lerp(myMaterial.color, brightColor * brightnessFactor, amount);
        // myMaterial._EmissiveIntensity =
    }
}
