using UnityEngine;

/// <summary>
/// Draws a visible goal line at a paddle's X position using a LineRenderer.
/// The line spans the hex boundary height at that X coordinate.
/// Attach via GameBootstrap — no manual setup needed.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class GoalLine : MonoBehaviour
{
    public Color lineColor = Color.white;
    public float lineWidth = 0.06f;

    private LineRenderer lr;

    /// <summary>
    /// Call after creating the GameObject to set line endpoints.
    /// </summary>
    public void Setup(float worldX, float yBottom, float yTop, Color color)
    {
        lineColor = color;

        lr = GetComponent<LineRenderer>();
        if (lr == null) lr = gameObject.AddComponent<LineRenderer>();

        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.SetPosition(0, new Vector3(worldX, yBottom, 0f));
        lr.SetPosition(1, new Vector3(worldX, yTop, 0f));

        // Dashed look via material-free approach: thin solid line
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.startColor = color;
        lr.endColor = color;
        lr.sortingOrder = 5; // draw on top of the hex background

        // Use the default sprite material so the color shows up
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.material.color = color;
    }
}
