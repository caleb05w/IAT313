using UnityEngine;
using TMPro;

// Resizes a SpriteRenderer to match the bounds of a TextMeshPro label.
// Attach to the Background child GameObject.
[RequireComponent(typeof(SpriteRenderer))]
public class LabelBackground : MonoBehaviour
{
    [SerializeField] private TextMeshPro label;
    [SerializeField] private Vector2 padding = new Vector2(0.2f, 0.1f);
    [SerializeField] private float smoothTime = 0.1f;

    private SpriteRenderer sr;
    private Vector3 currentScale;
    private Vector3 scaleVelocity;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        currentScale = transform.localScale;
    }

    void LateUpdate()
    {
        if (label == null) return;
        label.ForceMeshUpdate();
        Vector2 size = new Vector2(label.preferredWidth, label.preferredHeight);
        if (size == Vector2.zero) return;

        Vector3 targetScale = new Vector3(size.x + padding.x * 2f, size.y + padding.y * 2f, 1f);
        currentScale = Vector3.SmoothDamp(currentScale, targetScale, ref scaleVelocity, smoothTime);
        transform.localScale = currentScale;

        // Center background on the text's rendered bounds
        Vector3 center = label.textBounds.center;
        transform.position = label.transform.TransformPoint(center);
    }
}
