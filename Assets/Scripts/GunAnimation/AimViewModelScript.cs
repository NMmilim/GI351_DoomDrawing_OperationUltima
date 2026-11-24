using UnityEngine;
using UnityEngine.InputSystem;

public class RedDotAim : MonoBehaviour
{
    [Header("Sight & Camera Markers")]
    public Transform redDotSightPoint;
    public Transform cameraAimPoint;

    [Header("ADS Speed")]
    public float aimSpeed = 12f;

    [Header("Sound Manager")]
    public Transform soundPoint;
    public string aimSoundName = "Aim";
    public string unaimSoundName = "Unaim";

    private Vector3 hipLocalPos;
    private bool wasAiming;

    private void Start()
    {
        hipLocalPos = transform.localPosition;
    }

    private void Update()
    {
        if (redDotSightPoint == null ||
            cameraAimPoint == null)
        {
            return;
        }

        bool isAiming = Mouse.current != null &&
                        Mouse.current.rightButton.isPressed;

        bool isSprinting = Keyboard.current != null &&
                           Keyboard.current.leftShiftKey.isPressed; 

        bool shouldAim = isAiming && !isSprinting;

        if (shouldAim != wasAiming)
        {
            PlayAimSound(shouldAim);
            wasAiming = shouldAim;
        }

        Vector3 targetLocalPosition = hipLocalPos;

        if (shouldAim)
        {
            Vector3 sightOffset =
                redDotSightPoint.position - transform.position;

            Vector3 desiredWorldPosition =
                cameraAimPoint.position - sightOffset;

            targetLocalPosition =
                transform.parent.InverseTransformPoint(
                    desiredWorldPosition);
        }

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetLocalPosition,
            Time.deltaTime * aimSpeed);
    }

    private void PlayAimSound(bool aiming)
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("SoundManager instance is missing.", this);
            return;
        }

        string soundName = aiming
            ? aimSoundName
            : unaimSoundName;

        Vector3 position = soundPoint != null
            ? soundPoint.position
            : transform.position;

        SoundManager.Instance.PlaySound3D(
            soundName,
            position);
    }
}
