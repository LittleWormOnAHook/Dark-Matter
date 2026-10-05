using Project.Audio;
using Project.Combat;
using Project.EditorTools.GenesisStudio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project.EditorTools.Combat
{
    /// <summary>
    /// Combat Studio: combat core profile tuning, sandbox shortcuts, Genesis Studio bridge.
    /// </summary>
    public sealed class DMCombatStudioWindow : EditorWindow
    {
        private const string ProfileResourcesPath = "Assets/_Project/Resources/Combat/DM_CombatCoreProfile.asset";
        private const string AudioProfilePath = "Assets/_Project/Resources/GameAudioProfile.asset";
        private const string SandboxScenePath = "Assets/_Project/Scenes/Combat/Combat_Sandbox.unity";

        private static readonly string[] Tabs = { "Combat Core", "Sandbox", "Links" };

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

        private void OnEnable()
        {
            profile = LoadOrCreateProfile();
            serializedProfile = profile != null ? new SerializedObject(profile) : null;
            audioProfile = AssetDatabase.LoadAssetAtPath<GameAudioProfile>(AudioProfilePath);
            serializedAudioProfile = audioProfile != null ? new SerializedObject(audioProfile) : null;
            tab = EditorPrefs.GetInt("DM.CombatStudio.Tab", 0);
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
                "Poise, i-frames, status rules, and combat sandbox — runtime reads DM_CombatCoreProfile from Resources.");

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
                case 1:
                    DrawSandboxTab();
                    break;
                case 2:
                    DrawLinksTab();
                    break;
                default:
                    DrawCombatCoreTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawCombatCoreTab()
        {
            DMStudioStyles.DrawSection("Combat Core profile", DMStudioStyles.ContentPanel, () =>
            {
                EditorGUILayout.ObjectField("Asset", profile, typeof(DM_CombatCoreProfile), false);
                EditorGUILayout.HelpBox(
                    "Play mode reads Resources/Combat/DM_CombatCoreProfile. "
                    + "Same fields appear under Genesis Studio → Combat → Combat Core.",
                    MessageType.Info);
            });

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

                DMStudioStyles.DrawSection("Block / parry guard-break", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox(
                        "A held block is a light guard tap. A parry (block inside parryWindowSeconds) is the riposte.\n"
                        + "Souls, God of War, and Sekiro: block recoil is short (~0.35–0.45s lockout, small flinch, no hitstop). "
                        + "A parry is the opening (~0.7–0.85s lockout, harder shove, ~0.12s enemy pose hold, triple bell).\n"
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
                        "parryStaggerHitstopSeconds");
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

                DMStudioStyles.DrawSection("Melee hit detection", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox(
                        "Scales Invector vHitBox on player and humanoid enemy weapons. Wider block cone uses defaultDefenseRange (half-angle from forward).",
                        MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "meleeHitboxWidthScale",
                        "meleeHitboxReachScale",
                        "meleeBlockDefenseHalfAngle");
                });

                DMStudioStyles.DrawSection("Strong melee", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox(
                        "Sword out: tap left mouse (release before the charge time) for the light swing. "
                        + "Hold past strongMeleeChargeSeconds and the weapon stays drawn back until you release — no max hold, no auto-swing. "
                        + "Release then plays the strong sword clip. Right mouse stays block / parry. "
                        + "strongMeleeDamageMultiplier scales the normal melee roll.",
                        MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "strongMeleeChargeSeconds",
                        "strongMeleeDamageMultiplier");
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

        private void DrawSandboxTab()
        {
            DMStudioStyles.DrawSection("Combat sandbox scene", DMStudioStyles.ContentPanel, () =>
            {
                EditorGUILayout.HelpBox(
                    "Flat plane + training dummy + humanoid spawner. Use for poise, status, and hit reaction tests.",
                    MessageType.Info);

                if (GUILayout.Button("Open Combat Sandbox Scene"))
                {
                    if (System.IO.File.Exists(SandboxScenePath))
                        EditorSceneManager.OpenScene(SandboxScenePath);
                    else
                        EditorUtility.DisplayDialog(
                            "Combat Sandbox",
                            "Scene not found. Run Tools → Dark Matter Genesis → Combat → Create Combat Sandbox Scene first.",
                            "OK");
                }

                if (GUILayout.Button("Create / Refresh Combat Sandbox Scene"))
                    EditorApplication.ExecuteMenuItem(DarkMatterGenesisEditorMenus.Combat + "Create Combat Sandbox Scene");
            });
        }

        private void DrawLinksTab()
        {
            DMStudioStyles.DrawSection("Genesis Studio", DMStudioStyles.ContentPanel, () =>
            {
                EditorGUILayout.HelpBox("Open Genesis Studio for filtered Combat Core sliders during Play.", MessageType.None);
                if (GUILayout.Button("Open Genesis Studio"))
                    EditorApplication.ExecuteMenuItem("Tools/Dark Matter Genesis/Genesis Studio");
            });

            DMStudioStyles.DrawSection("Related authoring", DMStudioStyles.ContentPanel, () =>
            {
                if (GUILayout.Button("Build and apply melee animation set"))
                    DMMeleeAnimationSetApplier.BuildAndApply();

                if (GUILayout.Button("Genesis Studio → Combat → Ammo FX"))
                    EditorUtility.DisplayDialog(
                        "Ammo FX",
                        "In Genesis Studio, pick Combat → Ammo FX for projectile/impact profiles.",
                        "OK");
            });
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
