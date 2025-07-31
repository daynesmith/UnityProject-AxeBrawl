using UnityEngine;
using Mirror;

public class PlayerInteraction : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!isLocalPlayer) return; // Only let the local client do this

        NetworkDoor door = other.GetComponent<NetworkDoor>();
        if (door != null)
        {
            CmdRequestDoorOpen(door.netIdentity);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isLocalPlayer) return;

        NetworkDoor door = other.GetComponent<NetworkDoor>();
        if (door != null)
        {
            CmdNotifyDoorExit(door.netIdentity);
        }
    }

    [Command]
    void CmdRequestDoorOpen(NetworkIdentity doorIdentity)
    {
        NetworkDoor door = doorIdentity.GetComponent<NetworkDoor>();
        if (door != null)
        {
            door.PlayerEntered(transform.position);
        }
    }

    [Command]
    void CmdNotifyDoorExit(NetworkIdentity doorIdentity)
    {
        NetworkDoor door = doorIdentity.GetComponent<NetworkDoor>();
        if (door != null)
            door.PlayerExited();
    }
}
