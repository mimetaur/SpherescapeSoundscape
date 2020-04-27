using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetFrameRate : MonoBehaviour
{
    public int frameRate = 30;

    void Awake()
    {
        Application.targetFrameRate = frameRate;
    }


}
