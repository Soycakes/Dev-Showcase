using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles the player dash ability.
/// </summary>
public class PlayerAbilityDash : PlayerAbilityBase
{
    public float dashDuration = 0.2f;
    public float dashSpeed = 20f;
    public float extraIframeDuration = 0.25f;

    // Velocity retained after the dash ends, so movement doesn't stop dead.
    private const float DashEndHorizontalDamping = 0.2f;
    private const float DashEndVerticalDamping = 0.1f;
    public Vector3 dashDirection;
    private Rigidbody rigidBody;

    private bool is3D = false;

    // Prevents duplicate cleanup calls if the dash is interrupted.
    private bool isDashActive;

    private static readonly WaitForFixedUpdate WaitForFixedUpdateInstance = new WaitForFixedUpdate();

    protected override void OnStart()
    {
        rigidBody = playerController.rigidBody;
    }

    public override void UseAbility()
    {
        playerController.animator.SetBool("DashInp", true);
        playerController.SetIframeDuration(dashDuration + extraIframeDuration);
        is3D = playerController.is3DMovementEnabled;

        PlayerAbilityHammerSwing hammerSwing = playerController.GetComponent<PlayerAbilityHammerSwing>();
        if (hammerSwing != null && hammerSwing.isAirSwingActive)
        {
            hammerSwing.EndAbility();
        }

        StartCoroutine(StateCoroutine());
    }

    protected override IEnumerator StateCoroutine()
    {
        isDashActive = true;
        playerController.disableInputCounter++;
        playerController.isDashing = true;

        rigidBody.useGravity = false;

        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(horizontalInput) < 0.1f && Math.Abs(verticalInput) < 0.1f)
        {
            horizontalInput = playerController.GetPlayerFacingDirection().x;
        }
        dashDirection = is3D ?
            new Vector3(horizontalInput, 0f, verticalInput).normalized :
            new Vector3(horizontalInput, verticalInput, 0f).normalized;

        PlayerAudioManager.Instance.PlayerDash.Post(gameObject);

        float dashTime = 0f;
        while (dashTime < dashDuration)
        {
            rigidBody.velocity = dashDirection * dashSpeed;
            // Advance dash time by fixed physics delta time.
            dashTime += Time.fixedDeltaTime;
            yield return WaitForFixedUpdateInstance;
        }

        rigidBody.velocity = new Vector3(
            rigidBody.velocity.x * DashEndHorizontalDamping,
            rigidBody.velocity.y * DashEndVerticalDamping
        );
        EndAbility();

        // Reset iframe duration.
        playerController.SetIframeDuration(extraIframeDuration);
    }

    public override void EndAbility()
    {
        if (!isDashActive)
        {
            return;
        }
        isDashActive = false;

        playerController.disableInputCounter--;
        playerController.isDashing = false;
        PlayerAbilityGlide glide = playerController.GetComponent<PlayerAbilityGlide>();
        if(glide == null || !glide.isGlideActive)
        {
            rigidBody.useGravity = true;
        }
        playerController.animator.SetBool("DashInp", false);
    }
}
