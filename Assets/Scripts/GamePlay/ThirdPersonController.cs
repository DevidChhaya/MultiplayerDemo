using Fusion;
using TMPro;
using UnityEngine;

public class ThirdPersonController : NetworkBehaviour
{
    [Tooltip("Speed ​​at which the character moves. It is not affected by gravity or jumping.")]
    public float velocity = 5f;
    [Tooltip("This value is added to the speed value while the character is sprinting.")]
    public float sprintAdittion = 3.5f;
    [Tooltip("The higher the value, the higher the character will jump.")]
    public float jumpForce = 18f;
    [Tooltip("Stay in the air. The higher the value, the longer the character floats before falling.")]
    public float jumpTime = 0.85f;
    [Space]
    [Tooltip("Force that pulls the player down. Changing this value causes all movement, jumping and falling to be changed as well.")]
    public float gravity = 9.8f;


    [Space]
    [SerializeField] private TMP_Text playerName;

    [Networked] // Syncs the player name across the network.
    public string PlayerName { get; set; }

    private float jumpElapsedTime = 0;
    private bool isJumping = false;
    private bool isSprinting = false;
    private bool isCrouching = false;
    private float inputHorizontal;
    private float inputVertical;
    private bool inputJump;
    private bool inputCrouch;
    private bool inputSprint;

    private Animator animator;
    private CharacterController cc;

    [Networked]
    private Vector3 NetworkedPosition { get; set; }

    [Networked]
    private Quaternion NetworkedRotation { get; set; }

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            CameraController cameraObject = FindAnyObjectByType<CameraController>();
            cameraObject.player = transform;
            cameraObject.Init();

            PlayerName = PlayerPrefs.GetString("UserName");
        }

        UpdateNameDisplay();

        cc = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        if (animator == null)
            Debug.LogWarning("Hey buddy, you don't have the Animator component in your player. Without it, the animations won't work.");
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            // Input checkers
            inputHorizontal = data.Horizontal;
            inputVertical = data.Vertical;
            inputJump = data.Jump;
            inputSprint = data.Sprint;
            inputCrouch = data.Crouch;

            if (inputCrouch)
                isCrouching = !isCrouching;

            if (cc.isGrounded && animator != null)
            {
                animator.SetBool("crouch", isCrouching);

                float minimumSpeed = 0.9f;
                animator.SetBool("run", cc.velocity.magnitude > minimumSpeed);

                isSprinting = cc.velocity.magnitude > minimumSpeed && inputSprint;
                animator.SetBool("sprint", isSprinting);
            }

            if (animator != null)
                animator.SetBool("air", !cc.isGrounded);

            if (inputJump && cc.isGrounded)
            {
                isJumping = true;
            }

            HeadHittingDetect();
        }

        float velocityAdittion = 0;
        if (isSprinting)
            velocityAdittion = sprintAdittion;
        if (isCrouching)
            velocityAdittion = -(velocity * 0.50f);

        float directionX = inputHorizontal * (velocity + velocityAdittion) * Runner.DeltaTime;
        float directionZ = inputVertical * (velocity + velocityAdittion) * Runner.DeltaTime;
        float directionY = 0;

        if (isJumping)
        {
            directionY = Mathf.SmoothStep(jumpForce, jumpForce * 0.30f, jumpElapsedTime / jumpTime) * Runner.DeltaTime;
            jumpElapsedTime += Runner.DeltaTime;
            if (jumpElapsedTime >= jumpTime)
            {
                isJumping = false;
                jumpElapsedTime = 0;
            }
        }

        directionY = directionY - gravity * Runner.DeltaTime;

        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        forward = forward * directionZ;
        right = right * directionX;

        if (directionX != 0 || directionZ != 0)
        {
            float angle = Mathf.Atan2(forward.x + right.x, forward.z + right.z) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 0.15f);
        }

        Vector3 verticalDirection = Vector3.up * directionY;
        Vector3 horizontalDirection = forward + right;
        Vector3 movement = verticalDirection + horizontalDirection;
        cc.Move(movement);

        if (Object.HasStateAuthority)
        {
            // Synchronize player position and rotation
            NetworkedPosition = transform.position;
            NetworkedRotation = transform.rotation;
        }
        else
        {
            // Update position and rotation for other players
            transform.position = NetworkedPosition;
            transform.rotation = NetworkedRotation;
        }
    }

    private void HeadHittingDetect()
    {
        float headHitDistance = 1.1f;
        Vector3 ccCenter = transform.TransformPoint(cc.center);
        float hitCalc = cc.height / 2f * headHitDistance;

        if (Physics.Raycast(ccCenter, Vector3.up, hitCalc))
        {
            jumpElapsedTime = 0;
            isJumping = false;
        }
    }

    private void UpdateNameDisplay()
    {
        if (playerName != null)
        {
            playerName.text = PlayerName;
        }
    }
}
