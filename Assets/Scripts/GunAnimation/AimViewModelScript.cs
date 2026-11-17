using UnityEngine;
using UnityEngine.InputSystem;

public class RedDotAim : MonoBehaviour
{
    [Header("Sight & Camera Markers")]
    public Transform redDotSightPoint; // Empty object placed inside the gun's red dot sight
    public Transform cameraAimPoint;   // Empty object placed at center of screen (child of Camera)

    [Header("ADS Speed")]
    public float aimSpeed = 12f;

    private Vector3 hipLocalPos;

    void Start()
    {
        // Saves your starting hip-fire position automatically
        hipLocalPos = transform.localPosition;
    }

    void Update()
    {
        if (redDotSightPoint == null || cameraAimPoint == null) return;

        bool isAiming = Mouse.current != null && Mouse.current.rightButton.isPressed;
        Vector3 targetLocalPos = hipLocalPos;

        if (isAiming)
        {
            // Find the exact offset between WeaponHolder and the Red Dot sight
            Vector3 sightOffset = redDotSightPoint.position - transform.position;

            // Calculate where WeaponHolder must move so RedDotSightPoint lands on CameraAimPoint
            Vector3 desiredWorldPos = cameraAimPoint.position - sightOffset;
            targetLocalPos = transform.parent.InverseTransformPoint(desiredWorldPos);
        }

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocalPos, Time.deltaTime * aimSpeed);
    }
}
