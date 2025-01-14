using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public GameObject ballPrefab; // Префаб шарика
    public GameObject bonusBallPrefab; // Префаб бонусного шарика на время
    public GameObject lifeBonusBallPrefab; // Префаб бонусного шарика на жизнь
    public TextMeshProUGUI scoreText; // Текст для отображения счета
    public TextMeshProUGUI livesText; // Текст для отображения жизней
    public TextMeshProUGUI gameOverText; // Текст "Игра окончена"
    public TextMeshProUGUI timerText; // Текст для отображения времени
    public TextMeshProUGUI levelText; // Текст для отображения уровня
    public Button restartButton; // Кнопка рестарта
    public int score = 0; // Счет игрока
    public int lives = 3; // Количество жизней
    public int currentLevel = 1; // Текущий уровень
    public int ballsToSpawn = 3; // Количество шариков для появления
    public float timeLimit = 10f; // Время на выполнение уровня
    public int bonusChance = 10; // Шанс появления бонуса (в процентах)
    public int lifeBonusChance = 5; // Шанс появления бонуса на жизнь (в процентах)
    public float bonusTime = 5f; // Время, которое добавляет бонус

    private int currentSequence = 1; // Текущая цифра в последовательности
    private bool isGameOver = false;
    private float levelStartTime; // Время начала уровня

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

            // Запоминаем время начала уровня
            levelStartTime = Time.time;

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
                    Collider[] colliders = Physics.OverlapSphere(spawnPosition, 0.5f);
                    positionIsValid = colliders.Length == 0;

                    attempts++;
                } while (!positionIsValid && attempts < 100);

                // Если позиция валидна, создаем шарик
                if (positionIsValid)
                {
                    GameObject ball = Instantiate(ballPrefab, spawnPosition, Quaternion.identity);
                    ball.transform.parent = transform;
                    ball.GetComponent<Ball>().SetNumber(i + 1);
                }
            }

            // Создаем бонусный шарик на время (с определенной вероятностью)
            if (Random.Range(0, 100) < bonusChance)
            {
                SpawnBonusBall(bonusBallPrefab, 0); // Бонусный шарик на время имеет номер 0
            }

            // Создаем бонусный шарик на жизнь (с определенной вероятностью)
            if (Random.Range(0, 100) < lifeBonusChance)
            {
                SpawnBonusBall(lifeBonusBallPrefab, -2); // Бонусный шарик на жизнь имеет номер -2
            }

            // Ждем, пока игрок не нажмет на все шарики или не закончится время
            while (currentSequence <= ballsToSpawn && Time.time - levelStartTime < timeLimit)
            {
                // Обновляем таймер
                UpdateTimer(levelStartTime);
                yield return null;
            }

            // Если время вышло, игрок теряет жизнь
            if (Time.time - levelStartTime >= timeLimit)
            {
                LoseLife();
            }

            // Переходим на следующий уровень
            currentLevel++;
            ballsToSpawn++;
            timeLimit = Mathf.Max(5f, timeLimit - 1f); // Уменьшаем время на выполнение
            currentSequence = 1;

            // Обновляем UI
            UpdateUI();
        }
    }

    void SpawnBonusBall(GameObject bonusPrefab, int number)
    {
        Vector3 spawnPosition;
        bool positionIsValid;
        int attempts = 0;

        do
        {
            // Генерируем случайную позицию
            spawnPosition = new Vector3(Random.Range(-5f, 5f), Random.Range(-3f, 3f), 0);

            // Проверяем, нет ли других шариков в этой позиции
            Collider[] colliders = Physics.OverlapSphere(spawnPosition, 0.5f);
            positionIsValid = colliders.Length == 0;

            attempts++;
        } while (!positionIsValid && attempts < 100);

        // Если позиция валидна, создаем бонусный шарик
        if (positionIsValid)
        {
            GameObject bonusBall = Instantiate(bonusPrefab, spawnPosition, Quaternion.identity);
            bonusBall.transform.parent = transform;
            bonusBall.GetComponent<Ball>().SetNumber(number);
        }
    }

    void UpdateTimer(float startTime)
    {
        float remainingTime = timeLimit - (Time.time - startTime);
        timerText.text = "Time: " + Mathf.CeilToInt(remainingTime).ToString();
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
                else if (ball.number == 0)
                {
                    // Бонусный шарик на время
                    AddTime(bonusTime);
                    Destroy(ball.gameObject);
                }
                else if (ball.number == -2)
                {
                    // Бонусный шарик на жизнь
                    AddLife(1);
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

    void AddTime(float time)
    {
        timeLimit += time; // Добавляем время
        UpdateTimer(levelStartTime); // Обновляем таймер
    }

    void AddLife(int life)
    {
        lives += life; // Добавляем жизнь
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
        levelText.text = "Level: " + currentLevel; // Обновляем текст уровня
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
        timeLimit = 10f;
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