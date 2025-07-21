using Mirror;
using UnityEngine;
using System.Collections;

public class PlayerHealth : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHealthChanged))]
    public int currentHealth = 100;

    public int maxHealth = 100;
    public float respawnDelay = 3f;

    private HealthTextUI healthTextUI;

    public override void OnStartLocalPlayer()
    {
        SetupLocalUI();
    }

    void OnHealthChanged(int oldHealth, int newHealth)
    {
        Debug.Log($"Health changed on {gameObject.name}: {oldHealth} → {newHealth}");

        if (isLocalPlayer && healthTextUI != null)
        {
            healthTextUI.SetHealth(newHealth);
        }

        if (newHealth <= 0)
        {
            HandleDeath();
        }
    }

    [Server] // Server-only method
    public void ApplyDamage(int amount)
    {
        if (currentHealth <= 0) return;

        currentHealth = Mathf.Max(currentHealth - amount, 0);

        if (currentHealth == 0)
        {
            StartCoroutine(RespawnCoroutine());
        }
    }

    [Command]
    public void CmdTakeDamage(int amount)
    {
        ApplyDamage(amount); // this runs on the server
    }

    [Server]
    private IEnumerator RespawnCoroutine()
    {
        RpcHandleDeath(); // Hide the player on clients
        yield return new WaitForSeconds(respawnDelay);
        Transform spawnPoint = SpawnManager.instance.GetRandomSpawnPoint();
        Debug.Log(spawnPoint.position);
        // Move and reset health
        
        currentHealth = maxHealth;
        
        TargetTeleport(connectionToClient, spawnPoint.position, spawnPoint.rotation);

        RpcRespawn(); // Show the player on clients

    }

    [TargetRpc]
    private void TargetTeleport(NetworkConnection target, Vector3 pos, Quaternion rot)
    {
        // Disable movement before teleport
        GetComponent<Movement>().enabled = false;

        // Teleport
        transform.position = pos;
        transform.rotation = rot;

        // Optional: reset velocity/input if needed
        var move = GetComponent<Movement>();
        if (move != null)
            move.ResetState(); // You'll define this if needed

        
    }


    [ClientRpc]
    private void RpcHandleDeath()
    {
        // Disable visuals/controls on all clients
        if (isLocalPlayer)
        {
            GetComponent<Movement>().enabled = false;
        }

        // Hide mesh/graphics
        GetComponentInChildren<Renderer>().enabled = false;
    }

    [ClientRpc]
    private void RpcRespawn()
    {
        GetComponentInChildren<Renderer>().enabled = true;
        SetupLocalUI(); // <-- this ensures healthTextUI is reassigned after respawn
        StartCoroutine(EnableMovementDelayed());
    }

    private void SetupLocalUI()
    {
        if (isLocalPlayer)
        {
            healthTextUI = FindObjectOfType<HealthTextUI>();
            if (healthTextUI != null)
                healthTextUI.SetHealth(currentHealth);
        }
    }

    private IEnumerator EnableMovementDelayed()
    {
        yield return new WaitForSeconds(0.1f); // small delay lets NetworkTransform sync catch up
        if (isLocalPlayer)
        {
            GetComponent<Movement>().enabled = true;
        }
    }

    private void HandleDeath()
    {
        // Local death handling if needed
        // e.g. play death animation, sound, etc.
    }
    
}


    