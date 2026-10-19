#if UNITY_EDITOR
using Project.Combat.Turrets;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Combat
{
    [CustomEditor(typeof(DMTurretRuntime))]
    public sealed class DMTurretRuntimeEditor : UnityEditor.Editor
    {
        const string AuthorityPath = "instanceAuthority";
        const string TrackingPath = "trackingAuthority";
        const string VendorFxPath = "vendorFxAuthority";

        bool runtimeFoldout = true;
        bool combatFoldout = true;
        bool authorityFoldout = true;
        bool trackingFoldout = true;
        bool vendorFoldout = true;
        bool healthInteractFoldout = true;
        bool forgeRefsFoldout;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var runtime = (DMTurretRuntime)target;

            DrawRuntimeSection();
            DrawCombatAmmoSection(serializedObject.FindProperty(AuthorityPath));
            DrawInstanceAuthority(ref authorityFoldout, serializedObject.FindProperty(AuthorityPath));
            DrawTrackingAuthority(ref trackingFoldout, serializedObject.FindProperty(TrackingPath), runtime);
            DrawVendorFxAuthority(ref vendorFoldout, serializedObject.FindProperty(VendorFxPath), runtime);
            DrawHealthInteractSection(serializedObject.FindProperty(AuthorityPath));
            DrawForgeRefsSection(runtime);

            if (GUILayout.Button("Wire FORGE / FX / Barrel / seat camera"))
            {
                Undo.RecordObject(runtime, "Wire turret references");
                DMTurretPrefabBakeUtility.WireRuntimeReferences(runtime.gameObject);
                runtime.ApplyDesignerVisuals();
                EditorUtility.SetDirty(runtime);
            }

            if (GUILayout.Button("Apply ammo VFX + barrel now"))
            {
                Undo.RecordObject(runtime, "Apply turret visuals");
                if (runtime.FxController != null)
                    Undo.RecordObject(runtime.FxController, "Apply turret VFX");
                runtime.ApplyDesignerVisuals();
                EditorUtility.SetDirty(runtime);
            }

            serializedObject.ApplyModifiedProperties();
            // Heavy FX/barrel pushes belong in Edit mode only — during Play this runs on every
            // inspector repaint and inflates EditorLoop when a turret is selected.
            if (GUI.changed && !EditorApplication.isPlaying)
            {
                runtime.ApplyDesignerVisuals();
                runtime.ApplyTrackingToForge();
                runtime.ApplyVendorFxToController();
                if (runtime.ForgeTurret != null)
                    EditorUtility.SetDirty(runtime.ForgeTurret);
                if (runtime.FxController != null)
                    EditorUtility.SetDirty(runtime.FxController);
                EditorUtility.SetDirty(runtime);
            }
        }

        void DrawRuntimeSection()
        {
            runtimeFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(runtimeFoldout, "Runtime");
            if (runtimeFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("faction"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("controlMode"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("placedByPlayer"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawCombatAmmoSection(SerializedProperty authority)
        {
            combatFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(combatFoldout, "Combat & Ammo (prefab defaults)");
            if (combatFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ammoMode"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("vendorFxType"), new GUIContent("Vendor VFX"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ammoType"), new GUIContent("Ammo type (damage)"));
                DMTurretBarrelInspectorGui.Draw(serializedObject, authority);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("useProfileDamage"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("damageOverride"));
                EditorGUILayout.HelpBox(
                    "Values here are this prefab’s template defaults. Toggle overrides under Instance Authority to beat profile + defaults per field.",
                    MessageType.None);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        static void DrawInstanceAuthority(ref bool foldout, SerializedProperty authority)
        {
            if (authority == null)
                return;

            bool any = authority.FindPropertyRelative("overrideAmmoMode").boolValue
                || authority.FindPropertyRelative("overrideVendorFxType").boolValue
                || authority.FindPropertyRelative("overrideAmmoType").boolValue
                || authority.FindPropertyRelative("overrideBarrelKind").boolValue
                || authority.FindPropertyRelative("overrideDatabaseBarrel").boolValue
                || authority.FindPropertyRelative("overrideGameAmmoItems").boolValue
                || authority.FindPropertyRelative("overrideGameFireInterval").boolValue
                || authority.FindPropertyRelative("overrideGameSpread").boolValue
                || authority.FindPropertyRelative("overrideDamage").boolValue
                || authority.FindPropertyRelative("overrideSplashRadius").boolValue
                || authority.FindPropertyRelative("overrideBeamDamageInterval").boolValue
                || authority.FindPropertyRelative("overrideScanRange").boolValue
                || authority.FindPropertyRelative("overrideTrackRange").boolValue
                || authority.FindPropertyRelative("overrideFireRange").boolValue
                || authority.FindPropertyRelative("overrideMaxHealth").boolValue
                || authority.FindPropertyRelative("overrideAimAcceptDegrees").boolValue
                || authority.FindPropertyRelative("overrideAutoRestAfterNoTargetSeconds").boolValue
                || authority.FindPropertyRelative("overrideTargetLosGraceSeconds").boolValue
                || authority.FindPropertyRelative("overrideTargetLosFailuresBeforeDrop").boolValue
                || authority.FindPropertyRelative("overrideScanHeadingSpeed").boolValue
                || authority.FindPropertyRelative("overrideScanRetargetSeconds").boolValue
                || authority.FindPropertyRelative("overrideScanElevationDegrees").boolValue;

            DMTurretInspectorSectionsGui.DrawAuthoritySection(
                ref foldout,
                "Instance Authority",
                any,
                () =>
                {
                    DMTurretInspectorSectionsGui.DrawOverridePair(authority, "overrideAmmoMode", "ammoMode", "Ammo mode");
                    DMTurretInspectorSectionsGui.DrawOverridePair(authority, "overrideVendorFxType", "vendorFxType", "Vendor VFX");
                    DMTurretInspectorSectionsGui.DrawOverridePair(authority, "overrideAmmoType", "ammoType", "Ammo type");

                    EditorGUILayout.LabelField("Game combat ammo (ItemData path)", EditorStyles.miniBoldLabel);
                    DMTurretInspectorSectionsGui.DrawOverridePair(authority, "overrideGameAmmoItems", "gameWeaponItem", "Game weapon");
                    if (authority.FindPropertyRelative("overrideGameAmmoItems").boolValue)
                        EditorGUILayout.PropertyField(authority.FindPropertyRelative("gameAmmoItem"), new GUIContent("Game ammo"));

                    DMTurretInspectorSectionsGui.DrawOverridePair(authority, "overrideGameFireInterval", "gameFireInterval", "Game fire interval");
                    DMTurretInspectorSectionsGui.DrawOverridePair(authority, "overrideGameSpread", "gameSpreadDegrees", "Game spread (deg)");

                    EditorGUILayout.LabelField("Damage", EditorStyles.miniBoldLabel);
                    DMTurretInspectorSectionsGui.DrawOverridePair(authority, "overrideDamage", "damage", "Damage");
                    DMTurretInspectorSectionsGui.DrawOverridePair(authority, "overrideSplashRadius", "splashRadius", "Splash radius");
                    DMTurretInspectorSectionsGui.DrawOverridePair(authority, "overrideBeamDamageInterval", "beamDamageInterval", "Beam damage interval");

                    EditorGUILayout.LabelField("Ranges (meters)", EditorStyles.miniBoldLabel);
                    DMTurretInspectorSectionsGui.DrawOverridePair(authority, "overrideScanRange", "scanRange", "Scan range");
                    DMTurretInspectorSectionsGui.DrawOverridePair(authority, "overrideTrackRange", "trackRange", "Track range");
                    DMTurretInspectorSectionsGui.DrawOverridePair(authority, "overrideFireRange", "fireRange", "Fire range");

                    EditorGUILayout.LabelField("Auto brain", EditorStyles.miniBoldLabel);
                    DMTurretInspectorSectionsGui.DrawOverridePair(authority, "overrideAimAcceptDegrees", "aimAcceptDegrees", "Aim accept (deg)");
                    DMTurretInspectorSectionsGui.DrawOverridePair(authority, "overrideAutoRestAfterNoTargetSeconds", "autoRestAfterNoTargetSeconds", "Auto rest (sec)");
                    DMTurretInspectorSectionsGui.DrawOverridePair(authority, "overrideTargetLosGraceSeconds", "targetLosGraceSeconds", "LOS grace (sec)");
                    DMTurretInspectorSectionsGui.DrawOverridePair(authority, "overrideTargetLosFailuresBeforeDrop", "targetLosFailuresBeforeDrop", "LOS failures max");

                    EditorGUILayout.LabelField("Scan motion (non-auto)", EditorStyles.miniBoldLabel);
                    DMTurretInspectorSectionsGui.DrawOverridePair(authority, "overrideScanHeadingSpeed", "scanHeadingSpeed", "Scan heading speed");
                    DMTurretInspectorSectionsGui.DrawOverridePair(authority, "overrideScanRetargetSeconds", "scanRetargetSeconds", "Scan retarget (sec)");
                    DMTurretInspectorSectionsGui.DrawOverridePair(authority, "overrideScanElevationDegrees", "scanElevationDegrees", "Scan elevation X/Y");
                });
        }

        static void DrawTrackingAuthority(ref bool foldout, SerializedProperty tracking, DMTurretRuntime runtime)
        {
            if (tracking == null)
                return;

            bool any = tracking.FindPropertyRelative("overrideMountSwivel").boolValue
                || tracking.FindPropertyRelative("overrideHeadingTrackingSpeed").boolValue
                || tracking.FindPropertyRelative("overrideElevationTrackingSpeed").boolValue
                || tracking.FindPropertyRelative("overrideHeadingLimit").boolValue
                || tracking.FindPropertyRelative("overrideElevationLimit").boolValue
                || tracking.FindPropertyRelative("overrideSmoothControlling").boolValue
                || tracking.FindPropertyRelative("overrideDebugDraw").boolValue
                || tracking.FindPropertyRelative("overrideDebugTarget").boolValue
                || tracking.FindPropertyRelative("overrideAnimators").boolValue;

            DMTurretInspectorSectionsGui.DrawAuthoritySection(
                ref foldout,
                "Tracking Authority",
                any,
                () =>
                {
                    const bool autoEnable = true;

                    EditorGUILayout.LabelField("Mount / swivel", EditorStyles.miniBoldLabel);
                    DMTurretInspectorSectionsGui.DrawOverridePair(
                        tracking, "overrideMountSwivel", "mount", "Mount", autoEnable);
                    if (tracking.FindPropertyRelative("overrideMountSwivel").boolValue)
                        EditorGUILayout.PropertyField(tracking.FindPropertyRelative("swivel"), new GUIContent("Swivel"));

                    EditorGUILayout.LabelField("Tracking speeds", EditorStyles.miniBoldLabel);
                    DMTurretInspectorSectionsGui.DrawOverridePair(
                        tracking, "overrideHeadingTrackingSpeed", "headingTrackingSpeed", "Heading speed", autoEnable);
                    DMTurretInspectorSectionsGui.DrawOverridePair(
                        tracking, "overrideElevationTrackingSpeed", "elevationTrackingSpeed", "Elevation speed", autoEnable);

                    EditorGUILayout.LabelField("Limits (degrees)", EditorStyles.miniBoldLabel);
                    DMTurretInspectorSectionsGui.DrawOverridePair(
                        tracking, "overrideHeadingLimit", "headingLimit", "Heading limit X/Y", autoEnable);
                    DMTurretInspectorSectionsGui.DrawOverridePair(
                        tracking, "overrideElevationLimit", "elevationLimit", "Elevation limit X/Y", autoEnable);

                    EditorGUILayout.LabelField("Motion", EditorStyles.miniBoldLabel);
                    DMTurretInspectorSectionsGui.DrawOverridePair(
                        tracking, "overrideSmoothControlling", "smoothControlling", "Smooth controlling", autoEnable);

                    EditorGUILayout.LabelField("Debug / animators", EditorStyles.miniBoldLabel);
                    EditorGUILayout.PropertyField(
                        tracking.FindPropertyRelative("editorDrawVisionCones"),
                        new GUIContent("Draw vision cones (Scene)"));
                    DMTurretInspectorSectionsGui.DrawOverridePair(
                        tracking, "overrideDebugDraw", "debugDraw", "Debug draw", autoEnable);
                    DMTurretInspectorSectionsGui.DrawOverridePair(
                        tracking, "overrideDebugTarget", "debugTarget", "Debug target", autoEnable);
                    DMTurretInspectorSectionsGui.DrawOverridePair(
                        tracking, "overrideAnimators", "animators", "Animators", autoEnable);

                    EditorGUILayout.Space(4f);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Capture from F3DTurret"))
                    {
                        Undo.RecordObject(runtime, "Capture turret tracking");
                        runtime.CaptureTrackingFromForge(enableOverrides: true);
                        if (runtime.ForgeTurret != null)
                            Undo.RecordObject(runtime.ForgeTurret, "Capture turret tracking");
                        EditorUtility.SetDirty(runtime);
                    }

                    if (GUILayout.Button("Use DM_TurretProfile tracking"))
                    {
                        Undo.RecordObject(runtime, "Profile turret tracking");
                        runtime.CaptureTrackingFromProfile();
                        EditorUtility.SetDirty(runtime);
                    }

                    if (GUILayout.Button("Apply to F3DTurret now"))
                    {
                        runtime.ApplyTrackingToForge();
                        if (runtime.ForgeTurret != null)
                            EditorUtility.SetDirty(runtime.ForgeTurret);
                    }

                    EditorGUILayout.EndHorizontal();
                });
        }

        static void DrawVendorFxAuthority(ref bool foldout, SerializedProperty vendor, DMTurretRuntime runtime)
        {
            if (vendor == null)
                return;

            bool any = vendor.FindPropertyRelative("overrideVulcanFireRate").boolValue;
            DMTurretInspectorSectionsGui.DrawAuthoritySection(
                ref foldout,
                "FX / Vendor",
                any,
                () =>
                {
                    DMTurretInspectorSectionsGui.DrawOverridePair(vendor, "overrideVulcanFireRate", "vulcanFireRate", "Vulcan fire rate");
                    EditorGUILayout.HelpBox(
                        "Prefab refs (projectiles, muzzles, beams) stay on F3DFXController. DefaultFXType is driven from Combat & Ammo / Instance Authority.",
                        MessageType.None);

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Capture from F3DFXController"))
                    {
                        Undo.RecordObject(runtime, "Capture vendor FX");
                        runtime.CaptureVendorFxFromController(enableOverrides: true);
                        if (runtime.FxController != null)
                            Undo.RecordObject(runtime.FxController, "Capture vendor FX");
                        EditorUtility.SetDirty(runtime);
                    }

                    if (GUILayout.Button("Apply to F3DFXController now"))
                    {
                        runtime.ApplyVendorFxToController();
                        if (runtime.FxController != null)
                            EditorUtility.SetDirty(runtime.FxController);
                    }

                    EditorGUILayout.EndHorizontal();
                });
        }

        void DrawHealthInteractSection(SerializedProperty authority)
        {
            healthInteractFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(healthInteractFoldout, "Health & Interact");
            if (healthInteractFoldout)
            {
                EditorGUI.indentLevel++;
                if (authority != null)
                    DMTurretInspectorSectionsGui.DrawOverridePair(authority, "overrideMaxHealth", "maxHealth", "Max health");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("interactRangeMeters"), new GUIContent("Interact range (m)"));
                EditorGUILayout.HelpBox(
                    "Hull HP uses Instance Authority override or DM_TurretProfile. Interact range defers to Building ghost profile when set.",
                    MessageType.None);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawForgeRefsSection(DMTurretRuntime runtime)
        {
            forgeRefsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(forgeRefsFoldout, "FORGE3D references (auto-wired)");
            if (forgeRefsFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("forgeTurret"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("fxController"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("barrel"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("playerSeatCamera"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("barrelDatabase"), new GUIContent("Barrel database"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}
#endif
