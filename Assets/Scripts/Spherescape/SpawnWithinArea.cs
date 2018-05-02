using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnWithinArea : MonoBehaviour
{
    public GameObject ball;
    public GameObject area;
    public int numSimultaneousBalls = 1;
    public float initialDelay = 1.0f;
    public float spawnRate = 2.0f;
    [Space(10)]
    public bool useRandomBallTypes = false;
    public BallType[] ballTypes;

    private Bounds bounds;
    private PlayAmbience playAmbience;

    private void Start()
    {
        playAmbience = GetComponent<PlayAmbience>();
        var collider = area.GetComponent<BoxCollider>();
        if (collider != null) bounds = collider.bounds;
        InvokeRepeating("SpawnBall", initialDelay, spawnRate);
    }

    private void SpawnBall()
    {
        if (ball.tag == "Ball") playAmbience.PlayAmbientClip();
        for (int i = 0; i < numSimultaneousBalls; i++)
        {
            var location = GameUtils.RandomPointInBounds(bounds);
            GameObject obj = Instantiate(ball, location, Quaternion.identity);
            if (useRandomBallTypes) CustomizeNewBall(obj);
        }
    }

    private void CustomizeNewBall(GameObject newBall)
    {
        BallType newType = ballTypes[Random.Range(0, ballTypes.Length)];
        newBall.GetComponent<Renderer>().material = newType.mat;
        newBall.transform.localScale = newType.Scale;
        newBall.GetComponent<Rigidbody>().mass = newType.Mass;
    }
}
