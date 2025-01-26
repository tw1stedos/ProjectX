using UnityEngine;

public class BallDamage : MonoBehaviour
{
    [SerializeField] private int damage = 1; // Урон от шарика
    [SerializeField] private float hitForce = 0f; // Сила удара (установите 0, если не хотите применять силу)
    private bool hasDamaged = false; // Флаг для проверки, был ли уже нанесен урон

    private void OnCollisionEnter(Collision collision)
    {
        if (hasDamaged) return; // Если урон уже нанесен, выходим

        // Проверяем, есть ли у объекта здоровье
        CharacterHealth characterHealth = collision.gameObject.GetComponent<CharacterHealth>();
        ObjectHealth objectHealth = collision.gameObject.GetComponent<ObjectHealth>();

        if (characterHealth != null)
        {
            characterHealth.TakeDamage(damage);
            hasDamaged = true; // Устанавливаем флаг, что урон нанесен
            Debug.Log("Урон нанесен персонажу: " + damage);

            // Применяем силу к части тела, в которую попал шарик (если hitForce > 0)
            Rigidbody hitRigidbody = collision.rigidbody;
            if (hitRigidbody != null && hitForce > 0)
            {
                hitRigidbody.AddForce(collision.relativeVelocity * hitForce, ForceMode.Impulse);
            }
        }
        else if (objectHealth != null)
        {
            objectHealth.TakeDamage(damage);
            hasDamaged = true; // Устанавливаем флаг, что урон нанесен
            Debug.Log("Урон нанесен объекту: " + damage);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // Сбрасываем флаг, когда шарик перестает соприкасаться с объектом
        hasDamaged = false;
    }
}