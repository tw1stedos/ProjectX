using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public GameObject ballPrefab; // Префаб шарика
    public TextMeshProUGUI scoreText; // Текст для отображения счета
    public TextMeshProUGUI livesText; // Текст для отображения жизней
    public TextMeshProUGUI gameOverText; // Текст "Игра окончена"
    public Button restartButton; // Кнопка рестарта
    public int score = 0; // Счет игрока
    public int lives = 3; // Количество жизней
    public int currentLevel = 1; // Текущий уровень
    public int ballsToSpawn = 3; // Количество шариков для появления

    private int currentSequence = 1; // Текущая цифра в последовательности
    private bool isGameOver = false;

    void Start()
    {
        // Назначаем метод RestartGame на кнопку рестарта
        restartButton.onClick.AddListener(RestartGame);

        // Начинаем игру
        StartCoroutine(SpawnBalls());
        UpdateUI();
    }

    void Update()
    {
        if (!isGameOver && Input.GetMouseButtonDown(0))
        {
            CheckClick();
        }
    }

    IEnumerator SpawnBalls()
    {
        while (!isGameOver)
        {
            // Очищаем старые шарики
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            // Создаем новые шарики
            for (int i = 0; i < ballsToSpawn; i++)
            {
                Vector3 spawnPosition;
                bool positionIsValid;
                int attempts = 0;

                do
                {
                    // Генерируем случайную позицию
                    spawnPosition = new Vector3(Random.Range(-5f, 5f), Random.Range(-3f, 3f), 0);

                    // Проверяем, нет ли других шариков в этой позиции
                    Collider[] colliders = Physics.OverlapSphere(spawnPosition, 0.5f); // 0.5f — радиус шарика
                    positionIsValid = colliders.Length == 0; // Если коллайдеров нет, позиция валидна

                    attempts++;
                } while (!positionIsValid && attempts < 100); // Пытаемся найти валидную позицию до 100 раз

                // Если позиция валидна, создаем шарик
                if (positionIsValid)
                {
                    GameObject ball = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);
                    ball.transform.parent = transform;
                    ball.GetComponent<Ball>().SetNumber(i + 1);
                }
            }

            // Ждем, пока игрок не нажмет на все шарики
            yield return new WaitUntil(() => currentSequence > ballsToSpawn);

            // Переходим на следующий уровень
            currentLevel++;
            ballsToSpawn++;
            currentSequence = 1;

            // Обновляем UI
            UpdateUI();
        }
    }

    void CheckClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            Ball ball = hit.collider.GetComponent<Ball>();
            if (ball != null)
            {
                if (ball.number == currentSequence)
                {
                    // Правильный шаг
                    AddScore(10);
                    currentSequence++;
                    Destroy(ball.gameObject);
                }
                else
                {
                    // Неправильный шаг
                    LoseLife();
                }
            }
        }
    }

    void AddScore(int points)
    {
        score += points;
        UpdateUI();
    }

    void LoseLife()
    {
        lives--;
        UpdateUI();

        if (lives <= 0)
        {
            GameOver();
        }
    }

    void UpdateUI()
    {
        scoreText.text = "Score: " + score;
        livesText.text = "Lives: " + lives;
    }

    void GameOver()
    {
        isGameOver = true;
        gameOverText.gameObject.SetActive(true);
        restartButton.gameObject.SetActive(true); // Показываем кнопку рестарта
        StopAllCoroutines();
    }

    void RestartGame()
    {
        // Сбрасываем все параметры игры
        score = 0;
        lives = 3;
        currentLevel = 1;
        ballsToSpawn = 3;
        currentSequence = 1;
        isGameOver = false;

        // Очищаем сцену
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Скрываем кнопку рестарта и текст "Game Over"
        gameOverText.gameObject.SetActive(false);
        restartButton.gameObject.SetActive(false);

        // Начинаем игру заново
        StartCoroutine(SpawnBalls());
        UpdateUI();
    }
}