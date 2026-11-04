using UnityEngine;
using UnityEngine.InputSystem;
public class MouseMovement : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public float mousesens = 100f;
    float xrot;
    float yrot;
    void Start()
    {
        // Lock the cursor to the center of the screen
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 mouseDelta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
        // Get the mouse input
        float mouseX = mouseDelta.x * mousesens;
        float mouseY = mouseDelta.y * mousesens;
        // Update the rotation values based on mouse input
        xrot += mouseY;
        //clamp rotation
        xrot = Mathf.Clamp(xrot, -90f, 90f);
        // Apply the rotation to the camera
        yrot += mouseX;
        yrot = Mathf.Clamp(yrot, -90f, 90f);
        //Appply the rotation to the transform
        transform.localRotation = Quaternion.Euler(xrot, yrot, 0f);
    }
}
