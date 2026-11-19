using UnityEngine;
using UnityEngine.UI;

public class ScoreUpdate : MonoBehaviour
{
    private Text scoreText;
    public bool left = true;

    private void Awake()
    {
        scoreText = GetComponent<Text>();
    }

    private void Update()
    {
        if (GameManager.Instance == null)
            return;

        int score = left
            ? GameManager.Instance.scoreLeft
            : GameManager.Instance.scoreRight;

        scoreText.text = "Score: " + score;
    }

    public void UpdateScore(int score)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.addScore(score, left);
        }
    }
}