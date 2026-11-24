using UnityEngine;

/// <summary>
/// Renders 3D visual telegraphs on the ground for boss attacks (Charge Lane & Slam Ring).
/// Automatically raycasts down to snap every point onto uneven terrain/floors while ignoring the Boss itself.
/// </summary>
public class BossTelegraph : MonoBehaviour
{
    [Tooltip("Height offset above the ground surface to prevent Z-fighting flicker.")]
    [SerializeField] private float groundOffset = 0.12f;

    private LineRenderer _laneRenderer;
    private LineRenderer _circleRenderer;

    private void Awake()
    {
        _laneRenderer   = CreateTelegraphLine("Telegraph_ChargeLane", new Color(1f, 0.15f, 0.15f, 0.75f), 0.18f);
        _circleRenderer = CreateTelegraphLine("Telegraph_SlamCircle", new Color(1f, 0.35f, 0.1f, 0.8f), 0.22f);
    }

    private LineRenderer CreateTelegraphLine(string name, Color color, float width)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace     = true;
        lr.startWidth        = width;
        lr.endWidth          = width;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows    = false;
        lr.sortingOrder      = 10;

        Shader shader = Shader.Find("Sprites/Default")
                     ?? Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Unlit/Color");

        if (shader != null)
        {
            Material mat = new Material(shader);
            mat.color = color;
            lr.material = mat;
        }

        lr.enabled = false;
        return lr;
    }

    /// <summary>
    /// Shows a rectangular charge lane snapped onto the ground from origin in direction.
    /// </summary>
    public void ShowChargeLane(Vector3 origin, Vector3 direction, float length, float width)
    {
        if (_laneRenderer == null) return;

        Vector3 normDir = direction.normalized;
        Vector3 right   = Vector3.Cross(Vector3.up, normDir).normalized * (width * 0.5f);

        Vector3 p1 = SnapToGround(origin - right);
        Vector3 p2 = SnapToGround(origin + right);
        Vector3 p3 = SnapToGround(origin + normDir * length + right);
        Vector3 p4 = SnapToGround(origin + normDir * length - right);

        _laneRenderer.positionCount = 5;
        _laneRenderer.SetPositions(new Vector3[] { p1, p2, p3, p4, p1 });
        _laneRenderer.enabled = true;
    }

    public void HideChargeLane()
    {
        if (_laneRenderer != null) _laneRenderer.enabled = false;
    }

    /// <summary>
    /// Shows a danger circle snapped onto the ground for slams and AOEs.
    /// </summary>
    public void ShowSlamCircle(Vector3 center, float radius, int segments = 32)
    {
        if (_circleRenderer == null) return;

        _circleRenderer.positionCount = segments + 1;
        float angleStep = 360f / segments;

        for (int i = 0; i <= segments; i++)
        {
            float rad = (i * angleStep) * Mathf.Deg2Rad;
            Vector3 samplePos = center + new Vector3(Mathf.Cos(rad) * radius, 0f, Mathf.Sin(rad) * radius);
            _circleRenderer.SetPosition(i, SnapToGround(samplePos));
        }

        _circleRenderer.enabled = true;
    }

    public void HideSlamCircle()
    {
        if (_circleRenderer != null) _circleRenderer.enabled = false;
    }

    public void HideAll()
    {
        HideChargeLane();
        HideSlamCircle();
    }

    private Vector3 SnapToGround(Vector3 worldPos)
    {
        Vector3 rayStart = new Vector3(worldPos.x, worldPos.y + 8f, worldPos.z);
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 30f, ~0, QueryTriggerInteraction.Ignore);

        float bestY = float.NegativeInfinity;
        bool foundGround = false;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;
            // Ignore the boss itself and the player so lines always land on terrain/floors
            if (col.GetComponentInParent<BossHealth>() != null) continue;
            if (col.GetComponentInParent<PlayerMovement>() != null) continue;

            if (hits[i].point.y > bestY)
            {
                bestY = hits[i].point.y;
                foundGround = true;
            }
        }

        worldPos.y = (foundGround ? bestY : worldPos.y) + groundOffset;
        return worldPos;
    }
}
