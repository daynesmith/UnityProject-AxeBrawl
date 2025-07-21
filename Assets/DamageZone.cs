using UnityEngine;
using Mirror;

public class DamageZone : MonoBehaviour
{
    public int damageAmount = 10;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        NetworkIdentity identity = other.GetComponent<NetworkIdentity>();
        if (identity != null && identity.isServer)
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.ApplyDamage(damageAmount); // Server-only method
            }
        }
    }
}