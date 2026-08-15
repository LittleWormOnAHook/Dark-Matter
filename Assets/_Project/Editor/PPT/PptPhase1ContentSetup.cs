using System.Collections.Generic;
using System.IO;
using System.Text;
using Project.PPT;
using Project.Quests;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.EditorTools
{
    /// <summary>
    /// Idempotent PPT Phase 0+1 verify/wire. Prior cloud agents authored assets on disk
    /// without running Unity menus — use these entries to re-run foundation checks and
    /// Pioneer Guide / GERALD wiring in the Editor.
    /// </summary>
    public static class PptPhase1ContentSetup
    {
        private const string RegistryPath = "Assets/_Project/Resources/PPT/PptRegistry.asset";
        private const string PioneerGuidePrefabPath = "Assets/_Project/Prefabs/NPCs/QuestGiver_PioneerGuide.prefab";
        private const string ResourcesFolder = "Assets/_Project/Resources/PPT";

        private static readonly string[] RequiredPhase0Scripts =
        {
            "Assets/_Project/Scripts/PPT/Data/PptEntry.cs",
            "Assets/_Project/Scripts/PPT/Data/PptRegistry.cs",
            "Assets/_Project/Scripts/PPT/Data/PptNpcProfile.cs",
            "Assets/_Project/Scripts/PPT/Data/PptKeywordSource.cs",
            "Assets/_Project/Scripts/PPT/Data/PptDirectionPhraseSet.cs",
            "Assets/_Project/Scripts/PPT/Runtime/PptBootstrap.cs",
            "Assets/_Project/Scripts/PPT/Runtime/PptManager.cs",
            "Assets/_Project/Scripts/PPT/Runtime/PptNpcInteractor.cs",
            "Assets/_Project/Scripts/PPT/Runtime/PptKeywordLog.cs",
            "Assets/_Project/Scripts/PPT/Runtime/PptDirectionResolver.cs",
            "Assets/_Project/Scripts/PPT/Runtime/PptTerrainDirectionTracer.cs",
            "Assets/_Project/Scripts/UI/PptDirectionsMenuUI.cs",
            "Assets/_Project/Scripts/Vendor/IVendor.cs",
            "Assets/_Project/Features/PPT/Adapters/PptGameStateProvider.cs",
            "Assets/_Project/Features/GameState/Runtime/Snapshots/PptKnowledgeSnapshot.cs",
            "Assets/_Project/Scripts/Core/GameSaveData.cs",
            "Assets/_Project/Scripts/Core/GameSaveSystem.cs"
        };

        private static readonly string[] RequiredPhase1Assets =
        {
            "Assets/_Project/Resources/PPT/PptRegistry.asset",
            "Assets/_Project/Resources/PPT/PptEntry_Camp.asset",
            "Assets/_Project/Resources/PPT/PptEntry_SulfurDunes.asset",
            "Assets/_Project/Resources/PPT/PptEntry_OldRunesOfPedra.asset",
            "Assets/_Project/Resources/PPT/PptEntry_Mushrooms.asset",
            "Assets/_Project/Resources/PPT/PptNpcProfile_PioneerGuide.asset",
            "Assets/_Project/Resources/PPT/PptKeywordSource_Starter.asset",
            "Assets/_Project/Resources/PPT/PptDirectionPhrases.asset"
        };

        [MenuItem(DarkMatterGenesisEditorMenus.Ppt + "Phase 0+1 - Verify Foundation + Wire Sample Registry", false, 0)]
        public static void VerifyAndWirePhase01()
        {
            StringBuilder report = new StringBuilder();
            List<string> failures = new List<string>();

            report.AppendLine("=== PPT Phase 0 — Foundation ===");
            ValidatePhase0(report, failures);

            report.AppendLine();
            report.AppendLine("=== PPT Phase 1 — Sample registry + NPC wire ===");
            ValidatePhase1Assets(report, failures);

            PptRegistry registry = AssetDatabase.LoadAssetAtPath<PptRegistry>(RegistryPath);
            if (registry == null)
            {
                failures.Add($"Missing registry at {RegistryPath}");
            }
            else
            {
                ValidateRegistryLinks(registry, report, failures);

                int wired = 0;
                wired += WirePrefab(PioneerGuidePrefabPath, registry);
                wired += WireOpenScenes(registry);
                report.AppendLine($"Wired / refreshed {wired} NPC(s) (prefab + open scenes).");

                AssetDatabase.SaveAssets();
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (scene.IsValid() && scene.isLoaded && !string.IsNullOrEmpty(scene.path))
                        EditorSceneManager.MarkSceneDirty(scene);
                }
            }

            report.AppendLine();
            if (failures.Count == 0)
            {
                report.AppendLine("RESULT: Phase 0+1 PASS — foundation + sample content present; NPCs re-wired.");
                report.AppendLine("Playtest: start game → Tap E on GERALD → Directions (or Hold E) → pick Camp / Sulfur Dunes / Old Runes.");
                Debug.Log(report.ToString());
                EditorUtility.DisplayDialog("PPT Phase 0+1", report.ToString(), "OK");
            }
            else
            {
                report.AppendLine($"RESULT: Phase 0+1 FAIL ({failures.Count} issue(s))");
                for (int i = 0; i < failures.Count; i++)
                    report.AppendLine(" - " + failures[i]);
                Debug.LogError(report.ToString());
                EditorUtility.DisplayDialog("PPT Phase 0+1", report.ToString(), "OK");
            }
        }

        [MenuItem(DarkMatterGenesisEditorMenus.Ppt + "Phase 1 - Wire Pioneer Guide + Sample Registry", false, 10)]
        public static void WirePhase1Content()
        {
            PptRegistry registry = AssetDatabase.LoadAssetAtPath<PptRegistry>(RegistryPath);
            if (registry == null)
            {
                EditorUtility.DisplayDialog(
                    "PPT Phase 1",
                    "Missing Resources/PPT/PptRegistry.asset. Run Phase 0+1 verify first, or pull PPT content assets.",
                    "OK");
                return;
            }

            int wired = 0;
            wired += WirePrefab(PioneerGuidePrefabPath, registry);
            wired += WireOpenScenes(registry);

            AssetDatabase.SaveAssets();
            Debug.Log($"PPT Phase 1 wiring complete ({wired} NPC(s) updated). Hold E / Directions on Pioneer Guide.");
            EditorUtility.DisplayDialog(
                "PPT Phase 1",
                $"Wiring complete ({wired} NPC(s) updated).\nHold E or tap Directions on GERALD / Pioneer Guide.",
                "OK");
        }

        private static void ValidatePhase0(StringBuilder report, List<string> failures)
        {
            for (int i = 0; i < RequiredPhase0Scripts.Length; i++)
            {
                string path = RequiredPhase0Scripts[i];
                if (!File.Exists(path))
                {
                    failures.Add($"Missing Phase 0 file: {path}");
                    report.AppendLine($"FAIL missing {path}");
                }
                else
                {
                    report.AppendLine($"OK {path}");
                }
            }

            string saveSystem = File.ReadAllText("Assets/_Project/Scripts/Core/GameSaveSystem.cs");
            if (!saveSystem.Contains("CurrentSaveVersion = 22"))
                failures.Add("GameSaveSystem.CurrentSaveVersion is not 22");
            else
                report.AppendLine("OK save version 22");

            if (!saveSystem.Contains("pptKnownKeywordIds"))
                failures.Add("GameSaveSystem does not persist pptKnownKeywordIds");
            else
                report.AppendLine("OK pptKnownKeywordIds save/load");

            string saveData = File.ReadAllText("Assets/_Project/Scripts/Core/GameSaveData.cs");
            if (!saveData.Contains("pptKnownKeywordIds"))
                failures.Add("GameSaveData missing pptKnownKeywordIds field");
            else
                report.AppendLine("OK GameSaveData.pptKnownKeywordIds");

            string interactor = File.ReadAllText("Assets/_Project/Scripts/PPT/Runtime/PptNpcInteractor.cs");
            if (!interactor.Contains("IHoldWorldUsable") || !interactor.Contains("TryOpenDirectionsMenu"))
                failures.Add("PptNpcInteractor missing Hold-E / TryOpenDirectionsMenu contract");
            else
                report.AppendLine("OK Hold-E + TryOpenDirectionsMenu contract");

            string menuUi = File.ReadAllText("Assets/_Project/Scripts/UI/PptDirectionsMenuUI.cs");
            if (!menuUi.Contains("SetQuestDialogOpen") || !menuUi.Contains("Cursor.lockState"))
                failures.Add("PptDirectionsMenuUI does not unlock cursor / menu state");
            else
                report.AppendLine("OK directions menu unlocks cursor");

            string dialogUi = File.ReadAllText("Assets/_Project/Scripts/UI/QuestGiverDialogUI.cs");
            if (!dialogUi.Contains("Directions") || !dialogUi.Contains("askDirectionsCallback"))
                failures.Add("QuestGiverDialogUI missing Directions button hook");
            else
                report.AppendLine("OK quest dialog Directions button");
        }

        private static void ValidatePhase1Assets(StringBuilder report, List<string> failures)
        {
            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
            {
                failures.Add($"Missing folder {ResourcesFolder}");
                report.AppendLine($"FAIL {ResourcesFolder}");
                return;
            }

            for (int i = 0; i < RequiredPhase1Assets.Length; i++)
            {
                string path = RequiredPhase1Assets[i];
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (asset == null)
                {
                    failures.Add($"Missing Phase 1 asset: {path}");
                    report.AppendLine($"FAIL {path}");
                }
                else
                {
                    report.AppendLine($"OK {path}");
                }
            }
        }

        private static void ValidateRegistryLinks(PptRegistry registry, StringBuilder report, List<string> failures)
        {
            if (registry.Entries == null || registry.Entries.Length < 4)
                failures.Add("PptRegistry.entries should include Camp, Sulfur Dunes, Old Runes, Mushrooms");
            else
                report.AppendLine($"OK registry entries ({registry.Entries.Length})");

            if (registry.NpcProfiles == null || registry.NpcProfiles.Length < 1)
                failures.Add("PptRegistry.npcProfiles missing Pioneer Guide");
            else
            {
                bool found = false;
                for (int i = 0; i < registry.NpcProfiles.Length; i++)
                {
                    PptNpcProfile profile = registry.NpcProfiles[i];
                    if (profile != null && profile.NpcId == "pioneer_guide")
                    {
                        found = true;
                        if (!profile.HasTalkOption(PptTalkOptions.Directions))
                            failures.Add("Pioneer Guide profile lacks Directions talk option");
                        if (profile.PhraseSet == null)
                            failures.Add("Pioneer Guide profile missing phrase set");
                        break;
                    }
                }

                if (!found)
                    failures.Add("No npc profile with npcId pioneer_guide");
                else
                    report.AppendLine("OK pioneer_guide profile + Directions");
            }

            if (registry.KeywordSources == null || registry.KeywordSources.Length < 1)
                failures.Add("PptRegistry.keywordSources missing starter briefing");
            else
                report.AppendLine($"OK keyword sources ({registry.KeywordSources.Length})");
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

        private static int WireOpenScenes(PptRegistry registry)
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
