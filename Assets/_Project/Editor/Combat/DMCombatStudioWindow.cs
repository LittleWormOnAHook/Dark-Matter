using FORGE3D;
using Project.AI;
using Project.Audio;
using Project.Combat;
using Project.Combat.Turrets;
using Project.EditorTools;
using Project.EditorTools.GenesisStudio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project.EditorTools.Combat
{
    /// <summary>
    /// Combat Studio Phase 2 shell: Core / Melee (Player vs Enemy groupings) / Ranged / Throwables / Play / Roadmap.
    /// Runtime numbers live on DM_CombatCoreProfile. Combat_Sandbox is forensics only.
    /// </summary>
    public sealed class DMCombatStudioWindow : EditorWindow
    {
        private const string ProfileResourcesPath = "Assets/_Project/Resources/Combat/DM_CombatCoreProfile.asset";
        private const string AudioProfilePath = "Assets/_Project/Resources/GameAudioProfile.asset";
        private const string PlayableScenePath = "Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity";
        private const string SandboxScenePath = "Assets/_Project/Scenes/Combat/Combat_Sandbox.unity";
        private const string CombatPlanPath = "Assets/_Project/Documentation/Design/Combat/DMG_Combat_Plan_v2.md";

        public const int TabCore = 0;
        public const int TabMelee = 1;
        public const int TabRanged = 2;
        public const int TabThrowables = 3;
        public const int TabTurrets = 4;
        public const int TabPlay = 5;
        public const int TabDirector = 6;
        public const int TabRoadmap = 7;

        private const string DirectorProfilePath = "Assets/_Project/Resources/Combat/DM_CombatDirectorProfile.asset";
        private const string TurretProfilePath = DMTurretProfile.AssetPath;

        private static readonly string[] Tabs = { "Core", "Melee", "Ranged", "Throwables", "Turrets", "Play", "Director", "Roadmap" };

        private DM_CombatCoreProfile profile;
        private SerializedObject serializedProfile;
        private DM_CombatDirectorProfile directorProfile;
        private SerializedObject serializedDirector;
        private DMTurretProfile turretProfile;
        private SerializedObject serializedTurret;
        private GameAudioProfile audioProfile;
        private SerializedObject serializedAudioProfile;
        private Vector2 scrollPosition;
        private int tab;

        [MenuItem("Tools/Dark Matter Genesis/Combat/Combat Studio", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Combat_Studio)]
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
            turretProfile = LoadOrCreateTurretProfile();
            serializedTurret = turretProfile != null ? new SerializedObject(turretProfile) : null;
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
                "Core and Melee group tunables under Player vs Enemy. Ranged links, Throwables, Turrets (FORGE3D wrap), v1.6.6 Play, §31 roadmap.");

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

            Project.EditorTools.GenesisStudio.DMStudioSearch.DrawField("CombatStudioSearch"); // 2026-10-12 global Studio search
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            switch (tab)
            {
                case TabMelee:
                    DrawMeleeTab();
                    break;
                case TabRanged:
                    DrawRangedTab();
                    break;
                case TabThrowables:
                    DrawThrowablesTab();
                    break;
                case TabTurrets:
                    DrawTurretsTab();
                    break;
                case TabPlay:
                    DrawPlayTab();
                    break;
                case TabDirector:
                    DrawDirectorTab();
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
            if (profile == null)
            {
                var blockers = DMStudioActionValidation.CollectMissingProfileAsset(
                    ProfileResourcesPath,
                    "DM_CombatCoreProfile");
                DMStudioActionValidation.DrawBlockersHelpBox(blockers, "edit Combat Core");
            }

            if (serializedProfile == null)
                return;

            serializedProfile.Update();
            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(profile, "Edit Combat Core Profile");

            using (DMStudioStyles.BeginProfileInspector(serializedProfile))
            {
                DrawCombatActorHeader(
                    "Player",
                    "Kade defensive windows and dodge/dash invulnerability. Parry timing pairs with Melee → Player → block & parry.");

                DMStudioStyles.DrawSection("I-frames (dodge / dash)", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox(
                        "Invulnerability after a dodge roll or combat dash. Does not apply while blocking.",
                        MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(serializedProfile, "dodgeIFrameSeconds", "dashIFrameSeconds");
                });

                DMStudioStyles.DrawSection("Parry window", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox(
                        "Block input inside this window counts as a parry (riposte). Guard-break stagger on the attacker is under Melee → Enemy.",
                        MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(serializedProfile, "parryWindowSeconds");
                });

                DrawCombatActorHeader(
                    "Enemy & targets",
                    "Poise drain, break stagger, and break audio for humanoids and the training dummy.");

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

                DMStudioStyles.DrawSection("Attack interrupts", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox(
                        "A player melee hit that lands while an enemy is mid-swing can interrupt it. "
                        + "Always: poise break; hit inside the early window; heavy/charged hit vs a light swing. "
                        + "Otherwise a chance roll (base + early + crit + heavy + chain + low poise + low health - enemy resistance, capped at Max Chance). "
                        + "From the commit window on, only a poise break interrupts. Charged swing: super-armor from its commit start. "
                        + "After an interrupt: immunity (no more chance interrupts) and a recovery lock. "
                        + "Per-enemy: EnemyDefinition interruptResistance / attackArmor / poiseMultiplier. Parry and guard break use their own stagger.",
                        MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "enableAttackInterrupts",
                        "interruptBaseChanceLight",
                        "interruptBaseChanceFollowUp",
                        "interruptBaseChanceCharged",
                        "interruptEarlyWindowNormalized",
                        "interruptEarlyWindowGuaranteed",
                        "interruptEarlyBonus",
                        "interruptCritBonus",
                        "interruptHeavyHitBonus",
                        "interruptChainWindowSeconds",
                        "interruptChainBonusPerHit",
                        "interruptChainBonusMax",
                        "interruptLowPoiseThreshold",
                        "interruptLowPoiseBonus",
                        "interruptLowHealthThreshold",
                        "interruptLowHealthBonus",
                        "interruptMaxChance",
                        "interruptGuaranteedOnPoiseBreak",
                        "interruptGuaranteedOnHeavyVsLight",
                        "interruptCommitStartLight",
                        "interruptCommitStartCharged",
                        "interruptImmunitySeconds",
                        "interruptRecoverySeconds",
                        "interruptReactionSeconds",
                        "logInterruptsInPlay");
                });

                DMStudioStyles.DrawSection("Training dummy poise", DMStudioStyles.ContentPanel, () =>
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

                DrawCombatHitAudioSection();
                DrawPoiseStaggerAudioSection();

                DrawCombatActorHeader(
                    "Shared rules",
                    "Status stacks and future hitstop hooks that apply across combat actors.");

                DMStudioStyles.DrawSection("Status (global)", DMStudioStyles.ContentPanel, () =>
                {
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "statusMaxStacks",
                        "statusImmunityWindowSeconds",
                        "statusBossMultiplier");
                });

                DMStudioStyles.DrawSection("Hitstop hooks & debug", DMStudioStyles.ContentPanel, () =>
                {
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "hitstopLightFrames",
                        "hitstopHeavyFrames",
                        "enableMeleeHitstop",
                        "hitstopCritBonusFrames",
                        "hitstopHoldSpeed",
                        "hitstopFreezesAttacker",
                        "logCombatEventsInPlay");
                });

                DMStudioStyles.DrawSection("Damage number tiers", DMStudioStyles.ContentPanel, () =>
                {
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "useDamageNumberTiers",
                        "damageNumberNormalColor",
                        "damageNumberCritColor",
                        "damageNumberHeavyColor",
                        "damageNumberCritScale",
                        "damageNumberHeavyScale");
                });
                DMStudioStyles.DrawSection("Hit reactions: overview and recovery (Slice 2 / B4)", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox("Procedural reactions to a landed PLAYER melee hit. B4: poise break / heavy Big reaction / chain stumble lock the enemy's next swing for Hit Recovery Seconds (postSwingLock untouched). B1: torso twist toward the swing side + small backstep. B2: hit 2 in a chain = stronger flinch, hit 3 = full-body stumble, capped by the Big Reaction Immunity. B3: heavy hits slide the enemy back and bend the waist; committed swings and super-armor enemies only bend.", MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "enableHitReactions",
                        "hitRecoverySeconds",
                        "hitRecoveryOnPoiseBreak",
                        "hitRecoveryOnHeavyHit",
                        "hitRecoveryOnChainStumble",
                        "bigReactionImmunitySeconds",
                        "hitSlideMaxLedgeDrop",
                        "hitSlideMaxTotalMeters");
                });

                DMStudioStyles.DrawSection("Directional flinch (Slice 2 / B1)", DMStudioStyles.ContentPanel, () =>
                {
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "directionalFlinchEnabled",
                        "swingSideLightA",
                        "swingSideLightB",
                        "swingSideLightC",
                        "swingSideCharged",
                        "swingTwistDegrees",
                        "directionalFlinchDegrees",
                        "directionalFlinchSeconds",
                        "swingLateralBias",
                        "lightBackstepMeters",
                        "lightBackstepSeconds",
                        "poiseBreakBackstepMeters");
                });

                DMStudioStyles.DrawSection("Hit-chain escalation (Slice 2 / B2)", DMStudioStyles.ContentPanel, () =>
                {
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "chainReactionsEnabled",
                        "hitChainWindowSeconds",
                        "chainStrongHitCount",
                        "chainStumbleHitCount",
                        "chainStrongFlinchMultiplier",
                        "chainStrongBackstepMeters",
                        "chainStumbleSeconds",
                        "chainStumbleBackstepMeters",
                        "chainStumbleLeanDegrees");
                });

                DMStudioStyles.DrawSection("Heavy hit slide-back and waist bend (Slice 2 / B3)", DMStudioStyles.ContentPanel, () =>
                {
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "heavyReactionEnabled",
                        "heavyBigReactionSeconds",
                        "heavySlideMeters",
                        "heavySlideSeconds",
                        "heavyWaistBendDegrees",
                        "heavyWaistBendSeconds",
                        "heavyRespectsCommitment",
                        "heavyFallbackScale");
                });

                DMStudioStyles.DrawSection("Directional reactions v2: strike direction (Slice 2b)", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox("Slice 2b master switch and strike direction. The lean follows the blade motion at the hit (combo-step table above as fallback). Off = Slice 2 behaviour.", MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "enableDirectionalReactionsV2",
                        "swingDirectionSource",
                        "bladeVelocityMinSpeed",
                        "bladeVelocityWindowSeconds",
                        "strikeLeanSideShare");
                });

                DMStudioStyles.DrawSection("Light lean: weighted spring + head whip (Slice 2b)", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox("Light hits lean the spine / chest / head along the strike, fast attack then a spring settle with overshoot; the head starts late and swings further. Root never moves (Light Backstep Meters = 0). Hold Peak Through Hitstop = the lean snaps in during the freeze and releases after it.", MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "lightLeanDegrees",
                        "lightTwistDegrees",
                        "lightLeanAttackSeconds",
                        "lightLeanSettleSeconds",
                        "leanSpringFrequency",
                        "leanSpringDamping",
                        "leanSpineWeight",
                        "leanChestWeight",
                        "leanHeadWeight",
                        "headWhipDelaySeconds",
                        "headWhipMultiplier",
                        "critLeanMultiplier",
                        "leanHoldPeakThroughHitstop");
                });

                DMStudioStyles.DrawSection("Reaction weight: damage / heavy attack (Slice 2b)", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox("Every reaction scales with (damage / reference) ^ exponent, clamped, x heavy attack multiplier. Weapon heft shows up through damage. Hitstop Weight Bonus Frames adds freeze frames to the heaviest hits.", MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "reactionDamageReference",
                        "reactionDamageExponent",
                        "reactionDamageScaleMin",
                        "reactionDamageScaleMax",
                        "heavyAttackWeightMultiplier",
                        "hitstopWeightBonusFrames");
                });

                DMStudioStyles.DrawSection("Reaction randomness (Slice 2b)", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox("Rolled per hit. Avoid Repeat Variation = never the same clip / start variation twice in a row on one enemy and consecutive lean directions kept apart.", MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "leanAngleJitterDegrees",
                        "reactionIntensityRandomMin",
                        "reactionIntensityRandomMax",
                        "twistRandomDegrees",
                        "twistSignFlipChance",
                        "clipSpeedRandomMin",
                        "clipSpeedRandomMax",
                        "clipWeightRandomMin",
                        "clipWeightRandomMax",
                        "clipStartOffsetRandomMax",
                        "avoidRepeatVariation",
                        "heavyStumbleStepChance",
                        "heavyStumbleStepMeters");
                });

                DMStudioStyles.DrawSection("Medium / heavy directional clips (Slice 2b)", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox("PROTOFACTOR GetHitLight / GetHitHeavy on the DM_HitReact layers of Invector@ShooterMelee.controller. Arms and hands are masked so every enemy keeps its own sword / rifle / pistol / unarmed grip. Medium = chain hit 2 (additive torso), heavy = charged hit / stumble / poise break (body). Controllers without the layers fall back to the lean (+ Invector Big reaction on heavy).", MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "mediumUsesClip",
                        "mediumClipWeight",
                        "mediumClipSpeed",
                        "mediumLeanScale",
                        "mediumAltHeavyClipChance",
                        "mediumAltHeavyClipWeight",
                        "heavyUsesClip",
                        "heavyClipWeight",
                        "heavyClipSpeed",
                        "heavyLeanDegrees",
                        "heavyLeanSettleSeconds",
                        "reactionBlendInSeconds",
                        "reactionBlendOutSeconds");
                });

                DMStudioStyles.DrawSection("Knockdown: animation only, no ragdoll (Slice 2b)", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox("Heavy-tier hits roll the chance below (poise-break chance replaces the base; low-poise and crit bonuses; cap; x (1 - EnemyDefinition knockdownResistance)). Death* fall cut at the land point -> Dead* hold -> GetBackUp*, matched by strike side. Weapon stays in hand, aim off while down. Downed enemies take damage but get no new reactions; the down time is not extended. Ragdoll stays for death only.", MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "knockdownEnabled",
                        "knockdownChancePercent",
                        "knockdownLowPoiseBonusPercent",
                        "knockdownCritBonusPercent",
                        "knockdownOnPoiseBreakPercent",
                        "knockdownMaxChancePercent",
                        "knockdownPushMeters",
                        "knockdownPushSeconds",
                        "knockdownFallSpeed",
                        "knockdownFallExitFront",
                        "knockdownFallExitBack",
                        "knockdownFallExitLeft",
                        "knockdownFallExitRight",
                        "knockdownDownSeconds",
                        "knockdownDownRandomSeconds",
                        "getUpPlaybackSpeed",
                        "getUpExitNormalized",
                        "knockdownAimFadeInSeconds",
                        "getUpInvulnerableSeconds",
                        "knockdownImmunitySeconds",
                        "knockdownReleasesAttackSlot",
                        "knockdownMaxSlopeDegrees",
                        "knockdownLedgeCheckMeters",
                        "retireCritRagdollKnockdown",
                        "logHitReactionsInPlay");
                });

                DMStudioStyles.DrawSection("Player hit reactions (DM)", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox("Enemy hits on the player: lean every hit, light / heavy gated by the stagger chances, rare knockdown on heavy hits only (cap + cooldown). God mode / audit: see playerReactionsUnderGodMode.", MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "enablePlayerHitReactions",
                        "playerLeanDegreesMin",
                        "playerLeanDegreesMax",
                        "playerLeanAttackSeconds",
                        "playerLeanSettleSeconds",
                        "playerHeadWhipScale",
                        "playerLeanJitterDegrees",
                        "playerIntensityJitter",
                        "playerReactionIntensityMax",
                        "playerClipSpeedMin",
                        "playerClipSpeedMax",
                        "playerClipWeightMin",
                        "playerClipWeightMax",
                        "playerHeavyDamageFraction",
                        "playerPushMeters",
                        "playerPushSeconds",
                        "playerStumbleChance",
                        "playerLightLockSeconds",
                        "playerHeavyLockSeconds",
                        "playerHeavyMoveScale",
                        "playerKnockdownChance",
                        "playerKnockdownBonusChance",
                        "playerKnockdownChanceCap",
                        "playerKnockdownCooldownSeconds",
                        "playerDownHoldSecondsMin",
                        "playerDownHoldSecondsMax",
                        "playerDownedDamageScale",
                        "playerGetUpIFrameSeconds",
                        "playerRollCancel",
                        "playerRollCancelAfterSeconds",
                        "playerReactionsUnderGodMode",
                        "playerAdsLeanScale",
                        "logPlayerHitReactions",
                        "playerHitSoundUnderGodMode");
                });

            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedProfile.ApplyModifiedProperties();
                EditorUtility.SetDirty(profile);
            }
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
                DrawCombatActorHeader(
                    "Player",
                    "Kade sword hitboxes, attack/block facing, hold-to-strong, and parry clash VFX.");

                DMStudioStyles.DrawSection("Hitboxes & block cone", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox(
                        "Scales player vHitBox width and reach. meleeBlockDefenseHalfAngle is the frontal block/parry arc (degrees from forward).",
                        MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "meleeHitboxWidthScale",
                        "meleeHitboxReachScale",
                        "meleeBlockDefenseHalfAngle");
                });

                DMStudioStyles.DrawSection("Attack / block facing (yaw)", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox(
                        "Gate 0 attack yaw is shipped — do not retune unless it regresses. "
                        + "Attack auto-face uses a tighter cone than block; block assist shares max distance and turn speed. "
                        + "With enableCombatCameraSnap off, auto-face still rotates the body but the camera stays on its bearing and ECM2 focus lock uses CombatFocusController yawSmoothLambda.",
                        MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "meleeAttackAutoFaceHalfAngle",
                        "meleeBlockAutoFaceHalfAngle",
                        "meleeBlockAutoFaceMaxDistance",
                        "meleeBlockAutoFaceTurnSpeed",
                        "enableCombatCameraSnap",
                        "combatCameraSnapSeconds");
                });

                DMStudioStyles.DrawSection("Melee combo target lock", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox("One target per combo; eased wind-up-only turn, capped per swing; single rotation owner in FixedUpdate.", MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "meleeComboTargetLock",
                        "meleeTargetAcquireDistance",
                        "meleeTargetReleaseDistance",
                        "meleeTargetIntentConeHalfAngle",
                        "meleeTargetAngleWeight",
                        "meleeComboLockGraceSeconds",
                        "meleeRetargetStickAngle",
                        "meleeRetargetMinSeconds",
                        "meleeRetargetStickMagnitude",
                        "meleeAssistTurnSpeed",
                        "meleeAssistSmoothTime",
                        "meleeAssistMaxPerSwingDegrees",
                        "meleeAssistWindupFraction",
                        "meleeAssistSoftFadeDegrees",
                        "meleeTargetIgnoreDowned",
                        "lightComboForcesStrafe",
                        "logMeleeFacing");
                });

                DMStudioStyles.DrawSection("Melee block target lock", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox("Block auto-face: sticky target (acquire / release / cone / retarget from the combo lock), eased turn, single FixedUpdate owner.", MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "meleeBlockTargetLock",
                        "meleeBlockAssistTurnSpeed",
                        "meleeBlockAssistSmoothTime",
                        "meleeBlockLockGraceSeconds",
                        "meleeBlockFacingToleranceDegrees");
                });

                DMStudioStyles.DrawSection("Slow motion (charged attack / perfect parry)", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox("DMSlowMotionDirector owns Time.timeScale + fixedDeltaTime. Yields to menus, pause, loot, main menu and audits. Times are real seconds.", MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "enableChargedSlowMo",
                        "chargedSlowMoChance",
                        "chargedSlowMoCooldownSeconds",
                        "chargedSlowMoSeconds",
                        "chargedSlowMoTimeScale",
                        "chargedSlowMoEaseInSeconds",
                        "chargedSlowMoEaseOutSeconds",
                        "chargedSlowMoReach",
                        "chargedSlowMoConeHalfAngle",
                        "enableParrySlowMo",
                        "parrySlowMoChance",
                        "parrySlowMoTotalSeconds",
                        "parrySlowMoTimeScale",
                        "parrySlowMoEaseInSeconds",
                        "parrySlowMoEaseOutSeconds",
                        "parrySlowMoThreatRadius",
                        "parrySlowMoCooldownSeconds",
                        "slowMoAllowInAudit",
                        "logSlowMo");
                });

                DMStudioStyles.DrawSection("Player combat buffer (enemy AI)", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox(
                        "Global keep-away ring vs Kade only — Mathf.Max with each enemy prefab minCombatSeparation. "
                        + "Turn off enableGlobalCombatSeparation or set radius to 0 for prefab-only spacing. Does not retune Player_v7.",
                        MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "enableGlobalCombatSeparation",
                        "playerCombatBufferRadius");
                });

                DMStudioStyles.DrawSection("Light combo anim speed (A→B→C)", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox(
                        "Per-slot Animator.speed while in WeakAttacks/SwordAttack A, B, or C (regular chained lights). "
                        + "1 = clip default, 1.25 = 25% faster (0.75–2). Play reads profile.Live via PioneerMeleeDamageWindowTracker LateUpdate.",
                        MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "lightComboAnimSpeedA",
                        "lightComboAnimSpeedB",
                        "lightComboAnimSpeedC");
                });

                DMStudioStyles.DrawSection("Light combo attack move (A→B→C)", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox(
                        "Crimson-style strafe while the FullBody weak chain plays. Only SwordAttack A→B→C — not strong/charge, parry, or Hold-E upper-body combo. "
                        + "When enableLightComboAttackMove is on and lockMovementDuringLightCombo is off, Pioneer bypasses Invector LockMovement/LockRotation tags for that chain. "
                        + "Strafe speed is moveSpeed × lightComboStrafeSpeedMultiplier; lightComboForwardDriftMultiplier adds a small camera-forward nudge.",
                        MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "enableLightComboAttackMove",
                        "lightComboStrafeSpeedMultiplier",
                        "lightComboForwardDriftMultiplier",
                        "lockMovementDuringLightCombo");
                });

                DMStudioStyles.DrawSection("Light random pool anim speed", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox(
                        "WeakAttacks/SwordRandomAttack A/B/C — parallel random weak swings, not the combo chain.",
                        MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "lightRandomAnimSpeedA",
                        "lightRandomAnimSpeedB",
                        "lightRandomAnimSpeedC");
                });

                DMStudioStyles.DrawSection("Strong melee (hold release)", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox(
                        "Sword out: tap left mouse (release before the charge time) for the light swing. "
                        + "Hold past strongMeleeChargeSeconds and the weapon stays drawn back until you release — no max hold, no auto-swing. "
                        + "Release then plays the strong sword clip. Right mouse stays block / parry. "
                        + "strongMeleeDamageMultiplier scales the normal melee roll. "
                        + "strongMeleeDamageStartNormalized / End gate Strong SwordAttack B hitboxes (runtime + controller fallback). "
                        + "strongMeleeAnimSpeedMultiplier (0.75–2) drives charge + charged Strong A/B/C release. "
                        + "strongReleaseWeightA/B/C pick which strong clip plays on release (favor A). "
                        + "interactHoldComboAnimSpeed (default 1.75) for Hold E + light attack. "
                        + "strongMeleeAnimSpeedA/C tune strong A/C when those clips play outside charge. "
                        + "chargedHitsIgnoreEnemyWeapons skips the defender's sword / outgoing blade so a close charge waits for a body hit. Block and parry still register.",
                        MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "strongMeleeAnimSpeedA",
                        "strongMeleeAnimSpeedC",
                        "strongMeleeChargeSeconds",
                        "strongMeleeDamageMultiplier",
                        "strongMeleeDamageStartNormalized",
                        "strongMeleeDamageEndNormalized",
                        "strongMeleeAnimSpeedMultiplier",
                        "strongReleaseWeightA",
                        "strongReleaseWeightB",
                        "strongReleaseWeightC",
                        "interactHoldComboAnimSpeed",
                        "chargedHitsIgnoreEnemyWeapons");
                });

                DMStudioStyles.DrawSection("Block & parry (player feedback)", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox(
                        "Parry timing window lives on Core → Player → Parry window. "
                        + "Sparks spawn at blade contact on a successful parry.",
                        MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "parryClashVfxPrefab",
                        "parryClashVfxScale",
                        "parryClashVfxLifetimeSeconds");
                });

                DMStudioStyles.DrawSection("Camera shake", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox(
                        "Trauma on the existing CameraShake hub (0 = off). "
                        + "Useful amplitude is 0–1; values above 1 cap at full shake. "
                        + "Duration holds trauma then decays. "
                        + "Block is smallest, parry medium, charged hit (on a real enemy hit, not swing start) strongest.",
                        MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "chargedHitShakeAmplitude",
                        "chargedHitShakeDurationSeconds",
                        "parryShakeAmplitude",
                        "parryShakeDurationSeconds",
                        "blockShakeAmplitude",
                        "blockShakeDurationSeconds",
                        "lightHitShakeAmplitude",
                        "lightHitShakeDurationSeconds",
                        "heavyHitShakeAmplitude",
                        "heavyHitShakeDurationSeconds");
                });

                DrawCombatActorHeader(
                    "Enemy & targets",
                    "Humanoid melee reach, guard-break lockouts when Kade blocks or parries, and training dummy wobble.");

                DMStudioStyles.DrawSection("Melee hitboxes & strike range", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox(
                        "Scales humanoid enemy vHitBox and multiplies EnemyCombat.attackRange for AI swing distance. "
                        + "enemyMeleeStandoffFraction overrides prefab attackStandoffFraction when > 0 (lower = closer). "
                        + "enemyMeleeMaxOrbitFactor caps how far holders/engagers orbit inside weapon reach.",
                        MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "enemyMeleeHitboxWidthScale",
                        "enemyMeleeHitboxReachScale",
                        "enemyMeleeAttackRangeMultiplier",
                        "enemyMeleeStandoffFraction",
                        "enemyMeleeMaxOrbitFactor");
                });

                DMStudioStyles.DrawSection("Enemy melee spacing & misses", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox(
                        "Intentional miss: anim + hitbox feedback play but outgoing melee damage is 0 for that swing sequence. "
                        + "After a miss, DMEnemyBrain marks Press and the engager steps in (StepFlat, no NavMesh) for enemyMeleeMissStepSeconds.",
                        MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "enemyIntentionalMeleeMissChance",
                        "enemyMeleeMissStepSeconds",
                        "enemyMeleeMissStepStopFactor",
                        "enemyMeleeMissStepSpeedMultiplier");
                });

                DMStudioStyles.DrawSection("Guard-break stagger (attacker)", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox(
                        "A held block is a light guard tap. A parry (block inside parryWindowSeconds) is the riposte.\n"
                        + "Souls, God of War, and Sekiro: block recoil is short (~0.35–0.45s lockout at default, small flinch, no hitstop). "
                        + "blockStaggerSeconds is tunable 0–3s (profile default 0.4). "
                        + "A parry is the riposte opening (parryStaggerSeconds 0–10s, default 5; harder shove, ~0.12s enemy pose hold, triple bell).\n"
                        + "Block plays one quieter ding and a ~0.12s player guard pose. "
                        + "Parry plays the poise triple ring. "
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

                DMStudioStyles.DrawSection("Training dummy spring / stagger", DMStudioStyles.ContentPanel, () =>
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
            DrawRangedGenesisLinksPanel();

            DMStudioStyles.DrawSection("Combat Studio / Genesis", DMStudioStyles.ContentPanel, () =>
            {
                if (GUILayout.Button("Open Genesis Studio (Combat Core)", GUILayout.Height(28f)))
                    GenesisStudioWindow.OpenTo("combat", "combat-core");

                var meleeBlockers = DMStudioActionValidation.CollectMeleeBuildAndApplyBlockers();
                DMStudioActionValidation.DrawBlockersHelpBox(meleeBlockers, "Build and apply melee animation set");
                if (GUILayout.Button("Build and apply melee animation set", GUILayout.Height(28f)))
                {
                    DMStudioActionValidation.TryRunPrimaryAction(
                        DMStudioActionValidation.CombatStudioDialogTitle,
                        meleeBlockers,
                        DMMeleeAnimationSetApplier.BuildAndApply);
                }
            });
        }

        /// <summary>Shared with Genesis Studio → Combat → Ranged.</summary>
        public static void DrawRangedGenesisLinksPanel()
        {
            DMStudioStyles.DrawSection("Genesis ranged authoring", DMStudioStyles.ContentPanel, () =>
            {
                EditorGUILayout.HelpBox(
                    "Ammo FX, scope profile/overlay, Hit Catalog, and Surface Damage live in Genesis Studio → Combat. "
                    + "This tab is a jump list — it does not duplicate those inspectors.",
                    MessageType.Info);

                if (GUILayout.Button("Genesis Studio → Combat → Ammo FX", GUILayout.Height(28f)))
                    GenesisStudioWindow.OpenTo("combat", "ammo-fx");

                if (GUILayout.Button("Genesis Studio → Combat → Scope Profile", GUILayout.Height(28f)))
                    GenesisStudioWindow.OpenTo("combat", "scope-profile");

                if (GUILayout.Button("Genesis Studio → Combat → Scope Overlay", GUILayout.Height(28f)))
                    GenesisStudioWindow.OpenTo("combat", "scope-overlay");

                if (GUILayout.Button("Genesis Studio → Combat → Hit Catalog", GUILayout.Height(28f)))
                    GenesisStudioWindow.OpenTo("combat", "hit-catalog");

                if (GUILayout.Button("Genesis Studio → Combat → Surface Damage", GUILayout.Height(28f)))
                    GenesisStudioWindow.OpenTo("combat", "surface-damage");
            });
        }

        private void DrawThrowablesTab()
        {
            DrawProfileHeader("Throwables (DM_CombatCoreProfile)");
            if (profile == null)
            {
                var blockers = DMStudioActionValidation.CollectMissingProfileAsset(
                    ProfileResourcesPath,
                    "DM_CombatCoreProfile");
                DMStudioActionValidation.DrawBlockersHelpBox(blockers, "edit throwables");
                return;
            }

            if (serializedProfile == null)
                return;

            serializedProfile.Update();
            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(profile, "Edit Combat Throwables Profile");

            using (DMStudioStyles.BeginProfileInspector(serializedProfile))
            {
                EditorGUILayout.HelpBox(
                    "Grenade tunables on DM_CombatCoreProfile (Genesis Studio → Combat → Throwables). "
                    + "Runtime reads Live on detonation; splash skills still scale damage/radius when enabled.",
                    MessageType.Info);

                DMStudioStyles.DrawSection("Grenades — enable & input", DMStudioStyles.ContentPanel, () =>
                {
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "enableGrenades",
                        "grenadeHotbarLocalIndex",
                        "hotCrossGrenadeLoadHoldSeconds",
                        "grenadeCookSecondsBase",
                        "grenadeCookSecondsMax",
                        "grenadeCookOutSelfDamage",
                        "grenadeEnemyStaggerSeconds");
                });

                DMStudioStyles.DrawSection("Grenades — splash & detonation", DMStudioStyles.ContentPanel, () =>
                {
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "grenadeExplosionRadius",
                        "grenadeMinDamage",
                        "grenadeMaxDamage",
                        "grenadeInnerRadiusFullDamage",
                        "grenadeEdgeDamageFalloff",
                        "grenadeLandFuseSeconds",
                        "grenadeExplosionForce",
                        "grenadeExplosionUpwardsModifier",
                        "grenadeExplosionVfxPrefab",
                        "grenadeCameraShakeAmplitude",
                        "grenadeCameraShakeDurationSeconds",
                        "grenadeSplashDamagesPlayer",
                        "grenadeSplashDamagesCompanions",
                        "grenadePioneerSplashLayerMask",
                        "grenadeVExplosiveDamageLayerMask",
                        "grenadeVExplosiveForceLayerMask");
                });

                DMStudioStyles.DrawSection("Grenades — throw projector", DMStudioStyles.ContentPanel, () =>
                {
                    DMStudioStyles.DrawPropertyFields(
                        serializedProfile,
                        "grenadeLandingRingUsesSplashSkills",
                        "grenadeThrowArcLineColor",
                        "grenadeLandingRingColor",
                        "grenadeLandingRingSizeMultiplier",
                        "grenadeLandingRingIndicatorTexture");
                });
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedProfile.ApplyModifiedProperties();
                EditorUtility.SetDirty(profile);
            }

            DMStudioStyles.DrawSection("Genesis Studio", DMStudioStyles.ContentPanel, () =>
            {
                if (GUILayout.Button("Genesis Studio → Combat → Throwables", GUILayout.Height(28f)))
                    GenesisStudioWindow.OpenTo("combat", "combat-throwables");
            });
        }

        private void DrawPlayTab()
        {
            DMStudioStyles.DrawSection("Tune scene", DMStudioStyles.ContentPanel, () =>
            {
                EditorGUILayout.HelpBox(
                    "Tune on Dark Matter Genesis v1.6.6. Terrains and hierarchy stay on.",
                    MessageType.Info);

                if (GUILayout.Button("Open Dark Matter Genesis v1.6.6", GUILayout.Height(32f)))
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

        /// <summary>Phase 4 start: attack slots by enemy count. Same fields as Genesis Studio → Combat → Combat Director.</summary>
        private void DrawDirectorTab()
        {
            if (directorProfile == null)
            {
                directorProfile = AssetDatabase.LoadAssetAtPath<DM_CombatDirectorProfile>(DirectorProfilePath);
                serializedDirector = directorProfile != null ? new SerializedObject(directorProfile) : null;
            }

            DMStudioStyles.DrawSection("Combat Director (Phase 4 start)", DMStudioStyles.ContentPanel, () =>
            {
                EditorGUILayout.ObjectField("Asset", directorProfile, typeof(DM_CombatDirectorProfile), false);
                EditorGUILayout.HelpBox(
                    "Play mode reads Resources/Combat/DM_CombatDirectorProfile (Profile.Live). Same fields under Genesis Studio → Combat → Combat Director. "
                    + "Turn Enable Combat Director off to roll back to Phase 3 (one melee attacker per target). "
                    + "Flank and morale are stub fields only.",
                    MessageType.Info);
                if (GUILayout.Button("Genesis Studio → Combat → Combat Director", GUILayout.Height(24f)))
                    GenesisStudioWindow.OpenTo("combat", "combat-director-slots");
            });

            if (serializedDirector == null)
            {
                var blockers = DMStudioActionValidation.CollectMissingProfileAsset(
                    DirectorProfilePath,
                    "DM_CombatDirectorProfile");
                DMStudioActionValidation.DrawBlockersHelpBox(blockers, "edit Combat Director");
                EditorGUILayout.HelpBox("DM_CombatDirectorProfile.asset not found under Resources/Combat.", MessageType.Warning);
                return;
            }

            serializedDirector.Update();
            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(directorProfile, "Edit Combat Director Profile");

            using (DMStudioStyles.BeginProfileInspector(serializedDirector))
            {
                DMStudioStyles.DrawSection("Master / rollback", DMStudioStyles.ContentPanel, () =>
                {
                    DMStudioStyles.DrawPropertyFields(serializedDirector, "enableCombatDirector", "debugLogSlots");
                });

                DMStudioStyles.DrawSection("Attack slots by enemy count", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox(
                        "Token holder counts as slot 1. 1v1 = 1; small band 2–4 = 1–2; medium 5–8 = 2–3; larger fights scale by extra enemies. "
                        + "Intensity 0 = band Min, 1 = band Max. Hard max caps every band.",
                        MessageType.Info);
                    DMStudioStyles.DrawPropertyFields(
                        serializedDirector,
                        "intensity",
                        "smallGroupMaxEnemies",
                        "smallGroupSlotsMin",
                        "smallGroupSlotsMax",
                        "mediumGroupMaxEnemies",
                        "mediumGroupSlotsMin",
                        "mediumGroupSlotsMax",
                        "largeGroupBaseSlots",
                        "largeGroupEnemiesPerExtraSlot",
                        "largeGroupSlotsMax",
                        "maxSimultaneousAttackers");
                });

                DMStudioStyles.DrawSection("Slot rotation", DMStudioStyles.ContentPanel, () =>
                {
                    DMStudioStyles.DrawPropertyFields(
                        serializedDirector,
                        "slotMinHoldSeconds",
                        "slotHoldMaxSequences",
                        "slotHoldMaxSeconds",
                        "slotEntryGrace",
                        "minSecondsBetweenSlotChanges",
                        "slotReawardCooldown");
                });

                DMStudioStyles.DrawSection("Flanking (stub)", DMStudioStyles.ContentPanel, () =>
                {
                    DMStudioStyles.DrawPropertyFields(
                        serializedDirector,
                        "enableFlank",
                        "flankMaxConcurrent",
                        "flankMinHoldSeconds",
                        "flankPersonalityBonus");
                });

                DMStudioStyles.DrawSection("Group morale (stub)", DMStudioStyles.ContentPanel, () =>
                {
                    DMStudioStyles.DrawPropertyFields(
                        serializedDirector,
                        "enableMorale",
                        "startingMorale",
                        "retreatMoraleThreshold",
                        "leaderDeathMoraleDrop",
                        "casualtyMoraleDrop",
                        "gruesomeKillMoraleDrop",
                        "moraleRecoverPerSecond");
                });
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedDirector.ApplyModifiedProperties();
                EditorUtility.SetDirty(directorProfile);
            }
        }

        private void DrawRoadmapTab()
        {
            const string handoffPath =
                "Assets/_Project/Documentation/Architecture/CursorPlans/combat_master_plan_handoff.md";
            const string spacingPlanPath =
                "Assets/_Project/Documentation/Design/Combat/DM_Enemy_Spacing_And_Hit_Marks_Plan.md";

            DMStudioRoadmapPanel.DrawIntro(
                "§31 order from DMG_Combat_Plan_v2.md (v2.4, Oct 2026). Phase 3 signed off Oct 8 in v1.6.5. Active gate: §31 #4 Combat Director. Hybrid moving melee / Hold E / charge A–B–C: play-test WIP on branch before v2.5 doc bump.",
                MessageType.Info);

            DMStudioRoadmapPanel.DrawDocLinks(
                ("Combat Plan v2", CombatPlanPath),
                ("Master plan handoff", handoffPath),
                ("Spacing & hit marks plan", spacingPlanPath));

            DMStudioStyles.DrawSection("§31 Master plan", DMStudioStyles.ContentPanel, () =>
            {
                if (GUILayout.Button("Ping DMG_Combat_Plan_v2.md", GUILayout.Height(26f)))
                    PingCombatPlan();
            });

            DMStudioRoadmapPanel.DrawPhase(
                "§31 #1",
                "Audit",
                DMStudioRoadmapPanel.Status.Done,
                "Closed Oct 3, 2026 — architecture maps, Invector wrap, animation tag sheet.");

            DMStudioRoadmapPanel.DrawPhase(
                "§31 #2",
                "Core combat",
                DMStudioRoadmapPanel.Status.Done,
                "Signed off Oct 5, 2026 in v1.6.5: poise, i-frames, block/parry stagger, weak chain A→B→C, strong charge, Combat Core profile + Studio shell.");

            DMStudioRoadmapPanel.DrawPhase(
                "§31 #3",
                "Unified brain + engagement spacing",
                DMStudioRoadmapPanel.Status.Done,
                "Signed off Oct 8, 2026: DMEnemyBrain + DMEnemyEngagementDirector (D1 engager, Hold ring, hysteresis). Rollback via enableCombatDirector off. See DM_Enemy_Spacing_And_Hit_Marks_Plan.md §10.");

            DMStudioRoadmapPanel.DrawPhase(
                "§31 #4",
                "Combat Director (morale, intensity, flanking)",
                DMStudioRoadmapPanel.Status.InProgress,
                "Start (Oct 8, 2026): DM_CombatDirectorProfile + attack slots by enemy count on DMEnemyEngagementDirector (Director tab). Flank / morale / intensity events still stubs.");

            DMStudioRoadmapPanel.DrawPhase(
                "§31 #5",
                "Plasma sword template",
                DMStudioRoadmapPanel.Status.InProgress,
                "Attack, damage, hit react, burn branches partial; Blade / Survival skill wiring later.");

            DMStudioRoadmapPanel.DrawPhase(
                "§31 #6–8",
                "Momentum, Sick Stick, specials, Overdrive",
                DMStudioRoadmapPanel.Status.NotStarted,
                "Bindings unassigned — ask before mapping parry/finisher/special slots.");

            DMStudioRoadmapPanel.DrawPhase(
                "§31 #9",
                "Remaining elements",
                DMStudioRoadmapPanel.Status.InProgress,
                "Cryo / Energy / Laser / Ion display mapping locked; Ion ammo routing + feedback hardened on disk.");

            DMStudioRoadmapPanel.DrawPhase(
                "§31 #10",
                "Body damage & dismemberment",
                DMStudioRoadmapPanel.Status.InProgress,
                "CombatBodyPart zones + DMEnemyHitbox rig on disk; zone multipliers still 1.0 (deferred).");

            DMStudioRoadmapPanel.DrawPhase(
                "§31 #11–14",
                "Companions, Control/Marksman, environment, combat memory",
                DMStudioRoadmapPanel.Status.InProgress,
                "Trio + hex tree shipped (legacy); roles, noise, and memory systems partial or missing.");

            DMStudioRoadmapPanel.DrawPhase(
                "§31 #15–16",
                "Encounters & studio",
                DMStudioRoadmapPanel.Status.InProgress,
                "Surface encounter tables + Sulfur Hound prototype; Combat Studio + WorldChrome throttle shipped.");

            EditorGUILayout.Space(6f);
            DMStudioStyles.DrawSection("Oct 2026 combat extensions", DMStudioStyles.ContentPanel, () =>
            {
                EditorGUILayout.HelpBox(
                    "Recent disk work aligned with DM_Enemy_Spacing_And_Hit_Marks_Plan.md — tune in v1.6.5, not Combat_Sandbox.",
                    MessageType.None);
            });

            DMStudioRoadmapPanel.DrawPhase(
                "B",
                "Hit marks & DMHitbox layer",
                DMStudioRoadmapPanel.Status.Done,
                "Shipped with Phase 3: per-bone triggers (layer 26), HDRP decals, DM_EnemyHitMarkProfile + Genesis Hit Marks tab. Spot-check FRED/Android in Play as needed.");

            DMStudioRoadmapPanel.DrawPhase(
                "B+",
                "Melee DMHitbox probe + hit routing",
                DMStudioRoadmapPanel.Status.InProgress,
                "PioneerMeleeDmHitboxProbe, CombatHitResolver zone mapping, ranged refine onto hitboxes — verify FRED/Android in Play.");

            DMStudioRoadmapPanel.DrawPhase(
                "FX",
                "Status VFX attach + ION feedback",
                DMStudioRoadmapPanel.Status.InProgress,
                "Status effects attach at hit point with profile offset; enemy hit routing keeps decals and ION display consistent.");

            DMStudioRoadmapPanel.DrawPhase(
                "Cam",
                "Combat camera threat snap",
                DMStudioRoadmapPanel.Status.Done,
                "DMCombatCameraThreatSnap + combatCameraSnapSeconds on Combat Core profile (Genesis + Combat Studio Core).");

            DMStudioRoadmapPanel.DrawPhase(
                "A",
                "Engagement spacing (Part A)",
                DMStudioRoadmapPanel.Status.Done,
                "Merged into Phase 3 sign-off: token rotation, world-anchored hold points, facing cone. Ranged token pool unused; UITK debug overlay deferred.");

            DMStudioRoadmapPanel.DrawPhase(
                "§10",
                "Melee moving layers (FullBody / UpperBody)",
                DMStudioRoadmapPanel.Status.InProgress,
                "Policy in DM_Melee_Locomotion_Layer_Policy.md: Base locomotion, UpperBody Hold E combo, FullBody charge/release + hybrid moving. Play-test sign-off pending on WIP branch.");

            DMStudioRoadmapPanel.DrawPhase(
                "Sandbox",
                "Combat_Sandbox spawner",
                DMStudioRoadmapPanel.Status.Blocked,
                "Retired as tune target — forensics only on Play tab.");
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

        private void DrawCombatHitAudioSection()
        {
            if (audioProfile == null)
                audioProfile = AssetDatabase.LoadAssetAtPath<GameAudioProfile>(AudioProfilePath);

            if (serializedAudioProfile == null && audioProfile != null)
                serializedAudioProfile = new SerializedObject(audioProfile);

            if (serializedAudioProfile == null)
                return;

            serializedAudioProfile.Update();
            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(audioProfile, "Edit Combat Hit Audio");

            DMStudioStyles.DrawSection("Combat hit audio (GameAudioProfile)", DMStudioStyles.ContentPanel, () =>
            {
                EditorGUILayout.ObjectField("Game audio profile", audioProfile, typeof(GameAudioProfile), false);
                EditorGUILayout.HelpBox(
                    "Melee swing/hit clips for blades and punches. Ranged hit clips for gun/body impacts; "
                    + "empty ranged arrays fall back to melee hits.",
                    MessageType.Info);
                using (DMStudioStyles.BeginProfileInspector(serializedAudioProfile))
                {
                    DMStudioStyles.DrawPropertyFields(
                        serializedAudioProfile,
                        "meleeSwingClips",
                        "meleeHitClips",
                        "meleeCriticalHitClips",
                        "rangedHitClips",
                        "rangedCriticalHitClips");
                }
            });

            if (EditorGUI.EndChangeCheck())
            {
                serializedAudioProfile.ApplyModifiedProperties();
                EditorUtility.SetDirty(audioProfile);
            }
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
                    "When enemy poise hits zero, CombatEvents.Stagger plays this clip three times "
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

        private static void DrawCombatActorHeader(string title, string help)
        {
            EditorGUILayout.Space(12f);
            Rect r = GUILayoutUtility.GetRect(0f, 22f, GUILayout.ExpandWidth(true));
            EditorGUI.LabelField(r, title, DMStudioStyles.SectionTitle);
            DMStudioStyles.DrawAccentLine(r, new Color(0.56f, 0.12f, 0.37f, 1f), 2f);
            if (!string.IsNullOrEmpty(help))
                EditorGUILayout.HelpBox(help, MessageType.None);
            EditorGUILayout.Space(4f);
        }

        private void DrawTurretsTab()
        {
            if (turretProfile == null)
                turretProfile = LoadOrCreateTurretProfile();
            if (serializedTurret == null && turretProfile != null)
                serializedTurret = new SerializedObject(turretProfile);

            DMStudioStyles.DrawSection("Turret profile", DMStudioStyles.ContentPanel, () =>
            {
                EditorGUILayout.ObjectField("Asset", turretProfile, typeof(DMTurretProfile), false);
                EditorGUILayout.HelpBox(
                    "Designer authority: faction, ranges, ammo mode, vendor VFX, barrel, and Instance Authority live on DMTurretRuntime / this profile. "
                    + "Playable menu is Auto or Enter Turret plus a mixed game/vendor ammo cycle. "
                    + "Scene turrets bootstrap as Enemy; placed equipment uses Placement defaults.",
                    MessageType.Info);
            });

            if (serializedTurret != null && turretProfile != null)
            {
                serializedTurret.Update();
                EditorGUI.BeginChangeCheck();
                Undo.RecordObject(turretProfile, "Edit Turret Profile");
                using (DMStudioStyles.BeginProfileInspector(serializedTurret))
                {
                    DMStudioStyles.DrawSection("Placement defaults", DMStudioStyles.ContentPanel, () =>
                    {
                        DMStudioStyles.DrawPropertyFields(
                            serializedTurret,
                            "defaultPlacedFaction",
                            "defaultControlMode");
                    });
                    DMStudioStyles.DrawSection("Ranges", DMStudioStyles.ContentPanel, () =>
                    {
                        DMStudioStyles.DrawPropertyFields(
                            serializedTurret,
                            "scanRange",
                            "trackRange",
                            "fireRange");
                    });
                    DMStudioStyles.DrawSection("Scan / tracking", DMStudioStyles.ContentPanel, () =>
                    {
                        DMStudioStyles.DrawPropertyFields(
                            serializedTurret,
                            "autoRestAfterNoTargetSeconds",
                            "targetLosGraceSeconds",
                            "targetLosFailuresBeforeDrop",
                            "scanHeadingSpeed",
                            "scanRetargetSeconds",
                            "scanElevationDegrees",
                            "headingTrackingSpeed",
                            "elevationTrackingSpeed",
                            "headingLimit",
                            "elevationLimit");
                    });
                    DMStudioStyles.DrawSection("Ammo mode", DMStudioStyles.ContentPanel, () =>
                    {
                        DMStudioStyles.DrawPropertyFields(
                            serializedTurret,
                            "defaultAmmoMode",
                            "gameWeaponItem",
                            "gameAmmoItem",
                            "gameFireInterval",
                            "gameSpreadDegrees");
                    });
                    DMStudioStyles.DrawSection("Damage & hull", DMStudioStyles.ContentPanel, () =>
                    {
                        DMStudioStyles.DrawPropertyFields(
                            serializedTurret,
                            "ammoType",
                            "damage",
                            "splashRadius",
                            "beamDamageInterval",
                            "maxHealth");
                    });
                    DMStudioStyles.DrawSection("Enemy vs turret", DMStudioStyles.ContentPanel, () =>
                    {
                        DMStudioStyles.DrawPropertyFields(
                            serializedTurret,
                            "enemyDestroyChanceMin",
                            "enemyDestroyChanceMax",
                            "enemyFleeMargin",
                            "enemyTurretDecisionSeconds");
                    });
                    DMStudioStyles.DrawSection("Player seat camera", DMStudioStyles.ContentPanel, () =>
                    {
                        DMStudioStyles.DrawPropertyFields(
                            serializedTurret,
                            "playerCameraLocalPosition",
                            "playerCameraLocalEuler",
                            "playerCameraFov",
                            "playerAimMouseSensitivity");
                        EditorGUILayout.HelpBox(
                            "Baked prefabs get a disabled DMTurretPlayerCamera child on *SOCKET_SWIVEL. "
                            + "Re-bake or Register all templates after changing offsets.",
                            MessageType.None);
                    });
                    DMStudioStyles.DrawSection("Debug", DMStudioStyles.ContentPanel, () =>
                    {
                        DMStudioStyles.DrawPropertyFields(serializedTurret, "debugDraw");
                    });
                }

                if (EditorGUI.EndChangeCheck())
                {
                    serializedTurret.ApplyModifiedProperties();
                    EditorUtility.SetDirty(turretProfile);
                }
            }

            DMStudioStyles.DrawSection("FORGE3D Turret Editor", DMStudioStyles.ContentPanel, () =>
            {
                EditorGUILayout.HelpBox(
                    "Pick a template in the FORGE3D Turret Editor, then Add/Bake placeable. "
                    + "Register current template or Register all templates so Stone Equipment gets one part per type (si_turret_*).",
                    MessageType.Info);
                if (GUILayout.Button("Open FORGE3D Turret Editor…", GUILayout.Height(28f)))
                    F3DTurretEditorWindow.ShowWindow();
                if (GUILayout.Button("Add placeable turret (current template)", GUILayout.Height(24f)))
                    DMTurretEditorIntegration.MenuAddPlaceable();
                if (GUILayout.Button("Bake placeable prefab (current template)", GUILayout.Height(24f)))
                    DMTurretEditorIntegration.MenuBakeCurrentTemplate();
                if (GUILayout.Button("Bake selected scene turret → prefab", GUILayout.Height(24f)))
                    DMTurretEditorIntegration.MenuBakeSelection();
                if (GUILayout.Button("Register current template in Building Equipment", GUILayout.Height(24f)))
                    DMTurretEditorIntegration.MenuRegisterBuildingEquipment();
                if (GUILayout.Button("Register all templates in Building Equipment", GUILayout.Height(24f)))
                    DMTurretEditorIntegration.MenuRegisterAllBuildingEquipment();
                if (GUILayout.Button("Bind DM wrap on scene FORGE3D turrets", GUILayout.Height(24f)))
                {
                    F3DTurret[] turrets = Object.FindObjectsByType<F3DTurret>(FindObjectsInactive.Include);
                    int added = 0;
                    for (int i = 0; i < turrets.Length; i++)
                    {
                        if (turrets[i] == null)
                            continue;
                        if (turrets[i].GetComponent<DMTurretRuntime>() != null)
                            continue;
                        Undo.AddComponent<DMTurretRuntime>(turrets[i].gameObject);
                        added++;
                    }

                    DMTurretRuntime.StripAllDemoInput();
                    Debug.Log("[Combat Studio] Bound DMTurretRuntime on " + added + " FORGE3D turrets.");
                }
            });

            GameObject selected = Selection.activeGameObject;
            F3DTurret selectedTurret = selected != null ? selected.GetComponentInParent<F3DTurret>() : null;
            DMTurretRuntime selectedRuntime = selected != null
                ? selected.GetComponentInParent<DMTurretRuntime>()
                : null;
            DMStudioStyles.DrawSection("Selected scene turret", DMStudioStyles.ContentPanel, () =>
            {
                if (selectedTurret == null)
                {
                    EditorGUILayout.HelpBox("Select a FORGE3D turret in the Hierarchy to edit faction, ammo, VFX, and barrel.", MessageType.Info);
                    return;
                }

                EditorGUILayout.ObjectField("F3DTurret", selectedTurret, typeof(F3DTurret), true);
                if (selectedRuntime == null)
                {
                    if (GUILayout.Button("Add DMTurretRuntime"))
                        Undo.AddComponent<DMTurretRuntime>(selectedTurret.gameObject);
                    return;
                }

                EditorGUI.BeginChangeCheck();
                SerializedObject so = new SerializedObject(selectedRuntime);
                so.Update();
                EditorGUILayout.PropertyField(so.FindProperty("faction"));
                EditorGUILayout.PropertyField(so.FindProperty("controlMode"));
                EditorGUILayout.PropertyField(so.FindProperty("ammoMode"));
                EditorGUILayout.PropertyField(so.FindProperty("vendorFxType"), new GUIContent("Vendor VFX"));
                EditorGUILayout.PropertyField(so.FindProperty("ammoType"), new GUIContent("Ammo type (damage)"));
                SerializedProperty authority = so.FindProperty("instanceAuthority");
                DMTurretBarrelInspectorGui.Draw(so, authority);
                EditorGUILayout.PropertyField(so.FindProperty("useProfileDamage"));
                EditorGUILayout.PropertyField(so.FindProperty("damageOverride"));
                if (authority != null)
                    EditorGUILayout.PropertyField(authority, includeChildren: true);
                so.ApplyModifiedProperties();
                if (EditorGUI.EndChangeCheck())
                {
                    selectedRuntime.ApplyDesignerVisuals();
                    EditorUtility.SetDirty(selectedRuntime);
                }
            });

            DMStudioStyles.DrawSection("Genesis Studio", DMStudioStyles.ContentPanel, () =>
            {
                if (GUILayout.Button("Genesis Studio → Combat → Turrets", GUILayout.Height(28f)))
                    GenesisStudioWindow.OpenTo("combat", "combat-turrets");
            });
        }

        private static DMTurretProfile LoadOrCreateTurretProfile()
        {
            DMTurretProfile asset = AssetDatabase.LoadAssetAtPath<DMTurretProfile>(TurretProfilePath);
            if (asset != null)
                return asset;

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources/Combat"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources"))
                    AssetDatabase.CreateFolder("Assets/_Project", "Resources");
                AssetDatabase.CreateFolder("Assets/_Project/Resources", "Combat");
            }

            asset = ScriptableObject.CreateInstance<DMTurretProfile>();
            AssetDatabase.CreateAsset(asset, TurretProfilePath);
            AssetDatabase.SaveAssets();
            return asset;
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
