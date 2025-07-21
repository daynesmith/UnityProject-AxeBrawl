using Mirror;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHealthChanged))]
    public int currentHealth = 100;

    private HealthTextUI healthTextUI;

    public override void OnStartLocalPlayer()
    {
        healthTextUI = FindObjectOfType<HealthTextUI>();
        if (healthTextUI != null)
            healthTextUI.SetHealth(currentHealth);
    }

    void OnHealthChanged(int oldHealth, int newHealth)
    {
        if (isLocalPlayer && healthTextUI != null)
        {
            healthTextUI.SetHealth(newHealth);
        }
    }

    [Server] // Server-only method
    public void ApplyDamage(int amount)
    {
        currentHealth = Mathf.Max(currentHealth - amount, 0);
    }
}


