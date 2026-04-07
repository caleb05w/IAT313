using UnityEngine;

// Attach to the Rain GameObject (child of Main Camera).
// Keeps the emitter centered above whatever the camera is looking at.
[RequireComponent(typeof(ParticleSystem))]
public class RainFollowCamera : MonoBehaviour
{
    [Tooltip("How many units above the camera view to spawn rain")]
    public float heightAboveCamera = 8f;

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        PositionEmitter();
    }

    void LateUpdate()
    {
        PositionEmitter();
    }

    void PositionEmitter()
    {
        if (cam == null) return;
        Vector3 pos = cam.transform.position;
        transform.position = new Vector3(pos.x, pos.y + heightAboveCamera, 0f);
    }
}
