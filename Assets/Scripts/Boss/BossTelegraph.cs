using UnityEngine;

/// <summary>
/// Renders 3D visual telegraphs on the ground for boss attacks (Charge Lane & Slam Ring).
/// Procedural geometry rendered directly in world-space.
/// </summary>
public class BossTelegraph : MonoBehaviour
{
    private LineRenderer _laneRenderer;
    private LineRenderer _circleRenderer;

    private void Awake()
    {
        _laneRenderer   = CreateTelegraphLine("Telegraph_ChargeLane", new Color(1f, 0.15f, 0.15f, 0.65f), 0.15f);
        _circleRenderer = CreateTelegraphLine("Telegraph_SlamCircle", new Color(1f, 0.35f, 0.1f, 0.75f), 0.2f);
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

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Sprites/Default")
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
    /// Shows a rectangular charge lane on the ground from origin in direction.
    /// </summary>
    public void ShowChargeLane(Vector3 origin, Vector3 direction, float length, float width)
    {
        if (_laneRenderer == null) return;

        origin.y += 0.05f; // Slight offset above floor to prevent z-fighting
        Vector3 normDir = direction.normalized;
        Vector3 right   = Vector3.Cross(Vector3.up, normDir).normalized * (width * 0.5f);

        Vector3 p1 = origin - right;
        Vector3 p2 = origin + right;
        Vector3 p3 = origin + normDir * length + right;
        Vector3 p4 = origin + normDir * length - right;

        _laneRenderer.positionCount = 5;
        _laneRenderer.SetPositions(new Vector3[] { p1, p2, p3, p4, p1 });
        _laneRenderer.enabled = true;
    }

    public void HideChargeLane()
    {
        if (_laneRenderer != null) _laneRenderer.enabled = false;
    }

    /// <summary>
    /// Shows an expanding/fixed danger circle on the ground for slams and AOEs.
    /// </summary>
    public void ShowSlamCircle(Vector3 center, float radius, int segments = 32)
    {
        if (_circleRenderer == null) return;

        center.y += 0.05f;
        _circleRenderer.positionCount = segments + 1;
        float angleStep = 360f / segments;

        for (int i = 0; i <= segments; i++)
        {
            float rad = (i * angleStep) * Mathf.Deg2Rad;
            Vector3 point = center + new Vector3(Mathf.Cos(rad) * radius, 0f, Mathf.Sin(rad) * radius);
            _circleRenderer.SetPosition(i, point);
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
}
