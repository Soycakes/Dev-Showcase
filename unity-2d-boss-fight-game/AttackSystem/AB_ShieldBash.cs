using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles player shield bash collision effects.
/// Spawns mana orbs on projectile block, triggers boss knockback, and handles lifesteal.
/// </summary>
[CreateAssetMenu(fileName = "AB_ShieldBash", menuName = "Attack/Behavior/AB_ShieldBash")]
public class AB_ShieldBash : AttackBehavior
{
    public GameObject manaOrb;
    public float damageReduction = 0.5f;
    public float lifestealAmount = 0.15f;

    public override void CollideWithTarget(Attack attack, AttackData data, Transform transform, Collider collision, GameObject owner = null)
    {
        if (owner == null)
        {
            Debug.LogWarning("AB_ShieldBash requires an owner to resolve the PlayerController; ignoring collision.");
            return;
        }

        // Use local variable because the behavior asset is shared.
        bool knockback = false;

        PlayerController player = owner.GetComponent<PlayerController>();

        if (collision.CompareTag("AttackHitbox"))
        { 
            AttackData enemyAttackData = collision.gameObject.GetComponent<Attack>().data;

            // Bounce the jester back if in balancing act state.
            if (enemyAttackData.reflectsBalancingAct)
            {
                knockback = true;
                DuoJester duoJester = collision.GetComponentInParent<DuoJester>();
                duoJester?.GetComponent<DJ_S_BalancingAct>()?.ReverseDirection(Mathf.Sign(player.GetPlayerFacingDirection().x));
            }

            // Absorb projectile and restore mana.
            if (enemyAttackData.isProjectile)
            {
                Instantiate(manaOrb, transform.position, Quaternion.identity);
                Destroy(collision.gameObject);
            }
        }

        // Apply lifesteal if hitting a vulnerable boss.
        else if (collision.GetComponentInParent<IVulnerableBoss>() is IVulnerableBoss vulnerableBoss)
        {
            knockback = true;

            if (vulnerableBoss.IsVulnerable)
            {
                Stats playerStats = owner.GetComponent<Stats>();
                playerStats?.ChangeHealth(data.damage * lifestealAmount);
            }

            // Reverse jester direction.
            if (vulnerableBoss is DuoJester duoJester)
            {
                duoJester.GetComponent<DJ_S_BalancingAct>()?.ReverseDirection(Mathf.Sign(player.GetPlayerFacingDirection().x));
            }
        }

        if (knockback)
        {
            player.animator.SetBool("ShieldHit", true);
            player.Knockback(10.0f, 3f, 0.1f);
            Destroy(attack.gameObject);
        }
    }

    public override void Step(Attack attack, AttackData data, Transform transform, GameObject owner = null)
    {
        attack.UpdateVelocity();
    }
}
