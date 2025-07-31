using UnityEngine;
using Mirror;
using System.Collections;

public class NetworkDoor : NetworkBehaviour
{
    public Transform doorTransform;
    public float openAngle = 90f;
    public float openSpeed = 2f;

    private Quaternion closedRotation;
    private Quaternion openedRotationPositive;
    private Quaternion openedRotationNegative;

    private int playerCount = 0;
    private bool isOpen = false;

    void Start()
    {
        closedRotation = doorTransform.rotation;
        openedRotationPositive = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
        openedRotationNegative = closedRotation * Quaternion.Euler(0f, -openAngle, 0f);
    }

    [Server]
    public void PlayerEntered(Vector3 playerPos)
    {
        playerCount++;
        Debug.Log($"Player entered door area. Current count: {playerCount}");
        if (!isOpen)
        {
            isOpen = true;
            Vector3 localPlayerPos = transform.InverseTransformPoint(playerPos);
            Quaternion targetRotation = localPlayerPos.z < 0 ? openedRotationPositive : openedRotationNegative;

            RpcOpenDoor(targetRotation);
        }
    }

    [Server]
    public void PlayerExited()
    {
        playerCount = Mathf.Max(0, playerCount - 1);
        Debug.Log($"Player exited door area. Current count: {playerCount}");
        if (playerCount == 0 && isOpen)
        {
            isOpen = false;
            RpcCloseDoor();
        }
    }


    [ClientRpc]
    void RpcOpenDoor(Quaternion targetRotation)
    {
        StopAllCoroutines();
        StartCoroutine(RotateDoor(targetRotation));
    }

    [ClientRpc]
    void RpcCloseDoor()
    {
        StopAllCoroutines();
        StartCoroutine(RotateDoor(closedRotation));
    }

    IEnumerator RotateDoor(Quaternion targetRot)
    {
        while (Quaternion.Angle(doorTransform.rotation, targetRot) > 0.01f)
        {
            doorTransform.rotation = Quaternion.Slerp(doorTransform.rotation, targetRot, Time.deltaTime * openSpeed);
            yield return null;
        }
        doorTransform.rotation = targetRot;
    }
}
