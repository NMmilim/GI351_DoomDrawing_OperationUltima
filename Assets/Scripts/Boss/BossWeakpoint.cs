using UnityEngine;

/// <summary>
/// Attached to individual monster limb colliders (Head, Chest Core, Legs, Tail).
/// Provides Monster-Hunter-style weakpoint multipliers, part-break tracking, impact sparks, and floating damage numbers.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BossWeakpoint : MonoBehaviour, IBossHittable
{
    public enum BodyPart
    {
        Head,       // Critical weak point (2.0x damage, high stun)
        ChestCore,  // Core weak point (1.5x damage)
        Legs,       // Knockdown trip threshold (1.0x damage)
        Tail,       // Severable/armored (0.8x damage)
        ArmoredBody // Default body (1.0x damage)
    }

    [Header("Part Configuration")]
    public BodyPart bodyPart = BodyPart.ArmoredBody;

    [Tooltip("When enabled, automatically sets default multiplier & spark color based on Body Part.")]
    public bool usePartDefaults = true;

    [Tooltip("Damage multiplier applied when shooting this specific body part.")]
    public float damageMultiplier = 1.0f;

    [Tooltip("Parent BossHealth component. Auto-found in parents if null.")]
    public BossHealth bossHealth;

    [Header("Visual Feedback")]
    [Tooltip("Color of hit sparks for this part.")]
    public Color sparkColor = Color.white;

    private void OnValidate()
    {
        if (usePartDefaults)
        {
            ApplyPartDefaults();
        }
    }

    private void Awake()
    {
        if (bossHealth == null)
            bossHealth = GetComponentInParent<BossHealth>();

        if (usePartDefaults)
        {
            ApplyPartDefaults();
        }
    }

    /// <summary>
    /// Applies the standard multiplier and spark color for the selected BodyPart.
    /// </summary>
    public void ApplyPartDefaults()
    {
        switch (bodyPart)
        {
            case BodyPart.Head:
                damageMultiplier = 2.0f;
                sparkColor = new Color(1f, 0.9f, 0.2f); // Golden critical
                break;
            case BodyPart.ChestCore:
                damageMultiplier = 1.5f;
                sparkColor = new Color(1f, 0.4f, 0.1f); // Orange core
                break;
            case BodyPart.Tail:
                damageMultiplier = 0.8f;
                sparkColor = new Color(0.7f, 0.7f, 0.7f); // Armored gray
                break;
            default:
                damageMultiplier = 1.0f;
                sparkColor = Color.white;
                break;
        }
    }

    public void OnHit(int rawDamage, Vector3 hitPoint, Vector3 hitNormal, float extraMultiplier = 1f)
    {
        if (bossHealth != null && bossHealth.IsDead) return;

        int finalDamage = Mathf.RoundToInt(rawDamage * damageMultiplier * extraMultiplier);

        SpawnImpactSparks(hitPoint, hitNormal);
        SpawnDamageNumber(finalDamage, hitPoint);

        if (bossHealth != null)
        {
            bossHealth.RegisterDamage(finalDamage, bodyPart, hitPoint);
        }
    }

    private void SpawnImpactSparks(Vector3 point, Vector3 normal)
    {
        GameObject sparkObj = new GameObject("HitSpark");
        sparkObj.transform.position = point;
        sparkObj.transform.rotation = Quaternion.LookRotation(normal);

        ParticleSystem ps = sparkObj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.2f;
        main.startSpeed = 7f;
        main.startSize = 0.08f;
        main.startColor = sparkColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 10) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;

        var renderer = sparkObj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Sprites/Default");

        if (s != null)
        {
            Material mat = new Material(s);
            mat.SetColor("_BaseColor", sparkColor);
            mat.color = sparkColor;
            renderer.material = mat;
        }

        ps.Play();
        Destroy(sparkObj, 0.35f);
    }

    private void SpawnDamageNumber(int damage, Vector3 position)
    {
        GameObject numObj = new GameObject("DamageNumber");
        numObj.transform.position = position + Vector3.up * 0.4f;

        TextMesh tm = numObj.AddComponent<TextMesh>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
        {
            tm.font = font;
            MeshRenderer mr = numObj.GetComponent<MeshRenderer>();
            if (mr != null) mr.material = font.material;
        }

        bool isCrit = (bodyPart == BodyPart.Head);
        tm.text = isCrit ? $"CRIT! {damage}" : damage.ToString();
        tm.fontSize = isCrit ? 36 : 28;
        tm.characterSize = 0.08f;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = sparkColor;

        DamageNumberFloater floater = numObj.AddComponent<DamageNumberFloater>();
        floater.Initialize(isCrit);
    }
}

/// <summary>
/// Handles floating, billboarding, and fading of Monster Hunter style damage numbers.
/// Purely visual, no collider.
/// </summary>
public class DamageNumberFloater : MonoBehaviour
{
    private float _timer = 0f;
    private const float Lifetime = 0.65f;
    private Vector3 _startScale;

    public void Initialize(bool isCrit)
    {
        _startScale = isCrit ? Vector3.one * 1.35f : Vector3.one;
        transform.localScale = _startScale;
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        // Billboard towards camera
        Camera cam = Camera.main;
        if (cam != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }

        // Rise upward
        transform.position += Vector3.up * (2.0f * Time.deltaTime);

        // Scale pop then shrink out
        float progress = _timer / Lifetime;
        if (progress < 0.2f)
        {
            transform.localScale = Vector3.Lerp(Vector3.zero, _startScale, progress / 0.2f);
        }
        else
        {
            transform.localScale = Vector3.Lerp(_startScale, Vector3.zero, (progress - 0.2f) / 0.8f);
        }

        if (_timer >= Lifetime)
        {
            Destroy(gameObject);
        }
    }
}
