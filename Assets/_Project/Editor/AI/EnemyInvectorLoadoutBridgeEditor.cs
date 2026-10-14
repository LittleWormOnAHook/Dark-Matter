#if UNITY_EDITOR
using Project.AI.Invector;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.AI
{
    [CustomEditor(typeof(EnemyInvectorLoadoutBridge))]
    [CanEditMultipleObjects]
    public sealed class EnemyInvectorLoadoutBridgeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.name == "m_Script")
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.PropertyField(iterator);
                    EditorGUI.EndDisabledGroup();
                    continue;
                }

                if (iterator.name == "overrideRangedAmmoType")
                {
                    SerializedProperty overrideBrain = serializedObject.FindProperty("overrideBrain");
                    EditorGUI.BeginDisabledGroup(overrideBrain == null || !overrideBrain.boolValue);
                    EditorGUILayout.PropertyField(iterator, new GUIContent("Ranged Ammo Type"));
                    EditorGUI.EndDisabledGroup();
                    continue;
                }

                EditorGUILayout.PropertyField(iterator, true);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
