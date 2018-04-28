using UnityEngine;

[System.Serializable]
public class BallType : System.Object
{
    public Material mat;
    public float size = 0.25f;

    public Vector3 Scale
    {
        get
        {
            return new Vector3(size, size, size);
        }
    }

    public float Mass
    {
        get
        {
            return size * 4.0f;
        }
    }
}