#if UNITY_EDITOR
using Project.AI.Invector;
using Project.EditorTools.Theme;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Project.EditorTools.AI
{
    /// <summary>
    /// LoadoutBridge inspector. 2026-10-20b: UI Toolkit + GenesisTheme (same look as GenesisFallbackInspector),
    /// keeps the "Ranged Ammo Type" relabel and its Override Brain gate. Field names / serialization unchanged.
    /// </summary>
    [CustomEditor(typeof(EnemyInvectorLoadoutBridge))]
    [CanEditMultipleObjects]
    public sealed class EnemyInvectorLoadoutBridgeEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            if (GenesisTheme.Enabled && GenesisTheme.Apply(root))
                root.AddToClassList("g-inspector");

            SerializedProperty overrideBrain = serializedObject.FindProperty("overrideBrain");
            SerializedProperty it = serializedObject.GetIterator();
            bool enterChildren = true;
            while (it.NextVisible(enterChildren))
            {
                enterChildren = false;
                SerializedProperty prop = it.Copy();
                if (prop.name == "m_Script")
                {
                    var script = new PropertyField(prop);
                    script.SetEnabled(false);
                    root.Add(script);
                    continue;
                }

                if (prop.name == "overrideRangedAmmoType")
                {
                    var ammo = new PropertyField(prop, "Ranged Ammo Type");
                    var poolNote = new HelpBox("Ignored: Ranged Ammo Type Pool is in use.", HelpBoxMessageType.Info);
                    SerializedProperty pool = serializedObject.FindProperty("rangedAmmoTypePool");
                    void Refresh()
                    {
                        bool poolInUse = pool != null && (pool.hasMultipleDifferentValues || pool.arraySize > 0);
                        bool brainOn = overrideBrain != null && overrideBrain.boolValue;
                        ammo.SetEnabled(brainOn && !poolInUse);
                        ammo.tooltip = poolInUse ? "Ignored: Ranged Ammo Type Pool is in use." : "";
                        poolNote.style.display = poolInUse ? DisplayStyle.Flex : DisplayStyle.None;
                    }
                    Refresh();
                    if (overrideBrain != null) ammo.TrackPropertyValue(overrideBrain, _ => Refresh());
                    if (pool != null) ammo.TrackPropertyValue(pool, _ => Refresh()); // 2026-10-20c: grey out while the pool has entries
                    root.Add(ammo);
                    root.Add(poolNote);
                    continue;
                }

                root.Add(new PropertyField(prop));
            }

            return root;
        }
    }
}
#endif
