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

    [Header("Stratagem Sounds")]
    public string stratagemInputSoundName = "StratagemInput";
    public string airSupportSoundName = "AirSupport";
    public string orbitalStrikeSoundName = "OrbitalStrike";
    public string airSupportExplosionSoundName = "AirSupportExplosion";
    public string smokeLandedSoundName = "SmokeLanded";

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
    private static Texture2D _softCircleTex;

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
        PlayStratagemSound(stratagemInputSoundName);

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

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound2D(airSupportSoundName);
            // Play the jet fighter fly-by when air support is called.
        }

        StartCoroutine(ExecuteAirStrikeRoutine());
    }

    private IEnumerator ExecuteAirStrikeRoutine()
    {
        BossHealth boss = FindAnyObjectByType<BossHealth>();
        WrathBossAI bossAI = boss != null ? boss.GetComponent<WrathBossAI>() : null;
        Vector3 fallbackPos = SnapToGround(transform.position + transform.forward * 15f);

        // Spawn red orbital laser beam + ground ring
        GameObject beacon = CreateOrbitalBeacon("AirStrikeBeacon", GetRedLaserMaterial(), airStrikeExplosionRadius);

        float elapsed = 0f;
        while (elapsed < airStrikeDelay)
        {
            elapsed += Time.deltaTime;
            Vector3 targetPos = (boss != null && !boss.IsDead)
                ? SnapToGround(bossAI != null ? bossAI.GetFeetPosition() : boss.transform.position)
                : fallbackPos;
            beacon.transform.position = targetPos;
            yield return null;
        }

        Vector3 strikePos = (boss != null && !boss.IsDead)
            ? SnapToGround(bossAI != null ? bossAI.GetFeetPosition() : boss.transform.position)
            : fallbackPos;
        Destroy(beacon);

        PlayStratagemSound3D(
            airSupportExplosionSoundName,
            strikePos);

// Big glowing explosion sphere on the monster
SpawnExplosionSphere(
    strikePos + Vector3.up * 2.0f,
    airStrikeExplosionRadius);

CameraShake cameraShake = FindAnyObjectByType<CameraShake>();
if (cameraShake != null)
{
    cameraShake.Shake(2f, 0.8f);
}

        // Force damage onto the Boss
        if (boss != null && !boss.IsDead)
        {
            boss.RegisterDamage(airStrikeDamage, BossWeakpoint.BodyPart.ArmoredBody, strikePos + Vector3.up * 2.0f);
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

        PlayStratagemSound3D(
            airSupportExplosionSoundName,
            strikePos);
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
        StartCoroutine(ExecuteSmokeOrbitalRoutine(SnapToGround(transform.position)));
    }

    private IEnumerator ExecuteSmokeOrbitalRoutine(Vector3 dropPos)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySound2D(orbitalStrikeSoundName);
            
        }
        GameObject beacon = CreateOrbitalBeacon("SmokeBeacon", GetBlueLaserMaterial(), smokeRadius * 0.5f);
        beacon.transform.position = dropPos;

        yield return new WaitForSeconds(smokeCallDelay);
        Destroy(beacon);

        PlayStratagemSound3D(
            smokeLandedSoundName,
            dropPos);

// Build swirling ParticleSystem smoke carpet
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
        root.transform.position = center + Vector3.up * 0.4f;

        ParticleSystem ps = root.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = smokeDuration;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 3.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(radius * 0.35f, radius * 0.6f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.65f, 0.70f, 0.75f, 0.28f),
            new Color(0.82f, 0.86f, 0.90f, 0.38f));
        main.maxParticles = 120;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 28f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 35) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius * 0.85f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var colLife = ps.colorOverLifetime;
        colLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.75f, 0.78f, 0.82f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.45f, 0.2f), new GradientAlphaKey(0.35f, 0.75f), new GradientAlphaKey(0f, 1f) }
        );
        colLife.color = grad;

        var renderer = root.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = GetSmokeMaterial();

        ps.Play();
        return root;
    }

    private static Vector3 SnapToGround(Vector3 worldPos)
    {
        Vector3 rayStart = new Vector3(worldPos.x, worldPos.y + 8f, worldPos.z);
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 30f, ~0, QueryTriggerInteraction.Ignore);

        float bestY = float.NegativeInfinity;
        bool foundGround = false;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;
            if (col.GetComponentInParent<BossHealth>() != null) continue;
            if (col.GetComponentInParent<PlayerMovement>() != null) continue;

            if (hits[i].point.y > bestY)
            {
                bestY = hits[i].point.y;
                foundGround = true;
            }
        }

        if (foundGround) worldPos.y = bestY;
        return worldPos;
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

    private static Material GetSmokeMaterial()
    {
        if (_smokeMat != null) return _smokeMat;
        _smokeMat = CreateTranslucentMaterial(new Color(0.85f, 0.88f, 0.92f, 0.85f));
        _smokeMat.mainTexture = GetSoftCircleTexture();
        return _smokeMat;
    }

    private static Texture2D GetSoftCircleTexture()
    {
        if (_softCircleTex != null) return _softCircleTex;

        const int size = 64;
        _softCircleTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float half = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - half + 0.5f) / half;
                float dy = (y - half + 0.5f) / half;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = alpha * alpha; // Smooth soft smoke falloff
                _softCircleTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        _softCircleTex.Apply();
        return _softCircleTex;
    }

    private class SmokeZoneRuntime
    {
        public Vector3 center;
        public float radius;
        public float remaining;
        public GameObject visualRoot;
    }

    private void PlayStratagemSound(string soundName)
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning(
                "Stratagem sound failed: SoundManager.Instance is missing.",
                this);

            return;
        }

        SoundManager.Instance.PlaySound2D(soundName);
    }

    private void PlayStratagemSound3D(string soundName, Vector3 position)
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning(
                "Stratagem 3D sound failed: SoundManager.Instance is missing.",
                this);

            return;
        }

        SoundManager.Instance.PlaySound3D(soundName, position);
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
