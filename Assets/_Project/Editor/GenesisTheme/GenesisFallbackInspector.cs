using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Project.EditorTools.Theme
{
    /// <summary>
    /// Themed default inspector for Genesis-owned components and assets (scripts under Assets/_Project or
    /// Assets/Genesis PCG Rock Creation) that have no custom editor of their own. It is a fallback editor, so any real
    /// custom editor (ours, Invector's, Unity's) still wins. Everything else gets Unity's normal default inspector.
    /// </summary>
    public abstract class GenesisFallbackInspectorBase : UnityEditor.Editor
    {
        static readonly string[] OwnedRoots = { "Assets/_Project/", "Assets/Genesis PCG Rock Creation/" };
        static readonly Dictionary<Type, bool> s_Owned = new Dictionary<Type, bool>();

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            if (GenesisTheme.Enabled && IsGenesisOwned(target) && GenesisTheme.Apply(root))
                root.AddToClassList("g-inspector");

            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            return root;
        }

        public static bool IsGenesisOwned(Object obj)
        {
            if (obj == null) return false;
            Type type = obj.GetType();
            if (s_Owned.TryGetValue(type, out bool owned)) return owned;

            MonoScript script = null;
            if (obj is MonoBehaviour mb) script = MonoScript.FromMonoBehaviour(mb);
            else if (obj is ScriptableObject so) script = MonoScript.FromScriptableObject(so);

            string path = script != null ? AssetDatabase.GetAssetPath(script) : null;
            owned = false;
            if (!string.IsNullOrEmpty(path))
                foreach (string r in OwnedRoots)
                    if (path.StartsWith(r, StringComparison.Ordinal)) { owned = true; break; }

            s_Owned[type] = owned;
            return owned;
        }
    }

    [CustomEditor(typeof(MonoBehaviour), true, isFallback = true)]
    [CanEditMultipleObjects]
    public sealed class GenesisFallbackMonoInspector : GenesisFallbackInspectorBase { }

    [CustomEditor(typeof(ScriptableObject), true, isFallback = true)]
    [CanEditMultipleObjects]
    public sealed class GenesisFallbackAssetInspector : GenesisFallbackInspectorBase { }
}
