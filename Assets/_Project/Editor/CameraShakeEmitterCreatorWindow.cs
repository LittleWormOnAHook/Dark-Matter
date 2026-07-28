using Project.CameraFx;
using Project.EditorTools;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Authors camera-shake emitter prefabs for explosions, continuous rumble, pulse, and impacts.
/// Wire later from weather, grenades, weapons, and exposure zones.
/// </summary>
public class CameraShakeEmitterCreatorWindow : EditorWindow
{
    private const string DefaultFolder = ProjectAssetPaths.PrefabsEnvironmentCameraShake;

    private string emitterName = "CameraShake_Custom";
    private CameraShakePattern pattern = CameraShakePattern.OneShot;
    private CameraShakeEmitterMode mode = CameraShakeEmitterMode.Manual;
    private float trauma = 0.55f;
    private float radius = 40f;
    private float cooldownSeconds = 0.35f;
    private float pulseIntervalSeconds = 1.25f;
    private float pulseTraumaScale = 1f;
    private bool useProximityFalloff = true;
    private float proximityFalloffPower = 2f;
    private bool addTriggerCollider;
    private float triggerRadius = 6f;
    private AudioClip playClip;
    private float volume = 0.85f;
    private bool loopAudioWhileContinuous;
    private bool scaleBySfxSetting = true;
    private string outputFolder = DefaultFolder;
    private Vector2 scroll;

    [MenuItem(SurvivalPioneerEditorMenus.CameraShakeEmitterCreator, false, 55)]
    public static void ShowWindow()
    {
        GetWindow<CameraShakeEmitterCreatorWindow>("Camera Shake Emitter").minSize = new Vector2(420, 520);
    }

    [MenuItem(SurvivalPioneerEditorMenus.CameraShakeEmitterCreateAllPresets, false, 56)]
    public static void CreateAllPresetsMenu()
    {
        int count = CameraShakeEmitterPrefabBuilder.CreateAllPresetPrefabs(DefaultFolder);
        EditorUtility.DisplayDialog(
            "Camera Shake Emitters",
            $"Created/updated {count} emitter prefabs under:\n{DefaultFolder}",
            "OK");
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        GUILayout.Label("Camera Shake Emitter Prefab", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "One-shot = explosions/impacts. Continuous = rumble/vibration. Pulse = repeating bursts. " +
            "Proximity falloff strengthens shake nearer the emitter (or exposure-zone anchor).",
            MessageType.Info);

        emitterName = EditorGUILayout.TextField("Prefab Name", emitterName);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);

        GUILayout.Space(8);
        GUILayout.Label("Behavior", EditorStyles.boldLabel);
        pattern = (CameraShakePattern)EditorGUILayout.EnumPopup("Pattern", pattern);
        mode = (CameraShakeEmitterMode)EditorGUILayout.EnumPopup("Activation Mode", mode);
        trauma = EditorGUILayout.Slider("Trauma", trauma, 0f, 1f);
        radius = EditorGUILayout.FloatField("Falloff Radius", radius);
        cooldownSeconds = EditorGUILayout.FloatField("One-Shot Cooldown", cooldownSeconds);

        using (new EditorGUI.DisabledScope(pattern != CameraShakePattern.Pulse))
        {
            pulseIntervalSeconds = EditorGUILayout.FloatField("Pulse Interval", pulseIntervalSeconds);
            pulseTraumaScale = EditorGUILayout.Slider("Pulse Trauma Scale", pulseTraumaScale, 0f, 1f);
        }

        GUILayout.Space(8);
        GUILayout.Label("Proximity", EditorStyles.boldLabel);
        useProximityFalloff = EditorGUILayout.Toggle("Proximity Falloff", useProximityFalloff);
        proximityFalloffPower = EditorGUILayout.Slider("Falloff Power", proximityFalloffPower, 0.1f, 4f);

        GUILayout.Space(8);
        GUILayout.Label("Trigger (optional)", EditorStyles.boldLabel);
        addTriggerCollider = EditorGUILayout.Toggle(
            new GUIContent("Add Sphere Trigger", "Needed for OnTriggerEnter / WhileInsideTrigger."),
            addTriggerCollider);
        using (new EditorGUI.DisabledScope(!addTriggerCollider))
            triggerRadius = EditorGUILayout.FloatField("Trigger Radius", triggerRadius);

        GUILayout.Space(8);
        GUILayout.Label("Audio", EditorStyles.boldLabel);
        playClip = (AudioClip)EditorGUILayout.ObjectField("Clip", playClip, typeof(AudioClip), false);
        volume = EditorGUILayout.Slider("Volume", volume, 0f, 1f);
        scaleBySfxSetting = EditorGUILayout.Toggle("Scale By SFX Settings", scaleBySfxSetting);
        loopAudioWhileContinuous = EditorGUILayout.Toggle("Loop Audio (Continuous)", loopAudioWhileContinuous);

        GUILayout.Space(12);
        if (GUILayout.Button("Create / Overwrite Prefab", GUILayout.Height(32)))
            CreateSingle();

        GUILayout.Space(6);
        if (GUILayout.Button("Create All Built-In Presets"))
            CreateAllPresetsMenu();

        EditorGUILayout.EndScrollView();
    }

    private void CreateSingle()
    {
        if (string.IsNullOrWhiteSpace(emitterName))
        {
            EditorUtility.DisplayDialog("Camera Shake Emitter", "Enter a prefab name.", "OK");
            return;
        }

        var request = new CameraShakeEmitterPrefabBuilder.Request
        {
            Name = emitterName.Trim(),
            Pattern = pattern,
            Mode = mode,
            Trauma = trauma,
            Radius = radius,
            CooldownSeconds = cooldownSeconds,
            PulseIntervalSeconds = pulseIntervalSeconds,
            PulseTraumaScale = pulseTraumaScale,
            UseProximityFalloff = useProximityFalloff,
            ProximityFalloffPower = proximityFalloffPower,
            AddTriggerCollider = addTriggerCollider ||
                                 mode == CameraShakeEmitterMode.OnTriggerEnter ||
                                 mode == CameraShakeEmitterMode.WhileInsideTrigger,
            TriggerRadius = triggerRadius,
            Clip = playClip,
            Volume = volume,
            LoopAudioWhileContinuous = loopAudioWhileContinuous,
            ScaleBySfxSetting = scaleBySfxSetting
        };

        string path = CameraShakeEmitterPrefabBuilder.CreatePrefab(outputFolder, request);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null)
        {
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }

        Debug.Log($"[CameraShakeEmitterCreator] Saved {path}");
    }
}
