using Project.Building;
using Project.EditorTools.GenesisStudio;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Building
{
    /// <summary>
    /// Building Studio: Settings (placement profile, preview ghosts, snap, doors) and Library (styles, kits, parts).
    /// </summary>
    public sealed class DMBuildingStudioWindow : EditorWindow
    {
        const string AssetPath = DMBuildingGhostProfile.AssetPath;
        public const int TabSettings = 0;
        public const int TabLibrary = 1;
        public const int TabCreationFx = 2;
        public const int TabRoadmap = 3;

        static readonly string[] Tabs = { "Settings", "Library", "Creation Effects", "Roadmap" };

        DMBuildingGhostProfile profile;
        DMBuildingCreationFxProfile fxProfile;
        SerializedObject fxSerialized;
        Vector2 scrollPosition;
        int tab;
        readonly DMBuildingLibraryPanel libraryPanel = new DMBuildingLibraryPanel();

        [MenuItem("Tools/Dark Matter Genesis/Buildings/Building Studio", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Buildings_Building_Studio)]
        public static void Open()
        {
            GetWindow<DMBuildingStudioWindow>("Building Studio");
        }

        public static void OpenTab(int tabIndex)
        {
            var window = GetWindow<DMBuildingStudioWindow>("Building Studio");
            window.tab = Mathf.Clamp(tabIndex, 0, Tabs.Length - 1);
            EditorPrefs.SetInt("DM.BuildingStudio.Tab", window.tab);
            window.Show();
            window.Focus();
        }

        void OnEnable()
        {
            profile = LoadOrCreate();
            tab = EditorPrefs.GetInt("DM.BuildingStudio.Tab", 0);
        }

        void OnGUI()
        {
            using var genesisTheme = Project.EditorTools.Theme.GenesisImgui.Window(this);
            if (profile == null)
                profile = LoadOrCreate();

            DMStudioStyles.DrawHeroHeader("Building Studio", "Stone, Iron and Silicate build kits - placement, snap, and the part library.");
            DMStudioStyles.DrawHorizontalTabBar(() =>
            {
                for (int i = 0; i < Tabs.Length; i++)
                {
                    if (DMStudioStyles.DrawCategoryTab(Tabs[i], tab == i, new Color(0.75f, 0.18f, 0.48f, 1f)) && tab != i)
                    {
                        tab = i;
                        EditorPrefs.SetInt("DM.BuildingStudio.Tab", tab);
                        GUI.FocusControl(null);
                    }
                }
            });

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            switch (tab)
            {
                case TabLibrary:
                    libraryPanel.Draw();
                    break;
                case TabCreationFx:
                    DrawCreationFx();
                    break;
                case TabRoadmap:
                    DrawRoadmapTab();
                    break;
                default:
                    DrawSettings();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        void DrawRoadmapTab()
        {
            const string scopePlan =
                "Assets/_Project/Documentation/Design/DMG_Building_System_Scope_Plan.md";

            DMStudioRoadmapPanel.DrawIntro(
                "Scope lock from DMG_Building_System_Scope_Plan.md (Sep 2026 as-built). Component track largely on disk; story slices and materialize path remain.",
                MessageType.Info);

            DMStudioRoadmapPanel.DrawDocLinks(
                ("Building scope plan", scopePlan),
                ("Ghost profile", AssetPath),
                ("Disk status (World Engine)", "Assets/_Project/Documentation/Architecture/World_Engine_Disk_Status.md"));

            if (GUILayout.Button("Open Project Roadmap → Building", GUILayout.Height(28f)))
                DMProjectRoadmapWindow.OpenTab(1);

            DMStudioRoadmapPanel.DrawPhase(
                "C1",
                "Hold-B mode + style libraries",
                DMStudioRoadmapPanel.Status.Done,
                "Stone / Iron / Silicate kits, 4 m snap lattice, UITK build hotbar, save/load placed pieces.");

            DMStudioRoadmapPanel.DrawPhase(
                "C2",
                "Building Studio + Genesis tabs",
                DMStudioRoadmapPanel.Status.Done,
                "Preview ghosts, snap, placement, doors, M-key finishes, creation FX — profiles use Live at runtime.");

            DMStudioRoadmapPanel.DrawPhase(
                "C3",
                "Doors, power, storage",
                DMStudioRoadmapPanel.Status.InProgress,
                "Force-field doors, generator + base power, Build Hub zone, storage crates on disk.");

            DMStudioRoadmapPanel.DrawPhase(
                "S1–S7",
                "Structure slices (definition → BCP → campaign)",
                DMStudioRoadmapPanel.Status.InProgress,
                "BCP tabs partial; BuildingDefinition SO, wreck scans, and prologue placement quests not finished.");

            DMStudioRoadmapPanel.DrawPhase(
                "S2",
                "Full materialize path",
                DMStudioRoadmapPanel.Status.NotStarted,
                "Ghost commit, per-tick drain during hold, reverse dissolve — cancel refunds drained ticks.");

            DMStudioRoadmapPanel.DrawPhase(
                "Tiers",
                "Steel / Amalgam + upgrades",
                DMStudioRoadmapPanel.Status.NotStarted,
                "Skill gates and in-place tier upgrade deferred.");
        }

        void DrawSettings()
        {
            DMStudioStyles.DrawSection("Building profile", DMStudioStyles.ContentPanel, () =>
            {
                EditorGUILayout.ObjectField("Asset", profile, typeof(DMBuildingGhostProfile), false);
                EditorGUILayout.HelpBox(
                    "Play reads DM_BuildingGhostProfile from Resources. "
                    + "Valid snap preview uses Snap & Build fields; invalid seats use Blocked. "
                    + "Unbuilt ghosts still refund when build mode ends. Kit shapes, finishes and parts live per style under Library.",
                    MessageType.Info);
            });

            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(profile, "Edit Building Profile");
            using (DMStudioStyles.BeginProfileInspector(DMStudioStyles.ProfileMinLabelWidth + 40f))
            {
                DMStudioStyles.DrawSection(null, DMStudioStyles.ContentPanel, DrawPreviewGhostSection);
                DMStudioStyles.DrawSection(null, DMStudioStyles.ContentPanel, DrawBuiltTintSection);
                DMStudioStyles.DrawSection(null, DMStudioStyles.ContentPanel, DrawSnapSection);
                DMStudioStyles.DrawSection(null, DMStudioStyles.ContentPanel, DrawPlacementSection);
                DMStudioStyles.DrawSection(null, DMStudioStyles.ContentPanel, DrawDoorSection);
            }

            if (EditorGUI.EndChangeCheck())
            {
                profile.ghostAlpha = Mathf.Clamp01(profile.ghostAlpha);
                profile.blockedGhostAlpha = Mathf.Clamp01(profile.blockedGhostAlpha);
                profile.glassAlpha = Mathf.Clamp01(profile.glassAlpha);
                profile.edgeFacingDot = Mathf.Clamp01(profile.edgeFacingDot);
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
            }

            DMStudioStyles.DrawSection(null, DMStudioStyles.ContentPanel, DrawBuildHubTestSection);
        }

        void DrawPreviewGhostSection()
        {
            EditorGUILayout.LabelField("Preview holograms", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("Snap & build (valid seat)", EditorStyles.miniBoldLabel);
            profile.validGhostMaterial = (Material)EditorGUILayout.ObjectField(
                "Material (optional)",
                profile.validGhostMaterial,
                typeof(Material),
                false);
            profile.ghostColor = EditorGUILayout.ColorField(
                new GUIContent("Color"),
                profile.ghostColor,
                true,
                false,
                false);
            profile.ghostAlpha = EditorGUILayout.Slider("Alpha", profile.ghostAlpha, 0f, 1f);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Blocked (invalid / unaffordable)", EditorStyles.miniBoldLabel);
            profile.blockedGhostMaterial = (Material)EditorGUILayout.ObjectField(
                "Material (optional)",
                profile.blockedGhostMaterial,
                typeof(Material),
                false);
            profile.blockedGhostColor = EditorGUILayout.ColorField(
                new GUIContent("Color"),
                profile.blockedGhostColor,
                true,
                false,
                false);
            profile.blockedGhostAlpha = EditorGUILayout.Slider("Alpha", profile.blockedGhostAlpha, 0f, 1f);
        }

        void DrawBuiltTintSection()
        {
            EditorGUILayout.LabelField("Built piece tints", EditorStyles.boldLabel);
            profile.builtMaterial = (Material)EditorGUILayout.ObjectField(
                new GUIContent("Built material", "Used on built pieces when their style has no finish. Tinted by Finished mesh."),
                profile.builtMaterial,
                typeof(Material),
                false);
            profile.finishedColor = EditorGUILayout.ColorField(
                new GUIContent("Finished mesh"),
                profile.finishedColor,
                true,
                false,
                false);
            profile.glassColor = EditorGUILayout.ColorField(
                new GUIContent("Window glass"),
                profile.glassColor,
                true,
                false,
                false);
            profile.glassAlpha = EditorGUILayout.Slider("Glass alpha", profile.glassAlpha, 0f, 1f);
        }

        void DrawSnapSection()
        {
            EditorGUILayout.LabelField("Snap & grid", EditorStyles.boldLabel);
            profile.yawStepDegrees = EditorGUILayout.FloatField("Yaw degrees / notch", profile.yawStepDegrees);
            profile.heightStepMeters = EditorGUILayout.FloatField("Height step (m)", profile.heightStepMeters);
            profile.maxHeightOffsetMeters = EditorGUILayout.FloatField("Max height offset (m)", profile.maxHeightOffsetMeters);
            profile.largeModuleMeters = EditorGUILayout.FloatField("Large module (m)", profile.largeModuleMeters);
            profile.smallModuleMeters = EditorGUILayout.FloatField("Small module (m)", profile.smallModuleMeters);
            profile.edgeSnapRangeMeters = EditorGUILayout.FloatField("Edge snap range (m)", profile.edgeSnapRangeMeters);
            profile.topSnapRangeMeters = EditorGUILayout.FloatField("Top snap range (m)", profile.topSnapRangeMeters);
            profile.buildLookUpDegrees = EditorGUILayout.Slider("Build look up (deg)", profile.buildLookUpDegrees, 0f, 75f);
            profile.buildModeCameraDistanceMultiplier = EditorGUILayout.Slider(
                "Build camera distance ×",
                profile.buildModeCameraDistanceMultiplier,
                1f,
                3f);
            profile.buildModeCameraExtraMeters = EditorGUILayout.FloatField(
                "Build camera extra (m)",
                profile.buildModeCameraExtraMeters);
            profile.doorFrameRangeMeters = EditorGUILayout.FloatField("Door frame range (m)", profile.doorFrameRangeMeters);
            profile.edgeFacingDot = EditorGUILayout.Slider("Edge facing dot", profile.edgeFacingDot, 0f, 1f);
        }

        void DrawPlacementSection()
        {
            EditorGUILayout.LabelField("Placement", EditorStyles.boldLabel);
            profile.buildSeconds = EditorGUILayout.FloatField("Hold to build (s)", profile.buildSeconds);
            profile.destroyHoldSeconds = EditorGUILayout.FloatField("Hold to destroy (s)", profile.destroyHoldSeconds);
            profile.aimDistanceMeters = EditorGUILayout.FloatField("Aim distance (m)", profile.aimDistanceMeters);
            profile.doorSeatDropMeters = EditorGUILayout.FloatField("Door seat drop (m)", profile.doorSeatDropMeters);
            profile.overlapPaddingMeters = EditorGUILayout.FloatField("Overlap padding (m)", profile.overlapPaddingMeters);
            profile.newBaseClearanceMeters = EditorGUILayout.FloatField(new GUIContent("New base clearance (m)", "A foundation may stand alone only when no foundation is within this distance. 0 = only the very first foundation in the world."), profile.newBaseClearanceMeters);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Base power (generator)", EditorStyles.boldLabel);
            profile.basePowerSquareMeters = EditorGUILayout.FloatField(new GUIContent("Base power square (m)", "Side of the powered square around a base, centred on its first placed foundation. Equipment can stand on the ground inside it."), profile.basePowerSquareMeters);
            profile.generatorFuelRangeMeters = EditorGUILayout.FloatField(new GUIContent("Fuel crate range (m)", "Refuelling reaches Plasma Fuel in storage crates within this distance of the generator."), profile.generatorFuelRangeMeters);
            profile.generatorTankUnits = EditorGUILayout.IntField(new GUIContent("Tank capacity (units)", "1 Plasma Fuel = 1 unit."), profile.generatorTankUnits);
            profile.generatorMinutesPerUnit = EditorGUILayout.FloatField(new GUIContent("Minutes per fuel unit", "Gameplay minutes one unit lasts."), profile.generatorMinutesPerUnit);
            profile.powerLoadPeriodMinutes = EditorGUILayout.FloatField(new GUIContent("Load period (minutes)", "Each powered item and force field in the base adds its units once per period on top of the base burn."), profile.powerLoadPeriodMinutes);
            profile.poweredItemUnitsPerPeriod = EditorGUILayout.FloatField(new GUIContent("Powered item load (units)", "Extra fuel per load period for each light or powered item."), profile.poweredItemUnitsPerPeriod);
            profile.forceFieldUnitsPerPeriod = EditorGUILayout.FloatField(new GUIContent("Force field load (units)", "Extra fuel per load period for each force field."), profile.forceFieldUnitsPerPeriod);
            profile.forceFieldsNeedPower = EditorGUILayout.Toggle(new GUIContent("Force fields need power", "Unpowered force fields shut off and anyone can walk through."), profile.forceFieldsNeedPower);
            profile.lightsNeedPower = EditorGUILayout.Toggle(new GUIContent("Lights need power", "Lights on built pieces go dark with no powered generator."), profile.lightsNeedPower);

            // 0926-build-hub
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Build Hub zone", EditorStyles.boldLabel);
            profile.requireBuildHub = EditorGUILayout.Toggle(new GUIContent("Require Build Hub", "Every piece except the Build Hub must stand fully inside a Build Hub zone. Off = build anywhere (testing)."), profile.requireBuildHub);
            profile.buildHubZoneMeters = EditorGUILayout.FloatField(new GUIContent("Zone width / depth (m)", "X and Z size of a level 0 zone, centred on the hub."), profile.buildHubZoneMeters);
            profile.buildHubZoneHeightMeters = EditorGUILayout.FloatField(new GUIContent("Zone height (m)", "Y size of a level 0 zone, centred on the hub (half above, half below)."), profile.buildHubZoneHeightMeters);
            profile.buildHubZoneStepMeters = EditorGUILayout.FloatField(new GUIContent("Width step per level (m)", "Added to the zone width and depth per upgrade level."), profile.buildHubZoneStepMeters);
            profile.buildHubZoneHeightStepMeters = EditorGUILayout.FloatField(new GUIContent("Height step per level (m)", "Added to the zone height per upgrade level."), profile.buildHubZoneHeightStepMeters);
            profile.buildHubMaxLevel = EditorGUILayout.IntField(new GUIContent("Max upgrade level", "Highest Build Hub upgrade level."), profile.buildHubMaxLevel);
            profile.buildHubZoneMustBeClear = EditorGUILayout.Toggle(new GUIContent("Zone must be clear", "A hub's whole zone must be free of colliders except terrain (characters, creatures, items and built pieces are ignored; triggers count). Also checked on upgrade."), profile.buildHubZoneMustBeClear);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Patrol paths", EditorStyles.boldLabel);
            profile.blockPatrolPaths = EditorGUILayout.Toggle(new GUIContent("Block patrol paths", "Pieces may not be built on or across creature / pet patrol paths (DMIPathFollowProvider)."), profile.blockPatrolPaths);
            profile.patrolPathClearanceMeters = EditorGUILayout.FloatField(new GUIContent("Path clearance (m)", "The ghost bounds grow by this much before the patrol path test."), profile.patrolPathClearanceMeters);
            profile.patrolPathHeightToleranceMeters = EditorGUILayout.FloatField(new GUIContent("Path height tolerance (m)", "Extra vertical slack for the patrol path test (path points rarely sit exactly on the ground)."), profile.patrolPathHeightToleranceMeters);
        }

        /// <summary>0926-build-hub: play-mode test buttons for Build Hub upgrade levels (no crafting recipes yet).</summary>
        void DrawBuildHubTestSection()
        {
            EditorGUILayout.LabelField("Build Hub test (play mode)", EditorStyles.boldLabel);
            int hubs = Application.isPlaying ? DMBuildHub.Active.Count : 0;
            EditorGUILayout.LabelField("Hubs in world", Application.isPlaying ? hubs.ToString() : "- (enter play mode)");
            using (new EditorGUI.DisabledScope(hubs == 0))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Upgrade all hubs (+1)"))
                {
                    for (int i = 0; i < DMBuildHub.Active.Count; i++)
                    {
                        if (DMBuildHub.Active[i] != null)
                            DMBuildHub.Active[i].Upgrade();
                    }
                }
                if (GUILayout.Button("Reset hubs to level 0"))
                {
                    for (int i = 0; i < DMBuildHub.Active.Count; i++)
                    {
                        if (DMBuildHub.Active[i] != null)
                            DMBuildHub.Active[i].SetLevel(0);
                    }
                }
                EditorGUILayout.EndHorizontal();
                for (int i = 0; i < DMBuildHub.Active.Count && Application.isPlaying; i++)
                {
                    DMBuildHub hub = DMBuildHub.Active[i];
                    if (hub == null)
                        continue;
                    Vector3 size = hub.ZoneSize;
                    EditorGUILayout.LabelField(hub.name, "Level " + hub.UpgradeLevel + "  (" + size.x.ToString("0") + " x " + size.y.ToString("0") + " x " + size.z.ToString("0") + " m)");
                }
            }
        }

        void DrawDoorSection()
        {
            EditorGUILayout.LabelField("Doors (E swing)", EditorStyles.boldLabel);
            profile.doorSwingDegrees = EditorGUILayout.FloatField("Swing degrees", profile.doorSwingDegrees);
            profile.doorSwingSeconds = EditorGUILayout.FloatField("Swing duration (s)", profile.doorSwingSeconds);
            profile.doorInteractRangeMeters = EditorGUILayout.FloatField("Interact range (m)", profile.doorInteractRangeMeters);
            profile.gateSwingDegrees = EditorGUILayout.FloatField("Gate swing degrees", profile.gateSwingDegrees);
            profile.gateSwingSeconds = EditorGUILayout.FloatField("Gate swing duration (s)", profile.gateSwingSeconds);
        }

        void DrawCreationFx()
        {
            if (fxProfile == null)
            {
                fxProfile = LoadOrCreateFx();
                fxSerialized = null;
            }
            if (fxSerialized == null || fxSerialized.targetObject != fxProfile)
                fxSerialized = new SerializedObject(fxProfile);

            DMStudioStyles.DrawSection("Building Creation Effects", DMStudioStyles.ContentPanel, () =>
            {
                EditorGUILayout.ObjectField("Asset", fxProfile, typeof(DMBuildingCreationFxProfile), false);
                EditorGUILayout.HelpBox(
                    "Plays on a piece the moment a hold-to-build finishes: grow and shrink, bounce up and settle, optional VFX, "
                    + "optional material swap. The piece always ends on its exact seat. Pieces loaded from a save do not play it.",
                    MessageType.Info);
            });

            fxSerialized.Update();
            using (DMStudioStyles.BeginProfileInspector(fxSerialized))
            {
            DMStudioStyles.DrawSection(null, DMStudioStyles.ContentPanel, () =>
            {
                Prop("enabled", "Enabled");
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Pop & bounce", EditorStyles.boldLabel);
                Prop("duration", "Duration (s)");
                Prop("scalePunch", "Size punch (0.1 = 10%)");
                Prop("bounceHeightMeters", "Bounce height (m)");
                Prop("scaleCurve", "Size curve");
                Prop("bounceCurve", "Bounce curve");
                if (GUILayout.Button("Reset curves to smooth hump", GUILayout.Width(220f)))
                {
                    Undo.RecordObject(fxProfile, "Reset Creation FX Curves");
                    fxProfile.scaleCurve = DMBuildingCreationFxProfile.Hump();
                    fxProfile.bounceCurve = DMBuildingCreationFxProfile.Hump();
                    EditorUtility.SetDirty(fxProfile);
                    fxSerialized.Update();
                }
            });
            DMStudioStyles.DrawSection(null, DMStudioStyles.ContentPanel, () =>
            {
                EditorGUILayout.LabelField("VFX", EditorStyles.boldLabel);
                Prop("vfxPrefab", "VFX prefab");
                Prop("vfxDelay", "Delay (s)");
                Prop("vfxLifetime", "Lifetime (s)");
                Prop("vfxAnchor", "Spawn at");
                Prop("vfxScaleWithPiece", "Scale with piece size");
            });
            DMStudioStyles.DrawSection(null, DMStudioStyles.ContentPanel, () =>
            {
                EditorGUILayout.LabelField("Material swap", EditorStyles.boldLabel);
                Prop("swapMaterial", "Swap material");
                using (new EditorGUI.DisabledScope(!fxProfile.swapMaterial))
                {
                    Prop("swapMaterialAsset", "Material");
                    Prop("swapStart", "Start (s)");
                    Prop("swapDuration", "Duration (s)");
                }
            });
            DrawForceFields();
            }

            if (fxSerialized.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(fxProfile);
                AssetDatabase.SaveAssetIfDirty(fxProfile);
            }
        }

        // 0926-force-fields: material, colour, audio and behaviour for every force field door.
        void DrawForceFields()
        {
            DMStudioStyles.DrawSection(null, DMStudioStyles.ContentPanel, () =>
            {
                EditorGUILayout.LabelField("Force fields: look", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Force Field Door, Double Force Field Door, Force Field Gate and Force Field Hatch. They seat in the same frames as doors, "
                    + "stop bullets, enemies and weather, and open for the player and companions. Changes show live on placed fields.",
                    MessageType.None);
                Prop("forceFieldMaterial", "Material (empty = default)");
                Prop("forceFieldColor", "Colour");
                Prop("forceFieldEdgeColor", "Edge / ripple colour");
                Prop("forceFieldOpacity", "Idle opacity");
                Prop("forceFieldEdgeGlow", "Edge glow");
                Prop("forceFieldEdgeWidth", "Edge width");
                Prop("forceFieldPatternScale", "Pattern scale");
                Prop("forceFieldScrollSpeed", "Pattern speed");
            });
            DMStudioStyles.DrawSection(null, DMStudioStyles.ContentPanel, () =>
            {
                EditorGUILayout.LabelField("Force fields: pass-through shimmer", EditorStyles.boldLabel);
                Prop("forceFieldPulseBrightness", "Flash brightness");
                Prop("forceFieldPulseSeconds", "Flash / ripple time (s)");
                Prop("forceFieldRippleSpeed", "Ripple speed (m/s)");
                Prop("forceFieldRippleWidth", "Ripple width (m)");
            });
            DMStudioStyles.DrawSection(null, DMStudioStyles.ContentPanel, () =>
            {
                EditorGUILayout.LabelField("Force fields: audio", EditorStyles.boldLabel);
                Prop("forceFieldPassClip", "Pass sound (empty = crackle)");
                Prop("forceFieldPassVolume", "Pass volume");
                Prop("forceFieldPitchJitter", "Pitch variation");
                Prop("forceFieldSoundCooldown", "Sound cooldown (s)");
                Prop("forceFieldAudioMaxDistance", "Hearing distance (m)");
                Prop("forceFieldIdleHumClip", "Idle hum (empty = generated)");
                Prop("forceFieldIdleHumVolume", "Idle hum volume (0 = off)");
            });
            DMStudioStyles.DrawSection(null, DMStudioStyles.ContentPanel, () =>
            {
                EditorGUILayout.LabelField("Force fields: behaviour", EditorStyles.boldLabel);
                Prop("forceFieldLetFriendliesThrough", "Let player and companions through");
                using (new EditorGUI.DisabledScope(!fxProfile.forceFieldLetFriendliesThrough))
                {
                    Prop("forceFieldSenseDepthMeters", "Open distance (m)");
                    Prop("forceFieldCloseDelaySeconds", "Close delay (s)");
                }
            });
            DMStudioStyles.DrawSection(null, DMStudioStyles.ContentPanel, () =>
            {
                EditorGUILayout.LabelField("Force fields: corner blocks and power strips", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "A small block sits in each corner of every field. The strip through the middle of each block glows red while the base "
                    + "has no power (the field material and collider are off) and turns green when a fuelled generator powers the field.",
                    MessageType.None);
                Prop("forceFieldCorners", "Show corner blocks");
                Prop("forceFieldCornerMaterial", "Block material (empty = DM_ForceFieldCorner)");
                if (fxProfile.forceFieldCornerMaterial == null && GUILayout.Button("Create editable block material"))
                {
                    Material made = DMBuildingForceFields.EnsureCornerMaterial();
                    SerializedProperty slot = fxSerialized.FindProperty("forceFieldCornerMaterial");
                    if (made != null && slot != null)
                        slot.objectReferenceValue = made;
                }
                Prop("forceFieldCornerSize", "Block size (m)");
                Prop("forceFieldCornerDepth", "Block depth (m)");
                Prop("forceFieldStripMaterial", "Strip material (empty = generated glow)");
                Prop("forceFieldStripUnpoweredColor", "Strip colour, no power");
                Prop("forceFieldStripPoweredColor", "Strip colour, powered");
                Prop("forceFieldStripGlow", "Strip glow");
                Prop("forceFieldStripThickness", "Strip thickness (m)");
            });
        }

        void Prop(string name, string label)
        {
            SerializedProperty property = fxSerialized.FindProperty(name);
            if (property != null)
                EditorGUILayout.PropertyField(property, new GUIContent(label, property.tooltip));
        }

        static DMBuildingCreationFxProfile LoadOrCreateFx()
        {
            var existing = AssetDatabase.LoadAssetAtPath<DMBuildingCreationFxProfile>(DMBuildingCreationFxProfile.AssetPath);
            if (existing != null)
                return existing;

            var created = CreateInstance<DMBuildingCreationFxProfile>();
            AssetDatabase.CreateAsset(created, DMBuildingCreationFxProfile.AssetPath);
            AssetDatabase.SaveAssets();
            return created;
        }

        static DMBuildingGhostProfile LoadOrCreate()
        {
            var existing = AssetDatabase.LoadAssetAtPath<DMBuildingGhostProfile>(AssetPath);
            if (existing != null)
                return existing;

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources/Building"))
                AssetDatabase.CreateFolder("Assets/_Project/Resources", "Building");

            var created = CreateInstance<DMBuildingGhostProfile>();
            AssetDatabase.CreateAsset(created, AssetPath);
            AssetDatabase.SaveAssets();
            return created;
        }
    }
}
