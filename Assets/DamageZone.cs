using UnityEngine;

public class DamageZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        var health = other.GetComponent<PlayerHealth>();
        if (health != null && health.isLocalPlayer)
        {
            // Local player entered — request the server to damage them
            health.CmdTakeDamage(25);
        }
    }
}