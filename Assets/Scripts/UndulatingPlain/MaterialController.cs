#pragma warning disable 0618
// TODO upgrade project to 2018.1 and Substance Designer asset
// yes Unity I KNOW ProceduralMaterials are obsolete
// but you have given me no alternative in Unity 2017

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialController : MonoBehaviour
{
    private PlainManager plainManager;
    private Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
        plainManager = GameObject.Find("PlainManager").GetComponent<PlainManager>();
    }

    void Update()
    {
        ProceduralMaterial substance = rend.sharedMaterial as ProceduralMaterial;
        if (substance)
        {
            substance.SetProceduralFloat(UndulatingPlainConstants.metallicSubstancePropertyName, plainManager.GetMetalLerp());
            substance.SetProceduralFloat(UndulatingPlainConstants.iceSubstancePropertyName, plainManager.GetIceLerp());
            substance.RebuildTextures();
        }
    }
}
