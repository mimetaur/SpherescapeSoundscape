using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepelBrightnessController : MonoBehaviour
{

    public Color brightColor;
    public float brightnessFactor = 3.0f;
    private Repel repel;
    private Material myMaterial;
    public float updateRate = 1.0f;

    // Use this for initialization
    void Start()
    {
        repel = GetComponent<Repel>();
        myMaterial = GetComponent<Renderer>().material;
        InvokeRepeating("UpdateBrightness", updateRate, updateRate);
    }

    void UpdateBrightness()
    {
        float amount = GameUtils.Map(repel.numberOfBallsRepelling, 0, repel.expectedMaxNumberOfBallsRepelling, 0.0f, 1.0f);
        myMaterial.color = Color.Lerp(myMaterial.color, brightColor * brightnessFactor, amount);
    }
}
