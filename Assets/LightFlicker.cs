using UnityEngine;
using UnityEngine.Rendering.Universal;

// Attach to a GameObject with a Light2D component to make it flicker.
[RequireComponent(typeof(Light2D))]
public class LightFlicker : MonoBehaviour
{
    [Header("Intensity")]
    [SerializeField] private float baseIntensity = 1.5f;
    [SerializeField] private float flickerAmount = 0.4f;   // how much it varies
    [SerializeField] private float flickerSpeed = 8f;      // how fast the noise moves

    [Header("Radius (optional)")]
    [SerializeField] private bool flickerRadius = false;
    [SerializeField] private float baseRadius = 4f;
    [SerializeField] private float radiusAmount = 0.3f;

    private Light2D light2D;
    private float seed;

    void Awake()
    {
        light2D = GetComponent<Light2D>();
        seed = Random.Range(0f, 100f); // unique offset per light
    }

    void Update()
    {
        float noise = Mathf.PerlinNoise(seed + Time.time * flickerSpeed, 0f);
        light2D.intensity = baseIntensity + (noise - 0.5f) * 2f * flickerAmount;

        if (flickerRadius)
            light2D.pointLightOuterRadius = baseRadius + (noise - 0.5f) * 2f * radiusAmount;
    }
}
