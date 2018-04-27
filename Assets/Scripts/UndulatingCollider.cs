using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UndulatingCollider : MonoBehaviour
{
    public float UpdateCountSpeed = 0.1f;

    [Space]
    // to see live updating variables
    public int currentlyCollidingVertices = 0;
    public int totalCollidingVertices = 0;

    [Space]
    // these values are set based on
    // the current position/scale/rotation of the collider
    // in relation to the undulating plain
    public int maxSimultaneousVertexCollisions = 0;
    public int maxVertices = 0;

    private BoxCollider boxCollider;
    private Mesh cachedMesh;
    private UndulatingAudioController audioController;

    private HashSet<Vector3> allCollidingVertices = new HashSet<Vector3>();
    private HashSet<int> snapshotsOfSimultaneouslyCollidingVertices = new HashSet<int>();


    void Start()
    {
        boxCollider = GetComponent<BoxCollider>();
        audioController = GetComponent<UndulatingAudioController>();

        // moving this to a slower speed than update for performance
        // also to avoid making PD deal with 30fps updates
        InvokeRepeating("UpdateCounts", UpdateCountSpeed, UpdateCountSpeed);
    }


    void UpdateCounts()
    {
        // nothing happens if there isn't a ref to the mesh
        if (cachedMesh == null) return;

        // for now, recalculate this each time
        // it seems like this should be cacheable
        RecalculateVertexMaximums();

        totalCollidingVertices = allCollidingVertices.Count;

        // this should not be necessary. there is something weird
        // with duplicate vertices not being "caught" by the
        // HashSet so it keeps increasing forever
        // resetting also creates another, slower rhythm in the PD patch
        if (totalCollidingVertices > maxVertices) allCollidingVertices.Clear();

        SendVertexDataToAudioController();
    }

    void RecalculateVertexMaximums()
    {
        maxVertices = cachedMesh.vertexCount;
        maxSimultaneousVertexCollisions = snapshotsOfSimultaneouslyCollidingVertices.Max();
    }

    void SendVertexDataToAudioController()
    {
        float burst = GameUtils.Map(currentlyCollidingVertices, 0, maxSimultaneousVertexCollisions, 80, 127);
        audioController.SetBurst(burst);

        float build = GameUtils.Map(totalCollidingVertices, 0, maxVertices, 40, 127);
        audioController.SetBuild(build);
    }

    void OnTriggerEnter(Collider other)
    {
        if (cachedMesh == null)
        {
            // this works because we know the only GameObject
            // colliding with this collider is the undulating mesh
            cachedMesh = other.GetComponent<MeshFilter>().sharedMesh;
        }

        foreach (Vector3 vertex in cachedMesh.vertices)
        {
            Vector3 vertexInWorldCoords = other.transform.TransformPoint(vertex);
            if (boxCollider.bounds.Contains(vertexInWorldCoords))
            {
                allCollidingVertices.Add(vertexInWorldCoords);
                currentlyCollidingVertices++;
                snapshotsOfSimultaneouslyCollidingVertices.Add(currentlyCollidingVertices);
            }
        }
    }

    void OnTriggerExit()
    {
        currentlyCollidingVertices = 0;
    }
}
