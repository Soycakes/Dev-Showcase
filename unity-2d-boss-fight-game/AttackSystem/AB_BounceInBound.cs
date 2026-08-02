using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bounces a projectile inward at the bounds of the play area.
/// </summary>
[CreateAssetMenu(fileName = "AB_BounceInBound", menuName = "Attack/Behavior/AB_BounceInBound")]
public class AB_BounceInBound : AttackBehavior
{
    // Default boundary values for the arena.
    public Vector2 xBounds = new Vector2(-15.5f, 14.5f);
    public Vector2 yBounds = new Vector2(0.75f, 14f);

    // Collision behavior is ignored.
    public override void CollideWithTarget(Attack attack, AttackData data, Transform transform, Collider collision, GameObject owner = null) { }

    public override void Step(Attack attack, AttackData data, Transform transform, GameObject owner = null)
    {
        // Keep direction pointing inside boundaries.
        if (transform.position.x <= xBounds.x)
        {
            attack.direction.x = Mathf.Abs(attack.direction.x);
        }
        else if (transform.position.x >= xBounds.y)
        {
            attack.direction.x = -Mathf.Abs(attack.direction.x);
        }

        if (transform.position.y <= yBounds.x)
        {
            attack.direction.y = Mathf.Abs(attack.direction.y);
        }
        else if (transform.position.y >= yBounds.y)
        {
            attack.direction.y = -Mathf.Abs(attack.direction.y);
        }

        attack.UpdateVelocity();
    }
}
