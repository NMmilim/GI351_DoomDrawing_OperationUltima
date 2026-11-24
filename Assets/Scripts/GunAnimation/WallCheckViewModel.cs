using UnityEngine;

public class WeaponWallAvoidance : MonoBehaviour
{
    [Header("Barrel Collision Check")]
    public Transform firePoint;
    public LayerMask wallLayer = ~0;
    public float checkDistance = 1.3f;
    public float sphereRadius = 0.08f;

    [Header("Ignored Tags")]
    public string[] ignoredTags = { "Player", "Bullet" };

    [Header("Pushback & Tilt")]
    public float maxPushback = 0.35f;
    public float maxTiltUpAngle = 35f;
    public float smoothSpeed = 12f;

    private Vector3 startLocalPos;
    private Quaternion startLocalRot;

    private void Start()
    {
        startLocalPos = transform.localPosition;
        startLocalRot = transform.localRotation;

        if (firePoint == null)
            Debug.LogWarning("WeaponWallAvoidance: Fire Point is missing!");
    }

    private void LateUpdate()
    {
        if (firePoint == null)
            return;

        RaycastHit[] hits = Physics.SphereCastAll(
            firePoint.position,
            sphereRadius,
            firePoint.forward,
            checkDistance,
            wallLayer,
            QueryTriggerInteraction.Ignore);

        float closestDistance = checkDistance;

        foreach (RaycastHit hit in hits)
        {
            if (IsIgnoredCollider(hit.collider))
                continue;

            Debug.DrawLine(
                firePoint.position,
                hit.point,
                Color.red);

            if (hit.distance < closestDistance)
                closestDistance = hit.distance;
        }

        // Detect a wall that the barrel has already entered.
        Collider[] overlappingColliders = Physics.OverlapSphere(
            firePoint.position,
            sphereRadius,
            wallLayer,
            QueryTriggerInteraction.Ignore);

        foreach (Collider collider in overlappingColliders)
        {
            if (IsIgnoredCollider(collider))
                continue;

            closestDistance = 0f;
            break;
        }

        float pushRatio = 1f - closestDistance / checkDistance;
        pushRatio = Mathf.Clamp01(pushRatio);

        Vector3 targetPosition = startLocalPos +
                                 Vector3.back *
                                 (maxPushback * pushRatio);

        Quaternion targetRotation = startLocalRot *
                                    Quaternion.Euler(
                                        -maxTiltUpAngle * pushRatio,
                                        0f,
                                        0f);

        float smoothAmount = Time.deltaTime * smoothSpeed;

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPosition,
            smoothAmount);

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRotation,
            smoothAmount);
    }

    private bool IsIgnoredCollider(Collider collider)
    {
        Transform current = collider.transform;

        while (current != null)
        {
            foreach (string tagName in ignoredTags)
            {
                if (!string.IsNullOrEmpty(tagName) &&
                    current.CompareTag(tagName))
                {
                    return true;
                }
            }

            current = current.parent;
        }

        // Ignore the arms/gun hierarchy itself.
        return collider.transform.IsChildOf(transform);
    }

    private void OnDrawGizmosSelected()
    {
        if (firePoint == null)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            firePoint.position +
            firePoint.forward * checkDistance,
            sphereRadius);

        Gizmos.DrawLine(
            firePoint.position,
            firePoint.position + firePoint.forward * checkDistance);
    }
}