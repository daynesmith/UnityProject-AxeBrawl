using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkingSpeed = 7.5f;
    [SerializeField] private float runningSpeed = 11.5f;
    [SerializeField] private float jumpHeight = 8.0f;
    [SerializeField] private float gravity = 20.0f;
    public Camera playerCamera;

    [Header("Grounding")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private Transform groundedCheck;
    [SerializeField] private float groundedDistance = 2;

    [Header("Camera")]
    [SerializeField] private Transform camHolder;
    [SerializeField] private float lookSpeed = 2.0f;

    public float maxHeadPitch = 30f;          // Max up/down head rotation
    public float headTurnFollowSpeed = 2.5f;    // Smoothing for head turning
    public float maxHeadYaw = 145f;            // Max left/right head rotation from forward

    private float cameraPitch = 0f;           // Vertical camera rotation (X-axis)
    private float playerYaw = 0f;             // Accumulated yaw from mouse (Y-axis)

    private float headYawOffset = 0f;
    public Transform headTransform;
    [SyncVar(hook = nameof(OnHeadPitchChanged))] private float syncedHeadPitch;
    [SyncVar(hook = nameof(OnHeadYawChanged))] private float syncedHeadYaw;
    float targetPitch;
    float targetYaw;

    Vector2 inputDir;
    [SerializeField] NetworkAnimator networkAnimator;

    Vector3 lastPosition;
    Animator animator;

    

    [Header("AxeStuff")]
    [SerializeField] private GameObject heldAxeModel;
    [SerializeField] private GameObject thrownAxePrefab;
    [SerializeField] private Transform throwPoint;
    [SerializeField] private float throwForce = 20f;
    [SyncVar(hook = nameof(OnHasAxeChanged))]
    public bool hasAxe;

    [Header("Punch")]
    public float punchCooldown = 1f; // cooldown time in seconds
    private float nextPunchTime = 0f;

    public CharacterController characterController { get; private set; }
    public PlayerHealth characterHealth { get; private set; }
    public bool isGrounded { get; private set; }

    Vector3 velocity;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        characterHealth = GetComponent<PlayerHealth>();
        lastPosition = transform.position;
        if (networkAnimator == null)
        {
            networkAnimator = GetComponent<NetworkAnimator>();
        }

        if (animator == null) {
            Debug.LogError("animator not found in" + gameObject.name);
        }
        
    }


    void Update()
    {
        if (!isLocalPlayer) return;
        float distance = Vector3.Distance(transform.position, lastPosition);
        float moveSpeed = distance / Time.deltaTime;
        lastPosition = transform.position;
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool isMoving = horizontal != 0 || vertical != 0;
        bool isRunningAnim = isMoving && Input.GetKey(KeyCode.LeftShift);

        // Set animator parameters
        animator.SetBool("isWalking", isMoving && !isRunningAnim);
        animator.SetBool("isRunning", isRunningAnim);
        animator.SetBool("hasAxe", hasAxe);

        if (Input.GetKeyDown(KeyCode.Mouse0) && Time.time >= nextPunchTime && hasAxe == false)
        {
            networkAnimator.SetTrigger("punch");
            nextPunchTime = Time.time + punchCooldown;
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            characterHealth.CmdTakeDamage(100);
        }
        if (Input.GetKeyDown(KeyCode.Mouse0)&& Time.time >= nextPunchTime && hasAxe ==true)
        {
            networkAnimator.SetTrigger("throwAxe");
            nextPunchTime = Time.time + punchCooldown;

        }


        animator.SetFloat("speed", moveSpeed);
        CheckIfGrounded();
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        MovementHandler(isRunning);
    }

    private void MovementHandler(bool isRunning)
    {

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;
        if (!(FindObjectOfType<PauseMenu>()?.IsPaused() ?? false))
        {
            Vector3 moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
            Vector3 fixedMoveDir;

            moveDir *= (isRunning ? runningSpeed : walkingSpeed);
            moveDir = transform.TransformDirection(moveDir);

            if (Input.GetButtonDown("Jump") && isGrounded)
                velocity.y += Mathf.Sqrt(jumpHeight * -3.0f * -gravity);

            velocity.y -= gravity * Time.deltaTime;

            fixedMoveDir = moveDir;
            fixedMoveDir.y = velocity.y;

            characterController.Move(fixedMoveDir * Time.deltaTime);
        }
        else {
            Vector3 moveDir = new Vector3(0, 0, 0).normalized;
            Vector3 fixedMoveDir;

            moveDir *= (isRunning ? runningSpeed : walkingSpeed);
            moveDir = transform.TransformDirection(moveDir);

            velocity.y -= gravity * Time.deltaTime;

            fixedMoveDir = moveDir;
            fixedMoveDir.y = velocity.y;

            characterController.Move(fixedMoveDir * Time.deltaTime);
        }
    }

    public void CheckIfGrounded()
    {
        isGrounded = Physics.Raycast(groundedCheck.position, Vector3.down, groundedDistance, groundMask);
    }


    private void LateUpdate()
    {
        if (!isLocalPlayer) return;
        if (FindObjectOfType<PauseMenu>()?.IsPaused() ?? false)
            return;

        Vector2 inputDir = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

        // Rotate player on Y
        playerYaw += inputDir.x * lookSpeed;
        transform.rotation = Quaternion.Euler(0f, playerYaw, 0f);

        // Rotate camera up/down
        cameraPitch -= inputDir.y * lookSpeed;
        cameraPitch = Mathf.Clamp(cameraPitch, -89f, 89f); // Camera can go full up/down
        camHolder.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);

        // --- HEAD ROTATION ---
        // Clamp pitch for head only (not full camera pitch)
        float clampedHeadPitch = Mathf.Clamp(cameraPitch, -maxHeadPitch, maxHeadPitch);

        // Smoothly rotate head Y toward mouse movement (not same as camera yaw)
        float targetHeadYaw = Mathf.Clamp(inputDir.x * maxHeadYaw, -maxHeadYaw, maxHeadYaw);
        headYawOffset = Mathf.Lerp(headYawOffset, targetHeadYaw, Time.deltaTime * headTurnFollowSpeed);

        // Apply head rotation (local to body)
        headTransform.localRotation = Quaternion.Euler(clampedHeadPitch, headYawOffset, 0f);

        CmdSyncHeadRotation(clampedHeadPitch, headYawOffset);
    }

    private void OnDrawGizmos()
    {
        if (groundedCheck)
            Gizmos.DrawRay(groundedCheck.position, Vector3.down * groundedDistance);
    }

    public void ResetState()
    {
        velocity = Vector3.zero;
        inputDir = Vector2.zero;
    }

    void OnHeadPitchChanged(float oldVal, float newVal)
    {
        if (isLocalPlayer) return;

        UpdateHeadRotation(newVal, syncedHeadYaw);
    }

    void OnHeadYawChanged(float oldVal, float newVal)
    {
        if (isLocalPlayer) return;
        UpdateHeadRotation(syncedHeadPitch, newVal);
    }

    void UpdateHeadRotation(float pitch, float yaw)
    {
        headTransform.localRotation = Quaternion.Euler(pitch, yaw, 0);
    }

    [Command]
    void CmdSyncHeadRotation(float pitch, float yaw)
    {
        syncedHeadPitch = pitch;
        syncedHeadYaw = yaw;
    }

    [Command]
    public void CmdTryPickupAxe(NetworkIdentity axeNetIdentity)
    {
        if (hasAxe) return; // Optional: don't double-pickup

        GameObject axeObj = axeNetIdentity.gameObject;

        if (axeObj != null)
        {
            hasAxe = true;
            RpcShowHeldAxe(true);
            // You can do anything else here — e.g., spawn axe in hand
            NetworkServer.Destroy(axeObj); // Destroy on server; synced to all clients
        }
    }


    public void OnAxeThrowAnimationEvent()
    {
        if (!isLocalPlayer) return;// Only allow local player to throw
        Vector3 throwDirection = playerCamera.transform.forward;
        hasAxe = false;
        CmdThrowAxe(throwDirection);
    }

    [Command]
    void CmdThrowAxe(Vector3 throwDirection)
    {
        Quaternion throwRotation = Quaternion.LookRotation(throwDirection) * Quaternion.Euler(0, 90, 0);
        GameObject axe = Instantiate(thrownAxePrefab, throwPoint.position, throwRotation);

        Rigidbody rb = axe.GetComponent<Rigidbody>();
        Vector3 velocity = throwDirection * throwForce;
        Vector3 angularVelocity = axe.transform.forward * 20f;

        // Set velocity on the server (host)
        rb.velocity = velocity;
        rb.angularVelocity = angularVelocity;

        NetworkServer.Spawn(axe, connectionToClient); // Give client authority if needed

        // Tell all clients (including this one) to set velocity on their axe instance
        axe.GetComponent<ThrownAxe>().RpcSetVelocity(velocity, angularVelocity);

        hasAxe = false;
        RpcShowHeldAxe(false);
    }


    void OnHasAxeChanged(bool oldValue, bool newValue)
    {
        animator.SetBool("hasAxe", newValue);
    }

    [ClientRpc]
    void RpcShowHeldAxe(bool value)
    {
        heldAxeModel.SetActive(value);
    }
}
