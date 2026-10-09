using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Builds the procedural arena of the test scene: Pilar, floor, central pit, gravity zone, and lights.
/// </summary>
public static class TestSceneArenaBuilder
{
    private const int TurretPointCount = 4;
    // Fuera del pozo (radio 5): local 1.6 => mundo 6.4, y -0.45 => mundo 1.1, visible por encima del pozo.
    private const float TurretPointLocalRadius = 1.6f;
    private const float TurretPointLocalHeight = -0.45f;
    private const float PilarLightHeight = 3f;
    private const float PilarLightRangeMeters = 30f;
    private const float PilarLightIntensity = 2f;
    private const float SunIntensity = 1f;
    private const float PitKillRadiusMeters = 5f;
    // Mata si y <= pozo.y + 1.5: permite caminar sobre el borde sin morir, solo al caer.
    private const float PitKillHeightMeters = 1.5f;
    private const float PitEmissionIntensity = 0.3f;
    private const float GravityZoneColliderRadius = 0.5f;
    private const float GravityZoneLiftForce = 18f;
    private const float GravityZoneEffectRadiusMeters = 5f;
    private const float StandardTransparentMode = 3f;
    private const int TransparentRenderQueue = 3000;
    private const int FloatingParticleCount = 15;
    private const float FloatingParticleSpreadRadius = 0.4f;
    private const float FloatingParticleMinimumSize = 0.08f;
    private const float FloatingParticleMaximumSize = 0.18f;
    private const string BaseColorProperty = "_BaseColor";
    private const string ColorProperty = "_Color";
    private const string EmissionColorProperty = "_EmissionColor";
    private const string ModeProperty = "_Mode";
    private const string SourceBlendProperty = "_SrcBlend";
    private const string DestinationBlendProperty = "_DstBlend";
    private const string DepthWriteProperty = "_ZWrite";
    private const string AlphaTestKeyword = "_ALPHATEST_ON";
    private const string AlphaBlendKeyword = "_ALPHABLEND_ON";
    private static readonly Vector3 PilarPosition = new Vector3(0f, 2f, 0f);
    private static readonly Vector3 PilarScale = new Vector3(4f, 2f, 4f);
    // 100 x 100 unidades.
    private static readonly Vector3 FloorScale = new Vector3(10f, 1f, 10f);
    private static readonly Color DefaultFloorColor = new Color(0.55f, 0.55f, 0.55f, 1f);
    // Anillo alrededor del Pilar, ligeramente hundido pero con borde visible: hitbox = visual (radio 5).
    private static readonly Vector3 PitPosition = new Vector3(0f, -0.2f, 0f);
    private static readonly Vector3 PitScale = new Vector3(10f, 0.5f, 10f);
    private static readonly Vector3 GravityZonePosition = new Vector3(8f, 0.5f, 0f);
    private static readonly Vector3 GravityZoneScale = new Vector3(10f, 4f, 10f);
    private static readonly Color GravityZoneColor = new Color(0.6f, 0.1f, 1f, 0.35f);
    private static readonly Color FloatingParticleColor = new Color(0.8f, 0.4f, 1f, 0.9f);
    private static readonly Quaternion SunRotation = Quaternion.Euler(50f, -30f, 0f);

    /// <summary>Creates the Pilar with its emergency turret points and its ambient light.</summary>
    public static Pilar CreatePilar(Material material)
    {
        GameObject pilarObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pilarObject.name = "Pilar";
        pilarObject.transform.position = PilarPosition;
        pilarObject.transform.localScale = PilarScale;
        var pilar = pilarObject.AddComponent<Pilar>();
        if (material != null) pilarObject.GetComponent<Renderer>().material = material;

        var turretPoints = new Transform[TurretPointCount];
        for (int i = 0; i < TurretPointCount; i++)
        {
            var point = new GameObject($"PuntoTorreta_{i}");
            point.transform.SetParent(pilarObject.transform);
            float angle = (i / (float)TurretPointCount) * Mathf.PI * 2f;
            point.transform.localPosition = new Vector3(
                Mathf.Cos(angle) * TurretPointLocalRadius,
                TurretPointLocalHeight,
                Mathf.Sin(angle) * TurretPointLocalRadius);
            turretPoints[i] = point.transform;
        }

        pilar.puntosTorretas = turretPoints;

        var lightObject = new GameObject("LuzPilar");
        lightObject.transform.SetParent(pilarObject.transform);
        lightObject.transform.localPosition = Vector3.up * PilarLightHeight;
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = PilarLightRangeMeters;
        light.intensity = PilarLightIntensity;
        light.color = Color.cyan;
        return pilar;
    }

