using UnityEngine;
using Mirror;
using System.Collections;

public class AxePickup : NetworkBehaviour
{
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Collider[] colliders;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Movement player = other.GetComponent<Movement>();
        if (player != null && player.isLocalPlayer)
        {
            player.CmdTryPickupAxe(netIdentity);
        }
    }

    [Server]
    public void ScheduleRespawn()
    {
        Debug.Log($"Axe {netIdentity.netId} will respawn after 5 seconds.");
        StartCoroutine(RespawnAfterDelay(10f));
    }

    [Server]
    private IEnumerator RespawnAfterDelay(float delay)
    {
        Debug.Log($"De-activating axe {netIdentity.netId} for {delay} seconds.");
        RpcSetVisible(false);
        yield return new WaitForSeconds(delay);
        Debug.Log($"Re-activating axe {netIdentity.netId}.");
        RpcSetVisible(true);
    }

    [ClientRpc]
    private void RpcSetVisible(bool visible)
    {
        foreach (var r in renderers)
            r.enabled = visible;

        foreach (var c in colliders)
            c.enabled = visible;
    }
}
