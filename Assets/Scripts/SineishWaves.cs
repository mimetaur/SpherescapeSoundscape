using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SineishWaves : MonoBehaviour
{
    public float scale = 10.0f;
    public float speed = 1.0f;

    private Mesh mesh;
    private Vector3[] baseHeight;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        baseHeight = mesh.vertices;
    }

    void Update()
    {

        var vertices = new Vector3[baseHeight.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 newVertex = baseHeight[i];
            float sumOfVertexValues = baseHeight[i].x + baseHeight[i].y + baseHeight[i].z;
            float sinInput = Time.time * speed + sumOfVertexValues;
            newVertex.y += Mathf.Sin(sinInput) * scale;

            vertices[i] = newVertex;
        }
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }
}