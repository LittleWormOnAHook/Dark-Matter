#if UNITY_EDITOR
using Project.Player;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.EditorTools.Player
{
    /// <summary>
    /// 2026-11-03n. Draws DMPlayerSystemsProfile with one extra line under Presentation: the
    /// Subtitles tick is the SAME value as Main Menu > Settings > Game Options > Subtitles
    /// (GameSettings.SubtitlesEnabled / PlayerPrefs settings.game.subtitles). Default ON.
    /// Ticking here writes the shared value; the row refreshes from it every 500 ms, so the
    /// profile tick and the main menu toggle can never disagree.
    /// </summary>
    [CustomEditor(typeof(DMPlayerSystemsProfile))]
    [CanEditMultipleObjects]
    public sealed class DMPlayerSystemsProfileEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            if (Project.EditorTools.Theme.GenesisTheme.Enabled
                && Project.EditorTools.Theme.GenesisTheme.Apply(root))
                root.AddToClassList("g-inspector");

            // Same layout the Genesis fallback inspector would have produced.
            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            var note = new Label(
                "Subtitles here is the same setting as Main Menu \u25B8 Settings \u25B8 Game Options "
                + "\u25B8 Subtitles (PlayerPrefs settings.game.subtitles). Changing either changes both. "
                + "Default is ON for a fresh install; existing players keep their choice.");
            note.style.whiteSpace = WhiteSpace.Normal;
            note.style.marginTop = 4f;
            note.style.marginBottom = 2f;
            note.style.fontSize = 11f;
            note.style.opacity = 0.75f;
            root.Add(note);

            var bar = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 2f } };
            var tick = new Toggle("Subtitles") { style = { marginRight = 8f } };
            var current = new Label { style = { unityTextAlign = TextAnchor.MiddleLeft } };

            bool syncing = false;
            tick.RegisterValueChangedCallback(evt =>
            {
                if (syncing)
                    return;
                DMPlayerSystemsProfile.SetSubtitles(evt.newValue);
                Refresh();
            });

            bar.Add(tick);
            bar.Add(current);
            root.Add(bar);

            void Refresh()
            {
                bool on = DMPlayerSystemsProfile.SubtitlesEnabled;
                syncing = true;
                tick.SetValueWithoutNotify(on);
                syncing = false;
                current.text = on ? "Shared value: ON" : "Shared value: OFF";
            }

            Refresh();
            root.schedule.Execute(Refresh).Every(500);
            return root;
        }

        /// <summary>
        /// IMGUI path, used by Genesis Studio > Player (GenesisStudioWindow calls OnInspectorGUI on
        /// this editor). Same shared value, same default-ON behaviour.
        /// </summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                "Subtitles here is the same setting as Main Menu > Settings > Game Options > Subtitles "
                + "(PlayerPrefs settings.game.subtitles). Changing either changes both.",
                MessageType.None);

            bool on = DMPlayerSystemsProfile.SubtitlesEnabled;
            bool next = EditorGUILayout.Toggle("Subtitles (shared)", on);
            if (next != on)
                DMPlayerSystemsProfile.SetSubtitles(next);
        }
    }
}
#endif
