using Project.Quests;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    [CustomEditor(typeof(QuestGiverNpc))]
    public class QuestGiverNpcEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "Assign one or more Quest Offers. Each offer links a QuestDefinition asset with NPC dialogue and optional prerequisite quests.",
                MessageType.Info);

            DrawPropertiesExcluding(serializedObject, "m_Script");

            serializedObject.ApplyModifiedProperties();
        }
    }
}
