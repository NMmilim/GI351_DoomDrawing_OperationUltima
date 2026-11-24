using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates and manages the 3D procedural visuals of Wrath (The First Sin).
/// Creates an imposing obsidian behemoth with magma veins, glowing weakpoints, and enrage phase VFX.
/// </summary>
public class WrathBossVisuals : MonoBehaviour
{
    [Header("Color Theme")]
    public Color basaltColor = new Color(0.12f, 0.12f, 0.14f);
    public Color magmaDormantColor = new Color(0.85f, 0.25f, 0.05f);
    public Color magmaEnragedColor = new Color(1.0f, 0.1f, 0.0f);

    public Transform TorsoTransform { get; private set; }
    public Transform HeadTransform  { get; private set; }

    private readonly Vector3    _defaultTorsoPos = new Vector3(0f, 2.5f, 0f);
    private readonly Quaternion _defaultTorsoRot = Quaternion.identity;
    private readonly Vector3    _defaultHeadPos  = new Vector3(0f, 0.6f, 0.65f);
    private readonly Quaternion _defaultHeadRot  = Quaternion.identity;

    private readonly List<Renderer> _magmaRenderers = new List<Renderer>();
    private Material _dormantMagmaMat;
    private Material _enragedMagmaMat;
    private GameObject _dizzyVFXRoot;

    public void BuildBehemothModel(BossHealth health)
    {
        // Setup Materials
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Lit")
                          ?? Shader.Find("Universal Render Pipeline/Unlit")
                          ?? Shader.Find("Standard")
                          ?? Shader.Find("Diffuse");

        Material basaltMat = new Material(unlitShader) { color = basaltColor };
        _dormantMagmaMat   = new Material(unlitShader) { color = magmaDormantColor };
        _enragedMagmaMat   = new Material(unlitShader) { color = magmaEnragedColor };

        // 1. Torso (Main Armored Body)
        GameObject torso = CreatePart("Torso", PrimitiveType.Cube, transform,
            _defaultTorsoPos, new Vector3(3.2f, 2.2f, 4.5f), basaltMat);
        TorsoTransform = torso.transform;
        AttachWeakpoint(torso, BossWeakpoint.BodyPart.ArmoredBody, health, 1.0f);

        // 2. Chest Core (Weakpoint on Chest)
        GameObject core = CreatePart("ChestCore", PrimitiveType.Sphere, torso.transform,
            new Vector3(0f, 0f, 0.52f), new Vector3(0.55f, 0.55f, 0.25f), _dormantMagmaMat);
        AttachWeakpoint(core, BossWeakpoint.BodyPart.ChestCore, health, 1.5f);
        _magmaRenderers.Add(core.GetComponent<Renderer>());

        // 3. Head & Snout (Primary Weakpoint)
        GameObject head = CreatePart("Head", PrimitiveType.Cube, torso.transform,
            _defaultHeadPos, new Vector3(0.6f, 0.6f, 0.5f), basaltMat);
        HeadTransform = head.transform;
        AttachWeakpoint(head, BossWeakpoint.BodyPart.Head, health, 2.0f);

        // Horns on Head
        GameObject hornL = CreatePart("Horn_L", PrimitiveType.Cylinder, head.transform,
            new Vector3(-0.65f, 0.5f, -0.1f), new Vector3(0.2f, 0.7f, 0.2f), basaltMat);
        hornL.transform.localRotation = Quaternion.Euler(30f, 0f, -40f);

        GameObject hornR = CreatePart("Horn_R", PrimitiveType.Cylinder, head.transform,
            new Vector3(0.65f, 0.5f, -0.1f), new Vector3(0.2f, 0.7f, 0.2f), basaltMat);
        hornR.transform.localRotation = Quaternion.Euler(30f, 0f, 40f);

        // Eyes (Glowing Magma)
        GameObject eyeL = CreatePart("Eye_L", PrimitiveType.Sphere, head.transform,
            new Vector3(-0.35f, 0.15f, 0.52f), new Vector3(0.18f, 0.18f, 0.15f), _dormantMagmaMat);
        GameObject eyeR = CreatePart("Eye_R", PrimitiveType.Sphere, head.transform,
            new Vector3(0.35f, 0.15f, 0.52f), new Vector3(0.18f, 0.18f, 0.15f), _dormantMagmaMat);
        _magmaRenderers.Add(eyeL.GetComponent<Renderer>());
        _magmaRenderers.Add(eyeR.GetComponent<Renderer>());

        // 4. Forelegs & Hindlegs
        Vector3[] legOffsets = new Vector3[]
        {
            new Vector3(-1.4f, -0.7f, 1.3f),  // Front Left
            new Vector3( 1.4f, -0.7f, 1.3f),  // Front Right
            new Vector3(-1.4f, -0.7f, -1.3f), // Back Left
            new Vector3( 1.4f, -0.7f, -1.3f)  // Back Right
        };

        for (int i = 0; i < legOffsets.Length; i++)
        {
            GameObject leg = CreatePart($"Leg_{i}", PrimitiveType.Cylinder, torso.transform,
                legOffsets[i], new Vector3(0.35f, 0.75f, 0.35f), basaltMat);
            AttachWeakpoint(leg, BossWeakpoint.BodyPart.Legs, health, 1.0f);
        }

        // 5. Tail
        GameObject tail = CreatePart("Tail", PrimitiveType.Cylinder, torso.transform,
            new Vector3(0f, -0.1f, -0.7f), new Vector3(0.25f, 1.2f, 0.25f), basaltMat);
        tail.transform.localRotation = Quaternion.Euler(-55f, 0f, 0f);
        AttachWeakpoint(tail, BossWeakpoint.BodyPart.Tail, health, 0.8f);
    }

