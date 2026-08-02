using System.Collections;
using UnityEngine;

/// <summary>
/// Boss state for a teleport slam attack.
/// The boss teleports to random points, then slams down to create shockwaves.
/// </summary>
public class FW_S_TeleportSlam : EnemyState<FrogWizard>
{
    private const int TeleportCount = 3;
    private const float TeleportXRange = 10f;
    private const float TeleportYMin = 10f;
    private const float TeleportYMax = 15f;
    private const float TeleportZRange = 5f;
    private const float TeleportPauseDuration = 0.5f;
    private const float HardmodeTeleportPauseDuration = 0.25f;

    private const float ManaCost = 15f;
    private const float PhaseThreeSpeedMultiplier = 1.6f;

    private const float SlamDuration = 1f;
    private const float SlamEasingPower = 5f;
    private const float SlamGroundY = 1.5f;

    private const float PostSlamIdleDuration = 0.6f;
    private const float HardmodePostSlamIdleDuration = 0.2f;

    public override IEnumerator StateCoroutine()
    {
        float speedMultiplier = Controller.phase > 2 ? PhaseThreeSpeedMultiplier : 1f;

        Controller.PS_Teleport.Play();
        yield return Controller.Idle(1f);

        Controller.stats.ChangeMana(-ManaCost);

        float teleportPause = (Controller.isHardmode ? HardmodeTeleportPauseDuration : TeleportPauseDuration) / speedMultiplier;
        for (int i = 0; i < TeleportCount; i++)
        {
            transform.position = RandomTeleportPosition();
            FrogWizardAudioManager.Instance.FrogWizardTeleport.Post(gameObject);
            yield return Controller.Idle(teleportPause);
        }
        transform.position = RandomTeleportPosition();
        Controller.PS_Teleport.Stop();

        Controller.animator.SetTrigger("isEarthquake");
        Controller.animator.speed = 0.75f * speedMultiplier;
        yield return Controller.Idle(0.25f);

        yield return SlamToGround(speedMultiplier);

        AttackData attackData = GetSlamAttackData();
        FrogWizardAudioManager.Instance.FrogWizardEarthSlam.Post(gameObject);
        Controller.SpawnAttack(attackData, transform.position, new Vector2(1f, 0));
        Controller.SpawnAttack(attackData, transform.position, new Vector2(-1f, 0));

        yield return Controller.Idle(Controller.isHardmode ? HardmodePostSlamIdleDuration : PostSlamIdleDuration);
        Controller.EnterState<FW_S_Idle>();
    }

    // Stops the teleport particle effect when the state ends.
    public override void EndState()
    {
        Controller.PS_Teleport.Stop();
    }

    private Vector3 RandomTeleportPosition()
    {
        Vector3 position = new Vector3(
            Random.Range(-TeleportXRange, TeleportXRange),
            Random.Range(TeleportYMin, TeleportYMax),
            Random.Range(-TeleportZRange, TeleportZRange));
        position.x = Mathf.Clamp(position.x, Controller.mapBoundaryLeft, Controller.mapBoundaryRight);
        return position;
    }

    private IEnumerator SlamToGround(float speedMultiplier)
    {
        Vector3 startPosition = transform.position;
        Vector3 endPosition = new Vector3(transform.position.x, SlamGroundY, transform.position.z);
        endPosition.x = Mathf.Clamp(endPosition.x, Controller.mapBoundaryLeft, Controller.mapBoundaryRight);

        float elapsedTime = 0f;
        float moveDuration = SlamDuration / speedMultiplier;

        while (elapsedTime < moveDuration)
        {
            float t = Mathf.Pow(elapsedTime / moveDuration, SlamEasingPower);
            transform.position = Vector3.Lerp(startPosition, endPosition, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = endPosition;
    }

    // Selects slam attack data based on current phase and difficulty.
    private AttackData GetSlamAttackData()
    {
        AttackData[] normalByPhase = { Controller.attackDataEarthquakeHopP1, Controller.attackDataEarthquakeHopP2, Controller.attackDataEarthquakeHopP3 };
        AttackData[] hardmodeByPhase = { Controller.attackDataEarthquakeHopHardmodeP1, Controller.attackDataEarthquakeHopHardmodeP2, Controller.attackDataEarthquakeHopHardmodeP3 };

        int phaseIndex = Mathf.Clamp(Controller.phase - 1, 0, normalByPhase.Length - 1);
        return (Controller.isHardmode ? hardmodeByPhase : normalByPhase)[phaseIndex];
    }
}
