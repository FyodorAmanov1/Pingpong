using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Paddle controller with hexagon boundary clamping.
/// Works with the New Input System.
/// 
/// REQUIRED COMPONENTS (auto-added):
/// - Rigidbody2D
/// - BoxCollider2D
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PaddleController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 10f;

    [Header("Key Bindings")]
    public KeyCode moveUpKey = KeyCode.W;
    public KeyCode moveDownKey = KeyCode.S;

    private Rigidbody2D rb;
    private float moveInput;

    // Hex boundary info (computed at Start)
    private Vector2 hexCenter;
    private float hexHalfW; // world-space half-width of hex
    private float hexHalfH; // world-space half-height of hex
    private float paddleHalfHeight;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Find the hexagon boundary to compute movement limits
        GameObject hex = GameObject.Find("Hexagon Flat Top (1)");
        if (hex != null)
        {
            hexCenter = hex.transform.position;
            // Hex local half-dimensions * world scale
            hexHalfW = 0.5f * hex.transform.lossyScale.x;   // 0.5 * 12 = 6
            hexHalfH = (0.8828125f / 2f) * hex.transform.lossyScale.y; // 0.4414 * 10.5 ≈ 4.63
        }
        else
        {
            hexCenter = Vector2.zero;
            hexHalfW = 6f;
            hexHalfH = 4.63f;
        }

        // Paddle visual half-height in world
        paddleHalfHeight = transform.lossyScale.y * 0.5f;
    }

    void Update()
    {
        moveInput = 0f;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (IsKeyPressed(keyboard, moveUpKey))
                moveInput = 1f;
            else if (IsKeyPressed(keyboard, moveDownKey))
                moveInput = -1f;
        }
    }

    void FixedUpdate()
    {
        // Apply movement
        rb.linearVelocity = new Vector2(0f, moveInput * speed);

        // Clamp position inside the hexagon
        Vector2 pos = rb.position;
        float maxY = GetHexMaxY(pos.x);

        // Clamp so the paddle edges stay inside
        float clampedY = Mathf.Clamp(pos.y, hexCenter.y - maxY + paddleHalfHeight,
                                             hexCenter.y + maxY - paddleHalfHeight);

        if (Mathf.Abs(pos.y - clampedY) > 0.001f)
        {
            rb.position = new Vector2(pos.x, clampedY);
            rb.linearVelocity = Vector2.zero; // stop at the wall
        }
    }

    /// <summary>
    /// For a flat-top hexagon, compute the max Y distance from center
    /// at a given world X position. The hex has 3 zones:
    ///   |x| <= halfW/2 → full height (hexHalfH)
    ///   halfW/2 < |x| <= halfW → linear slope down to 0
    /// </summary>
    float GetHexMaxY(float worldX)
    {
        float dx = Mathf.Abs(worldX - hexCenter.x);

        if (dx <= hexHalfW * 0.5f)
        {
            // In the flat middle section
            return hexHalfH;
        }
        else if (dx <= hexHalfW)
        {
            // On the angled sides — linearly decreasing
            float t = (dx - hexHalfW * 0.5f) / (hexHalfW * 0.5f);
            return hexHalfH * (1f - t);
        }
        else
        {
            // Outside hex entirely
            return 0f;
        }
    }

    private bool IsKeyPressed(Keyboard kb, KeyCode key)
    {
        switch (key)
        {
            case KeyCode.W:           return kb.wKey.isPressed;
            case KeyCode.S:           return kb.sKey.isPressed;
            case KeyCode.A:           return kb.aKey.isPressed;
            case KeyCode.D:           return kb.dKey.isPressed;
            case KeyCode.UpArrow:     return kb.upArrowKey.isPressed;
            case KeyCode.DownArrow:   return kb.downArrowKey.isPressed;
            case KeyCode.LeftArrow:   return kb.leftArrowKey.isPressed;
            case KeyCode.RightArrow:  return kb.rightArrowKey.isPressed;
            case KeyCode.I:           return kb.iKey.isPressed;
            case KeyCode.K:           return kb.kKey.isPressed;
            default: return false;
        }
    }
}
