using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boss state for a spectral blade fan attack.
/// Spawns a fan of blades that spin up and then fire at high velocity.
/// </summary>
public class MF_S_BladeFan : EnemyState<MysteriousFigure>
{
    // (angle between blades, blade count) presets, picked at random.
    private static readonly (float offsetAngle, int projectileCount)[] Patterns =
    {
        (45f, 5),
        (36f, 6),
        (30f, 7),
        (30f, 12),
        (24f, 15),
    };

    private const float PreCastDelay = 0.5f;
    private const float PostFireDelay = 0.5f;
    private const float MoveRightChance = 0.33f;
    private const float LaunchVelocity = 55f;

    public float swordOffset = 3.5f;
    public float shootDelay = 0.75f;

    private List<GameObject> swords = new();

    public override IEnumerator StateCoroutine()
    {
        (float swordOffsetAngle, int numberOfProjectile) = Patterns[Random.Range(0, Patterns.Length)];

        Controller.OrientBoss();
        Controller.animator.SetTrigger("Cast");
        Controller.animator.speed = 1f;

        yield return Controller.Idle(PreCastDelay);

        Vector3 direction = (Controller.player.transform.position - Controller.transform.position).normalized;
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        for (int i = 0; i < numberOfProjectile; i++)
        {
            float offset = swordOffsetAngle * (i - ((numberOfProjectile - 1) / 2f));
            float angle = baseAngle + offset;

            Vector3 attackDirection = Quaternion.Euler(0, 0, angle) * Vector3.right;
            Vector3 attackPosition = Controller.transform.position + attackDirection * swordOffset;

            GameObject attack = Controller.SpawnAttack(Controller.attackDataSpectralSword, attackPosition, attackDirection);
            GameObject mesh = Instantiate(Controller.attackPrefabSpectralSwordLine, attack.transform);
            attack.GetComponentInChildren<SwordSpinUp>().enabled = false;
            mesh.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            attack.GetComponent<BoxCollider>().enabled = false;
            swords.Add(attack);
        }

        yield return Controller.Idle(shootDelay);

        foreach (GameObject sword in swords)
        {
            sword.GetComponent<Attack>().SetVelocity(LaunchVelocity);
            sword.GetComponent<BoxCollider>().enabled = true;
        }
        swords.Clear();

        yield return Controller.Idle(PostFireDelay);

        if (Random.value < MoveRightChance)
        {
            Controller.EnterState<MF_S_MoveRight>();
        }
        else
        {
            Controller.EnterState<MF_S_Idle>();
        }
    }
}
