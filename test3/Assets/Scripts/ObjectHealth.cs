using UnityEngine;

public class ObjectHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100; // Максимальное здоровье
    [SerializeField] private int currentHealth; // Текущее здоровье
    [SerializeField] private GameObject explosionEffect; // Эффект взрыва
    [SerializeField] private float explosionEffectLifetime = 3f; // Время жизни эффекта взрыва (в секундах)
    [SerializeField] private GameObject destroyedPrefab; // Префаб разрушенного объекта (например, бочка с осколками)
    [SerializeField] private float explosionRadius = 5f; // Радиус взрыва
    [SerializeField] private int explosionDamage = 50; // Урон от взрыва
    [SerializeField] private float explosionForce = 500f; // Сила взрыва для осколков
    [SerializeField] private bool leaveDebris = true; // Оставлять ли осколки после взрыва
    [SerializeField] private float debrisLifetime = 6f; // Время жизни осколков (в секундах)

    [Header("Повреждений при падении")]
    [SerializeField] private float fallDamageThreshold = 10f; // Порог скорости для нанесения урона от падения
    [SerializeField] private int fallDamageMultiplier = 5; // Множитель урона от падения

    private Rigidbody rb;
    private bool isDead = false; // Флаг для проверки, уничтожен ли объект

    public delegate void OnDeath(GameObject deadObject);
    public static event OnDeath OnDeathEvent;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogWarning("Rigidbody компонент отсутствует. Повреждение от падения не будет работать.");
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isDead) return; // Если объект уже уничтожен, выходим

        if (rb != null)
        {
            // Рассчитываем скорость падения
            float fallSpeed = collision.relativeVelocity.magnitude;

            // Если скорость падения превышает порог, наносим урон
            if (fallSpeed > fallDamageThreshold)
            {
                int damage = Mathf.RoundToInt((fallSpeed - fallDamageThreshold) * fallDamageMultiplier);
                TakeDamage(damage);
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return; // Если объект уже уничтожен, выходим

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return; // Если объект уже уничтожен, выходим
        isDead = true; // Устанавливаем флаг, что объект уничтожен

        OnDeathEvent?.Invoke(gameObject); // Уведомляем о смерти объекта

        // Создаем эффект взрыва
        if (explosionEffect != null)
        {
            GameObject explosion = Instantiate(explosionEffect, transform.position, transform.rotation);
            Destroy(explosion, explosionEffectLifetime); // Уничтожаем эффект взрыва через заданное время
        }

        // Создаем разрушенный объект (например, бочка с осколками), если leaveDebris = true
        if (leaveDebris && destroyedPrefab != null)
        {
            GameObject destroyedObject = Instantiate(destroyedPrefab, transform.position, transform.rotation);

            // Применяем силу взрыва к осколкам
            Rigidbody[] rigidbodies = destroyedObject.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody rb in rigidbodies)
            {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }

            // Уничтожаем осколки через debrisLifetime секунд
            Destroy(destroyedObject, debrisLifetime);
        }

        // Наносим урон nearby объектам
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider nearbyObject in colliders)
        {
            // Наносим урон персонажам
            CharacterHealth characterHealth = nearbyObject.GetComponent<CharacterHealth>();
            if (characterHealth != null)
            {
                characterHealth.TakeDamage(explosionDamage);
            }

            // Наносим урон объектам
            ObjectHealth objectHealth = nearbyObject.GetComponent<ObjectHealth>();
            if (objectHealth != null && objectHealth != this) // Исключаем текущий объект
            {
                objectHealth.TakeDamage(explosionDamage);
            }
        }

        Destroy(gameObject); // Уничтожаем объект
    }

    // Визуализация радиуса взрыва в редакторе
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}