using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 5f;

    private CharacterController controller;
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundLayer;
    public float gravity;
    private Vector3 velocity;
    private bool isGrounded;
    bool isMoving;
    private Vector3 LastPos = new Vector3(0, 0, 0);

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        isGrounded = groundCheck != null ? Physics.CheckSphere(groundCheck.position, groundDistance, groundLayer) : controller.isGrounded;

        // Reset vertical velocity when grounded
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Read input using the New Input System (matches MouseMovement style)
        float x = 0f;
        float z = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.dKey.isPressed ) x += 1f;
            if (Keyboard.current.aKey.isPressed ) x -= 1f;
            if (Keyboard.current.wKey.isPressed ) z += 1f;
            if (Keyboard.current.sKey.isPressed ) z -= 1f;
        }

        //Creating the moving Vector
        Vector3 move = transform.right * x + transform.forward * z;
        //Actually moving player
        controller.Move(move * speed * Time.deltaTime);

        bool jumpPressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        if (jumpPressed && isGrounded)
        {
            //Actually Jumping
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
        }

        //falling down
        velocity.y += gravity * Time.deltaTime;
        //Executing the falling down
        controller.Move(velocity * Time.deltaTime);

        isMoving = (LastPos != transform.position && isGrounded == true) ? true : false;
        LastPos = transform.position;
        speed = (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed && isMoving) ? 10f : 5f;
    }
}
