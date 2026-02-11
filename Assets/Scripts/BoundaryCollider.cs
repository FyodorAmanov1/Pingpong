using UnityEngine;

/// <summary>
/// Attach this to the hexagon boundary object.
/// It replaces the broken 2-point EdgeCollider2D with a proper closed
/// hexagonal boundary that the ball can actually bounce off of.
/// </summary>
public class BoundaryCollider : MonoBehaviour
{
    [Tooltip("Extra thickness for EdgeCollider2D to prevent ball tunneling.")]
    public float edgeRadius = 0.15f;

    [Tooltip("If true, auto-generates hexagon collider points. Enable for the main hex boundary.")]
    public bool isHexBoundary = false;

    void Awake()
    {
        // --- Build or fix the collider ---
        EdgeCollider2D edge = GetComponent<EdgeCollider2D>();

        if (isHexBoundary && edge != null)
        {
            // The hex boundary's EdgeCollider2D only has 2 points (a single line).
            // Replace it with a proper closed hexagon loop.
            edge.points = GenerateFlatTopHexagonPoints();
            edge.edgeRadius = edgeRadius;
            Debug.Log($"BoundaryCollider: Generated hexagon collider on '{gameObject.name}' with {edge.points.Length} points.");
        }
        else if (edge != null)
        {
            edge.edgeRadius = edgeRadius;
        }

        // Apply bounce material to whatever collider we have
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            PhysicsMaterial2D mat = new PhysicsMaterial2D("BoundaryBounce");
            mat.bounciness = 1f;
            mat.friction = 0f;
            col.sharedMaterial = mat;
        }
        else
        {
            Debug.LogWarning("BoundaryCollider: No Collider2D found on " + gameObject.name);
        }

        // Make sure boundary is static
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Static;
        }
    }

    /// <summary>
    /// Generate points for a flat-top hexagon EdgeCollider2D that matches
    /// the "Hexagon Flat Top" sprite shape (1.0 x ~0.883 in local coords).
    /// The last point equals the first to close the loop.
    /// </summary>
    Vector2[] GenerateFlatTopHexagonPoints()
    {
        // A flat-top regular hexagon inscribed in a box of width=1, height≈0.866
        // The sprite "Hexagon Flat Top" has m_Size (1, 0.8828125) so we match that.
        float halfW = 0.5f;
        float halfH = 0.44140625f; // 0.8828125 / 2

        // 6 vertices + 1 closing vertex, going clockwise from right
        return new Vector2[]
        {
            new Vector2( halfW,    0f),          // right
            new Vector2( halfW * 0.5f,  halfH),  // top-right
            new Vector2(-halfW * 0.5f,  halfH),  // top-left
            new Vector2(-halfW,    0f),          // left
            new Vector2(-halfW * 0.5f, -halfH),  // bottom-left
            new Vector2( halfW * 0.5f, -halfH),  // bottom-right
            new Vector2( halfW,    0f),          // close the loop
        };
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Boundary hit by: " + collision.gameObject.name);
    }
}
