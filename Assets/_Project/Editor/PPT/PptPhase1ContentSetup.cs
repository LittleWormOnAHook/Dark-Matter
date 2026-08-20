using Project.PPT;
using Project.Quests;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public static class PptPhase1ContentSetup
    {
        private const string RegistryPath = "Assets/_Project/Resources/PPT/PptRegistry.asset";
        private const string PioneerGuidePrefabPath = "Assets/_Project/Prefabs/NPCs/QuestGiver_PioneerGuide.prefab";

        [MenuItem(DarkMatterGenesisEditorMenus.Ppt + "Phase 1 - Wire Pioneer Guide + Sample Registry", false, 10)]
        public static void WirePhase1Content()
        {
            PptRegistry registry = AssetDatabase.LoadAssetAtPath<PptRegistry>(RegistryPath);
            if (registry == null)
            {
                EditorUtility.DisplayDialog(
                    "PPT Phase 1",
                    "Missing Resources/PPT/PptRegistry.asset. Pull the PPT content assets first.",
                    "OK");
                return;
            }

            int wired = 0;
            wired += WirePrefab(PioneerGuidePrefabPath, registry);
            wired += WireSceneGuides(registry);

            AssetDatabase.SaveAssets();
            Debug.Log($"PPT Phase 1 wiring complete ({wired} NPC(s) updated). Hold E on Pioneer Guide to ask directions.");
        }

        private static int WirePrefab(string prefabPath, PptRegistry registry)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                return 0;

            string assetPath = AssetDatabase.GetAssetPath(prefab);
            GameObject contents = PrefabUtility.LoadPrefabContents(assetPath);
            try
            {
                if (!WireNpc(contents, registry))
                    return 0;

                PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
                return 1;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static int WireSceneGuides(PptRegistry registry)
        {
            int count = 0;
            QuestGiverNpc[] givers = Object.FindObjectsByType<QuestGiverNpc>(FindObjectsInactive.Include);
            for (int i = 0; i < givers.Length; i++)
            {
                QuestGiverNpc giver = givers[i];
                if (!IsPhase1DirectionsNpc(giver))
                    continue;

                if (WireNpc(giver.gameObject, registry))
                    count++;
            }

            return count;
        }

        private static bool WireNpc(GameObject npc, PptRegistry registry)
        {
            if (npc == null)
                return false;

            PptNpcInteractor interactor = npc.GetComponent<PptNpcInteractor>();
            if (interactor == null)
                interactor = npc.AddComponent<PptNpcInteractor>();

            PptNpcGestureController gesture = npc.GetComponent<PptNpcGestureController>();
            if (gesture == null)
                gesture = npc.AddComponent<PptNpcGestureController>();

            SerializedObject so = new SerializedObject(interactor);
            so.FindProperty("npcId").stringValue = "pioneer_guide";
            PptNpcProfile profile = FindPioneerGuideProfile(registry);
            so.FindProperty("profile").objectReferenceValue = profile;
            so.ApplyModifiedPropertiesWithoutUndo();

            Transform visualRoot = npc.transform.Find("Body");
            if (visualRoot == null && npc.transform.childCount > 0)
                visualRoot = npc.transform.GetChild(0);

            if (visualRoot != null)
            {
                SerializedObject gestureSo = new SerializedObject(gesture);
                gestureSo.FindProperty("visualRoot").objectReferenceValue = visualRoot;
                gestureSo.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(npc);
            return true;
        }

        private static PptNpcProfile FindPioneerGuideProfile(PptRegistry registry)
        {
            if (registry?.NpcProfiles == null)
                return null;

            for (int i = 0; i < registry.NpcProfiles.Length; i++)
            {
                PptNpcProfile profile = registry.NpcProfiles[i];
                if (profile != null && profile.NpcId == "pioneer_guide")
                    return profile;
            }

            return registry.NpcProfiles.Length > 0 ? registry.NpcProfiles[0] : null;
        }

        private static bool IsPhase1DirectionsNpc(QuestGiverNpc giver)
        {
            if (giver == null)
                return false;

            string objectName = giver.gameObject.name;
            if (objectName.IndexOf("ALEXO", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;

            if (objectName.Equals("GERALD", System.StringComparison.OrdinalIgnoreCase))
                return true;

            if (objectName.IndexOf("PioneerGuide", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return string.Equals(giver.NpcId, "pioneer_guide", System.StringComparison.Ordinal)
                && objectName.IndexOf("Guide", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
