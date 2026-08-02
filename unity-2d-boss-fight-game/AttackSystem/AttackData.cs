using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data container holding stats, visuals, animation loops, and collider sizes for attacks.
/// </summary>
[CreateAssetMenu(fileName = "AttackData", menuName = "Attack/AttackData")]
public class AttackData : ScriptableObject
{
    public Sprite sprite;
    public Sprite[] frames;
    public AttackBehavior behavior;

    public int frameRate = 8;
    public bool playOnStart = true;
    public bool destroyAfterAnimation = false;
    public bool loopAnimation = false;

    public Vector3 hitboxSize = new Vector3(1f, 1f, 0.2f);
    public Vector3 hitboxOffset = new Vector3(0f, 0f, 0f);

    public Vector2 spriteScale = new Vector2(1f, 1f);
    public Vector3 spriteOffset = new Vector3(0f, 0f, 0f);
    public int spriteRotationOffset = 0;

    public float damage = 10f;
    public bool canHitPlayer = false;
    public bool canHitEnemy = false;
    public bool canHitTerrain = false;
    public bool canHitAttack = false;

    public float startingVelocity = 0f;
    public float terminalVelocity = -1f;
    public float acceleration = 0f;
    public float gravity = 0f;

    public float timeBeforeDestroy = 0f;

    public bool isDestroyedOnHit = false;
    public bool isRotationLocked = false;
    public bool isRotationFlipped = false;
    public bool isProjectile = false;

}
