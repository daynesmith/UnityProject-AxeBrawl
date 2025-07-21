using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour
{
    public float punchRange = 1.5f;
    public int punchDamage = 25;
    public Transform punchOrigin;

    // Called locally when you press the punch key
    public void TryPunch()
    {
        if (!isLocalPlayer) return;
        CmdPerformPunch();
    }

    // Run punch logic on server
    [Command]
    void CmdPerformPunch()
    {
        Collider[] hits = Physics.OverlapSphere(punchOrigin.position, punchRange);

        HashSet<GameObject> hitPlayers = new HashSet<GameObject>();

        foreach (Collider hit in hits)
        {
            // Only process capsule colliders to avoid double hits
            if (!(hit is CapsuleCollider)) continue;

            if (hit.CompareTag("Player"))
            {
                GameObject playerObj = hit.gameObject;

                // Walk up the hierarchy if needed (optional)
                while (playerObj.transform.parent != null && !playerObj.CompareTag("Player"))
                    playerObj = playerObj.transform.parent.gameObject;

                if (!hitPlayers.Contains(playerObj))
                {
                    PlayerHealth targetHealth = playerObj.GetComponent<PlayerHealth>();

                    if (targetHealth != null && targetHealth != GetComponent<PlayerHealth>())
                    {
                        targetHealth.ApplyDamage(punchDamage);
                        hitPlayers.Add(playerObj);
                    }
                }
            }
        }
    }
}
