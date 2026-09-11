using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Map
{
    /// <summary>
    /// Play-mode keyboard nudge for map calibration profile (saved on Play exit).
    /// Arrow keys = UV offset, Alt+arrows = map-zero anchor, Q/E = north offset, Page Up/Down = UV scale.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(200)]
    public sealed class DMWorldMapCalibrationRuntime : MonoBehaviour
    {
        [SerializeField] private DMWorldMapCalibrationProfile profile;
        [SerializeField] private WorldMapProvider mapProvider;
        [SerializeField] private float uvStep = 0.001f;
        [SerializeField] private float uvStepFine = 0.0002f;
        [SerializeField] private float uvStepCoarse = 0.005f;
        [SerializeField] private float northStepDegrees = 1f;
        [SerializeField] private bool showOnScreenHelp = true;

        private void Awake()
        {
            if (mapProvider == null)
                mapProvider = GetComponent<WorldMapProvider>() ?? WorldMapProvider.Instance;

            if (profile == null && mapProvider != null)
                profile = mapProvider.CalibrationProfile;

            if (profile == null)
                profile = Resources.Load<DMWorldMapCalibrationProfile>(DMWorldMapCalibrationProfile.ResourcesPath);
        }

        private void Update()
        {
            if (profile == null || !profile.enableRuntimeTuning || !Application.isPlaying)
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            bool shift = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            bool ctrl = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
            bool alt = keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed;
            float step = ctrl ? uvStepCoarse : shift ? uvStepFine : uvStep;

            Vector2 uvDelta = Vector2.zero;
            if (keyboard.leftArrowKey.wasPressedThisFrame) uvDelta.x -= step;
            if (keyboard.rightArrowKey.wasPressedThisFrame) uvDelta.x += step;
            if (keyboard.downArrowKey.wasPressedThisFrame) uvDelta.y -= step;
            if (keyboard.upArrowKey.wasPressedThisFrame) uvDelta.y += step;

            bool changed = false;
            if (uvDelta.sqrMagnitude > 0f)
            {
                if (alt && shift)
                    profile.NudgePlayerIconUv(uvDelta);
                else if (alt)
                    profile.NudgeMapZeroUv(uvDelta);
                else
                    profile.NudgeUvOffset(uvDelta);
                changed = true;
            }

            if (keyboard.qKey.wasPressedThisFrame)
            {
                profile.NudgeNorthOffset(-northStepDegrees);
                changed = true;
            }

            if (keyboard.eKey.wasPressedThisFrame)
            {
                profile.NudgeNorthOffset(northStepDegrees);
                changed = true;
            }

            if (keyboard.pageUpKey.wasPressedThisFrame)
            {
                float scaleStep = shift ? 0.005f : 0.02f;
                if (ctrl)
                    profile.NudgeUvScale(scaleStep, 0f);
                else if (alt)
                    profile.NudgeUvScale(0f, scaleStep);
                else
                    profile.NudgeUvScale(scaleStep, scaleStep);
                changed = true;
            }

            if (keyboard.pageDownKey.wasPressedThisFrame)
            {
                float scaleStep = shift ? 0.005f : 0.02f;
                if (ctrl)
                    profile.NudgeUvScale(-scaleStep, 0f);
                else if (alt)
                    profile.NudgeUvScale(0f, -scaleStep);
                else
                    profile.NudgeUvScale(-scaleStep, -scaleStep);
                changed = true;
            }

            if (changed)
                NotifyChanged();
        }

        private void NotifyChanged()
        {
            if (mapProvider == null)
                mapProvider = WorldMapProvider.Instance;

            mapProvider?.NotifyCalibrationChanged();
        }

        private void OnGUI()
        {
            if (!showOnScreenHelp || profile == null || !profile.enableRuntimeTuning || !Application.isPlaying)
                return;

            const int width = 440;
            GUI.Box(new Rect(8f, 8f, width, 168f), "Map calibration (Play tweaks save on exit)");
            GUILayout.BeginArea(new Rect(16f, 28f, width - 16f, 142f));
            GUILayout.Label($"UV offset: {profile.mapUvOffset.x:F4}, {profile.mapUvOffset.y:F4}");
            GUILayout.Label($"Map zero UV: {profile.mapZeroUv01.x:F4}, {profile.mapZeroUv01.y:F4}");
            GUILayout.Label($"Player icon UV: {profile.mapPlayerIconUvOffset.x:F4}, {profile.mapPlayerIconUvOffset.y:F4}");
            GUILayout.Label($"North offset: {profile.mapDisplayNorthOffsetDegrees:F1}°");
            GUILayout.Label($"UV scale X/Y: {profile.mapUvScaleX:F3} / {profile.mapUvScaleY:F3}");
            GUILayout.Label("Arrows=UV | Alt+Arrows=zero | Shift+Alt+Arrows=icon | Q/E=north | PgUp/Dn=scale (Ctrl=X Alt=Y)");
            GUILayout.EndArea();
        }
    }
}
