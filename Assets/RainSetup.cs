using UnityEngine;

// Attach to your Rain GameObject alongside a Particle System.
// Tweak values in the Inspector, then right-click this component -> "Apply Rain Settings".
// Remove the component once it looks good.
[RequireComponent(typeof(ParticleSystem))]
public class RainSetup : MonoBehaviour
{
    [Header("Drop Appearance")]
    public float startSize     = 0.06f;   // 2px at 32PPU
    public Color dropColor     = new Color(0.8f, 0.9f, 1f, 0.25f); // more transparent

    [Header("Movement")]
    public float fallSpeed     = 10f;     // units/sec downward
    public float driftX        = -1f;     // sideways drift (negative = left)

    [Header("Emission")]
    public int   emissionRate  = 200;
    public int   maxParticles  = 750;
    public float lifetime      = 1.2f;

    [Header("Streak Shape")]
    public float velocityScale = 0.02f;   // lower = shorter streaks
    public float lengthScale   = 1.5f;

    [Header("Sorting (render order)")]
    public string sortingLayer = "Default";
    public int    orderInLayer = 100;      // high number = renders on top of everything

    [Header("Emitter Position")]
    public float spawnHeight   = 16f;     // Y position above scene (camera is at Y~7.6)
    public float emitterWidth  = 40f;     // wider spread = more spaced out drops

    [ContextMenu("Apply Rain Settings")]
    public void Apply()
    {
        var ps  = GetComponent<ParticleSystem>();
        var ren = GetComponent<ParticleSystemRenderer>();

        // Reset transform scale/rotation — bad scale causes most rain issues
        transform.localScale    = Vector3.one;
        transform.localRotation = Quaternion.identity;
        transform.position      = new Vector3(transform.position.x, spawnHeight, transform.position.z);

        // --- Main ---
        var main             = ps.main;
        main.loop            = true;
        main.prewarm         = true;
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0f);
        main.startSize       = new ParticleSystem.MinMaxCurve(startSize);
        main.startLifetime   = new ParticleSystem.MinMaxCurve(lifetime);
        main.startColor      = new ParticleSystem.MinMaxGradient(dropColor);
        main.maxParticles    = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.Local; // follows emitter so camera can't outrun it
        main.scalingMode     = ParticleSystemScalingMode.Local;

        // --- Emission ---
        var emission          = ps.emission;
        emission.enabled      = true;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(emissionRate);

        // --- Shape ---
        var shape       = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(emitterWidth, 0.5f, 0.1f);

        // --- Velocity over Lifetime ---
        var vel     = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space   = ParticleSystemSimulationSpace.Local;
        vel.x       = new ParticleSystem.MinMaxCurve(driftX);
        vel.y       = new ParticleSystem.MinMaxCurve(-fallSpeed);
        vel.z       = new ParticleSystem.MinMaxCurve(0f);

        // --- Renderer ---
        ren.renderMode        = ParticleSystemRenderMode.Stretch;
        ren.velocityScale     = velocityScale;
        ren.lengthScale       = lengthScale;
        ren.sortingLayerName  = sortingLayer;
        ren.sortingOrder      = orderInLayer;

        Debug.Log("[RainSetup] Settings applied.");
    }
}
