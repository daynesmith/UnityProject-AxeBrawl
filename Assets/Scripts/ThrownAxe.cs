using UnityEngine;
using Mirror;

public class ThrownAxe : NetworkBehaviour
{
    private Rigidbody rb;

    private bool hasStuck = false;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    [ClientRpc]
    public void RpcSetVelocity(Vector3 velocity, Vector3 angularVelocity)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();

        rb.velocity = velocity;
        rb.angularVelocity = angularVelocity;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasStuck) return; // Only stick once
        hasStuck = true;

        // Stop physics
        Rigidbody rb = GetComponent<Rigidbody>();
        
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        // Stick to what was hit
        transform.parent = collision.transform;

        if (isServer) // Ensure this runs only on the server
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                PlayerHealth player = collision.gameObject.GetComponent<PlayerHealth>();
                if (player != null)
                {
                    player.ApplyDamage(100); // Or however much damage you want
                    Destroy(gameObject, 3f);
                }
            }
        }


        // Optional: destroy after a while if needed
        Destroy(gameObject, 10f);
    }

}


