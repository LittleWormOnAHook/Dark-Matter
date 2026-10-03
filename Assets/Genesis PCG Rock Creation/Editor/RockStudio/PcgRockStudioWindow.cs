#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GenesisPCG.RockCreation.Editor
{
    /// <summary>Standalone host for <see cref="PcgRockStudioPanel"/> (Tools/Genesis PCG Rock Creation/Rocks and Cliffs Studio).</summary>
    public sealed class PcgRockStudioWindow : EditorWindow
    {
        public const string MenuPath = "Tools/Genesis PCG Rock Creation/Rocks and Cliffs Studio";

        public PcgRockStudioPanel Panel { get; private set; }

        [MenuItem(MenuPath, false, 0)]
        public static PcgRockStudioWindow Open()
        {
            var w = GetWindow<PcgRockStudioWindow>();
            w.titleContent = new GUIContent("Rocks and Cliffs");
            w.minSize = new Vector2(960f, 640f);
            w.Show();
            return w;
        }

        private void CreateGUI()
        {
            Panel = new PcgRockStudioPanel();
            Panel.style.flexGrow = 1f;
            rootVisualElement.Add(Panel);
        }

        private void OnBecameVisible() { if (Panel != null) Panel.SetSuspended(false); }
        private void OnBecameInvisible() { if (Panel != null) Panel.SetSuspended(true); }
    }
}
#endif
