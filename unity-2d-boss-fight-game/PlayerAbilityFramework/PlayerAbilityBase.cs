using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for player abilities.
/// Manages cooldowns and use conditions.
/// </summary>
public abstract class PlayerAbilityBase : MonoBehaviour
{
    protected PlayerController playerController;
    protected AttackData attackData;

    // Ability settings.
    public float manaCost = 0f;
    public float cooldown = 0f;
    public TimeSpan timeLastUse;

    public string hotkeyString = "";
    protected KeyCode? hotkeyKeycode = null;

    public event Action AbilityUsed;
    public event Action CooldownReset;

    private void Start()
    {
        timeLastUse = TimeSpan.FromSeconds(Time.time - cooldown);
        playerController = GetComponent<PlayerController>();
        
        OnStart();
    }

    public void ResetCoolDown(float cooldownToSet = 0f)
    {
        float newCooldown = Mathf.Clamp(cooldownToSet, 0f, cooldown);
        timeLastUse = TimeSpan.FromSeconds(Time.time - (cooldown - newCooldown));
        CooldownReset?.Invoke();
    }

    public void SetCoolDown(float cooldownToSet = 0f)
    {
        timeLastUse = TimeSpan.FromSeconds(Time.time - cooldown + cooldownToSet);
        CooldownReset?.Invoke();
    }

    protected void LoadAttackData(string name)
    {
        attackData = Resources.Load<AttackData>($"AttackData/{name}");
        if (attackData == null)
        {
            Debug.LogError("LoadAttackData " + name + " failed for " + gameObject);
        }
    }

    /// <summary>
    /// Checks if the ability can be used based on player states.
    /// </summary>
    protected virtual bool IsAbilityUsable()
    {
        if (Time.timeScale <= 0f)
        {
            return false;
        }

        if (playerController.disableInputCounter > 0)
        {
            return false;
        }

        if (playerController.disableAbilityCounter > 0)
        {
            return false;
        }

        if ((TimeSpan.FromSeconds(Time.time) - timeLastUse).TotalSeconds < cooldown)
        {
            return false;
        }

        if (playerController.stats.mana < manaCost)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Evaluates ability trigger input.
    /// </summary>
    public void AbilityInput()
    {
        if (!enabled)
        {
            return;
        }

        if (IsAbilityUsable()) 
        {
            OnAbilityUsed();
            UseAbility();
        }
    }

    /// <summary>
    /// Consumes resources and triggers callbacks when used.
    /// </summary>
    protected void OnAbilityUsed()
    {
        timeLastUse = TimeSpan.FromSeconds(Time.time);
        if (manaCost > 0)
        {
            playerController.stats.ChangeMana(-manaCost);
        }
        AbilityUsed?.Invoke();
    }

    /// <summary>
    /// Called when the ability is initialized.
    /// </summary>
    protected abstract void OnStart();

    /// <summary>
    /// Triggers the ability activation logic.
    /// </summary>
    public abstract void UseAbility();

    /// <summary>
    /// Runs the asynchronous execution logic.
    /// </summary>
    protected abstract IEnumerator StateCoroutine();

    /// <summary>
    /// Cleans up ability state when stopped or interrupted.
    /// </summary>
    public abstract void EndAbility();
}
