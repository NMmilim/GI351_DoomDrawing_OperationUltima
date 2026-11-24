using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float DefaultSpeed;
    public float jumpForce = 5f;
    public float stamina = 100f;
    public float maxStamina = 100f;
    public float gravity = -9.81f;
    public float dashForce = 20f;
    public float dashDamp = 0.9f; // Damping factor for dash speed
    
    [Header("Slide Settings")]
    public float slideDuration = 3f; // Slide lasts 3 seconds
    private float slideTimer = 0f;

    private CharacterController controller;
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundLayer;

    private Vector3 velocity;
    private Vector3 dashVelocity;
    private bool isGrounded;
    bool isMoving;
    bool isSprinting;
    bool isCrouching;
    bool isSliding;
    

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (gravity == 0) gravity = -9.81f;
    }

    void Update()
    {
        isGrounded = groundCheck != null ? Physics.CheckSphere(groundCheck.position, groundDistance, groundLayer) : controller.isGrounded;
        // Read input using the New Input System
        float x = 0f;
        float z = 0f;
        //===================================================================
        //=============================== Movement WASD vector===============
        //===================================================================
        if (Keyboard.current != null)
        {
            if (Keyboard.current.dKey.isPressed) x += 1f;
            if (Keyboard.current.aKey.isPressed) x -= 1f;
            if (Keyboard.current.wKey.isPressed) z += 1f;
            if (Keyboard.current.sKey.isPressed) z -= 1f;
        }
        
        Vector3 move = transform.right *  x + transform.forward *  z; 
        if (move.sqrMagnitude > 1f) move.Normalize();
        isMoving = (move.sqrMagnitude > 0.001f) && isGrounded;
        //==============================================================
        //===================JUMP & FALLING ============================
        //==============================================================
        // Falling down / gravity
        velocity.y += gravity * Time.deltaTime;
          // Reset vertical velocity when grounded
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        } 
        // Jump
        bool jumpPressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        if (jumpPressed && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }
        //===================================================================

        float speed = DefaultSpeed;
        // Apply Speed & Stamina
        if (isSliding)
        {
            speed = DefaultSpeed * 2.5f;
            stamina -= 15f * Time.deltaTime; // Drains stamina during the slide
        }
        else if (isSprinting)
        {
            speed = DefaultSpeed * 1.7f;
            stamina -= 10f * Time.deltaTime;
        }
        else if (isCrouching)
        {
            speed = DefaultSpeed/2;
            stamina += 15f * Time.deltaTime; // Recover stamina
        }
        else
        {
            stamina += 10f * Time.deltaTime; // Recover stamina
        }
        //===================================================================
        //==========================CONTROL==================================
        //===================================================================
        // Start slide: Press Ctrl while sprinting (and have stamina)
        if (!isSliding && Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed && Keyboard.current.leftCtrlKey.wasPressedThisFrame && isMoving && stamina > 0)
        {
            isSliding = true;
            slideTimer = slideDuration; // Start 3-second timer
        }

        // Handle active slide
        if (isSliding)
        {
            slideTimer -= Time.deltaTime;

            // Stop sliding if 3 seconds passed, or stamina drained to 0, or stopped moving
            if (slideTimer <= 0f || stamina <= 0f || !isMoving)
            {
                isSliding = false;
            }
        } 
        if (Keyboard.current != null && Keyboard.current.altKey.wasPressedThisFrame && stamina > 40)
        {
            stamina -= 40f;
            Vector3 dashDirection = move != Vector3.zero ? move.normalized : transform.forward;
            dashVelocity = dashDirection * dashForce;
        }
        //===================================================================
        //===================================================================
        //===================================================================
        stamina = Mathf.Clamp(stamina, 0f, maxStamina);
        // Normal states when not sliding
        isSprinting = (!isSliding && Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed && stamina > 0 && isMoving);
        isCrouching = (!isSliding && Keyboard.current != null && Keyboard.current.leftCtrlKey.isPressed && !Keyboard.current.leftShiftKey.isPressed);

       
        Debug.Log(speed);
        Vector3 totalVelocity = (move * speed) + velocity + dashVelocity;
        controller.Move(totalVelocity * Time.deltaTime);
        dashVelocity = Vector3.MoveTowards(dashVelocity, Vector3.zero, dashDamp * Time.deltaTime);
       
        
    }
}