    /// <summary>Creates the arena floor, gray when no material is supplied.</summary>
    public static GameObject CreateFloor(Material material)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Arena";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = FloorScale;
        Renderer renderer = floor.GetComponent<Renderer>();
        if (material != null)
        {
            renderer.material = material;
        }
        else if (renderer != null)
        {
            Material floorMaterial = renderer.material;
            if (floorMaterial.HasProperty(BaseColorProperty)) floorMaterial.SetColor(BaseColorProperty, DefaultFloorColor);
            if (floorMaterial.HasProperty(ColorProperty)) floorMaterial.SetColor(ColorProperty, DefaultFloorColor);
            floorMaterial.color = DefaultFloorColor;
        }

        return floor;
    }

    /// <summary>Creates the inactive central pit with its lethal <see cref="PozoKill"/> trigger.</summary>
    public static GameObject CreatePit()
    {
        GameObject pit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pit.name = "PozoCentral";
        // Collider de caída: un BoxCollider trigger es más robusto que el del cilindro.
        Object.Destroy(pit.GetComponent<Collider>());
        var trigger = pit.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = Vector3.one;
        trigger.center = Vector3.zero;
        var pitKill = pit.AddComponent<PozoKill>();
        pitKill.radioMortal = PitKillRadiusMeters;
        pitKill.alturaMortal = PitKillHeightMeters;
        if (!pit.TryGetComponent(out Rigidbody body)) body = pit.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        pit.transform.position = PitPosition;
        pit.transform.localScale = PitScale;

        Material pitMaterial = pit.GetComponent<Renderer>().material;
        pitMaterial.color = Color.black;
        // Borde emisivo para que se vea.
        if (pitMaterial.HasProperty(EmissionColorProperty)) pitMaterial.SetColor(EmissionColorProperty, Color.red * PitEmissionIntensity);
        pit.SetActive(false);
        return pit;
    }

    /// <summary>Creates the inactive altered-gravity zone with its trigger, effect, and floating particles.</summary>
    public static GameObject CreateGravityZone()
    {
        GameObject zone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        zone.name = "ZonaGravedad";
        var collider = zone.GetComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = GravityZoneColliderRadius;
        zone.transform.position = GravityZonePosition;
        // Más grande y achatada (exagerado).
        zone.transform.localScale = GravityZoneScale;
        Renderer renderer = zone.GetComponent<Renderer>();
        if (renderer != null) renderer.material = CreateTransparentMaterial(GravityZoneColor);

        var effect = zone.AddComponent<ZonaGravedadEffect>();
        effect.fuerzaAscenso = GravityZoneLiftForce;
        effect.radioEfecto = GravityZoneEffectRadiusMeters;
        AddFloatingParticles(zone.transform);
        zone.SetActive(false);
        return zone;
    }

    /// <summary>Creates the directional sun light.</summary>
    public static void CreateSunLight()
    {
        var sunObject = new GameObject("DirectionalLight");
        var sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = SunIntensity;
        sunObject.transform.rotation = SunRotation;
    }

    private static Material CreateTransparentMaterial(Color color)
    {
        var material = new Material(
            Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default"));
        if (material.HasProperty(ColorProperty)) material.color = color;
        else if (material.HasProperty(BaseColorProperty)) material.SetColor(BaseColorProperty, color);
        if (material.HasProperty(ModeProperty)) material.SetFloat(ModeProperty, StandardTransparentMode);
        if (material.HasProperty(SourceBlendProperty)) material.SetInt(SourceBlendProperty, (int)BlendMode.SrcAlpha);
        if (material.HasProperty(DestinationBlendProperty)) material.SetInt(DestinationBlendProperty, (int)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty(DepthWriteProperty)) material.SetInt(DepthWriteProperty, 0);
        material.DisableKeyword(AlphaTestKeyword);
        material.EnableKeyword(AlphaBlendKeyword);
        material.renderQueue = TransparentRenderQueue;
        return material;
    }

    private static void AddFloatingParticles(Transform zone)
    {
        for (int i = 0; i < FloatingParticleCount; i++)
        {
            GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            particle.name = "ParticulaFlotante";
            Object.Destroy(particle.GetComponent<Collider>());
            particle.transform.SetParent(zone);
            particle.transform.localPosition = Random.insideUnitSphere * FloatingParticleSpreadRadius;
            particle.transform.localScale = Vector3.one * Random.Range(FloatingParticleMinimumSize, FloatingParticleMaximumSize);
            particle.GetComponent<Renderer>().material.color = FloatingParticleColor;
            particle.AddComponent<ParticulaFlotante>();
        }
    }
}
