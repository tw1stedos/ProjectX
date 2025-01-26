using UnityEngine;

public class CharacterHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100; // Максимальное здоровье
    [SerializeField] private int currentHealth;
    [SerializeField] private Animator animator; // Аниматор персонажа
    [SerializeField] private RagdollActivator ragdoll; // Компонент для активации рэгдолла
    

    public delegate void OnDeath(GameObject deadObject);
    public static event OnDeath OnDeathEvent;

    void Start()
    {
        currentHealth = maxHealth;

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (ragdoll == null)
        {
            ragdoll = GetComponent<RagdollActivator>();
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        OnDeathEvent?.Invoke(gameObject); // Уведомляем о смерти объекта

        // Останавливаем анимации и включаем рэгдолл
        if (animator != null)
        {
            animator.enabled = false;
        }

        if (ragdoll != null)
        {
            ragdoll.SetRagdoll(true);
        }
    }
}