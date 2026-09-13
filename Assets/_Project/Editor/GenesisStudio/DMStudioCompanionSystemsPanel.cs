#if UNITY_EDITOR
using System;
using Project.Companions;
using Project.Companions.Invector;
using Project.EditorTools.Companions;
using Project.UI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.GenesisStudio
{
    /// <summary>
    /// Companion loadouts, abilities, roster behavior, and live-scene AI in one studio tab.
    /// </summary>
    internal sealed class DMStudioCompanionSystemsPanel : IDisposable
    {
        private enum SystemsSection
        {
            Roster = 0,
            Abilities = 1,
            LiveAi = 2
        }

        private SystemsSection section;
        private readonly DMStudioAssetPanel rosterPanel = new DMStudioAssetPanel();
        private readonly DMStudioAssetPanel abilityPanel = new DMStudioAssetPanel();
        private UnityEditor.Editor followEditor;
        private UnityEditor.Editor loadoutEditor;
        private UnityEditor.Editor combatEditor;
        private PioneerCompanionAgent liveAgent;
        private Vector2 liveListScroll;

        public void Dispose()
        {
            rosterPanel.Dispose();
            abilityPanel.Dispose();
            DestroyEditor(ref followEditor);
            DestroyEditor(ref loadoutEditor);
            DestroyEditor(ref combatEditor);
        }

        public void Draw()
        {
            EditorGUILayout.HelpBox(
                "Roster data owns starting weapons/tools and PioneerBehaviorProfile (follow, wander, combat spacing). " +
                "Zero locomotion / animation / combat fields inherit class defaults at runtime — use Fill class defaults if the inspector looks empty. " +
                "Abilities are the equippable weapon / tool / buff assets. Live AI edits the scene companion while playing.",
                MessageType.Info);
            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginHorizontal();
            DrawSectionTab("Roster / Behavior", SystemsSection.Roster);
            DrawSectionTab("Abilities", SystemsSection.Abilities);
            DrawSectionTab("Live AI", SystemsSection.LiveAi);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(8f);

            switch (section)
            {
                case SystemsSection.Roster:
                    rosterPanel.DrawFolder(
                        CompanionCatalogRegistryUtility.DataFolder,
                        "t:NamedPioneerDefinition",
                        "Named companions: origin, class, preferred weapon/tool ids, buffs, and expedition AI.");
                    break;
                case SystemsSection.Abilities:
                    abilityPanel.DrawFolder(
                        "Assets/_Project/Resources/CompanionAbilities",
                        "t:CompanionAbilityData",
                        "Weapon, tool, deployable, and buff abilities with class gates and AI priority.");
                    break;
                case SystemsSection.LiveAi:
                    DrawLiveAi();
                    break;
            }
        }

        private void DrawSectionTab(string label, SystemsSection id)
        {
            if (DMStudioStyles.DrawSubTab(label, section == id))
                section = id;
        }

        private void DrawLiveAi()
        {
            PioneerCompanionAgent[] agents = UnityEngine.Object.FindObjectsByType<PioneerCompanionAgent>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            if (agents == null || agents.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "No live companions in the open scene. Enter Play with an expedition trio, or drop a Companion prefab in the scene.",
                    MessageType.None);
                return;
            }

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
            DMStudioStyles.DrawSection(
                "Scene Companions",
                DMStudioStyles.SidebarPanel,
                () =>
                {
                    liveListScroll = EditorGUILayout.BeginScrollView(liveListScroll, GUILayout.ExpandHeight(true));
                    for (int i = 0; i < agents.Length; i++)
                    {
                        PioneerCompanionAgent agent = agents[i];
                        if (agent == null)
                            continue;

                        bool selected = liveAgent == agent;
                        if (GUILayout.Button(agent.name, selected ? DMStudioStyles.ListButtonSelected : DMStudioStyles.ListButton))
                        {
                            liveAgent = agent;
                            Selection.activeGameObject = agent.gameObject;
                        }
                    }

                    EditorGUILayout.EndScrollView();
                },
                DarkMatterGenesisUiPalette.SlateGray);

            DMStudioStyles.DrawSection(
                "Inspector",
                DMStudioStyles.ContentPanel,
                () => DrawLiveInspector(agents),
                DarkMatterGenesisUiPalette.RichFuchsia);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLiveInspector(PioneerCompanionAgent[] agents)
        {
            PioneerCompanionAgent selected = liveAgent;
            if (selected == null)
            {
                selected = agents[0];
                liveAgent = selected;
            }

            CompanionFollowController follow = selected.GetComponent<CompanionFollowController>();
            CompanionInvectorLoadoutBridge loadout = selected.GetComponent<CompanionInvectorLoadoutBridge>();
            CompanionCombatController combat = selected.GetComponent<CompanionCombatController>();

            EditorGUILayout.LabelField(selected.name, DMStudioStyles.SectionTitle);
            EditorGUILayout.Space(4f);

            DrawLiveComponent("Follow / AI", follow, ref followEditor);
            EditorGUILayout.Space(10f);
            DrawLiveComponent("Weapon Loadout", loadout, ref loadoutEditor);
            EditorGUILayout.Space(10f);
            DrawLiveComponent("Combat", combat, ref combatEditor);
        }

        private static void DrawLiveComponent(string title, Component component, ref UnityEditor.Editor editor)
        {
            EditorGUILayout.LabelField(title, DMStudioStyles.SectionTitle);
            if (component == null)
            {
                EditorGUILayout.HelpBox("Missing on this companion.", MessageType.Warning);
                DestroyEditor(ref editor);
                return;
            }

            if (editor == null || editor.target != component)
            {
                DestroyEditor(ref editor);
                editor = UnityEditor.Editor.CreateEditor(component);
            }

            editor.OnInspectorGUI();
        }

        private static void DestroyEditor(ref UnityEditor.Editor editor)
        {
            if (editor == null)
                return;

            UnityEngine.Object.DestroyImmediate(editor);
            editor = null;
        }
    }
}
#endif
