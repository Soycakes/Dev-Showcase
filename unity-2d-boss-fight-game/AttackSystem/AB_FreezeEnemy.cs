using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Binds the hit enemy instead of dealing damage.
/// </summary>
[CreateAssetMenu(fileName = "AB_FreezeEnemy", menuName = "Attack/Behavior/AB_FreezeEnemy")]
public class AB_FreezeEnemy : AttackBehavior
{
    public override void CollideWithTarget(Attack attack, AttackData data, Transform transform, Collider collision, GameObject owner = null)
    {
        EnemyController ec = collision.transform.parent?.GetComponentInChildren<EnemyController>();
        ec?.TriggerBindEffect();
    }

    public override void Step(Attack attack, AttackData data, Transform transform, GameObject owner = null)
    {
        attack.UpdateVelocity();
    }
}
