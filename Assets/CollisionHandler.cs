using UnityEngine;

// Handles physical collisions (e.g. walls, floors, obstacles).
// Attach to any GameObject that should physically block or respond to collisions.
// Requires a Collider2D (Box, Circle, or Polygon) with "Is Trigger" unchecked.
// At least one of the colliding objects must have a Rigidbody2D.
public class CollisionHandler : MonoBehaviour
{
    // Toggle to show a red outline of the collider in the Scene view
    [SerializeField] private bool showHitbox = false;

    void OnDrawGizmos()
    {
        // Only draw if showHitbox is enabled in the Inspector
        if (!showHitbox) return;

        Gizmos.color = Color.red;

        // Box Collider — draws a wire rectangle matching the collider size
        var box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.offset, box.size);
            return;
        }

        // Circle Collider — draws a wire circle matching the collider radius
        var circle = GetComponent<CircleCollider2D>();
        if (circle != null)
        {
            Gizmos.DrawWireSphere(transform.position + (Vector3)circle.offset, circle.radius);
            return;
        }

        // Polygon Collider — traces each edge of the polygon shape
        var poly = GetComponent<PolygonCollider2D>();
        if (poly != null)
        {
            Vector2[] points = poly.points;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 a = transform.TransformPoint(points[i]);
                Vector2 b = transform.TransformPoint(points[(i + 1) % points.Length]);
                Gizmos.DrawLine(a, b);
            }
        }
    }

    // Fires once when this object makes contact with another collider
    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Collision Enter: " + collision.gameObject.name);
    }

    // Fires every frame while this object remains in contact with another collider
    void OnCollisionStay2D(Collision2D collision)
    {
        Debug.Log("Collision Stay: " + collision.gameObject.name);
    }

    // Fires once when this object separates from another collider
    void OnCollisionExit2D(Collision2D collision)
    {
        Debug.Log("Collision Exit: " + collision.gameObject.name);
    }
}
