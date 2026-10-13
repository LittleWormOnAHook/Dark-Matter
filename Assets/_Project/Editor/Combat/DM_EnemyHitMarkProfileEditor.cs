#if UNITY_EDITOR
using System.Collections.Generic;
using Project.Combat;
using Project.EditorTools.GenesisStudio;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Combat
{
    /// <summary>
    /// 2026-10-12: grouped Blood FX layout for DM_EnemyHitMarkProfile (Genesis Studio > Combat > Hit Marks).
    /// Layout only: every serialized field keeps its value. Obsolete fields (debug glow flags, old coolant slot)
    /// are hidden unless "Show obsolete" is on. Unlisted fields fall into "Other" so nothing disappears.
    /// Works with DMStudioSearch: a query filters fields by label / name / tooltip and opens the matching groups.
    /// </summary>
    [CustomEditor(typeof(DM_EnemyHitMarkProfile))]
    public sealed class DM_EnemyHitMarkProfileEditor : UnityEditor.Editor, IDMStudioSearchAware
    {
        private sealed class Group
        {
            public string Title; public string[] Fields; public bool DefaultOpen; public string Note;
            public Group(string t, bool open, string note, params string[] f) { Title = t; DefaultOpen = open; Note = note; Fields = f; }
        }

        private static readonly Group[] Groups =
        {
            new Group("Blood FX (KriptoFX) - pools", true, "Per body type prefab lists: any / left / right. Flesh = red, Android = green, Mech = yellow.",
                "useVolumetricBlood", "fleshVolumetric", "androidVolumetric", "mechVolumetric", "volumetricOnPlayer"),
            new Group("Blood FX (KriptoFX) - look", true, "Fluid brightness x glow per type (hue kept).",
                "volumetricFluidExposureComp", "volumetricFluidBrightness", "fluidMaxChannel", "fluidGlowFlesh", "fluidGlowAndroid", "fluidGlowMech",
                "volumetricLightIntensity", "androidFluidColor", "androidDecalTint", "mechFluidColor", "mechDecalTint"),
            new Group("Blood FX (KriptoFX) - direction", true, null,
                "volumetricFollowSwing", "volumetricYawOffset", "volumetricSideThreshold", "volumetricSwapSides"),
            new Group("Blood FX (KriptoFX) - ground & decals", true, null,
                "volumetricGroundSnap", "volumetricGroundMask", "volumetricGroundRayDistance",
                "volumetricDecalRandomRotation", "volumetricDecalScaleMin", "volumetricDecalScaleMax", "volumetricDecalDepth", "volumetricDecalGroundOffset",
                "volumetricDecalMode", "volumetricPlaneLodDistance", "fleshPlaneDecal", "androidPlaneDecal", "mechPlaneDecal"),
            new Group("Blood FX (KriptoFX) - budget, distance, lifetime", true, null,
                "volumetricBudget", "volumetricMaxDistance", "volumetricLifetime"),
            new Group("Slash & burn marks", false, "Per-type tints, size and limits.",
                "maxBurnMarksPerEnemy", "regenFadeSeconds", "androidMarkTint", "robotMarkTint",
                "slashSize", "slashProjectionDepth", "meleeSlashMarkSizeMultiplier",
                "burnSize", "burnProjectionDepth", "rangedAmmoMarkSizeMultiplier", "markSurfaceNormalOffset", "markDepthCenterBias",
                "burnAngleFade", "decalDrawDistance",
                "glowCoolSeconds", "glowResidual", "glowSizeScale", "emberGlowSeconds", "laserAlwaysGlowBurn",
                "bulletBurnMaterial", "glowBurnMaterial", "glowBurnHotMaterial", "bulletBurnHotMaterial", "burnEmberPrefab"),
            new Group("Player marks", false, null,
                "playerMarkLifetimeSeconds", "playerMarkFadeSeconds", "playerMarkSizeScale", "playerMarkProjectionDepth"),
            new Group("Sparks", false, "Mechs keep sparks on top of the yellow fluid.",
                "sparkHitSmallPrefab", "sparkHitLargePrefab", "sparkScaleMultiplier"),
            new Group("Hit FX budget & scaling", false, null,
                "fxBurstBudget", "perEnemyMinInterval", "fxMaxDistance", "goreIntensity", "heavyHitSplatterMultiplier",
                "chainHitSplatterBonus", "chainHitSplatterMaxBonus"),
            new Group("Hitboxes & zone damage", false, null,
                "hitboxTemplateScale", "hitboxInflate", "hitboxSweepRadius", "addFillerHitboxes",
                "headMultiplier", "torsoMultiplier", "armMultiplier", "legMultiplier",
                "enemyDecalRenderingLayer", "enemyDecalRenderingLayerFallbackIndex"),
            new Group("Legacy / fallback particles", false, "Used only when KriptoFX can't spawn (budget, distance, off).",
                "bloodHitPrefab", "humanoidResponse", "androidResponse", "robotResponse",
                "splatterDamageScale", "splatterFullScaleDamage", "bloodEntryNormalBlend", "exitSprayDamageThreshold", "exitSprayStrength"),
        };

        private static readonly HashSet<string> Obsolete = new HashSet<string>
        {
            "coolantHitPrefab", "coolantScaleMultiplier",
            "debugBrightHitMarkEmission", "debugHitMarkEmissionIntensity", "debugMeleeSlashEmissiveGlow",
            "debugBrightRangedMarks", "debugHotGlowSizeScale", "debugRangedImpactSpark",
        };

        private static readonly Dictionary<string, string> Labels = new Dictionary<string, string>
        {
            { "useVolumetricBlood", "Use KriptoFX blood" }, { "fleshVolumetric", "Flesh pool (red)" },
            { "androidVolumetric", "Android pool (green)" }, { "mechVolumetric", "Mech pool (yellow)" },
            { "volumetricOnPlayer", "Blood on player hits" },
            { "volumetricFluidExposureComp", "Exposure compensation" }, { "volumetricFluidBrightness", "Fluid brightness" },
            { "fluidMaxChannel", "Fluid max channel" }, { "fluidGlowFlesh", "Glow - Flesh" },
            { "fluidGlowAndroid", "Glow - Android" }, { "fluidGlowMech", "Glow - Mech" },
            { "volumetricLightIntensity", "Light intensity" },
            { "volumetricFollowSwing", "Follow blade swing" }, { "volumetricYawOffset", "Yaw offset" },
            { "volumetricSideThreshold", "Left/right threshold" }, { "volumetricSwapSides", "Swap left/right" },
            { "volumetricGroundSnap", "Ground snap" }, { "volumetricGroundMask", "Ground mask" },
            { "volumetricGroundRayDistance", "Ground ray distance" }, { "volumetricDecalMode", "Decal mode" },
            { "volumetricPlaneLodDistance", "Plane decal distance" }, { "fleshPlaneDecal", "Flesh plane decals" },
            { "androidPlaneDecal", "Android plane decals" }, { "mechPlaneDecal", "Mech plane decals" },
            { "volumetricBudget", "Budget (live effects)" }, { "volumetricDecalRandomRotation", "Decal random rotation" },
            { "volumetricDecalScaleMin", "Decal scale min" }, { "volumetricDecalScaleMax", "Decal scale max" }, { "volumetricDecalDepth", "Decal projection depth" }, { "volumetricDecalGroundOffset", "Decal ground offset" }, { "volumetricMaxDistance", "Max distance" },
            { "volumetricLifetime", "Lifetime (s)" },
            { "androidMarkTint", "Android mark tint" }, { "robotMarkTint", "Mech mark tint" },
            { "meleeSlashMarkSizeMultiplier", "Slash size multiplier" }, { "maxBurnMarksPerEnemy", "Max marks per enemy" },
            { "bloodHitPrefab", "Old blood particle" }, { "humanoidResponse", "Flesh response" },
            { "androidResponse", "Android response" }, { "robotResponse", "Mech response" },
        };

        private readonly Dictionary<string, bool> _open = new Dictionary<string, bool>();
        private bool _showObsolete;

        private static string Label(SerializedProperty p) => Labels.TryGetValue(p.name, out string l) ? l : p.displayName;

        private static bool PropMatches(SerializedProperty p)
        {
            if (!DMStudioSearch.Active) return true;
            if (DMStudioSearch.Matches(Label(p)) || DMStudioSearch.Matches(p.displayName) || DMStudioSearch.Matches(p.name) || DMStudioSearch.Matches(p.tooltip))
                return true;
            if (!p.hasVisibleChildren) return false;
            SerializedProperty it = p.Copy(), end = p.GetEndProperty();
            bool enter = true;
            while (it.NextVisible(enter) && !SerializedProperty.EqualContents(it, end))
            {
                enter = true;
                if (DMStudioSearch.Matches(it.displayName) || DMStudioSearch.Matches(it.name)) return true;
            }
            return false;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var used = new HashSet<string> { "m_Script" };
            foreach (Group g in Groups) DrawGroup(g.Title, g.Note, g.DefaultOpen, g.Fields, used);

            // anything not listed (new fields) so nothing is lost
            var rest = new List<string>();
            SerializedProperty iter = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iter.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (!used.Contains(iter.name) && !Obsolete.Contains(iter.name)) rest.Add(iter.name);
            }
            if (rest.Count > 0) DrawGroup("Other", null, false, rest.ToArray(), used);

            EditorGUILayout.Space(4f);
            _showObsolete = EditorGUILayout.ToggleLeft("Show obsolete fields (debug glow flags, old coolant slot)", _showObsolete);
            if (_showObsolete)
                DrawGroup("Obsolete (hidden by default)", "Kept for old data; coolant is replaced by the KriptoFX fluid, debug glow is off.", true, new List<string>(Obsolete).ToArray(), used, forceOpen: true);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawGroup(string title, string note, bool defaultOpen, string[] fields, HashSet<string> used, bool forceOpen = false)
        {
            var props = new List<SerializedProperty>();
            bool titleHit = DMStudioSearch.Active && DMStudioSearch.Matches(title);
            foreach (string f in fields)
            {
                used.Add(f);
                SerializedProperty p = serializedObject.FindProperty(f);
                if (p == null) continue;
                if (titleHit || PropMatches(p)) props.Add(p);
            }
            if (props.Count == 0) return;

            if (!_open.TryGetValue(title, out bool open)) open = defaultOpen;
            bool shownOpen = forceOpen || DMStudioSearch.Active || open;
            bool newOpen = EditorGUILayout.Foldout(shownOpen, title, true, EditorStyles.foldoutHeader);
            if (!DMStudioSearch.Active && !forceOpen && newOpen != open) _open[title] = newOpen;
            if (!newOpen) return;

            using (new EditorGUI.IndentLevelScope())
            {
                if (!string.IsNullOrEmpty(note) && !DMStudioSearch.Active)
                    EditorGUILayout.LabelField(note, EditorStyles.wordWrappedMiniLabel);
                foreach (SerializedProperty p in props)
                    EditorGUILayout.PropertyField(p, new GUIContent(Label(p), p.tooltip), true);
            }
            EditorGUILayout.Space(3f);
        }
    }
}
#endif
