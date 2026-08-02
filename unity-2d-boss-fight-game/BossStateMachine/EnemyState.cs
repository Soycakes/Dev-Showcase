using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for boss states.
/// Encapsulates individual boss actions and patterns.
/// </summary>
public abstract class EnemyState : MonoBehaviour
{
    protected EnemyController enemyController;

    public void Initialize(EnemyController enemyController)
    {
        this.enemyController = enemyController;
    }

    /// <summary>
    /// Runs the state logic, animations, and movement.
    /// </summary>
    public abstract IEnumerator StateCoroutine();
    public virtual void OnEnter() { }
    public virtual void OnExit() { }
    public virtual bool CanEnter() => true;
    public virtual bool CanExit() => true;

    /// <summary>
    /// Cleans up state assets, objects, or animations on exit or interruption.
    /// </summary>
    public virtual void EndState() { }
}

/// <summary>
/// Generic base class for states bound to a specific boss controller type.
/// </summary>
public abstract class EnemyState<T> : EnemyState where T : EnemyController
{
    protected T Controller => (T)enemyController;
}
