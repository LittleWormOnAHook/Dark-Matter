using Project.Combat;
using Project.Data;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    [CustomEditor(typeof(DMAmmoFxProfile))]
    public sealed class DMAmmoFxProfileEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            DMAmmoFxProfile profile = (DMAmmoFxProfile)target;
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Ammo Profile Sync", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This asset is the full ammo authoring profile (stats + FX + per-ammo hit marks). " +
                "Recoil Vertical/Horizontal are the live camera kick. " +
                "Rifle Camera fields on Ammo Recoil Profile still override two-hand weapons. " +
                "Invector weapon recoilUp / recoilLeft / recoilRight do nothing. " +
                "Fire Rate is shots or burst groups per second and overrides the weapon when > 0. " +
                "Shots Per Burst 1 = single; 2-5 fires that many rounds at Burst Fire Rate.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Seed Hit-Mark Rows From Catalog"))
            {
                Undo.RecordObject(profile, "Seed ammo hit marks");
                profile.SeedSurfacesFromCatalog();
                EditorUtility.SetDirty(profile);
            }

            DMAmmoFxProfile selectedProfile = Selection.activeObject as DMAmmoFxProfile;
            using (new EditorGUI.DisabledScope(selectedProfile == null || selectedProfile == profile))
            {
                if (GUILayout.Button("Copy From Selected Profile"))
                {
                    Undo.RecordObject(profile, "Copy ammo profile");
                    profile.CopyFrom(selectedProfile);
                    EditorUtility.SetDirty(profile);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    [CustomPropertyDrawer(typeof(DMHitMarkSurface))]
    public sealed class DMHitMarkSurfaceDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            float height = EditorGUIUtility.singleLineHeight * 3f + EditorGUIUtility.standardVerticalSpacing * 3f;
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("decals"), true);
            height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("hitEffects"), true);
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect foldout = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldout, property.isExpanded, label, true);
            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;
            float y = foldout.yMax + EditorGUIUtility.standardVerticalSpacing;
            float line = EditorGUIUtility.singleLineHeight;
            SerializedProperty tag = property.FindPropertyRelative("tag");
            string current = string.IsNullOrEmpty(tag.stringValue) ? "Untagged" : tag.stringValue;
            tag.stringValue = EditorGUI.TagField(new Rect(position.x, y, position.width, line), "Tag", current);
            y += line + EditorGUIUtility.standardVerticalSpacing;

            SerializedProperty hitMark = property.FindPropertyRelative("hitMark");
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, line), hitMark);
            y += line + EditorGUIUtility.standardVerticalSpacing;

            SerializedProperty decals = property.FindPropertyRelative("decals");
            float decalHeight = EditorGUI.GetPropertyHeight(decals, true);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, decalHeight), decals, true);
            y += decalHeight + EditorGUIUtility.standardVerticalSpacing;

            SerializedProperty hitEffects = property.FindPropertyRelative("hitEffects");
            float effectHeight = EditorGUI.GetPropertyHeight(hitEffects, true);
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, effectHeight), hitEffects, true);

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }
    }
}
