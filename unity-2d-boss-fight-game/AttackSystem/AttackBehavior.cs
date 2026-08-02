using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for custom attack movement and collision logic.
/// </summary>
public abstract class AttackBehavior : ScriptableObject
{
    public abstract void CollideWithTarget(Attack attack, AttackData data, Transform transform, Collider collision, GameObject owner = null);
    
    /// <summary>
    /// Updates the attack position and velocity.
    /// </summary>
    public abstract void Step(Attack attack, AttackData data, Transform transform, GameObject owner = null);
}
