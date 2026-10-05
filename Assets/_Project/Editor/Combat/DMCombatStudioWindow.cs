using Project.Audio;
using Project.Combat;
using Project.EditorTools.GenesisStudio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project.EditorTools.Combat
{
    /// <summary>
    /// Combat Studio Phase 2 shell: Core / Melee / Ranged / Play (v1.6.5) / Roadmap.
    /// Runtime numbers live on DM_CombatCoreProfile. Combat_Sandbox is forensics only.
    /// </summary>
    public sealed class DMCombatStudioWindow : EditorWindow
    {
        private const string ProfileResourcesPath = "Assets/_Project/Resources/Combat/DM_CombatCoreProfile.asset";
        private const string AudioProfilePath = "Assets/_Project/Resources/GameAudioProfile.asset";
        private const string PlayableScenePath = "Assets/_Project/Scenes/Dark Matter Genesis v1.6.5.unity";
        private const string SandboxScenePath = "Assets/_Project/Scenes/Combat/Combat_Sandbox.unity";
        private const string CombatPlanPath = "Assets/_Project/Documentation/Design/Combat/DMG_Combat_Plan_v2.md";

        public const int TabCore = 0;
        public const int TabMelee = 1;
        public const int TabRanged = 2;
        public const int TabPlay = 3;
        public const int TabRoadmap = 4;

        private static readonly string[] Tabs = { "Core", "Melee", "Ranged", "Play", "Roadmap" };

        private DM_CombatCoreProfile profile;
        private SerializedObject serializedProfile;
        private GameAudioProfile audioProfile;
        private SerializedObject serializedAudioProfile;
        private Vector2 scrollPosition;
        private int tab;

        [MenuItem("Tools/Dark Matter Genesis/Combat/Combat Studio")]
        public static void Open()
        {
            GetWindow<DMCombatStudioWindow>("Combat Studio");
        }

        public static void OpenTab(int tabIndex)
        {
            DMCombatStudioWindow window = GetWindow<DMCombatStudioWindow>("Combat Studio");
            window.tab = Mathf.Clamp(tabIndex, 0, Tabs.Length - 1);
            EditorPrefs.SetInt("DM.CombatStudio.Tab", window.tab);
            window.Show();
            window.Focus();
        }

        private void OnEnable()
        {
            profile = LoadOrCreateProfile();
            serializedProfile = profile != null ? new SerializedObject(profile) : null;
            audioProfile = AssetDatabase.LoadAssetAtPath<GameAudioProfile>(AudioProfilePath);
            serializedAudioProfile = audioProfile != null ? new SerializedObject(audioProfile) : null;
            tab = EditorPrefs.GetInt("DM.CombatStudio.Tab", 0);
            if (tab < 0 || tab >= Tabs.Length)
                tab = 0;
        }

        private void OnGUI()
        {
            using var genesisTheme = Project.EditorTools.Theme.GenesisImgui.Window(this);
            if (profile == null)
                profile = LoadOrCreateProfile();

            if (serializedProfile == null && profile != null)
                serializedProfile = new SerializedObject(profile);

            DMStudioStyles.DrawHeroHeader(
                "Combat Studio",
                "Core, melee, ranged links, v1.6.5 Play, and §31 roadmap — runtime reads DM_CombatCoreProfile.");

            DMStudioStyles.DrawHorizontalTabBar(() =>
            {
                for (int i = 0; i < Tabs.Length; i++)
                {
                    if (DMStudioStyles.DrawCategoryTab(Tabs[i], tab == i, new Color(0.56f, 0.12f, 0.37f, 1f)) && tab != i)
                    {
                        tab = i;
                        EditorPrefs.SetInt("DM.CombatStudio.Tab", tab);
                        GUI.FocusControl(null);
                    }
                }
            });

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            switch (tab)
            {
                case TabMelee:
                    DrawMeleeTab();
                    break;
                case TabRanged:
                    DrawRangedTab();
                    break;
                case TabPlay:
                    DrawPlayTab();
                    break;
                case TabRoadmap:
                    DrawRoadmapTab();
                    break;
                default:
                    DrawCoreTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawCoreTab()
        {
            DrawProfileHeader("Combat Core profile");
            if (serializedProfile == null)
                return;

            serializedProfile.Update();
            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(profile, "Edit Combat Core Profile");

            using (DMStudioStyles.BeginProfileInspector(serializedProfile))
            {
                DMStudioStyles.DrawSection("Poise", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox(
                        "Recommended humanoid drain: max 100, health→poise 0.35, per-hit cap 0.4. "
                        + "A ~40 damage swing then costs ~14 poise (~7 hits to break). "
                        + "Per-hit cap stops one swing from always breaking; set it to 1 to allow one-shots.",
                        MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "trainingDummyPoiseMax",
                        "poiseDamageFromHealthMultiplier",
                        "maxPoiseDamageFractionPerHit",
                        "poiseRegenPerSecond",
                        "poiseRegenDelayAfterHit");
                });

                DMStudioStyles.DrawSection("Humanoid poise", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox(
                        "Enemy humanoids only — not the training dummy. "
                        + "CombatPoise reads DM_CombatCoreProfile.Live for poise-break stagger (~0.32s baseline). "
                        + "Playable drain: humanoidPoiseMax 100 with health→poise 0.35. "
                        + "Max 20 + multiplier ~1 makes every 20+ damage swing break poise.",
                        MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "humanoidPoiseMax",
                        "humanoidPoiseStaggerSeconds");
                });

                DMStudioStyles.DrawSection("I-Frames", DMStudioStyles.ContentPanel, () =>
                {
                    DMStudioStyles.DrawPropertyFields(serializedProfile, "dodgeIFrameSeconds", "dashIFrameSeconds");
                });

                DMStudioStyles.DrawSection("Status", DMStudioStyles.ContentPanel, () =>
                {
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "statusMaxStacks",
                        "statusImmunityWindowSeconds",
                        "statusBossMultiplier");
                });

                DMStudioStyles.DrawSection("Hooks & debug", DMStudioStyles.ContentPanel, () =>
                {
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "hitstopLightFrames",
                        "hitstopHeavyFrames",
                        "parryWindowSeconds",
                        "logCombatEventsInPlay");
                });
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedProfile.ApplyModifiedProperties();
                EditorUtility.SetDirty(profile);
            }

            DrawPoiseStaggerAudioSection();
        }

        private void DrawMeleeTab()
        {
            DrawProfileHeader("Melee profile");
            if (serializedProfile == null)
                return;

            serializedProfile.Update();
            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(profile, "Edit Combat Core Profile");

            using (DMStudioStyles.BeginProfileInspector(serializedProfile))
            {
                DMStudioStyles.DrawSection("Hit detection", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox(
                        "Scales Invector vHitBox on player and humanoid enemy weapons. Wider block cone uses defaultDefenseRange (half-angle from forward).",
                        MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "meleeHitboxWidthScale",
                        "meleeHitboxReachScale",
                        "meleeBlockDefenseHalfAngle",
                        "enemyMeleeHitboxWidthScale",
                        "enemyMeleeHitboxReachScale",
                        "enemyMeleeAttackRangeMultiplier");
                });

                DMStudioStyles.DrawSection("Attack / block facing", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox(
                        "Gate 0 attack yaw is shipped — do not retune unless it regresses. "
                        + "Attack auto-face uses a tighter cone than block; both share max distance and turn speed.",
                        MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "meleeAttackAutoFaceHalfAngle",
                        "meleeBlockAutoFaceHalfAngle",
                        "meleeBlockAutoFaceMaxDistance",
                        "meleeBlockAutoFaceTurnSpeed");
                });

                DMStudioStyles.DrawSection("Block / parry guard-break", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox(
                        "A held block is a light guard tap. A parry (block inside parryWindowSeconds) is the riposte.\n"
                        + "Souls, God of War, and Sekiro: block recoil is short (~0.35–0.45s lockout at default, small flinch, no hitstop). "
                        + "blockStaggerSeconds is tunable 0–3s (profile default 0.4). "
                        + "A parry is the riposte opening (parryStaggerSeconds 0–10s, default 5; harder shove, ~0.12s enemy pose hold, triple bell).\n"
                        + "Block plays one quieter ding and a ~0.12s player guard pose. "
                        + "Parry plays the poise triple ring. The player uses that same mild guard pose. "
                        + "blockStaggerHitstopSeconds stays 0 — a block does not freeze either animator. "
                        + "Parry hitstop is the enemy animator only, not Time.timeScale.",
                        MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "blockStaggerSeconds",
                        "parryStaggerSeconds",
                        "blockParryStaggerImpulseScale",
                        "parryStaggerImpulseBonus",
                        "blockStaggerHitstopSeconds",
                        "parryStaggerHitstopSeconds",
                        "parryClashVfxPrefab",
                        "parryClashVfxScale",
                        "parryClashVfxLifetimeSeconds");
                });

                DMStudioStyles.DrawSection("Strong melee", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox(
                        "Sword out: tap left mouse (release before the charge time) for the light swing. "
                        + "Hold past strongMeleeChargeSeconds and the weapon stays drawn back until you release — no max hold, no auto-swing. "
                        + "Release then plays the strong sword clip. Right mouse stays block / parry. "
                        + "strongMeleeDamageMultiplier scales the normal melee roll. "
                        + "strongMeleeDamageStartNormalized / End gate Strong SwordAttack B hitboxes (runtime + controller fallback).",
                        MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "strongMeleeChargeSeconds",
                        "strongMeleeDamageMultiplier",
                        "strongMeleeDamageStartNormalized",
                        "strongMeleeDamageEndNormalized",
                        "strongMeleeAnimSpeedMultiplier");
                });

                DMStudioStyles.DrawSection("Dummy spring / stagger", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox(
                        "Training dummy hit wobble. Runtime reads DM_CombatCoreProfile.Live; "
                        + "serialized TrainingDummy fields are fallback if the profile is missing.\n"
                        + "Lower spring + lower damping = longer cartoon bounce. "
                        + "Poise-break uses the same spring with trainingDummyPoiseStaggerImpulseScale "
                        + "(1 = same as a normal hit, >1 = stronger wobble).\n"
                        + "Exaggerated wobble: spring 10 / damp 3, rot spring 12 / damp 3, "
                        + "impulse 2.2, torque 80, max pos 1.0, max rot 50, stagger scale 3.",
                        MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "trainingDummyPositionSpring",
                        "trainingDummyPositionDamping",
                        "trainingDummyRotationSpring",
                        "trainingDummyRotationDamping",
                        "trainingDummyHitImpulse",
                        "trainingDummyHitTorque",
                        "trainingDummyMaxPositionOffset",
                        "trainingDummyMaxRotationOffset",
                        "trainingDummyPoiseStaggerImpulseScale");
                });
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedProfile.ApplyModifiedProperties();
                EditorUtility.SetDirty(profile);
            }
        }

        private void DrawRangedTab()
        {
            DMStudioStyles.DrawSection("Genesis ranged authoring", DMStudioStyles.ContentPanel, () =>
            {
                EditorGUILayout.HelpBox(
                    "Ammo FX, Hit Catalog, and Surface Damage already live in Genesis Studio → Combat. "
                    + "This tab is a jump list — it does not duplicate those inspectors.",
                    MessageType.Info);

                if (GUILayout.Button("Genesis Studio → Combat → Ammo FX", GUILayout.Height(28f)))
                    GenesisStudioWindow.OpenTo("combat", "ammo-fx");

                if (GUILayout.Button("Genesis Studio → Combat → Hit Catalog", GUILayout.Height(28f)))
                    GenesisStudioWindow.OpenTo("combat", "hit-catalog");

                if (GUILayout.Button("Genesis Studio → Combat → Surface Damage", GUILayout.Height(28f)))
                    GenesisStudioWindow.OpenTo("combat", "surface-damage");
            });

            DMStudioStyles.DrawSection("Combat Studio / Genesis", DMStudioStyles.ContentPanel, () =>
            {
                if (GUILayout.Button("Open Genesis Studio (Combat Core)", GUILayout.Height(28f)))
                    GenesisStudioWindow.OpenTo("combat", "combat-core");

                if (GUILayout.Button("Build and apply melee animation set", GUILayout.Height(28f)))
                    DMMeleeAnimationSetApplier.BuildAndApply();
            });
        }

        private void DrawPlayTab()
        {
            DMStudioStyles.DrawSection("Tune scene", DMStudioStyles.ContentPanel, () =>
            {
                EditorGUILayout.HelpBox(
                    "Phase 2 combat is accepted in Dark Matter Genesis v1.6.5. Terrains and hierarchy stay on.",
                    MessageType.Info);

                if (GUILayout.Button("Open Dark Matter Genesis v1.6.5", GUILayout.Height(32f)))
                    OpenSceneIfExists(PlayableScenePath, "Playable scene");
            });

            DMStudioStyles.DrawSection("Combat_Sandbox (retired)", DMStudioStyles.ContentPanel, () =>
            {
                EditorGUILayout.HelpBox(
                    "Combat_Sandbox is retired as the tune target. Leftover scene is forensics only — do not retune melee there.",
                    MessageType.Warning);

                using (new EditorGUI.DisabledScope(!System.IO.File.Exists(SandboxScenePath)))
                {
                    if (GUILayout.Button("Open leftover Combat_Sandbox (forensics)", GUILayout.Height(24f)))
                        OpenSceneIfExists(SandboxScenePath, "Combat Sandbox");
                }
            });
        }

        private void DrawRoadmapTab()
        {
            DMStudioStyles.DrawSection("Master plan", DMStudioStyles.ContentPanel, () =>
            {
                EditorGUILayout.HelpBox(
                    "§31 implementation order from DMG_Combat_Plan_v2.md. Phase 2 = #2 Core combat. "
                    + "Do not start #3 utility brain, Director, Momentum, or Sick Stick until Phase 2 sign-off.",
                    MessageType.Info);

                if (GUILayout.Button("Ping DMG_Combat_Plan_v2.md", GUILayout.Height(28f)))
                    PingCombatPlan();
            });

            DrawRoadmapCard("1", "Audit", "Done Oct 3, 2026", "Architecture maps, Invector wrap, animation tag sheet.");
            DrawRoadmapCard(
                "2",
                "Core combat",
                "Partial — Phase 2 in progress",
                "Damage, hit detection, poise, i-frames, status, Combat Core. Tune in v1.6.5. Gate 0 attack yaw already shipped.");
            DrawRoadmapCard("3", "Unified brain", "Missing — next after Phase 2", "Utility scoring, archetype, personality, awareness. Migrate Humanoid_Enemy.");
            DrawRoadmapCard("4", "Combat Director", "Missing", "Tokens, coordination, flanking, intensity, morale.");
            DrawRoadmapCard("5", "Plasma sword template", "Partial", "Attack, damage, hit react, burn, Blade / Survival branches.");
            DrawRoadmapCard("6", "Momentum + finishers", "Missing", "Build-up, finisher selection on the migrated enemy.");
            DrawRoadmapCard("7–8", "Sick Stick & specials", "Missing", "Sick Stick trigger/puke, specials, Overdrive.");
            DrawRoadmapCard("9", "Remaining elements", "Names locked", "Cryo, Energy, Laser, Ion display mapping locked; implementation later.");
            DrawRoadmapCard("10", "Body & dismemberment", "Missing", "Body damage on one enemy, element-specific finishers.");
            DrawRoadmapCard("11–14", "Companions / skills / memory", "Partial / missing", "Roles, Control/Marksman, environment noise, combat memory.");
            DrawRoadmapCard("15–16", "Encounters & studio", "Partial", "Authored encounters; Studio completion + performance (WorldChrome throttle shipped).");
        }

        private static void DrawRoadmapCard(string step, string title, string status, string note)
        {
            DMStudioStyles.DrawSection("§31 #" + step + "  " + title, DMStudioStyles.ContentPanel, () =>
            {
                EditorGUILayout.LabelField("Status", status);
                EditorGUILayout.HelpBox(note, MessageType.None);
            });
        }

        private void DrawProfileHeader(string title)
        {
            DMStudioStyles.DrawSection(title, DMStudioStyles.ContentPanel, () =>
            {
                EditorGUILayout.ObjectField("Asset", profile, typeof(DM_CombatCoreProfile), false);
                EditorGUILayout.HelpBox(
                    "Play mode reads Resources/Combat/DM_CombatCoreProfile. "
                    + "Same fields appear under Genesis Studio → Combat → Combat Core.",
                    MessageType.Info);
            });
        }

        private void DrawPoiseStaggerAudioSection()
        {
            if (audioProfile == null)
                audioProfile = AssetDatabase.LoadAssetAtPath<GameAudioProfile>(AudioProfilePath);

            if (serializedAudioProfile == null && audioProfile != null)
                serializedAudioProfile = new SerializedObject(audioProfile);

            if (serializedAudioProfile == null)
            {
                DMStudioStyles.DrawSection("Poise break audio", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox(
                        "GameAudioProfile not found at Resources/GameAudioProfile.asset.",
                        MessageType.Warning);
                });
                return;
            }

            serializedAudioProfile.Update();
            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(audioProfile, "Edit Poise Stagger Audio");

            DMStudioStyles.DrawSection("Poise break audio (triple ding)", DMStudioStyles.ContentPanel, () =>
            {
                EditorGUILayout.ObjectField("Game audio profile", audioProfile, typeof(GameAudioProfile), false);
                EditorGUILayout.HelpBox(
                    "When poise hits zero, CombatEvents.Stagger plays this clip three times "
                    + "(rising pitch). Empty slots fall back to level-up / achievement clips.",
                    MessageType.Info);
                using (DMStudioStyles.BeginProfileInspector(serializedAudioProfile))
                {
                    DMStudioStyles.DrawPropertyFields(
                        serializedAudioProfile,
                        "poiseStaggerDingClips",
                        "poiseStaggerDingSpacing",
                        "poiseStaggerDingVolume");
                }
            });

            if (EditorGUI.EndChangeCheck())
            {
                serializedAudioProfile.ApplyModifiedProperties();
                EditorUtility.SetDirty(audioProfile);
            }
        }

        private static void OpenSceneIfExists(string scenePath, string title)
        {
            if (!System.IO.File.Exists(scenePath))
            {
                EditorUtility.DisplayDialog(title, "Scene not found:\n" + scenePath, "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EditorSceneManager.OpenScene(scenePath);
        }

        private static void PingCombatPlan()
        {
            Object plan = AssetDatabase.LoadAssetAtPath<Object>(CombatPlanPath);
            if (plan == null)
            {
                EditorUtility.DisplayDialog("Combat plan", "Missing:\n" + CombatPlanPath, "OK");
                return;
            }

            EditorGUIUtility.PingObject(plan);
            Selection.activeObject = plan;
        }

        private static DM_CombatCoreProfile LoadOrCreateProfile()
        {
            DM_CombatCoreProfile asset = AssetDatabase.LoadAssetAtPath<DM_CombatCoreProfile>(ProfileResourcesPath);
            if (asset != null)
                return asset;

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources/Combat"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources"))
                    AssetDatabase.CreateFolder("Assets/_Project", "Resources");
                AssetDatabase.CreateFolder("Assets/_Project/Resources", "Combat");
            }

            asset = ScriptableObject.CreateInstance<DM_CombatCoreProfile>();
            AssetDatabase.CreateAsset(asset, ProfileResourcesPath);
            AssetDatabase.SaveAssets();
            return asset;
        }
    }
}
