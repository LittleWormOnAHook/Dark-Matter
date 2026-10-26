#if UNITY_EDITOR
// 2026-10-25q: Day/night controls shared by Studio World > Sky & Orbit and Io Clock (Undo + SetDirty; auto-saved by DMSkyEditPreview / PlayMode saver).
using Project.World.Clock;
using Project.World.Sky;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.GenesisStudio
{
    public static class DMStudioDayNight
    {
        public static void Draw(DM_IoSkyProfile sky, DMIoClockProfile clk)
        {
            if (sky == null) sky = DM_IoSkyProfile.Live;
            if (clk == null) clk = DMIoClockProfile.Live;
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Day / Night (25q)", EditorStyles.boldLabel);
            if (sky != null)
            {
                float v = EditorGUILayout.Slider("Day Fraction", sky.dayFraction, 0.2f, 0.9f);
                if (!Mathf.Approximately(v, sky.dayFraction)) { Undo.RecordObject(sky, "Day Fraction"); sky.dayFraction = v; EditorUtility.SetDirty(sky); }
                EditorGUILayout.BeginHorizontal();
                DayBtn(sky, "Short Days 0.35", 0.35f); DayBtn(sky, "Subtle 0.55", 0.55f); DayBtn(sky, "Default 0.5", 0.5f); DayBtn(sky, "Long Days 0.65", 0.65f); DayBtn(sky, "Extreme 0.8", 0.8f);
                EditorGUILayout.EndHorizontal();
            }
            if (clk != null)
            {
                float r = EditorGUILayout.Slider("Real Seconds / Game Hour", clk.realSecondsPerGameHour, 1f, 3600f);
                if (!Mathf.Approximately(r, clk.realSecondsPerGameHour)) { Undo.RecordObject(clk, "Real sec per hour"); clk.realSecondsPerGameHour = r; EditorUtility.SetDirty(clk); }
                float n = EditorGUILayout.Slider("Night Time Scale", clk.nightTimeScale, 0.1f, 5f);
                if (!Mathf.Approximately(n, clk.nightTimeScale)) { Undo.RecordObject(clk, "Night Time Scale"); clk.nightTimeScale = n; EditorUtility.SetDirty(clk); }
                float b = EditorGUILayout.Slider("Night Transition (deg)", clk.nightTransitionDeg, 0.01f, 30f);
                if (!Mathf.Approximately(b, clk.nightTransitionDeg)) { Undo.RecordObject(clk, "Night Transition"); clk.nightTransitionDeg = b; EditorUtility.SetDirty(clk); }
                EditorGUILayout.BeginHorizontal();
                string[] names = { "Nights: Subtle", "Nights: Default", "Nights: Extreme" };
                for (int i = 0; i < 3; i++)
                    if (GUILayout.Button(names[i])) { Undo.RecordObject(clk, "Night preset"); clk.ApplyNightScalePreset(i); EditorUtility.SetDirty(clk); }
                EditorGUILayout.EndHorizontal();
            }
            if (sky != null && clk != null)
            {
                float hpd = Mathf.Max(1, clk.hoursPerDay), noon = hpd * 0.5f, dayH = sky.dayFraction * hpd, nightH = hpd - dayH;
                float rs = clk.realSecondsPerGameHour, ns = Mathf.Clamp(clk.nightTimeScale, 0.1f, 5f);
                EditorGUILayout.HelpBox(string.Format(
                    "Sunrise ~{0:0.00} h   Sunset ~{1:0.00} h   (noon {2:0.#} h, eclipse centred)\nDay {3:0.#} game h = {4:0.0} real min   Night {5:0.#} game h = {6:0.0} real min   Full Io day = {7:0.0} real min\n(night real length uses Night Time Scale; transition blend ignored)",
                    noon - dayH * 0.5f, noon + dayH * 0.5f, noon, dayH, dayH * rs / 60f, nightH, nightH * rs / ns / 60f, (dayH * rs + nightH * rs / ns) / 60f), MessageType.None);
            }
        }

        private static void DayBtn(DM_IoSkyProfile sky, string label, float f)
        {
            if (GUILayout.Button(label)) { Undo.RecordObject(sky, "Day Fraction preset"); sky.ApplyDayFractionPreset(f); EditorUtility.SetDirty(sky); }
        }
    }
}
#endif
