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
    [SerializeField] private float lookXLimit = 80f;
    float rotationX = 0;
    Vector2 inputDir;
    [SerializeField] NetworkAnimator networkAnimator;

    Vector3 lastPosition;
    Animator animator;

    [SyncVar(hook = nameof(OnHasAxeChanged))]
    public bool hasAxe;

    [Header("AxeStuff")]
    [SerializeField] private GameObject heldAxeModel;


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

        inputDir = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        rotationX += -inputDir.y * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        camHolder.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, inputDir.x * lookSpeed, 0);
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
