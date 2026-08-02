using System.Collections;
using UnityEngine;

/// <summary>
/// Boss state for a fireball jump and leap attack.
/// The boss hops toward or away from the player, firing fireballs in midair.
/// </summary>
public class FW_S_FireballHop : EnemyState<FrogWizard>
{
    private const float PhaseSpeedMultiplier = 1.4f;

    private const float HopJumpHeight = 1f;
    private const float HopJumpDuration = 0.33f;

    // Animator speed by phase, fastest in phase 3.
    private static readonly float[] AnimatorSpeedByPhase = { 1.1f, 1.8f, 2f };
    private const float PreLeapIdleDuration = 0.2f;

    private const float LeapDistance = 10f;
    private const float ShortHopDistanceMultiplier = 0.4f;
    private const float GroundY = 1.5f;
    private const float LeapZOffset = 5f;
    private const float LeapDuration = 0.5f;
    private const float LeapArcHeight = 4f;
    private const float PostLeapIdleDuration = 1f;

    private const float FireballManaCost = 5f;
    private const float FireballTimingPhase2 = 0.5f;
    private const float FireballTimingDefault = 0.95f;

    private bool isShortHop = false;

    public override IEnumerator StateCoroutine()
    {
        float speedMultiplier = Controller.phase > 1 ? PhaseSpeedMultiplier : 1f;

        // Phase 2 forces a short hop
        // phase 3 alternates short/long each cast.
        if (Controller.phase == 2)
        {
            isShortHop = true;
        }

        yield return null;
        yield return Controller.StateJump(Controller.transform.position, HopJumpHeight, HopJumpDuration / speedMultiplier);
        yield return Controller.StateJump(Controller.transform.position, HopJumpHeight, HopJumpDuration / speedMultiplier);

        Controller.animator.SetTrigger("isFireStaff");
        Controller.animator.speed = AnimatorSpeedByPhase[Mathf.Clamp(Controller.phase - 1, 0, AnimatorSpeedByPhase.Length - 1)];
        yield return Controller.Idle(PreLeapIdleDuration);

        Vector3 startPosition = transform.position;
        Vector3 direction = Random.value > 0.5f ? Controller.GetDirectionToPlayer() : Controller.GetDirectionFromPlayer();

        float jumpDistance = direction.x * LeapDistance * (isShortHop ? ShortHopDistanceMultiplier : 1f);
        Vector3 endPosition = new Vector3(transform.position.x + jumpDistance, GroundY, transform.position.z + direction.z * LeapZOffset);
        endPosition.x = Mathf.Clamp(endPosition.x, Controller.mapBoundaryLeft, Controller.mapBoundaryRight);

        bool hasAttacked = false;
        float fireballTime = Controller.phase == 2 ? FireballTimingPhase2 : FireballTimingDefault;

        if (Controller.phase == 3)
        {
            SpawnFireball();
        }

        float elapsedTime = 0f;
        while (elapsedTime < LeapDuration)
        {
            float t = elapsedTime / LeapDuration;
            Vector3 midPosition = Vector3.Lerp(startPosition, endPosition, t);
            midPosition.y = GroundY + Mathf.Sin(t * Mathf.PI) * LeapArcHeight;

            transform.position = midPosition;

            elapsedTime += Time.deltaTime;

            if (!hasAttacked && t >= fireballTime)
            {
                SpawnFireball();
                hasAttacked = true;
            }

            yield return null;
        }

        transform.position = endPosition;

        yield return Controller.Idle(PostLeapIdleDuration);

        if (Controller.phase == 3)
        {
            isShortHop = !isShortHop;
        }

        Controller.EnterState<FW_S_Idle>();
    }

    private void SpawnFireball()
    {
        Controller.stats.ChangeMana(-FireballManaCost);
        FrogWizardAudioManager.Instance.FrogWizardFireball.Post(gameObject);
        Controller.SpawnFireballTowardsPlayer();
    }
}
