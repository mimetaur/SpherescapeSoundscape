using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RangeInt
{
    public int lowBound;
    public int highBound;

    public RangeInt(int lowBound_, int highBound_)
    {
        lowBound = lowBound_;
        highBound = highBound_;
    }

    public int GetRandomValueFromRange()
    {
        return (int)Mathf.Floor(Random.Range(lowBound, highBound));
    }
}
