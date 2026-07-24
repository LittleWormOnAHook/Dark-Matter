using Project.CameraFx;
using Project.Core;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Shared builder used by the Camera Shake Emitter creator window and batch preset menu.
/// </summary>
public static class CameraShakeEmitterPrefabBuilder
{
    public struct Request
    {
        public string Name;
        public CameraShakePattern Pattern;
        public CameraShakeEmitterMode Mode;
        public float Trauma;
        public float Radius;
        public float CooldownSeconds;
        public float PulseIntervalSeconds;
        public float PulseTraumaScale;
        public bool UseProximityFalloff;
        public float ProximityFalloffPower;
        public bool AddTriggerCollider;
        public float TriggerRadius;
        public AudioClip Clip;
        public float Volume;
        public bool LoopAudioWhileContinuous;
        public bool ScaleBySfxSetting;
    }

    public static int CreateAllPresetPrefabs(string folder)
    {
        EnsureFolder(folder);

        AudioClip explode = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Invector-3rdPersonController/Basic Locomotion/Audio/Others/flashbang-explode.ogg");
        AudioClip rumble = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/_Project/Audio/Rumble Fire.mp3");
        AudioClip impact = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Audio/Others/Stone Impact.wav");
        AudioClip vehicleImpact = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Audio/Vehicles/vehicleImpact.wav");

        Request[] presets =
        {
            Preset("CameraShake_Explosion_Small", CameraShakePattern.OneShot, CameraShakeEmitterMode.OnEnable,
                0.4f, 22f, explode, 0.7f, false, false, 0f),
            Preset("CameraShake_Explosion_Medium", CameraShakePattern.OneShot, CameraShakeEmitterMode.OnEnable,
                0.65f, 40f, explode, 0.85f, false, false, 0f),
            Preset("CameraShake_Explosion_Large", CameraShakePattern.OneShot, CameraShakeEmitterMode.OnEnable,
                0.9f, 70f, explode, 1f, false, false, 0f),
            Preset("CameraShake_Impact_Light", CameraShakePattern.OneShot, CameraShakeEmitterMode.Manual,
                0.25f, 12f, impact, 0.55f, false, false, 0f),
            Preset("CameraShake_Impact_Heavy", CameraShakePattern.OneShot, CameraShakeEmitterMode.Manual,
                0.5f, 18f, vehicleImpact != null ? vehicleImpact : impact, 0.8f, false, false, 0f),
            Preset("CameraShake_Grenade", CameraShakePattern.OneShot, CameraShakeEmitterMode.OnEnable,
                0.7f, 35f, explode, 0.9f, false, false, 0f),
            Preset("CameraShake_Continuous_Rumble", CameraShakePattern.Continuous, CameraShakeEmitterMode.Manual,
                0.22f, 35f, rumble, 0.45f, true, false, 0f),
            Preset("CameraShake_Continuous_Quake", CameraShakePattern.Continuous, CameraShakeEmitterMode.WhileInsideTrigger,
                0.35f, 45f, rumble, 0.6f, true, true, 12f),
            Preset("CameraShake_Pulse_Aftershock", CameraShakePattern.Pulse, CameraShakeEmitterMode.Manual,
                0.4f, 40f, explode, 0.65f, false, false, 0f, 1.6f),
            Preset("CameraShake_Pulse_Noise", CameraShakePattern.Pulse, CameraShakeEmitterMode.WhileInsideTrigger,
                0.28f, 30f, rumble, 0.5f, true, true, 10f, 0.85f),
            Preset("CameraShake_Environmental_Storm", CameraShakePattern.Continuous, CameraShakeEmitterMode.Manual,
                0.18f, 80f, rumble, 0.4f, true, false, 0f),
            Preset("CameraShake_ExposureZone_Hook", CameraShakePattern.Continuous, CameraShakeEmitterMode.Manual,
                0.28f, 25f, rumble, 0.55f, true, false, 0f),
        };

        int count = 0;
        for (int i = 0; i < presets.Length; i++)
        {
            CreatePrefab(folder, presets[i]);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return count;
    }

    public static string CreatePrefab(string folder, Request request)
    {
        EnsureFolder(folder);

        string safeName = string.IsNullOrWhiteSpace(request.Name) ? "CameraShake_Emitter" : request.Name.Trim();
        string path = $"{folder}/{safeName}.prefab";

        var go = new GameObject(safeName);
        var emitter = go.AddComponent<CameraShakeEmitter>();
        var audio = go.GetComponent<AudioSource>();
        if (audio == null)
            audio = go.AddComponent<AudioSource>();

        audio.playOnAwake = false;
        audio.loop = false;
        audio.volume = 1f;
        GameplayAudioUtility.ConfigureWorldSpatialSource(
            audio,
            minDistance: 4f,
            maxDistance: Mathf.Max(25f, request.Radius));

        if (request.AddTriggerCollider)
        {
            var sphere = go.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = Mathf.Max(0.5f, request.TriggerRadius);
        }

        var so = new SerializedObject(emitter);
        so.FindProperty("mode").enumValueIndex = (int)request.Mode;
        so.FindProperty("pattern").enumValueIndex = (int)request.Pattern;
        so.FindProperty("trauma").floatValue = Mathf.Clamp01(request.Trauma);
        so.FindProperty("radius").floatValue = Mathf.Max(0f, request.Radius);
        so.FindProperty("cooldownSeconds").floatValue = Mathf.Max(0f, request.CooldownSeconds);
        so.FindProperty("pulseIntervalSeconds").floatValue = Mathf.Max(0.05f, request.PulseIntervalSeconds);
        so.FindProperty("pulseTraumaScale").floatValue = Mathf.Clamp01(request.PulseTraumaScale);
        so.FindProperty("useProximityFalloff").boolValue = request.UseProximityFalloff;
        so.FindProperty("proximityFalloffPower").floatValue = Mathf.Clamp(request.ProximityFalloffPower, 0.1f, 4f);
        so.FindProperty("playClip").objectReferenceValue = request.Clip;
        so.FindProperty("audioSource").objectReferenceValue = audio;
        so.FindProperty("volume").floatValue = Mathf.Clamp01(request.Volume);
        so.FindProperty("scaleBySfxSetting").boolValue = request.ScaleBySfxSetting;
        so.FindProperty("loopAudioWhileContinuous").boolValue = request.LoopAudioWhileContinuous;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        UnityEngine.Object.DestroyImmediate(go);
        return path;
    }

    private static Request Preset(
        string name,
        CameraShakePattern pattern,
        CameraShakeEmitterMode mode,
        float trauma,
        float radius,
        AudioClip clip,
        float volume,
        bool loopContinuous,
        bool addTrigger,
        float triggerRadius,
        float pulseInterval = 1.25f)
    {
        return new Request
        {
            Name = name,
            Pattern = pattern,
            Mode = mode,
            Trauma = trauma,
            Radius = radius,
            CooldownSeconds = 0.35f,
            PulseIntervalSeconds = pulseInterval,
            PulseTraumaScale = 1f,
            UseProximityFalloff = true,
            ProximityFalloffPower = 2f,
            AddTriggerCollider = addTrigger ||
                                 mode == CameraShakeEmitterMode.OnTriggerEnter ||
                                 mode == CameraShakeEmitterMode.WhileInsideTrigger,
            TriggerRadius = triggerRadius > 0f ? triggerRadius : Mathf.Max(4f, radius * 0.25f),
            Clip = clip,
            Volume = volume,
            LoopAudioWhileContinuous = loopContinuous,
            ScaleBySfxSetting = true
        };
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
