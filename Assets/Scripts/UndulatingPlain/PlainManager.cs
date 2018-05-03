using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlainManager : MonoBehaviour
{
    public float metallicCycleTime = 10f;
    public float iceCycleTime = 30f;

    private float metalLerp;
    private float iceLerp;

    private void Update()
    {
        metalLerp = Mathf.PingPong(Time.time * 2 / metallicCycleTime, UndulatingPlainConstants.metalLerpValueRange) + UndulatingPlainConstants.metalLerpValueOffset;
        iceLerp = Mathf.PingPong(Time.time * 2 / iceCycleTime, UndulatingPlainConstants.iceLerpValueRange) + UndulatingPlainConstants.iceLerpValueOffset;
    }

    public float GetMetalLerp()
    {
        return metalLerp;
    }

    public float GetIceLerp()
    {
        return iceLerp;
    }
}
