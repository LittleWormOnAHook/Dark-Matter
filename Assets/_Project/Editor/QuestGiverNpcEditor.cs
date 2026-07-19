using Project.Map;
using Project.Quests;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    [CustomEditor(typeof(QuestGiverNpc))]
    public class QuestGiverNpcEditor : UnityEditor.Editor
    {
        private SerializedProperty questOffersProperty;

        private void OnEnable()
        {
            questOffersProperty = serializedObject.FindProperty("questOffers");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "Assign quest offers below. Each offer links a QuestDefinition with dialogue and optional prerequisites. " +
                "Use Delete to remove an offer from this NPC.",
                MessageType.Info);

            DrawPropertiesExcluding(serializedObject, "m_Script", "questOffers");

            EditorGUILayout.Space(8f);
            DrawMapMarkerTools();
            EditorGUILayout.Space(8f);
            DrawQuestOffers();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMapMarkerTools()
        {
            QuestGiverNpc npc = (QuestGiverNpc)target;
            MapMarker marker = npc.GetComponent<MapMarker>();

            EditorGUILayout.LabelField("Minimap Marker", EditorStyles.boldLabel);
            if (marker == null)
            {
                EditorGUILayout.HelpBox("No MapMarker on this NPC — it will not appear on the map/minimap.", MessageType.Warning);
                if (GUILayout.Button("Add Map Marker", GUILayout.Height(24f)))
                {
                    Undo.AddComponent<MapMarker>(npc.gameObject);
                    marker = npc.GetComponent<MapMarker>();
                    if (marker != null)
                    {
                        marker.ConfigureQuestGiver(npc.GetComponent<QuestGiverNpc>() != null
                            ? serializedObject.FindProperty("displayName").stringValue
                            : npc.name);
                        EditorUtility.SetDirty(npc.gameObject);
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("MapMarker present — this NPC appears on the map/minimap.", MessageType.None);
                if (GUILayout.Button("Refresh Map Marker Label", GUILayout.Height(24f)))
                {
                    marker.ConfigureQuestGiver(serializedObject.FindProperty("displayName").stringValue);
                    EditorUtility.SetDirty(marker);
                }
            }
        }
        private void DrawQuestOffers()
        {
            if (questOffersProperty == null)
                return;

            EditorGUILayout.LabelField("Quest Offers", EditorStyles.boldLabel);

            if (questOffersProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No quest offers assigned. Add one below.", MessageType.None);
            }

            for (int i = 0; i < questOffersProperty.arraySize; i++)
            {
                SerializedProperty offerProperty = questOffersProperty.GetArrayElementAtIndex(i);
                if (offerProperty == null)
                    continue;

                SerializedProperty questProperty = offerProperty.FindPropertyRelative("quest");
                QuestDefinition quest = questProperty.objectReferenceValue as QuestDefinition;
                string questLabel = quest != null
                    ? (!string.IsNullOrWhiteSpace(quest.title) ? quest.title : quest.name)
                    : "(Unassigned)";

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Offer {i + 1}: {questLabel}", EditorStyles.boldLabel);
                if (GUILayout.Button("Delete", GUILayout.Width(64f)))
                {
                    questOffersProperty.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(questProperty, new GUIContent("Quest"));
                EditorGUILayout.PropertyField(offerProperty.FindPropertyRelative("prerequisiteQuestIds"), true);
                EditorGUILayout.PropertyField(offerProperty.FindPropertyRelative("makeAvailableOnTalk"));

                EditorGUILayout.LabelField("Dialogue", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(offerProperty.FindPropertyRelative("offerDialogue"));
                EditorGUILayout.PropertyField(offerProperty.FindPropertyRelative("progressDialogue"));
                EditorGUILayout.PropertyField(offerProperty.FindPropertyRelative("readyDialogue"));
                EditorGUILayout.PropertyField(offerProperty.FindPropertyRelative("rewardDialogue"));
                EditorGUILayout.PropertyField(offerProperty.FindPropertyRelative("doneDialogue"));

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4f);
            }

            EditorGUILayout.Space(4f);
            if (GUILayout.Button("Add Quest Offer", GUILayout.Height(26f)))
                questOffersProperty.InsertArrayElementAtIndex(questOffersProperty.arraySize);
        }
    }
}
