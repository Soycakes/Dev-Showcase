using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for boss controllers.
/// Manages state transitions and core boss logic.
/// </summary>
public abstract class EnemyController : MonoBehaviour
{
    public GameObject attackPrefab;
    public Stats stats;
    public float totalHealth = 0f;

    public SpriteRenderer spriteRenderer;
    public Animator animator;

    // Default phase, used to scale difficulty or timing.
    public int phase = 1;

    public GameObject player;
    public EnemyState currentState;
    protected Dictionary<Type, EnemyState> stateDictionary = new();

    // Reference to the active state coroutine so it can be stopped.
    private Coroutine runningStateCoroutine;

    public float mapBoundaryLeft = 0f;
    public float mapBoundaryRight = 0f;

    /// <summary>
    /// Initializes stats and events, then calls OnStart.
    /// </summary>
    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        stats = GetComponent<Stats>();

        if (stats == null)
        {
            Debug.LogError($"{name}: no Stats component found; boss health tracking will not work.", this);
            return;
        }

        stats.onHealthChangeEvent += OnHitEffect;
        stats.onHealthChangeEvent += PhaseCheck;
        stats.onDeathEvent += OnDeathEffect;
        totalHealth = stats.health;

        if (!spriteRenderer)
        {
            spriteRenderer = transform.Find("Sprite")?.GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer && !animator)
        {
            animator = spriteRenderer.GetComponent<Animator>();
        }

        OnStart();
    }

    protected abstract void OnStart();

    public abstract void PhaseCheck(float f);
    public abstract void OnHitEffect(float healthChange);
    public abstract void OnDeathEffect();

    public abstract void CleanState();

    private void OnDisable()
    {
        if (stats != null)
        {
            stats.onHealthChangeEvent -= OnHitEffect;
            stats.onHealthChangeEvent -= PhaseCheck;
            stats.onDeathEvent -= OnDeathEffect;
        }
    }

    /// <summary>
    /// Switches the boss to a new state. Caches state instances in a dictionary.
    /// </summary>
    /// <typeparam name="T">The state class type to enter.</typeparam>
    public virtual void EnterState<T>() where T : EnemyState
    {
        // Retrieve state from dictionary or add it if missing.
        if (!stateDictionary.TryGetValue(typeof(T), out EnemyState nextState))
        {
            nextState = gameObject.AddComponent<T>();
            nextState.Initialize(this);
            stateDictionary.Add(typeof(T), nextState);
        }

        // Exit the current state if one is active.
        if (currentState != null)
        {
            if (!currentState.CanExit() || !nextState.CanEnter())
            {
                return;
            }

            currentState.OnExit();
            currentState.EndState();

            if (runningStateCoroutine != null)
            {
                StopCoroutine(runningStateCoroutine);
            }
        }

        // Start the new state.
        currentState = nextState;
        currentState.OnEnter();
        runningStateCoroutine = StartCoroutine(currentState.StateCoroutine());
    }

    #region Vector Helper Methods
    public float GetDistanceToPlayer()
    {
        return Vector3.Distance(this.transform.position, player.transform.position);
    }

    public Vector3 GetDirectionToPlayer()
    {
        return (player.transform.position - transform.position).normalized;
    }

    public Vector3 GetDirectionFromPlayer()
    {
        return (transform.position - player.transform.position).normalized;
    }

    public Vector3 GetDirectionToPlayer(float offsetAngle)
    {
        return Quaternion.Euler(0, 0, offsetAngle) * GetDirectionToPlayer();
    }
    #endregion

    /// <summary>
    /// Orients the boss toward the player.
    /// </summary>
    public abstract void OrientBoss();

    public virtual GameObject SpawnAttack(AttackData attackData, Vector3 location, Vector3 direction, float offsetX = 0f, float offsetY = 0f)
    {
        Vector3 offset = new Vector3(offsetX * Mathf.Sign(direction.x), offsetY);

        GameObject attack = Instantiate(attackPrefab, location + offset, Quaternion.identity);

        Attack attackScript = attack.GetComponent<Attack>();

        attackScript.owner = transform.gameObject;
        attackScript.data = attackData;
        attackScript.direction = direction;

        return attack;
    }

    public virtual IEnumerator Idle(float duration)
    {
        OrientBoss();
        yield return new WaitForSeconds(duration);
        OrientBoss();
    }

    public virtual void TriggerBindEffect()
    {
        StopAllCoroutines();
        StartCoroutine(StateBound());
    }

    protected virtual IEnumerator StateBound()
    {
        yield return null;
    }

    public virtual void SetUntargettable(bool isUntargettable)
    {
        var hitbox = transform.Find("Hitbox");
        if (hitbox != null && hitbox.TryGetComponent<BoxCollider>(out var col))
        {
            col.enabled = !isUntargettable;
        }
    }
}
