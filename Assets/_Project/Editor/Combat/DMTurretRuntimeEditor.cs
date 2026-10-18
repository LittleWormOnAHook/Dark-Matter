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
        bool authorityFoldout = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var runtime = (DMTurretRuntime)target;

            EditorGUILayout.LabelField("Identity / targeting", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("faction"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("controlMode"));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Ammo (game or vendor VFX)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ammoMode"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("vendorFxType"), new GUIContent("Vendor VFX"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ammoType"), new GUIContent("Ammo type (damage)"));
            SerializedProperty authority = serializedObject.FindProperty(AuthorityPath);
            DMTurretBarrelInspectorGui.Draw(serializedObject, authority);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useProfileDamage"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("damageOverride"));

            if (authority != null)
                DrawInstanceAuthority(ref authorityFoldout, authority);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("FORGE3D / seat", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("forgeTurret"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fxController"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("barrel"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("playerSeatCamera"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("placedByPlayer"));

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
            if (GUI.changed)
            {
                runtime.ApplyDesignerVisuals();
                EditorUtility.SetDirty(runtime);
            }
        }

        static void DrawInstanceAuthority(ref bool foldout, SerializedProperty authority)
        {
            EditorGUILayout.Space(4f);
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
                || authority.FindPropertyRelative("overrideMaxHealth").boolValue;

            string header = any ? "Instance Authority (overrides active)" : "Instance Authority";
            foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, header);
            if (!foldout)
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
                return;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUI.indentLevel++;
            DrawOverridePair(authority, "overrideAmmoMode", "ammoMode", "Ammo mode");
            DrawOverridePair(authority, "overrideVendorFxType", "vendorFxType", "Vendor VFX");
            DrawOverridePair(authority, "overrideAmmoType", "ammoType", "Ammo type");

            EditorGUILayout.LabelField("Game combat ammo (ItemData path)", EditorStyles.miniBoldLabel);
            DrawOverridePair(authority, "overrideGameAmmoItems", "gameWeaponItem", "Game weapon");
            if (authority.FindPropertyRelative("overrideGameAmmoItems").boolValue)
                EditorGUILayout.PropertyField(authority.FindPropertyRelative("gameAmmoItem"), new GUIContent("Game ammo"));

            DrawOverridePair(authority, "overrideGameFireInterval", "gameFireInterval", "Game fire interval");
            DrawOverridePair(authority, "overrideGameSpread", "gameSpreadDegrees", "Game spread (deg)");

            EditorGUILayout.LabelField("Damage", EditorStyles.miniBoldLabel);
            DrawOverridePair(authority, "overrideDamage", "damage", "Damage");
            DrawOverridePair(authority, "overrideSplashRadius", "splashRadius", "Splash radius");
            DrawOverridePair(authority, "overrideBeamDamageInterval", "beamDamageInterval", "Beam damage interval");

            EditorGUILayout.LabelField("Ranges (meters)", EditorStyles.miniBoldLabel);
            DrawOverridePair(authority, "overrideScanRange", "scanRange", "Scan range");
            DrawOverridePair(authority, "overrideTrackRange", "trackRange", "Track range");
            DrawOverridePair(authority, "overrideFireRange", "fireRange", "Fire range");

            EditorGUILayout.LabelField("Hull", EditorStyles.miniBoldLabel);
            DrawOverridePair(authority, "overrideMaxHealth", "maxHealth", "Max health");

            EditorGUI.indentLevel--;
        }

        static void DrawOverridePair(
            SerializedProperty authority,
            string toggleName,
            string valueName,
            string label)
        {
            SerializedProperty toggle = authority.FindPropertyRelative(toggleName);
            SerializedProperty value = authority.FindPropertyRelative(valueName);
            if (toggle == null || value == null)
                return;

            EditorGUILayout.BeginHorizontal();
            toggle.boolValue = EditorGUILayout.ToggleLeft(label, toggle.boolValue, GUILayout.Width(180f));
            using (new EditorGUI.DisabledScope(!toggle.boolValue))
                EditorGUILayout.PropertyField(value, GUIContent.none);
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
