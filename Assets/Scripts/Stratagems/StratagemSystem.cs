using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum StratagemDir
{
    Up,
    Down,
    Left,
    Right
}

/// <summary>
/// Helldivers 2-inspired 2-Stratagem MVP:
///   Hold Q + Arrow Keys:
///     1. Air Strike    (Up, Right, Right) -> 3s orbital lock-on -> big explosion sphere + heavy damage on Boss
///     2. Smoke Orbital (Down, Left, Right) -> deploys smoke carpet hiding player & forcing Boss into Roaming state
/// </summary>
public class StratagemSystem : MonoBehaviour
{
    [Header("Stratagem 1: Air Strike")]
    public string airStrikeName = "AIR STRIKE";
    public StratagemDir[] airStrikeCode = { StratagemDir.Up, StratagemDir.Right, StratagemDir.Right };
    public float airStrikeDelay = 3f;
    public float airStrikeCooldown = 12f;
    public int airStrikeDamage = 250;
    public float airStrikeExplosionRadius = 6f;

    [Header("Stratagem 2: Smoke Orbital")]
    public string smokeName = "SMOKE ORBITAL";
    public StratagemDir[] smokeCode = { StratagemDir.Down, StratagemDir.Left, StratagemDir.Right };
    public float smokeCallDelay = 1f;
    public float smokeCooldown = 15f;
    public float smokeDuration = 8f;
    public float smokeRadius = 9f;

    // Runtime state (read by StratagemHUD)
    public bool IsMenuOpen { get; private set; }
    public IReadOnlyList<StratagemDir> CurrentInput => _currentInput;
    public float AirStrikeCooldownRemaining => _airStrikeCdTimer;
    public float SmokeCooldownRemaining => _smokeCdTimer;
    public float AirStrikeIncomingRemaining => _airStrikeIncomingTimer;
    public float SmokeActiveRemaining => _smokeActiveTimer;
    public bool IsPlayerConcealed => _activeSmokeZones.Count > 0 && IsPlayerInsideAnySmoke();
    public bool InputErrorFlash => _errorFlashTimer > 0f;

    private readonly List<StratagemDir> _currentInput = new List<StratagemDir>();
    private readonly List<SmokeZoneRuntime> _activeSmokeZones = new List<SmokeZoneRuntime>();

    private float _airStrikeCdTimer;
    private float _smokeCdTimer;
    private float _airStrikeIncomingTimer;
    private float _smokeActiveTimer;
    private float _errorFlashTimer;
    private bool _lastConcealedState;

    private static Material _redLaserMat;
    private static Material _explosionMat;
    private static Material _blueLaserMat;
    private static Material _smokeMat;

    private void Start()
    {
        EnsureHUD();
    }

