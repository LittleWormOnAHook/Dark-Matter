#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace GenesisPCG.RockCreation.Editor
{
    /// <summary>Icon grid of a <see cref="PcgAssetLibrary"/>: search, style / category filter, size slider, drag to the Scene View.</summary>
    public sealed class PcgStudioLibraryView : VisualElement
    {
        public Action<PcgAssetLibrary.Entry> OpenInPanel;
        public Action<PcgAssetLibrary.Entry> RerenderRequested;
        public Action<string> Status;

        private readonly ToolbarSearchField m_search;
        private readonly DropdownField m_style, m_category;
        private readonly SliderInt m_size;
        private readonly ScrollView m_scroll;
        private readonly VisualElement m_grid;
        private readonly Label m_count, m_empty;
        private PcgAssetLibrary m_lib;
        private string m_selectedId;

        private const string AllStyles = "All styles", AllCategories = "All";

        public PcgStudioLibraryView()
        {
            AddToClassList("pcg-library");

            var bar = new VisualElement();
            bar.AddToClassList("pcg-library__bar");
            Add(bar);
            m_search = new ToolbarSearchField();
            m_search.AddToClassList("pcg-library__search");
            m_search.RegisterValueChangedCallback(_ => Rebuild());
            bar.Add(m_search);
            m_category = new DropdownField(new List<string> { AllCategories, "Rocks", "Cliffs" }, 0);
            m_category.AddToClassList("pcg-library__filter");
            m_category.RegisterValueChangedCallback(_ => Rebuild());
            bar.Add(m_category);
            m_style = new DropdownField(new List<string> { AllStyles }, 0);
            m_style.AddToClassList("pcg-library__filter");
            m_style.RegisterValueChangedCallback(_ => Rebuild());
            bar.Add(m_style);
            var sizeLabel = new Label("Size");
            sizeLabel.AddToClassList("pcg-label-dim");
            bar.Add(sizeLabel);
            m_size = new SliderInt(64, 256) { value = EditorPrefs.GetInt("GenesisPCG.Studio.LibraryTile", 112) };
            m_size.AddToClassList("pcg-library__size");
            m_size.RegisterValueChangedCallback(e =>
            {
                EditorPrefs.SetInt("GenesisPCG.Studio.LibraryTile", e.newValue);
                foreach (VisualElement t in m_grid.Children()) SizeTile(t, e.newValue);
            });
            bar.Add(m_size);
            var place = new Toggle("New variation on place") { value = PcgStudioLibraryUtil.NewVariationOnPlace, tooltip = "Placing from the library rolls a new seed (locked seeds are kept), like paste / Shift+drag." };
            place.AddToClassList("pcg-toggle");
            place.RegisterValueChangedCallback(e => PcgStudioLibraryUtil.NewVariationOnPlace = e.newValue);
            bar.Add(place);
            m_count = new Label();
            m_count.AddToClassList("pcg-label-dim");
            bar.Add(m_count);
            var ping = new Button(() => { if (m_lib != null) EditorGUIUtility.PingObject(m_lib); }) { text = "Ping Library" };
            ping.AddToClassList("pcg-btn");
            PcgCutCorners.Apply(ping);
            bar.Add(ping);

            m_scroll = new ScrollView(ScrollViewMode.Vertical);
            m_scroll.AddToClassList("pcg-library__scroll");
            Add(m_scroll);
            m_grid = new VisualElement();
            m_grid.AddToClassList("pcg-library__grid");
            m_scroll.Add(m_grid);
            m_empty = new Label("Library is empty. Use Save to Library to add the current rock.");
            m_empty.AddToClassList("pcg-library__empty");
            m_scroll.Add(m_empty);

            RegisterCallback<AttachToPanelEvent>(_ => { PcgAssetLibrary.Changed -= OnLibChanged; PcgAssetLibrary.Changed += OnLibChanged; Undo.undoRedoPerformed -= Refresh; Undo.undoRedoPerformed += Refresh; Refresh(); });
            RegisterCallback<DetachFromPanelEvent>(_ => { PcgAssetLibrary.Changed -= OnLibChanged; Undo.undoRedoPerformed -= Refresh; });
        }

        public PcgAssetLibrary Library => m_lib;
        public int TileSize { get => m_size.value; set => m_size.value = value; }
        public string SelectedId => m_selectedId;

        private void OnLibChanged(PcgAssetLibrary lib) => Refresh();

        public void Refresh()
        {
            m_lib = PcgStudioLibraryUtil.Load(false);
            var styles = new List<string> { AllStyles };
            if (m_lib != null)
                styles.AddRange(m_lib.entries.Where(e => e != null && e.style != null).Select(e => e.style.name).Distinct().OrderBy(s => s));
            string cur = m_style.value;
            m_style.choices = styles;
            m_style.SetValueWithoutNotify(styles.Contains(cur) ? cur : AllStyles);
            Rebuild();
        }

        public void Select(string id)
        {
            m_selectedId = id;
            foreach (VisualElement t in m_grid.Children())
                t.EnableInClassList("pcg-tile--selected", (t.userData as PcgAssetLibrary.Entry)?.id == id);
        }

        private void Rebuild()
        {
            m_grid.Clear();
            var list = new List<PcgAssetLibrary.Entry>();
            if (m_lib != null)
            {
                string q = (m_search.value ?? "").Trim();
                foreach (PcgAssetLibrary.Entry e in m_lib.entries)
                {
                    if (e == null) continue;
                    if (q.Length > 0 && e.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0 &&
                        (e.style == null || e.style.name.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0)) continue;
                    if (m_style.value != AllStyles && (e.style == null || e.style.name != m_style.value)) continue;
                    if (m_category.value != AllCategories && !string.Equals(e.category, m_category.value, StringComparison.OrdinalIgnoreCase)) continue;
                    list.Add(e);
                }
            }
            foreach (PcgAssetLibrary.Entry e in list) m_grid.Add(MakeTile(e));
            int total = m_lib != null ? m_lib.entries.Count : 0;
            m_count.text = list.Count == total ? $"{total} items" : $"{list.Count} / {total} items";
            m_empty.style.display = list.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            m_empty.text = total == 0 ? "Library is empty. Use Save to Library to add the current rock." : "No items match the filter.";
        }

        private static string StyleLabel(DmRockStyle s) => s == null ? "" : s.name.Replace("DM_RockStyle_", "");

        private VisualElement MakeTile(PcgAssetLibrary.Entry e)
        {
            var tile = new VisualElement { userData = e, tooltip = $"{e.Name}\n{(e.prefab != null ? AssetDatabase.GetAssetPath(e.prefab) : "missing prefab")}\nStyle {StyleLabel(e.style)}  ·  seed {e.seed}\nClick: select · Drag: place in Scene · Right-click: more" };
            tile.AddToClassList("pcg-tile");
            PcgCutCorners.Apply(tile);
            var img = new Image { image = e.icon != null ? e.icon : (Texture)AssetPreview.GetMiniThumbnail(e.prefab), scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            img.AddToClassList("pcg-tile__icon");
            tile.Add(img);
            var name = new Label(e.Name) { pickingMode = PickingMode.Ignore };
            name.AddToClassList("pcg-tile__name");
            tile.Add(name);
            var meta = new Label(StyleLabel(e.style)) { pickingMode = PickingMode.Ignore };
            meta.AddToClassList("pcg-tile__meta");
            tile.Add(meta);
            if (e.prefab == null) tile.AddToClassList("pcg-tile--missing");
            tile.EnableInClassList("pcg-tile--selected", e.id == m_selectedId);
            SizeTile(tile, m_size.value);

            Vector2 down = default;
            bool pressed = false;
            tile.RegisterCallback<PointerDownEvent>(ev =>
            {
                if (ev.button != 0) return;
                pressed = true;
                down = ev.position;
            });
            tile.RegisterCallback<PointerMoveEvent>(ev =>
            {
                if (!pressed || (ev.pressedButtons & 1) == 0 || e.prefab == null) return;
                if (((Vector2)ev.position - down).sqrMagnitude < 36f) return;
                pressed = false;
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new Object[] { e.prefab };
                DragAndDrop.paths = new[] { AssetDatabase.GetAssetPath(e.prefab) };
                DragAndDrop.SetGenericData(PcgStudioLibraryUtil.DragKey, e.id);
                DragAndDrop.StartDrag(e.Name);
                Status?.Invoke($"Dragging {e.Name}: drop it on the ground in the Scene View.");
            });
            tile.RegisterCallback<PointerUpEvent>(ev =>
            {
                if (ev.button != 0 || !pressed) return;
                pressed = false;
                Select(e.id);
                if (e.prefab != null)
                {
                    Selection.activeObject = e.prefab;
                    EditorGUIUtility.PingObject(e.prefab);
                }
                if (ev.clickCount >= 2) OpenInPanel?.Invoke(e);
            });
            tile.RegisterCallback<PointerLeaveEvent>(_ => pressed = false);
            tile.AddManipulator(new ContextualMenuManipulator(ev =>
            {
                ev.menu.AppendAction("Open in Editor Panel", _ => OpenInPanel?.Invoke(e), e.prefab != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                ev.menu.AppendAction("Select Prefab", _ => { Selection.activeObject = e.prefab; EditorGUIUtility.PingObject(e.prefab); }, e.prefab != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                ev.menu.AppendAction("Rename", _ => BeginRename(tile, name, e));
                ev.menu.AppendAction("Re-render Icon", _ => RerenderRequested?.Invoke(e), e.prefab != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                ev.menu.AppendSeparator();
                ev.menu.AppendAction("Remove from Library (keeps the prefab)", _ =>
                {
                    PcgStudioLibraryUtil.Remove(m_lib, e);
                    Status?.Invoke($"Removed {e.Name} from the library (prefab kept).");
                });
            }));
            return tile;
        }

        private static void SizeTile(VisualElement tile, int size)
        {
            tile.style.width = size + 12;
            VisualElement icon = tile.Q(className: "pcg-tile__icon");
            if (icon != null) { icon.style.width = size; icon.style.height = size; }
        }

        private void BeginRename(VisualElement tile, Label label, PcgAssetLibrary.Entry e)
        {
            var field = new TextField { value = e.Name };
            field.AddToClassList("pcg-tile__rename");
            label.style.display = DisplayStyle.None;
            tile.Insert(tile.IndexOf(label), field);
            bool done = false;
            void Commit(bool apply)
            {
                if (done) return;
                done = true;
                if (apply && !string.IsNullOrWhiteSpace(field.value) && field.value != e.Name)
                {
                    PcgStudioLibraryUtil.Rename(m_lib, e, field.value);
                    Status?.Invoke($"Renamed to {e.Name}.");
                }
                else
                {
                    field.RemoveFromHierarchy();
                    label.style.display = DisplayStyle.Flex;
                }
            }
            field.RegisterCallback<KeyDownEvent>(k =>
            {
                if (k.keyCode == KeyCode.Return || k.keyCode == KeyCode.KeypadEnter) Commit(true);
                else if (k.keyCode == KeyCode.Escape) Commit(false);
            }, TrickleDown.TrickleDown);
            field.RegisterCallback<FocusOutEvent>(_ => Commit(true));
            field.schedule.Execute(() => { field.Focus(); field.SelectAll(); });
        }

        /// <summary>Test / automation helper: renames without the inline field.</summary>
        public void RenameEntry(PcgAssetLibrary.Entry e, string name) => PcgStudioLibraryUtil.Rename(m_lib, e, name);
    }
}
#endif
