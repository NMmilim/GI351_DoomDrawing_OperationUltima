using UnityEngine;
using UnityEngine.InputSystem;

public class MouseMovement : MonoBehaviour
{
    public float mousesens = 10f;
    public Transform playerBody;

    float xrot;

    void Start()
    {
        // Lock the cursor to the center of the screen
        Cursor.lockState = CursorLockMode.Locked;

        // Auto-assign playerBody if Camera is a child of Player
        if (playerBody == null && transform.parent != null)
        {
            playerBody = transform.parent;
        }
    }

    void Update()
    {
        Vector2 mouseDelta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;

        // Get the mouse input
        float mouseX = mouseDelta.x * mousesens * Time.deltaTime;
        float mouseY = mouseDelta.y * mousesens * Time.deltaTime;

        // Up/down rotation (Pitch) - only tilts the camera
        xrot -= mouseY;
        xrot = Mathf.Clamp(xrot, -90f, 90f);
        transform.localRotation = Quaternion.Euler(xrot, 0f, 0f);

        // Left/right rotation (Yaw) - turns the player body
        if (playerBody != null){ playerBody.Rotate(Vector3.up * mouseX);}
    }
}
