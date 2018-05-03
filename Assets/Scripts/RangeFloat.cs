using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RangeFloat
{
    public float lowBound;
    public float highBound;

    public RangeFloat(float lowBound_, float highBound_)
    {
        lowBound = lowBound_;
        highBound = highBound_;
    }

    public float GetRandomValueFromRange()
    {
        return Mathf.Floor(Random.Range(lowBound, highBound));
    }

    public float Low()
    {
        return lowBound;
    }

    public float High()
    {
        return highBound;
    }
}
