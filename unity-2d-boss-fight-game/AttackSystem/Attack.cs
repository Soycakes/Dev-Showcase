using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CollisionType
{
    Player,
    Enemy,
    Terrain,
}

/// <summary>
/// This script was architected by me, written by a developer teammate, then refactored & maintained by me.
/// 
/// Container script for attacks and projectiles.
/// Binds attack data, visual animations, and collision behaviors.
/// </summary>
public class Attack : MonoBehaviour
{
    public GameObject owner;
    public AttackData data;
    public Vector3 direction;

    private GameObject spriteObject;
    private SpriteRenderer spriteRenderer;
    private BoxCollider hitboxCollider;
    private Rigidbody rigidBody;
    private float velocity;

    void Start()
    {
        InitializeData(data);

        if(!data.isRotationLocked)
        {
            RotateTowardsDirection();
        }

        if (data.playOnStart)
        {
            StartCoroutine(PlayAnimation());
        }
    }

    public void InitializeData(AttackData data)
    {
        Transform spriteTransform = transform.Find("Sprite");
        if (spriteTransform == null)
        {
            Debug.LogWarning($"{name}: no child named 'Sprite' found; skipping sprite setup.", this);
        }
        else
        {
            spriteObject = spriteTransform.gameObject;
            spriteRenderer = spriteObject.GetComponent<SpriteRenderer>();
        }

        hitboxCollider = GetComponent<BoxCollider>();
        rigidBody = GetComponent<Rigidbody>();
        this.data = data;

        if (data == null)
        {
            return;
        }

        if (spriteRenderer != null && (data.sprite != null || data.frames?.Length > 0))
        {
            spriteRenderer.sprite = data.sprite;
            spriteObject.transform.localScale = data.spriteScale;
            spriteObject.transform.position = transform.position + data.spriteOffset;
            spriteObject.transform.rotation = Quaternion.Euler(0, 0, data.spriteRotationOffset);
        }

        hitboxCollider.size = data.hitboxSize;
        hitboxCollider.center = data.hitboxOffset;

        // Use custom velocity if already modified.
        if(velocity == 0f)
        {
            velocity = data.startingVelocity;
        }

        if (data.timeBeforeDestroy > 0f)
        {
            DestroyAfterTime();
        }
    }

    void FixedUpdate()
    {
        if (data == null)
        {
            return;
        }

        if (data.behavior == null)
        {
            UpdateVelocity();
        }
        else
        {
            data.behavior.Step(this, data, transform, owner);
        }

        if (!data.isRotationLocked)
        {
            RotateTowardsDirection();
        }

        if (data.isRotationFlipped && spriteRenderer != null)
        {
            spriteRenderer.flipX = direction.x < 0;
        }
    }

    public void UpdateVelocity()
    {
        float delta = Time.fixedDeltaTime;

        if (data.gravity != 0f)
        {
            direction.y += data.gravity * delta;
        }

        velocity += data.acceleration * delta;

        if (velocity > data.terminalVelocity && data.terminalVelocity > 0)
        {
            velocity = data.terminalVelocity;
        }

        Vector3 nextPosition = transform.position + direction * velocity * delta;

        // Move via rigidbody if kinematic to support smooth physics updates.
        if (rigidBody != null && rigidBody.isKinematic)
        {
            rigidBody.MovePosition(nextPosition);
        }
        else
        {
            transform.position = nextPosition;
        }
    }

    public void RotateTowardsDirection()
    {
        if (direction == Vector3.zero)
        {
            return;
        }

        if (Mathf.Abs(direction.z) > Mathf.Abs(direction.x) ||
            Mathf.Abs(direction.z) > Mathf.Abs(direction.y))
        {
            transform.rotation = Quaternion.LookRotation(direction);
            transform.Rotate(-90f, 0f, 0f);
        }
        else
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle + 90f));
        }
    }

    // Handles collision logic and applies behavior effects.
    void OnTriggerEnter(Collider collision)
    {
        if (data == null)
        {
            return;
        }

        bool collisionFlag = true;

        if (data.canHitPlayer && collision.CompareTag("PlayerHitbox"))
        {
            DamageObject(collision);
        }
        else if (data.canHitEnemy && collision.CompareTag("EnemyHitbox"))
        {
            DamageObject(collision);
        }
        else if (data.canHitTerrain && collision.CompareTag("TerrainHitbox"))
        {
            // Terrain
        }
        else if (data.canHitAttack && collision.CompareTag("AttackHitbox"))
        {
            // Attacks
        }
        else
        {
            collisionFlag = false;
        }

        if (collisionFlag)
        {
            if (data.behavior != null)
            {
                data.behavior.CollideWithTarget(this, data, transform, collision, owner);
            }

            if (data.isDestroyedOnHit)
            {
                Destroy(gameObject);
            }
        }
    }

    // Inflicts damage by searching the hierarchy for the Stats component.
    private void DamageObject(Collider collision)
    {
        Stats stats = collision.gameObject.GetComponent<Stats>() ??
                      collision.transform.parent?.GetComponent<Stats>() ??
                      collision.transform.parent?.GetComponent<EnemyController>()?.stats ??
                      collision.transform.parent?.transform.parent?.GetComponent<EnemyController>()?.stats;

        if (stats == null)
        {
            Debug.LogWarning($"Attack hit {collision.name} (tagged for damage) but no Stats component was found in its hierarchy.", collision);
            return;
        }

        stats.ChangeHealth(-data.damage);
    }

    private IEnumerator PlayAnimation()
    {
        if (data == null)
        {
            yield break;
        }

        if (data.frames == null || data.frames.Length == 0 || spriteRenderer == null)
        {
            yield break;
        }

        WaitForSeconds frameDelay = new WaitForSeconds(1f / data.frameRate);

        while (true)
        {
            foreach (Sprite frame in data.frames)
            {
                spriteRenderer.sprite = frame;
                yield return frameDelay;
            }

            if (data.destroyAfterAnimation)
            {
                Destroy(gameObject);
                yield break;
            }

            if (!data.loopAnimation)
            {
                yield break;
            }
        }
    }

    public float GetVelocity()
    {
        return velocity;
    }

    public void SetVelocity(float velocity)
    {
        this.velocity = velocity;
    }

    public void DestroyAfterTime(float? time = null)
    {
        Destroy(gameObject, time ?? data.timeBeforeDestroy);
    }
}
