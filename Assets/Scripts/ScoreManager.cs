using UnityEngine;

/// <summary>
/// Manages scoring for both players.
/// Attach to any GameObject (e.g., Main Camera).
///
/// Scoring rule:
///   If the ball passes behind Paddle1 (left side) → Player 2 scores.
///   If the ball passes behind Paddle2 (right side) → Player 1 scores.
///
/// Displays scores on-screen using OnGUI (no extra UI objects needed).
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Scores")]
    public int scorePlayer1 = 0;
    public int scorePlayer2 = 0;

    [Header("Display")]
    [Tooltip("Font size for the score display.")]
    public int fontSize = 36;

    private GUIStyle scoreStyle;

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void OnGUI()
    {
        if (scoreStyle == null)
        {
            scoreStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold
            };
        }

        float sw = Screen.width;

        // Player 1 score — left quarter
        scoreStyle.alignment = TextAnchor.UpperCenter;
        scoreStyle.normal.textColor = Color.cyan;
        GUI.Label(new Rect(sw * 0.1f, 15f, sw * 0.3f, 60f),
                  $"P1: {scorePlayer1}", scoreStyle);

        // Player 2 score — right quarter
        scoreStyle.normal.textColor = Color.red;
        GUI.Label(new Rect(sw * 0.6f, 15f, sw * 0.3f, 60f),
                  $"P2: {scorePlayer2}", scoreStyle);
    }

    /// <summary>
    /// Call this when a player scores a goal.
    /// player = 1 → Player 1 scored (ball went behind Paddle2).
    /// player = 2 → Player 2 scored (ball went behind Paddle1).
    /// </summary>
    public void ScoreGoal(int player)
    {
        if (player == 1)
        {
            scorePlayer1++;
            Debug.Log($"GOAL! Player 1 scores! ({scorePlayer1} - {scorePlayer2})");
        }
        else if (player == 2)
        {
            scorePlayer2++;
            Debug.Log($"GOAL! Player 2 scores! ({scorePlayer1} - {scorePlayer2})");
        }

        // Reset the ball
        BallController ball = FindAnyObjectByType<BallController>();
        if (ball != null)
        {
            ball.ResetBall();
        }
    }

    /// <summary>
    /// Reset both scores to zero.
    /// </summary>
    public void ResetScores()
    {
        scorePlayer1 = 0;
        scorePlayer2 = 0;
        Debug.Log("Scores reset.");
    }
}
