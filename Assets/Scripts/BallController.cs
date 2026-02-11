using UnityEngine;

/// <summary>
/// Ball controller with manual reflection off walls to prevent sticking.
/// Finds the hexagon center automatically and keeps the ball inside.
/// 
/// REQUIRED COMPONENTS (auto-added):
/// - Rigidbody2D
/// - CircleCollider2D
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class BallController : MonoBehaviour
{
    [Header("Speed Settings")]
    public float speed = 5f;
    public float maxSpeed = 10f;
    public float minSpeed = 4f;
    public float paddleSpeedBoost = 1.05f;

    [Header("Reflection")]
    [Tooltip("Minimum angle (degrees) from the wall surface. Prevents shallow bounces.")]
    public float minBounceAngle = 15f;
    [Tooltip("Maximum angle (degrees) from the wall surface.")]
    public float maxBounceAngle = 75f;
    [Tooltip("Random angle spread (±degrees) added to each bounce.")]
    public float randomSpread = 10f;
    [Tooltip("How much speed affects the bounce angle. Higher speed → steeper bounce. 0 = no effect.")]
    public float speedAngleInfluence = 3f;

    [Header("Boundary Safety")]
    [Tooltip("Max distance from hex center before auto-reset.")]
    public float maxDistanceFromCenter = 7f;

    private Rigidbody2D rb;
    private Vector3 hexCenter;
    private float currentSpeed;

    // Goal detection — X positions of the paddle lines
    private float goalLineLeft;
    private float goalLineRight;
    private bool goalScored = false; // prevents double-scoring

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();

        // Configure Rigidbody2D
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;

        // CircleCollider2D
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<CircleCollider2D>();
            Debug.Log("BallController: Auto-added missing CircleCollider2D.");
        }

        // Bounce material — bounciness=1 so physics reflects, we fix the angle manually
        PhysicsMaterial2D mat = new PhysicsMaterial2D("BallBounce");
        mat.bounciness = 1f;
        mat.friction = 0f;
        rb.sharedMaterial = mat;
        col.sharedMaterial = mat;

        // Find hex center
        GameObject hex = GameObject.Find("Hexagon Flat Top (1)");
        if (hex != null)
        {
            hexCenter = hex.transform.position;
        }
        else
        {
            hexCenter = transform.position;
        }

        // Goal lines: set to each paddle's X position
        // If the ball passes beyond this X, the opponent scores
        GameObject p1 = GameObject.Find("Paddle1");
        GameObject p2 = GameObject.Find("Paddle2");
        if (p1 != null)
            goalLineLeft = p1.transform.position.x;
        else
            goalLineLeft = hexCenter.x - 4f; // fallback
        if (p2 != null)
            goalLineRight = p2.transform.position.x;
        else
            goalLineRight = hexCenter.x + 4f; // fallback

        Debug.Log($"BallController: Goal lines at x={goalLineLeft:F2} (P1) and x={goalLineRight:F2} (P2)");

        currentSpeed = speed;
        transform.position = hexCenter;
        Invoke(nameof(LaunchBall), 1f);
    }

    void LaunchBall()
    {
        // Launch in a fully random direction (0–360°)
        float angle = Random.Range(0f, 360f);
        float rad = angle * Mathf.Deg2Rad;

        currentSpeed = speed;
        rb.linearVelocity = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * currentSpeed;
    }

    void FixedUpdate()
    {
        // Maintain constant speed
        if (rb.linearVelocity.magnitude > 0.1f)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * currentSpeed;
        }
    }

    void Update()
    {
        // Goal detection: ball passed behind a paddle's line
        if (!goalScored && ScoreManager.Instance != null)
        {
            float bx = transform.position.x;

            if (bx < goalLineLeft)
            {
                // Ball went behind Paddle1 → Player 2 scores
                goalScored = true;
                ScoreManager.Instance.ScoreGoal(2);
                return; // ScoreGoal calls ResetBall
            }
            else if (bx > goalLineRight)
            {
                // Ball went behind Paddle2 → Player 1 scores
                goalScored = true;
                ScoreManager.Instance.ScoreGoal(1);
                return;
            }
        }

        // Safety: if ball escapes without triggering a goal, reset
        float dist = Vector2.Distance(transform.position, hexCenter);
        if (dist > maxDistanceFromCenter)
        {
            Debug.LogWarning($"BallController: Ball escaped! dist={dist:F1}");
            ResetBall();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Unity's physics has ALREADY reflected the velocity (bounciness=1).
        // We take the physics-reflected direction as a base and then modify
        // the angle based on speed, incoming direction, and randomness.

        Vector2 normal = collision.GetContact(0).normal;
        Vector2 physicsDir = rb.linearVelocity.normalized;

        // 1) Compute the base angle from the wall normal that physics gave us
        //    (0° = straight into wall, 90° = parallel to wall)
        float baseAngleFromNormal = Vector2.Angle(physicsDir, normal);

        // 2) Speed influence: faster ball → bounce steeper (closer to normal)
        //    Normalized speed ratio 0..1 across minSpeed..maxSpeed
        float speedRatio = Mathf.InverseLerp(minSpeed, maxSpeed, currentSpeed);
        float speedOffset = -speedAngleInfluence * speedRatio; // negative = steeper

        // 3) Random spread
        float randomOffset = Random.Range(-randomSpread, randomSpread);

        // 4) Combine: start from the physics angle, add speed and random offsets
        float finalAngleFromNormal = baseAngleFromNormal + speedOffset + randomOffset;

        // 5) Clamp to safe range (minBounceAngle..maxBounceAngle from SURFACE,
        //    which is (90-max)..(90-min) from NORMAL)
        float minFromNormal = 90f - maxBounceAngle;
        float maxFromNormal = 90f - minBounceAngle;
        finalAngleFromNormal = Mathf.Clamp(finalAngleFromNormal, minFromNormal, maxFromNormal);

        // 6) Build the final direction: rotate the normal by the final angle
        //    Keep the same side (left/right of normal) as the physics reflection
        float side = Mathf.Sign(Vector3.Cross(normal, physicsDir).z);
        Vector2 finalDir = RotateVector(normal, side * finalAngleFromNormal);

        // 7) Safety: ensure velocity points AWAY from the wall
        if (Vector2.Dot(finalDir, normal) < 0.05f)
        {
            finalDir = RotateVector(normal, side * minFromNormal);
        }

        // Speed boost on paddle hit
        if (collision.gameObject.CompareTag("Paddle"))
        {
            currentSpeed = Mathf.Min(currentSpeed * paddleSpeedBoost, maxSpeed);
        }

        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);
        rb.linearVelocity = finalDir.normalized * currentSpeed;
    }

    /// <summary>
    /// Rotate a 2D vector by the given angle in degrees.
    /// </summary>
    Vector2 RotateVector(Vector2 v, float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    /// <summary>
    /// Reset the ball to hex center and relaunch.
    /// </summary>
    public void ResetBall()
    {
        CancelInvoke();
        goalScored = false;
        transform.position = hexCenter;
        rb.linearVelocity = Vector2.zero;
        Invoke(nameof(LaunchBall), 1f);
    }
}
