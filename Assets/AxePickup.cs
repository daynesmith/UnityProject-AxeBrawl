using UnityEngine;
using Mirror;

public class AxePickup : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Movement player = other.GetComponent<Movement>();
        if (player != null && player.isLocalPlayer)
        {
            player.CmdTryPickupAxe(netIdentity);
        }
    }
}
