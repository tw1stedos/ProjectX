using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private int winScore = 10; // Очки для победы
    [SerializeField] private Text scoreText; // Текст для отображения счета
    [SerializeField] private GameObject winPanel; // Панель победы
    [SerializeField] private GameObject restartButton; // Кнопка рестарта
    [SerializeField] private GameObject nextLevelButton; // Кнопка перехода на следующий уровень
    [SerializeField] private float winDelay = 2f; // Задержка перед остановкой игры

    private int score = 0;

    public delegate void ScoreUpdated(int newScore);
    public static event ScoreUpdated OnScoreUpdated;

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
        // Скрываем кнопки при старте игры
        if (restartButton != null)
        {
            restartButton.SetActive(false);
        }
        if (nextLevelButton != null)
        {
            nextLevelButton.SetActive(false);
        }
        if (winPanel != null)
        {
            winPanel.SetActive(false); // Скрываем панель победы
        }
    }

    private void OnEnable()
    {
        // Подписываемся на событие уничтожения объекта
        ObjectHealth.OnDeathEvent += OnObjectDestroyed;
    }

    private void OnDisable()
    {
        // Отписываемся от события при выключении
        ObjectHealth.OnDeathEvent -= OnObjectDestroyed;
    }

    // Метод, который вызывается при уничтожении объекта
    private void OnObjectDestroyed(GameObject destroyedObject)
    {
        AddScore(1); // Добавляем очко за уничтожение объекта
    }

    public void AddScore(int points)
    {
        score += points;
        OnScoreUpdated?.Invoke(score); // Вызываем событие обновления счета

        if (scoreText != null)
        {
            scoreText.text = "Очки: " + score;
        }

        if (score >= winScore) // Условие победы
        {
            StartCoroutine(WinGameWithDelay()); // Запускаем корутину для плавного завершения
        }
    }

    // Корутина для задержки перед остановкой игры
    private IEnumerator WinGameWithDelay()
    {
        // Ждем указанное время перед остановкой игры
        yield return new WaitForSeconds(winDelay);

        // Останавливаем игру
        Time.timeScale = 0;

        // Показываем панель победы и кнопки
        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }
        if (restartButton != null)
        {
            restartButton.SetActive(true);
        }
        if (nextLevelButton != null)
        {
            nextLevelButton.SetActive(true);
        }
    }

    // Метод для рестарта игры
    public void RestartGame()
    {
        Time.timeScale = 1; // Возвращаем нормальную скорость игры
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // Перезагружаем текущую сцену
    }

    // Метод для перехода на следующий уровень
    public void LoadNextLevel()
    {
        Time.timeScale = 1; // Возвращаем нормальную скорость игры
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1; // Индекс следующей сцены
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings) // Проверяем, существует ли следующая сцена
        {
            SceneManager.LoadScene(nextSceneIndex); // Загружаем следующую сцену
        }
        else
        {
            Debug.LogWarning("Следующая сцена не найдена!");
            SceneManager.LoadScene(0); // Возвращаемся на первую сцену (например, главное меню)
        }
    }
}