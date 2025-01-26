using UnityEngine;

public class RagdollActivator : MonoBehaviour
{
    private Rigidbody[] rigidbodies;
    private Collider[] colliders;

    void Start()
    {
        rigidbodies = GetComponentsInChildren<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
        SetRagdoll(false); // Отключаем рэгдолл в начале
    }

    public void SetRagdoll(bool isActive)
    {
        foreach (var rb in rigidbodies)
        {
            rb.isKinematic = !isActive;
        }

        foreach (var collider in colliders)
        {
            collider.enabled = isActive;
        }
    }
}