using System.Collections;
using UnityEngine;

public class PlayerFootsteps : MonoBehaviour
{
    [Header("SoundManager Settings")]
    public string footstepSoundName = "Footstep";

    [Header("Step Intervals (Seconds)")]
    public float walkInterval = 0.45f;
    public float sprintInterval = 0.30f;
    public float crouchInterval = 0.65f;

    [Header("Pitch Settings")]
    public float walkPitch = 1.0f;
    public float sprintPitch = 1.2f;
    public float crouchPitch = 0.85f;
    [Range(0f, 0.2f)] public float pitchVariation = 0.05f;

    private PlayerMovement playerMovement;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        StartCoroutine(PlayFootsteps());
    }

    IEnumerator PlayFootsteps()
    {
        while (true)
        {
            if (playerMovement != null && playerMovement.isMoving && !playerMovement.isSliding)
            {
                // Determine interval and base pitch based on movement state
                float currentInterval = walkInterval;
                float targetPitch = walkPitch;

                if (playerMovement.isSprinting)
                {
                    currentInterval = sprintInterval;
                    targetPitch = sprintPitch;
                }
                else if (playerMovement.isCrouching)
                {
                    currentInterval = crouchInterval;
                    targetPitch = crouchPitch;
                }

                // Add slight random variation so steps feel natural
                float finalPitch = targetPitch + Random.Range(-pitchVariation, pitchVariation);

                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlaySound2D(footstepSoundName, finalPitch);
                }

                yield return new WaitForSeconds(currentInterval);
            }
            else
            {
                yield return null;
            }
        }
    }
}
