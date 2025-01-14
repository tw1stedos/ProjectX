using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public Text scoreText;
    public Text timerText;
    private int score = 0;
    private float gameTime = 60f; // Время игры в секундах

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateScoreText();
        UpdateTimerText();
    }

    public void AddScore(int points)
    {
        score += points;
        UpdateScoreText();
    }

    private void UpdateScoreText()
    {
        scoreText.text = "Score: " + score;
    }

    public void StartTimer()
    {
        StartCoroutine(GameTimer());
    }

    private IEnumerator GameTimer()
    {
        while (gameTime > 0)
        {
            yield return new WaitForSeconds(1);
            gameTime--;
            UpdateTimerText();
        }

        EndGame();
    }

    private void UpdateTimerText()
    {
        timerText.text = "Time: " + gameTime.ToString("0");
    }

    private void EndGame()
    {
        Debug.Log("Game Over! Final Score: " + score);
        SaveScore();
        // Здесь можно добавить логику для завершения игры, например, переход на экран результатов
    }

    private void SaveScore()
    {
        PlayerPrefs.SetInt("HighScore", score);
        PlayerPrefs.Save();
    }
}