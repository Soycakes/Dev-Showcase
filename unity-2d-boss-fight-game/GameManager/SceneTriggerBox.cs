using System;
using UnityEngine;

/// <summary>
/// Trigger volume that fires an event on player contact and destroys itself.
/// </summary>
public class SceneTriggerBox : MonoBehaviour
{
    public event Action PlayerEntered;

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerEntered?.Invoke();
            Destroy(gameObject);
        }
    }
}
