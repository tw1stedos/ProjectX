using UnityEngine;

public class BallMovement : MonoBehaviour
{
    public float speed = 2f; // Скорость движения
    private Vector3 direction; // Направление движения

    void Start()
    {
        // Выбираем случайное направление
        direction = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0).normalized;
    }

    void Update()
    {
        // Двигаем шарик
        transform.Translate(direction * speed * Time.deltaTime);

        // Отскок от краев экрана
        if (Mathf.Abs(transform.position.x) > 5f || Mathf.Abs(transform.position.y) > 3f)
        {
            direction = -direction; // Меняем направление
        }
    }
}