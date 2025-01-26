using UnityEngine;

public class BallShooter : MonoBehaviour
{
    [SerializeField] private GameObject ballPrefab; // Префаб шарика
    [SerializeField] private float minBallSpeed = 5f; // Минимальная скорость шарика
    [SerializeField] private float maxBallSpeed = 20f; // Максимальная скорость шарика
    [SerializeField] private float shootCooldown = 0.5f; // Задержка между выстрелами
    [SerializeField] private float chargeTime = 1f; // Время для полного заряда выстрела
    [SerializeField] private LineRenderer aimLine; // Линия прицела

    private float lastShootTime;
    private float chargeStartTime;
    private bool isCharging = false;

    void Start()
    {
        // Инициализация линии прицела
        if (aimLine != null)
        {
            aimLine.positionCount = 2; // Линия состоит из двух точек
            aimLine.enabled = false; // Скрываем линию прицела в начале
        }
    }

    void Update()
    {
        // Начало заряда выстрела
        if (Input.GetMouseButtonDown(0) && Time.time > lastShootTime + shootCooldown)
        {
            StartCharging();
        }

        // Заряд выстрела
        if (isCharging)
        {
            ChargeShot();
        }

        // Выстрел
        if (Input.GetMouseButtonUp(0) && isCharging)
        {
            ShootBall();
            isCharging = false;
            if (aimLine != null)
            {
                aimLine.enabled = false; // Скрываем линию прицела после выстрела
            }
        }
    }

    void StartCharging()
    {
        isCharging = true;
        chargeStartTime = Time.time;

        // Показываем линию прицела
        if (aimLine != null)
        {
            aimLine.enabled = true;
        }
    }

    void ChargeShot()
    {
        // Обновляем линию прицела
        if (aimLine != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Vector3 direction = ray.direction;

            // Рассчитываем начальную и конечную точки линии прицела
            Vector3 startPoint = transform.position;
            Vector3 endPoint = startPoint + direction * maxBallSpeed;

            aimLine.SetPosition(0, startPoint);
            aimLine.SetPosition(1, endPoint);
        }
    }

    void ShootBall()
    {
        // Создаем шарик
        GameObject ball = Instantiate(ballPrefab, transform.position, Quaternion.identity);

        // Направление выстрела
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 direction = ray.direction;

        // Рассчитываем силу выстрела на основе времени заряда
        float chargeDuration = Time.time - chargeStartTime;
        float chargeRatio = Mathf.Clamp01(chargeDuration / chargeTime);
        float ballSpeed = Mathf.Lerp(minBallSpeed, maxBallSpeed, chargeRatio);

        // Запускаем шарик
        Rigidbody ballRigidbody = ball.GetComponent<Rigidbody>();
        ballRigidbody.velocity = direction * ballSpeed;

        // Уничтожаем шарик через 5 секунд (чтобы избежать накопления шариков на сцене)
        Destroy(ball, 5f);

        // Обновляем время последнего выстрела
        lastShootTime = Time.time;
    }
}