    private GameObject CreatePart(string name, PrimitiveType type, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPos;
        part.transform.localScale = localScale;

        Renderer r = part.GetComponent<Renderer>();
        if (r != null && mat != null)
        {
            r.material = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        return part;
    }

    private void AttachWeakpoint(GameObject part, BossWeakpoint.BodyPart bodyPart, BossHealth health, float multiplier)
    {
        BossWeakpoint wp = part.AddComponent<BossWeakpoint>();
        wp.bodyPart = bodyPart;
        wp.bossHealth = health;
        wp.ApplyPartDefaults();
        wp.usePartDefaults = false;
        wp.damageMultiplier = multiplier;
    }

    public void SetEnragedAppearance(bool isEnraged)
    {
        Material targetMat = isEnraged ? _enragedMagmaMat : _dormantMagmaMat;
        foreach (Renderer r in _magmaRenderers)
        {
            if (r != null) r.material = targetMat;
        }
    }

    /// <summary>
    /// Smoothly blends the boss between upright combat stance (progress = 0)
    /// and collapsed knockdown posture (progress = 1).
    /// </summary>
    public void SetKnockdownPose(float progress)
    {
        if (TorsoTransform == null) return;

        progress = Mathf.Clamp01(progress);

        // Collapse Torso down to floor level (Y: 2.5 -> 0.95) and roll to flank (tilt)
        Vector3 targetTorsoPos = Vector3.Lerp(_defaultTorsoPos, new Vector3(0f, 0.95f, 0f), progress);
        Quaternion targetTorsoRot = Quaternion.Slerp(_defaultTorsoRot, Quaternion.Euler(12f, 0f, -22f), progress);

        // Heave slightly when fully down (struggling beast breath)
        if (progress > 0.9f)
        {
            float heave = Mathf.Sin(Time.time * 6f) * 0.05f;
            targetTorsoPos.y += heave;
        }

        TorsoTransform.localPosition = targetTorsoPos;
        TorsoTransform.localRotation = targetTorsoRot;

        if (HeadTransform != null)
        {
            // Droop head down towards the ground
            Vector3 targetHeadPos = Vector3.Lerp(_defaultHeadPos, new Vector3(0f, 0.25f, 0.75f), progress);
            Quaternion targetHeadRot = Quaternion.Slerp(_defaultHeadRot, Quaternion.Euler(22f, 0f, 15f), progress);
            HeadTransform.localPosition = targetHeadPos;
            HeadTransform.localRotation = targetHeadRot;
        }
    }

    /// <summary>
    /// Permanent defeated collapse pose on the ground upon boss death.
    /// </summary>
    public void SetDefeatedPose()
    {
        if (TorsoTransform != null)
        {
            TorsoTransform.localPosition = new Vector3(0f, 0.7f, 0f);
            TorsoTransform.localRotation = Quaternion.Euler(5f, 0f, -65f);
        }
        HideDizzyStars();
    }

    /// <summary>
    /// Spawns 3 glowing golden stars orbiting above the behemoth's head while knocked down.
    /// </summary>
    public void ShowDizzyStars(float duration)
    {
        HideDizzyStars();

        if (HeadTransform == null) return;

        _dizzyVFXRoot = new GameObject("DizzyStarsRoot");
        _dizzyVFXRoot.transform.SetParent(HeadTransform, false);
        _dizzyVFXRoot.transform.localPosition = new Vector3(0f, 1.2f, 0f);

        Shader unlit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material starMat = new Material(unlit);
        starMat.SetColor("_BaseColor", new Color(1f, 0.9f, 0.2f));
        starMat.SetColor("_Color", new Color(1f, 0.9f, 0.2f));

        for (int i = 0; i < 3; i++)
        {
            GameObject star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            star.name = $"Star_{i}";
            star.transform.SetParent(_dizzyVFXRoot.transform, false);
            star.transform.localScale = Vector3.one * 0.22f;

            Collider sc = star.GetComponent<Collider>();
            if (sc != null) DestroyImmediate(sc);

            Renderer sr = star.GetComponent<Renderer>();
            if (sr != null) sr.material = starMat;
        }

        DizzyOrbit orbit = _dizzyVFXRoot.AddComponent<DizzyOrbit>();
        orbit.Initialize(duration);
    }

    public void HideDizzyStars()
    {
        if (_dizzyVFXRoot != null)
        {
            Destroy(_dizzyVFXRoot);
            _dizzyVFXRoot = null;
        }
    }
}

/// <summary>
/// Animates 3 dizzy stars in an orbit above the boss's head during stun.
/// </summary>
public class DizzyOrbit : MonoBehaviour
{
    private float _duration;
    private float _timer;

    public void Initialize(float duration)
    {
        _duration = duration;
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        transform.Rotate(Vector3.up, 180f * Time.deltaTime, Space.Self);

        for (int i = 0; i < transform.childCount; i++)
        {
            float angle = (i * 120f + _timer * 90f) * Mathf.Deg2Rad;
            float radius = 0.85f;
            transform.GetChild(i).localPosition = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(_timer * 4f + i) * 0.1f, Mathf.Sin(angle) * radius);
        }

        if (_timer >= _duration)
        {
            Destroy(gameObject);
        }
    }
}
