using UnityEngine;

/// <summary>
/// MASTER SETUP SCRIPT — Attach this to any GameObject in the scene (e.g., Main Camera).
/// It finds all game objects by name and sets up all missing components, colliders,
/// physics materials, and tags at runtime. This fixes the broken scene.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    void Awake()
    {
        Debug.Log("=== GameBootstrap: Setting up the game ===");

        SetupBall();
        SetupPaddle("Paddle1", KeyCode.W, KeyCode.S);
        SetupPaddle("Paddle2", KeyCode.UpArrow, KeyCode.DownArrow);
        SetupHexBoundary();
        SetupObstacles();
        SetupScoreManager();
        SetupGoalLines();

        Debug.Log("=== GameBootstrap: Setup complete ===");
    }

    void SetupBall()
    {
        GameObject ball = FindInScene("Circle");
        if (ball == null) { Debug.LogError("GameBootstrap: Cannot find 'Circle' (the ball)!"); return; }

        // Tag
        ball.tag = "Ball";

        // Rigidbody2D
        Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
        if (rb == null) rb = ball.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;

        // CircleCollider2D
        CircleCollider2D col = ball.GetComponent<CircleCollider2D>();
        if (col == null) col = ball.AddComponent<CircleCollider2D>();

        // Bounce material
        PhysicsMaterial2D mat = CreateBounceMaterial();
        rb.sharedMaterial = mat;
        col.sharedMaterial = mat;

        // BallController
        if (ball.GetComponent<BallController>() == null)
            ball.AddComponent<BallController>();

        Debug.Log("GameBootstrap: Ball setup OK.");
    }

    void SetupPaddle(string name, KeyCode upKey, KeyCode downKey)
    {
        GameObject paddle = FindInScene(name);
        if (paddle == null) { Debug.LogWarning($"GameBootstrap: Cannot find '{name}'!"); return; }

        // Tag
        paddle.tag = "Paddle";

        // Rigidbody2D
        Rigidbody2D rb = paddle.GetComponent<Rigidbody2D>();
        if (rb == null) rb = paddle.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // BoxCollider2D
        if (paddle.GetComponent<BoxCollider2D>() == null)
            paddle.AddComponent<BoxCollider2D>();

        // PaddleController
        PaddleController ctrl = paddle.GetComponent<PaddleController>();
        if (ctrl == null) ctrl = paddle.AddComponent<PaddleController>();
        ctrl.moveUpKey = upKey;
        ctrl.moveDownKey = downKey;

        Debug.Log($"GameBootstrap: Paddle '{name}' setup OK. Up={upKey}, Down={downKey}");
    }

    void SetupHexBoundary()
    {
        // "Hexagon Flat Top (1)" has the EdgeCollider2D but only 2 points.
        // We need to replace those with a proper closed hexagon.
        GameObject hex = FindInScene("Hexagon Flat Top (1)");
        if (hex == null) { Debug.LogWarning("GameBootstrap: Cannot find 'Hexagon Flat Top (1)'!"); return; }

        hex.tag = "Boundary";

        EdgeCollider2D edge = hex.GetComponent<EdgeCollider2D>();
        if (edge == null) edge = hex.AddComponent<EdgeCollider2D>();

        // The sprite is "Hexagon Flat Top" with m_Size (1, 0.8828125).
        // The object has localScale (12, 10.5, 1).
        // EdgeCollider2D points are in LOCAL space (before scale is applied).
        // So we define the hex shape to match the sprite, and Unity scales it up.
        //
        // Flat-top hex vertices for a unit-width sprite:
        //   width = 1.0, height = 0.8828125
        float halfW = 0.5f;
        float halfH = 0.8828125f / 2f; // = 0.44140625

        edge.points = new Vector2[]
        {
            new Vector2( halfW,          0f),       // right
            new Vector2( halfW * 0.5f,   halfH),    // top-right
            new Vector2(-halfW * 0.5f,   halfH),    // top-left
            new Vector2(-halfW,          0f),       // left
            new Vector2(-halfW * 0.5f,  -halfH),    // bottom-left
            new Vector2( halfW * 0.5f,  -halfH),    // bottom-right
            new Vector2( halfW,          0f),       // close the loop back to right
        };
        edge.edgeRadius = 0.02f; // small thickness (in local units — gets scaled by 12x)

        // Bounce material
        PhysicsMaterial2D mat = CreateBounceMaterial();
        edge.sharedMaterial = mat;

        Debug.Log($"GameBootstrap: Hexagon boundary setup with {edge.points.Length} points. " +
                  $"World size: ~{halfW * 2f * hex.transform.lossyScale.x} x {halfH * 2f * hex.transform.lossyScale.y}");
    }

    void SetupObstacles()
    {
        string[] names = { "Triangle", "Triangle (1)", "Triangle (2)", "Triangle (3)", "Triangle (4)" };
        foreach (string n in names)
        {
            GameObject obj = FindInScene(n);
            if (obj == null) continue;

            obj.tag = "Boundary";

            EdgeCollider2D edge = obj.GetComponent<EdgeCollider2D>();
            if (edge != null)
            {
                edge.edgeRadius = 0.05f;
                PhysicsMaterial2D mat = CreateBounceMaterial();
                edge.sharedMaterial = mat;
            }
        }
        Debug.Log("GameBootstrap: Obstacles setup OK.");
    }

    void SetupScoreManager()
    {
        if (FindAnyObjectByType<ScoreManager>() == null)
        {
            gameObject.AddComponent<ScoreManager>();
        }
        Debug.Log("GameBootstrap: ScoreManager setup OK.");
    }

    void SetupGoalLines()
    {
        // Get hex geometry to compute line height at each paddle's X
        GameObject hex = FindInScene("Hexagon Flat Top (1)");
        if (hex == null) { Debug.LogWarning("GameBootstrap: Cannot find hex for goal lines!"); return; }

        Vector3 hexPos = hex.transform.position;
        float hexHalfW = 0.5f * hex.transform.lossyScale.x;     // 6
        float hexHalfH = 0.44140625f * hex.transform.lossyScale.y; // ~4.63

        GameObject p1 = FindInScene("Paddle1");
        GameObject p2 = FindInScene("Paddle2");

        if (p1 != null)
        {
            float x = p1.transform.position.x;
            float h = GetHexHeightAtX(x, hexPos.x, hexHalfW, hexHalfH);
            CreateGoalLineObject("GoalLine_P1", x, hexPos.y - h, hexPos.y + h,
                                 new Color(0f, 0.9f, 1f, 0.5f)); // semi-transparent cyan
        }

        if (p2 != null)
        {
            float x = p2.transform.position.x;
            float h = GetHexHeightAtX(x, hexPos.x, hexHalfW, hexHalfH);
            CreateGoalLineObject("GoalLine_P2", x, hexPos.y - h, hexPos.y + h,
                                 new Color(1f, 0.3f, 0.3f, 0.5f)); // semi-transparent red
        }

        Debug.Log("GameBootstrap: Goal lines setup OK.");
    }

    /// <summary>
    /// Returns the half-height of the flat-top hex at a given world X.
    /// </summary>
    float GetHexHeightAtX(float worldX, float hexCenterX, float halfW, float halfH)
    {
        float dx = Mathf.Abs(worldX - hexCenterX);
        if (dx <= halfW * 0.5f)
            return halfH; // flat middle section
        else if (dx <= halfW)
            return halfH * (1f - (dx - halfW * 0.5f) / (halfW * 0.5f)); // angled side
        else
            return 0f;
    }

    void CreateGoalLineObject(string name, float x, float yBottom, float yTop, Color color)
    {
        GameObject go = new GameObject(name);
        go.AddComponent<LineRenderer>();
        GoalLine gl = go.AddComponent<GoalLine>();
        gl.Setup(x, yBottom, yTop, color);
    }

    PhysicsMaterial2D CreateBounceMaterial()
    {
        PhysicsMaterial2D mat = new PhysicsMaterial2D("Bounce");
        mat.bounciness = 1f;
        mat.friction = 0f;
        return mat;
    }

    /// <summary>
    /// Recursively find a GameObject by name, including inactive objects.
    /// </summary>
    GameObject FindInScene(string name)
    {
        // First try the fast path
        GameObject go = GameObject.Find(name);
        if (go != null) return go;

        // Search all root objects including inactive
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            var found = FindChildRecursive(root.transform, name);
            if (found != null) return found.gameObject;
        }
        return null;
    }

    Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            var result = FindChildRecursive(parent.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }
}
