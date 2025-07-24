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

        // Optional: destroy after a while if needed
        Destroy(gameObject, 10f);
    }

}


