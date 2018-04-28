using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartOsc : MonoBehaviour
{
    void Start()
    {
        OSCHandler.Instance.Init();
    }
}