    private void EnsureHUD()
    {
        if (FindAnyObjectByType<StratagemHUD>() != null) return;

        // Prefer attaching to the existing HUDCanvas -> Stratagem panel
        GameObject stratagemPanel = GameObject.Find("Stratagem");
        if (stratagemPanel != null)
        {
            stratagemPanel.AddComponent<StratagemHUD>().Bind(this);
            return;
        }

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas != null)
        {
            canvas.gameObject.AddComponent<StratagemHUD>().Bind(this);
        }
    }

    private void Update()
    {
        if (_airStrikeCdTimer > 0f) _airStrikeCdTimer = Mathf.Max(0f, _airStrikeCdTimer - Time.deltaTime);
        if (_smokeCdTimer > 0f) _smokeCdTimer = Mathf.Max(0f, _smokeCdTimer - Time.deltaTime);
        if (_airStrikeIncomingTimer > 0f) _airStrikeIncomingTimer = Mathf.Max(0f, _airStrikeIncomingTimer - Time.deltaTime);
        if (_errorFlashTimer > 0f) _errorFlashTimer = Mathf.Max(0f, _errorFlashTimer - Time.deltaTime);

        UpdateSmokeZones();
        HandleStratagemInput();
    }

    private void HandleStratagemInput()
    {
        if (Keyboard.current == null) return;

        bool holdMenu = Keyboard.current.qKey.isPressed;
        if (!holdMenu)
        {
            if (IsMenuOpen)
            {
                IsMenuOpen = false;
                _currentInput.Clear();
            }
            return;
        }

        IsMenuOpen = true;

        StratagemDir? pressed = null;
        if (Keyboard.current.upArrowKey.wasPressedThisFrame) pressed = StratagemDir.Up;
        else if (Keyboard.current.downArrowKey.wasPressedThisFrame) pressed = StratagemDir.Down;
        else if (Keyboard.current.leftArrowKey.wasPressedThisFrame) pressed = StratagemDir.Left;
        else if (Keyboard.current.rightArrowKey.wasPressedThisFrame) pressed = StratagemDir.Right;

        if (!pressed.HasValue) return;

        _currentInput.Add(pressed.Value);

        // Check exact match for Air Strike
        if (_airStrikeCdTimer <= 0f && MatchesExact(airStrikeCode))
        {
            _currentInput.Clear();
            TriggerAirStrike();
            return;
        }

        // Check exact match for Smoke Orbital
        if (_smokeCdTimer <= 0f && MatchesExact(smokeCode))
        {
            _currentInput.Clear();
            TriggerSmokeOrbital();
            return;
        }

        // Keep typing if current input is still a valid prefix of any ready stratagem
        bool validPrefix =
            (_airStrikeCdTimer <= 0f && MatchesPrefix(airStrikeCode)) ||
            (_smokeCdTimer <= 0f && MatchesPrefix(smokeCode));

        if (!validPrefix)
        {
            _currentInput.Clear();
            _errorFlashTimer = 0.25f;
        }
    }

    public bool MatchesPrefix(StratagemDir[] code)
    {
        if (code == null || _currentInput.Count > code.Length) return false;
        for (int i = 0; i < _currentInput.Count; i++)
        {
            if (_currentInput[i] != code[i]) return false;
        }
        return true;
    }

    private bool MatchesExact(StratagemDir[] code)
    {
        return code != null && _currentInput.Count == code.Length && MatchesPrefix(code);
    }

    // -----------------------------------------------------------------
    //  Stratagem 1: Air Strike (3s lock-on -> big explosion sphere + damage on Boss)
    // -----------------------------------------------------------------
    private void TriggerAirStrike()
    {
        _airStrikeCdTimer = airStrikeCooldown;
        _airStrikeIncomingTimer = airStrikeDelay;
        StartCoroutine(ExecuteAirStrikeRoutine());
    }

    private IEnumerator ExecuteAirStrikeRoutine()
    {
        BossHealth boss = FindAnyObjectByType<BossHealth>();
        Vector3 fallbackPos = transform.position + transform.forward * 15f;

        // Spawn red orbital laser beam + ground ring
        GameObject beacon = CreateOrbitalBeacon("AirStrikeBeacon", GetRedLaserMaterial(), airStrikeExplosionRadius);

        float elapsed = 0f;
        while (elapsed < airStrikeDelay)
        {
            elapsed += Time.deltaTime;
            Vector3 targetPos = (boss != null && !boss.IsDead) ? boss.transform.position : fallbackPos;
            beacon.transform.position = targetPos;
            yield return null;
        }

        Vector3 strikePos = (boss != null && !boss.IsDead) ? boss.transform.position : fallbackPos;
        Destroy(beacon);

        // Big glowing explosion sphere on the monster
        SpawnExplosionSphere(strikePos + Vector3.up * 1.5f, airStrikeExplosionRadius);

        // Force damage onto the Boss
        if (boss != null && !boss.IsDead)
        {
            boss.RegisterDamage(airStrikeDamage, BossWeakpoint.BodyPart.ArmoredBody, strikePos + Vector3.up * 1.5f);
        }
        else
        {
            // Fallback AOE if boss wasn't cached or multiple damageables exist
            Collider[] hits = Physics.OverlapSphere(strikePos, airStrikeExplosionRadius);
            HashSet<BossHealth> damaged = new HashSet<BossHealth>();
            foreach (Collider c in hits)
            {
                BossHealth bh = c.GetComponentInParent<BossHealth>();
                if (bh != null && !bh.IsDead && damaged.Add(bh))
                {
                    bh.RegisterDamage(airStrikeDamage, BossWeakpoint.BodyPart.ArmoredBody, strikePos);
                }
            }
        }
    }

    private void SpawnExplosionSphere(Vector3 center, float radius)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "AirStrikeExplosionBall";
        sphere.transform.position = center;
        sphere.transform.localScale = Vector3.one * 0.5f;

        Collider col = sphere.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Renderer r = sphere.GetComponent<Renderer>();
        if (r != null) r.sharedMaterial = GetExplosionMaterial();

        StratagemExplosionVFX vfx = sphere.AddComponent<StratagemExplosionVFX>();
        vfx.Initialize(radius * 2f, 0.75f);
    }

    // -----------------------------------------------------------------
    //  Stratagem 2: Smoke Orbital (smoke carpet hides player & turns Boss to Roaming)
    // -----------------------------------------------------------------
    private void TriggerSmokeOrbital()
    {
        _smokeCdTimer = smokeCooldown;
        StartCoroutine(ExecuteSmokeOrbitalRoutine(transform.position));
    }

    private IEnumerator ExecuteSmokeOrbitalRoutine(Vector3 dropPos)
    {
        GameObject beacon = CreateOrbitalBeacon("SmokeBeacon", GetBlueLaserMaterial(), smokeRadius * 0.5f);
        beacon.transform.position = dropPos;

        yield return new WaitForSeconds(smokeCallDelay);
        Destroy(beacon);

        // Build visual smoke carpet (cluster of soft translucent smoke puffs + rising particles)
        GameObject smokeRoot = BuildSmokeCarpetVisual(dropPos, smokeRadius);

        SmokeZoneRuntime zone = new SmokeZoneRuntime
        {
            center = dropPos,
            radius = smokeRadius,
            remaining = smokeDuration,
            visualRoot = smokeRoot
        };
        _activeSmokeZones.Add(zone);
    }

    private void UpdateSmokeZones()
    {
        float maxRemaining = 0f;
        for (int i = _activeSmokeZones.Count - 1; i >= 0; i--)
        {
            SmokeZoneRuntime z = _activeSmokeZones[i];
            z.remaining -= Time.deltaTime;

            // Gentle scale-down fade during the last 1.2 seconds
            if (z.visualRoot != null && z.remaining < 1.2f && z.remaining > 0f)
            {
                float t = Mathf.Clamp01(z.remaining / 1.2f);
                z.visualRoot.transform.localScale = Vector3.one * Mathf.Lerp(0.15f, 1f, t);
            }

            if (z.remaining <= 0f)
            {
                if (z.visualRoot != null) Destroy(z.visualRoot);
                _activeSmokeZones.RemoveAt(i);
            }
            else if (z.remaining > maxRemaining)
            {
                maxRemaining = z.remaining;
            }
        }

        _smokeActiveTimer = maxRemaining;

        // Notify Boss AI whenever player concealed state changes
        bool concealedNow = IsPlayerInsideAnySmoke();
        if (concealedNow != _lastConcealedState)
        {
            _lastConcealedState = concealedNow;
            WrathBossAI[] bosses = FindObjectsByType<WrathBossAI>(FindObjectsSortMode.None);
            foreach (WrathBossAI boss in bosses)
            {
                boss.SetPlayerHidden(concealedNow);
            }
        }
    }

    private bool IsPlayerInsideAnySmoke()
    {
        Vector3 p = transform.position;
        for (int i = 0; i < _activeSmokeZones.Count; i++)
        {
            SmokeZoneRuntime z = _activeSmokeZones[i];
            float dx = p.x - z.center.x;
            float dz = p.z - z.center.z;
            if ((dx * dx + dz * dz) <= (z.radius * z.radius))
            {
                return true;
            }
        }
        return false;
    }

    private GameObject BuildSmokeCarpetVisual(Vector3 center, float radius)
    {
        GameObject root = new GameObject("SmokeOrbitalCarpet");
        root.transform.position = center;

        Material smokeMat = GetSmokeMaterial();

        // Central puff + ring of overlapping smoke puffs
        int puffCount = 10;
        for (int i = 0; i < puffCount; i++)
        {
            GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            puff.name = $"SmokePuff_{i}";
            puff.transform.SetParent(root.transform, false);

            Collider c = puff.GetComponent<Collider>();
            if (c != null) c.enabled = false;

            if (i == 0)
            {
                puff.transform.localPosition = Vector3.up * 1.6f;
                puff.transform.localScale = new Vector3(radius * 0.9f, 3.6f, radius * 0.9f);
            }
            else
            {
                float angle = (i - 1) * (360f / (puffCount - 1)) * Mathf.Deg2Rad;
                float dist = radius * 0.55f;
                puff.transform.localPosition = new Vector3(Mathf.Cos(angle) * dist, 1.3f, Mathf.Sin(angle) * dist);
                float puffSize = radius * 0.65f;
                puff.transform.localScale = new Vector3(puffSize, 3.0f, puffSize);
            }

            Renderer r = puff.GetComponent<Renderer>();
            if (r != null)
            {
                r.sharedMaterial = smokeMat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        return root;
    }

    // -----------------------------------------------------------------
    //  Shared Beacon & Cached Materials
    // -----------------------------------------------------------------
    private GameObject CreateOrbitalBeacon(string objName, Material mat, float ringRadius)
    {
        GameObject root = new GameObject(objName);

        // Tall vertical sky beam
        GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beam.transform.SetParent(root.transform, false);
        beam.transform.localPosition = Vector3.up * 25f;
        beam.transform.localScale = new Vector3(0.35f, 25f, 0.35f);
        Collider bc = beam.GetComponent<Collider>();
        if (bc != null) bc.enabled = false;
        Renderer br = beam.GetComponent<Renderer>();
        if (br != null)
        {
            br.sharedMaterial = mat;
            br.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // Flat ground marker disc
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.transform.SetParent(root.transform, false);
        disc.transform.localPosition = Vector3.up * 0.1f;
        disc.transform.localScale = new Vector3(ringRadius * 2f, 0.04f, ringRadius * 2f);
        Collider dc = disc.GetComponent<Collider>();
        if (dc != null) dc.enabled = false;
        Renderer dr = disc.GetComponent<Renderer>();
        if (dr != null)
        {
            dr.sharedMaterial = mat;
            dr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        return root;
    }

    private static Material CreateTranslucentMaterial(Color color)
    {
        Shader shader = Shader.Find("Sprites/Default")
                     ?? Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Unlit/Color");
        Material mat = new Material(shader);
        mat.color = color;
        return mat;
    }

    private static Material GetRedLaserMaterial() =>
        _redLaserMat != null ? _redLaserMat : (_redLaserMat = CreateTranslucentMaterial(new Color(1f, 0.15f, 0.1f, 0.45f)));

    private static Material GetExplosionMaterial() =>
        _explosionMat != null ? _explosionMat : (_explosionMat = CreateTranslucentMaterial(new Color(1f, 0.4f, 0.05f, 0.85f)));

    private static Material GetBlueLaserMaterial() =>
        _blueLaserMat != null ? _blueLaserMat : (_blueLaserMat = CreateTranslucentMaterial(new Color(0.25f, 0.7f, 1f, 0.45f)));

    private static Material GetSmokeMaterial() =>
        _smokeMat != null ? _smokeMat : (_smokeMat = CreateTranslucentMaterial(new Color(0.75f, 0.78f, 0.82f, 0.42f)));

    private class SmokeZoneRuntime
    {
        public Vector3 center;
        public float radius;
        public float remaining;
        public GameObject visualRoot;
    }
}

/// <summary>
/// Expands the Air Strike explosion sphere rapidly and destroys it cleanly.
/// </summary>
public class StratagemExplosionVFX : MonoBehaviour
{
    private float _targetDiameter;
    private float _duration;
    private float _timer;

    public void Initialize(float targetDiameter, float duration)
    {
        _targetDiameter = targetDiameter;
        _duration = duration;
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        float progress = Mathf.Clamp01(_timer / _duration);

        // Quick punchy expansion (0..0.35), then collapse away (0.35..1.0)
        float scale;
        if (progress < 0.35f)
        {
            scale = Mathf.Lerp(0.5f, _targetDiameter, progress / 0.35f);
        }
        else
        {
            scale = Mathf.Lerp(_targetDiameter, 0.05f, (progress - 0.35f) / 0.65f);
        }

        transform.localScale = Vector3.one * scale;

        if (_timer >= _duration)
        {
            Destroy(gameObject);
        }
    }
}
