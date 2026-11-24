using UnityEngine;
using UnityEngine.InputSystem;

public class ViewmodelBob : MonoBehaviour
{
    [Header("Player Reference")]
    public PlayerMovement playerMovement;

    [Header("Bobbing Settings")]
    public float baseBobFrequency = 10f;
    public float horizontalAmount = 0.015f;
    public float verticalAmount = 0.012f;

    [Header("State Multipliers")]
    public float crouchBobMultiplier = 0.6f;
    public float sprintBobMultiplier = 1.5f;

    [Range(0f, 1f)]
    public float adsBobMultiplier = 0.2f;

    public float smoothSpeed = 10f;

    [Header("Jump View Bob")]
    public float jumpOffset = 0.08f;
    public float landingKick = 0.12f;
    public float jumpSmoothSpeed = 8f;
    public float landingRecoverySpeed = 5f;

    private CharacterController characterController;
    private float bobTimer;
    private float currentJumpOffset;
    private float currentLandingKick;
    private bool wasGrounded;
    private Vector3 startLocalPos;

    private void Start()
    {
        startLocalPos = transform.localPosition;

        if (playerMovement != null)
        {
            characterController =
                playerMovement.GetComponent<CharacterController>();
        }

        wasGrounded = characterController == null ||
                      characterController.isGrounded;
    }

    private void Update()
    {
        if (playerMovement == null)
            return;

        if (characterController == null)
        {
            characterController =
                playerMovement.GetComponent<CharacterController>();
        }

        bool isGrounded = characterController == null ||
                          characterController.isGrounded;

        bool isAiming = Mouse.current != null &&
                        Mouse.current.rightButton.isPressed;

        Vector3 targetBobOffset = Vector3.zero;

        if (playerMovement.isMoving &&
            !playerMovement.isSliding &&
            isGrounded)
        {
            float speedMultiplier = 1f;
            float intensity = 1f;

            if (playerMovement.isSprinting)
            {
                speedMultiplier = 1.7f;
                intensity = sprintBobMultiplier;
            }
            else if (playerMovement.isCrouching)
            {
                speedMultiplier = 0.5f;
                intensity = crouchBobMultiplier;
            }

            if (isAiming)
                intensity *= adsBobMultiplier;

            bobTimer += Time.deltaTime *
                        baseBobFrequency *
                        speedMultiplier;

            float offsetX = Mathf.Cos(bobTimer) *
                            horizontalAmount *
                            intensity;

            float offsetY = Mathf.Abs(Mathf.Sin(bobTimer)) *
                            verticalAmount *
                            intensity;

            targetBobOffset = new Vector3(
                offsetX,
                -offsetY,
                0f);
        }
        else if (!isGrounded)
        {
            bobTimer = 0f;
        }
        else
        {
            bobTimer = 0f;
        }

        // Move the viewmodel slightly down while airborne.
        float targetJumpOffset = isGrounded ? 0f : -jumpOffset;

        currentJumpOffset = Mathf.Lerp(
            currentJumpOffset,
            targetJumpOffset,
            Time.deltaTime * jumpSmoothSpeed);

        // Add a downward kick when landing.
        if (isGrounded && !wasGrounded)
            currentLandingKick = landingKick;

        currentLandingKick = Mathf.MoveTowards(
            currentLandingKick,
            0f,
            landingRecoverySpeed * Time.deltaTime);

        Vector3 targetPosition = startLocalPos +
                                 targetBobOffset +
                                 new Vector3(
                                     0f,
                                     currentJumpOffset - currentLandingKick,
                                     0f);

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPosition,
            Time.deltaTime * smoothSpeed);

        wasGrounded = isGrounded;
    }
}
