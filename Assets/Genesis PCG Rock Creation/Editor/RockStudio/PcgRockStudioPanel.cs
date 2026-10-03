#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace GenesisPCG.RockCreation.Editor
{
    /// <summary>
    /// Rocks and Cliffs studio panel (UI Toolkit, reusable): sources drop zone, preset / style / kit / material / blend
    /// pickers, seed, 512 icon window, Create / Save as Prefab / Save to Library and the library grid. Host it in any
    /// window (see <see cref="PcgRockStudioWindow"/>); theme it with USS (pcg-* classes) or <see cref="PcgStudioExtensions"/>.
    /// Uses the existing combiner / baker / snap code; no host or theme dependencies.
    /// </summary>
    public sealed class PcgRockStudioPanel : VisualElement, IPcgStudioContext
    {
        // ------------------------------------------------------------------------------------------------
        // Model

        /// <summary>One dropped prefab / mesh / scene object.</summary>
        public sealed class SourceItem
        {
            public Object obj;
            /// <summary>Set when loaded from a saved rock (exact base model, kept as is).</summary>
            public DmRockCombiner.BaseModel model;
            public bool IsSceneObject => obj is GameObject g && !EditorUtility.IsPersistent(g);
            public DmRockCombiner TargetRock => obj is GameObject g && !EditorUtility.IsPersistent(g) ? g.GetComponent<DmRockCombiner>() : null;
            public string Kind => model != null ? "saved" : obj is Mesh ? "mesh" : TargetRock != null ? "rock · target" : IsSceneObject ? "scene" : "prefab";
            public string Name => obj != null ? obj.name : model != null && model.source != null ? model.source.name : model != null && model.mesh != null ? model.mesh.name : "(missing)";
        }

        /// <summary>Settings the panel applies to a combiner (null slots = the preset's).</summary>
        public sealed class Spec
        {
            public DmRockPreset preset;
            public DmRockKit kit;
            public DmRockStyle style;
            public Material material;
            public Material blend;
            public bool matchTerrain = true;
            public int seed = 1;
            public DmRockFeatureSettings features = new DmRockFeatureSettings();
        }

        public readonly List<SourceItem> Sources = new List<SourceItem>();
        public int BaseIndex;
        public readonly Spec Settings = new Spec();
        public bool HideSceneSources = true;
        public bool LiveEditCreated = true;

        public DmRockCombiner CreatedRock { get; private set; }
        public GameObject LastSavedPrefab { get; private set; }
        public PcgAssetLibrary.Entry EditingEntry { get; private set; }
        public string LastStatus { get; private set; } = "";

        // ------------------------------------------------------------------------------------------------
        // UI

        private readonly PcgIconView m_icon;
        private readonly PcgStudioLibraryView m_library;
        private readonly VisualElement m_rocksBody, m_cliffsBody;
        private VisualElement m_sourceList, m_drop;
        private readonly Button m_tabRocks, m_tabCliffs, m_recapture;
        private readonly Label m_status, m_editing;
        private DropdownField m_preset, m_style, m_kit, m_material, m_blend;
        private IntegerField m_seed;
        private Toggle m_matchTerrain, m_useStyleChances, m_guaranteeNook, m_hideSources, m_liveEdit;
        private Slider m_passage, m_nook;
        private TextField m_name, m_folder;
        private List<DmRockPreset> m_presets = new List<DmRockPreset>();
        private List<DmRockStyle> m_styles = new List<DmRockStyle>();
        private List<DmRockKit> m_kits = new List<DmRockKit>();
        private List<Material> m_materials = new List<Material>();
        private List<Material> m_blends = new List<Material>();
        private bool m_cliffsBuilt;
        private string m_section = "Rocks";

        private const string FromPreset = "(from preset)", NoneLabel = "(none)";

        public PcgRockStudioPanel()
        {
            AddToClassList("pcg-studio");
            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(PcgStudioExtensions.StyleSheetPath);
            if (uss != null) styleSheets.Add(uss);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("pcg-studio__scroll");
            Add(scroll);
            VisualElement root = scroll.contentContainer;

            // Header
            var header = new VisualElement();
            header.AddToClassList("pcg-studio__header");
            var title = new Label("ROCKS AND CLIFFS");
            title.AddToClassList("pcg-header");
            title.AddToClassList("pcg-title");
            header.Add(title);
            var sub = new Label("Drop base prefabs, pick a profile and style, preview at 512 and save as prefab / library item.");
            sub.AddToClassList("pcg-subtitle");
            header.Add(sub);
            root.Add(header);

            // Section tabs
            var tabs = new VisualElement();
            tabs.AddToClassList("pcg-tabs");
            m_tabRocks = Tab("ROCKS", () => ShowSection("Rocks"));
            m_tabCliffs = Tab("CLIFFS", () => ShowSection("Cliffs"));
            tabs.Add(m_tabRocks);
            tabs.Add(m_tabCliffs);
            root.Add(tabs);

            // Body: controls | icon window
            var body = new VisualElement();
            body.AddToClassList("pcg-body");
            root.Add(body);
            var left = new VisualElement();
            left.AddToClassList("pcg-col-left");
            body.Add(left);
            m_rocksBody = new VisualElement();
            m_rocksBody.AddToClassList("pcg-section");
            left.Add(m_rocksBody);
            m_cliffsBody = new VisualElement();
            m_cliffsBody.AddToClassList("pcg-section");
            left.Add(m_cliffsBody);

            var right = new VisualElement();
            right.AddToClassList("pcg-col-right");
            body.Add(right);
            VisualElement iconCard = Card(right, "ICON WINDOW  ·  512 × 512");
            var iconBar = Row(iconCard);
            iconBar.Add(Btn("Reset View", () => m_icon.ResetView(), "Back to the default icon angle."));
            iconBar.Add(Btn("Front", () => m_icon.SetView(0f, 12f, m_icon.Zoom)));
            iconBar.Add(Btn("Top", () => m_icon.SetView(m_icon.Yaw, 80f, m_icon.Zoom)));
            m_recapture = Btn("Recapture Icon", RecaptureIcon, "Save the icon of the library item being edited from the current angle.");
            iconBar.Add(m_recapture);
            m_icon = new PcgIconView();
            PcgCutCorners.Apply(m_icon);
            iconCard.Add(m_icon);
            m_editing = new Label();
            m_editing.AddToClassList("pcg-label-dim");
            iconCard.Add(m_editing);

            // Status
            m_status = new Label();
            m_status.AddToClassList("pcg-status");
            root.Add(m_status);

            // Library
            VisualElement libCard = Card(root, "LIBRARY");
            libCard.AddToClassList("pcg-card--library");
            m_library = new PcgStudioLibraryView
            {
                OpenInPanel = OpenEntry,
                RerenderRequested = RerenderEntry,
                Status = SetStatus,
            };
            libCard.Add(m_library);

            BuildRocks();
            ShowSection("Rocks");
            RefreshEditing();

            RegisterCallback<AttachToPanelEvent>(_ => { RefreshChoices(); UpdatePreview(); });
            RegisterCallback<DetachFromPanelEvent>(_ => DisposeCliffBuilders());

            PcgStudioExtensions.RaisePanelCreated(this);
        }

        public PcgIconView IconView => m_icon;
        public PcgStudioLibraryView LibraryView => m_library;
        public PcgAssetLibrary Library => PcgStudioLibraryUtil.Load(false);
        public string DefaultPrefabFolder => string.IsNullOrEmpty(m_folder?.value) ? PcgStudioExtensions.PrefabFolder : m_folder.value;
        public string Section => m_section;

        /// <summary>Frees the preview's GPU memory while the host hides the panel.</summary>
        public void SetSuspended(bool suspended) => m_icon.Suspended = suspended;

        // ------------------------------------------------------------------------------------------------
        // Layout helpers

        private Button Tab(string text, Action click)
        {
            var b = new Button(click) { text = text };
            b.AddToClassList("pcg-tab");
            PcgCutCorners.Apply(b);
            return b;
        }

        private static VisualElement Card(VisualElement parent, string title)
        {
            var card = new VisualElement();
            card.AddToClassList("pcg-card");
            PcgCutCorners.Apply(card);
            if (!string.IsNullOrEmpty(title))
            {
                var t = new Label(title);
                t.AddToClassList("pcg-header");
                t.AddToClassList("pcg-card__title");
                card.Add(t);
            }
            parent.Add(card);
            return card;
        }

        private static VisualElement Row(VisualElement parent)
        {
            var r = new VisualElement();
            r.AddToClassList("pcg-row");
            parent.Add(r);
            return r;
        }

        private static Button Btn(string text, Action click, string tip = null, bool primary = false)
        {
            var b = new Button(click) { text = text, tooltip = tip };
            b.AddToClassList("pcg-btn");
            if (primary) b.AddToClassList("pcg-btn-primary");
            PcgCutCorners.Apply(b);
            return b;
        }

        private void ShowSection(string s)
        {
            m_section = s;
            bool rocks = s != "Cliffs";
            m_rocksBody.style.display = rocks ? DisplayStyle.Flex : DisplayStyle.None;
            m_cliffsBody.style.display = rocks ? DisplayStyle.None : DisplayStyle.Flex;
            m_tabRocks.EnableInClassList("pcg-tab--selected", rocks);
            m_tabCliffs.EnableInClassList("pcg-tab--selected", !rocks);
            if (!rocks && !m_cliffsBuilt) BuildCliffs();
            if (rocks) UpdatePreview();
        }

        // ------------------------------------------------------------------------------------------------
        // Rocks section

        private void BuildRocks()
        {
            // Sources
            VisualElement src = Card(m_rocksBody, "SOURCES");
            m_drop = new VisualElement();
            m_drop.AddToClassList("pcg-dropzone");
            PcgCutCorners.Apply(m_drop);
            var dropLabel = new Label("Drop 1+ prefabs, meshes or scene objects here") { pickingMode = PickingMode.Ignore };
            dropLabel.AddToClassList("pcg-dropzone__label");
            m_drop.Add(dropLabel);
            var dropHint = new Label("From the Project window or the Scene / Hierarchy. Pick the base (anchor); a dropped scene rock becomes the target.") { pickingMode = PickingMode.Ignore };
            dropHint.AddToClassList("pcg-label-dim");
            m_drop.Add(dropHint);
            m_drop.RegisterCallback<DragEnterEvent>(_ => m_drop.AddToClassList("pcg-dropzone--hover"));
            m_drop.RegisterCallback<DragLeaveEvent>(_ => m_drop.RemoveFromClassList("pcg-dropzone--hover"));
            m_drop.RegisterCallback<DragExitedEvent>(_ => m_drop.RemoveFromClassList("pcg-dropzone--hover"));
            m_drop.RegisterCallback<DragUpdatedEvent>(_ =>
            {
                DragAndDrop.visualMode = DragAndDrop.objectReferences.Any(Accepts) ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
            });
            m_drop.RegisterCallback<DragPerformEvent>(_ =>
            {
                m_drop.RemoveFromClassList("pcg-dropzone--hover");
                DragAndDrop.AcceptDrag();
                AddSources(DragAndDrop.objectReferences);
            });
            src.Add(m_drop);
            m_sourceList = new VisualElement();
            m_sourceList.AddToClassList("pcg-sourcelist");
            src.Add(m_sourceList);
            var srcBar = Row(src);
            srcBar.Add(Btn("Add Selection", () => AddSources(Selection.objects), "Add the selected Project / Scene objects."));
            srcBar.Add(Btn("Clear", ClearSources));
            m_hideSources = new Toggle("Hide scene sources on Create") { value = HideSceneSources, tooltip = "Like DM PCG Creator (from Selection): the original scene objects are disabled (Undo restores them)." };
            m_hideSources.AddToClassList("pcg-toggle");
            m_hideSources.RegisterValueChangedCallback(e => HideSceneSources = e.newValue);
            src.Add(m_hideSources);

            // Profile & style
            VisualElement prof = Card(m_rocksBody, "PROFILE & STYLE");
            m_preset = Picker(prof, "Profile / Preset", () => SetPreset(PickedAt(m_presets, m_preset, true)));
            m_style = Picker(prof, "Style", () => { Settings.style = PickedAt(m_styles, m_style, true); OnSpecChanged(); });
            m_kit = Picker(prof, "Kit", () => { Settings.kit = PickedAt(m_kits, m_kit, true); OnSpecChanged(); });
            m_material = Picker(prof, "Material", () => { Settings.material = PickedAt(m_materials, m_material, true); OnSpecChanged(); });
            m_blend = Picker(prof, "Blend / Top Layer", () => { Settings.blend = PickedAt(m_blends, m_blend, true); OnSpecChanged(); });
            var seedRow = Row(prof);
            m_seed = new IntegerField("Seed") { value = Settings.seed };
            m_seed.AddToClassList("pcg-field");
            m_seed.AddToClassList("pcg-grow");
            m_seed.RegisterValueChangedCallback(e => { Settings.seed = e.newValue; OnSpecChanged(); });
            seedRow.Add(m_seed);
            seedRow.Add(Btn("Reseed", Reseed, "New random seed."));
            var refresh = Row(prof);
            refresh.Add(Btn("Refresh Lists", RefreshChoices, "Re-scan presets, styles, kits and materials."));

            VisualElement tog = Card(m_rocksBody, "OPTIONS");
            m_matchTerrain = ToggleF(tog, "Match Terrain", "Blend / fade / top deposits follow the terrain layer under the rock.", v => Settings.matchTerrain = v);
            m_useStyleChances = ToggleF(tog, "Use Style Passage / Nook Chances", null, v => { Settings.features.useStyleChances = v; SyncFeatureUI(); });
            m_passage = SliderF(tog, "Passage Chance", v => Settings.features.passageChance = v);
            m_nook = SliderF(tog, "Nook Chance", v => Settings.features.nookChance = v);
            m_guaranteeNook = ToggleF(tog, "Guarantee Nook", null, v => Settings.features.guaranteeNook = v);
            m_liveEdit = new Toggle("Live-edit the created rock") { value = LiveEditCreated, tooltip = "After Create, setting changes also update that scene rock (Undo-able)." };
            m_liveEdit.AddToClassList("pcg-toggle");
            m_liveEdit.RegisterValueChangedCallback(e => LiveEditCreated = e.newValue);
            tog.Add(m_liveEdit);

            // Output
            VisualElement outCard = Card(m_rocksBody, "OUTPUT");
            m_name = new TextField("Name") { value = "" };
            m_name.AddToClassList("pcg-field");
            m_name.textEdition.placeholder = "auto (Rock_<Style>_<seed>)";
            outCard.Add(m_name);
            var folderRow = Row(outCard);
            m_folder = new TextField("Prefab Folder") { value = EditorPrefs.GetString("GenesisPCG.Studio.PrefabFolder", PcgStudioExtensions.PrefabFolder) };
            m_folder.AddToClassList("pcg-field");
            m_folder.AddToClassList("pcg-grow");
            m_folder.RegisterValueChangedCallback(e => EditorPrefs.SetString("GenesisPCG.Studio.PrefabFolder", e.newValue));
            // Keep the long path from pushing the "…" button onto its own line: the field shrinks and clips instead.
            folderRow.style.flexWrap = Wrap.NoWrap;
            m_folder.style.flexGrow = 1; m_folder.style.flexShrink = 1; m_folder.style.flexBasis = 0; m_folder.style.minWidth = 0;
            m_folder.style.overflow = Overflow.Hidden;
            folderRow.Add(m_folder);
            var pick = Btn("…", PickFolder, "Choose the default prefab folder.");
            pick.style.flexShrink = 0;
            folderRow.Add(pick);
            var actions = Row(outCard);
            actions.AddToClassList("pcg-actions");
            actions.Add(Btn("Create Prefab", () => Create(), "Combine into a new rock in the open scene (or update the target rock).", false));
            actions.Add(Btn("Save as Prefab", () => SaveAsPrefabInteractive(), "Bake and write a prefab asset (path picker).", false));
            actions.Add(Btn("Save to Library", () => SaveToLibrary(), "Save as prefab (if needed) and add it to the library with an icon from the current angle.", true));

            RebuildSourceList();
        }

        private DropdownField Picker(VisualElement parent, string label, Action changed)
        {
            var d = new DropdownField(label, new List<string> { FromPreset }, 0);
            d.AddToClassList("pcg-field");
            d.RegisterValueChangedCallback(_ => changed());
            parent.Add(d);
            return d;
        }

        private Toggle ToggleF(VisualElement parent, string label, string tip, Action<bool> set)
        {
            var t = new Toggle(label) { tooltip = tip };
            t.AddToClassList("pcg-toggle");
            t.RegisterValueChangedCallback(e => { set(e.newValue); OnSpecChanged(); });
            parent.Add(t);
            return t;
        }

        private Slider SliderF(VisualElement parent, string label, Action<float> set)
        {
            var s = new Slider(label, 0f, 1f) { showInputField = true };
            s.AddToClassList("pcg-field");
            s.RegisterValueChangedCallback(e => { set(e.newValue); OnSpecChanged(); });
            parent.Add(s);
            return s;
        }

        private static T PickedAt<T>(List<T> list, DropdownField d, bool firstIsNull) where T : Object
        {
            int i = d.index - (firstIsNull ? 1 : 0);
            return i >= 0 && i < list.Count ? list[i] : null;
        }

        private static List<T> FindAll<T>(string filter) where T : Object =>
            AssetDatabase.FindAssets(filter).Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)))
                         .Where(o => o != null).Distinct().OrderBy(o => o.name).ToList();

        /// <summary>Re-scans presets / styles / kits / materials / blend templates and refreshes the pickers.</summary>
        public void RefreshChoices()
        {
            m_presets = FindAll<DmRockPreset>("t:DmRockPreset");
            m_styles = FindAll<DmRockStyle>("t:DmRockStyle");
            m_kits = FindAll<DmRockKit>("t:DmRockKit");
            m_materials = DmRockLibrary.MaterialChoices();
            foreach (DmRockPreset p in m_presets)
                if (p.materialOverride != null && !m_materials.Contains(p.materialOverride)) m_materials.Add(p.materialOverride);
            m_blends = DmRockBlendMaterials.Templates();
            SyncUI();
            m_library.Refresh();
        }

        private void Fill<T>(DropdownField d, List<T> list, T value, string nullLabel, Func<T, string> label) where T : Object
        {
            if (value != null && !list.Contains(value)) list.Add(value);
            var names = new List<string> { nullLabel };
            names.AddRange(list.Select(label));
            // DropdownField needs unique labels.
            for (int i = 1; i < names.Count; i++)
                if (names.IndexOf(names[i]) != i) names[i] += $"  ({i})";
            d.choices = names;
            int idx = value != null ? list.IndexOf(value) + 1 : 0;
            d.SetValueWithoutNotify(names[Mathf.Clamp(idx, 0, names.Count - 1)]);
        }

        private static string Short(Object o, params string[] prefixes)
        {
            string n = o != null ? o.name : "";
            foreach (string p in prefixes) if (n.StartsWith(p, StringComparison.Ordinal)) return n.Substring(p.Length);
            return n;
        }

        private void SyncUI()
        {
            if (m_preset == null) return;
            Fill(m_preset, m_presets, Settings.preset, NoneLabel, p => Short(p, "DM_RockPreset_"));
            Fill(m_style, m_styles, Settings.style, FromPreset, s => Short(s, "DM_RockStyle_"));
            Fill(m_kit, m_kits, Settings.kit, FromPreset, k => k.name);
            Fill(m_material, m_materials, Settings.material, FromPreset, m => m.name);
            Fill(m_blend, m_blends, Settings.blend, FromPreset, m => Short(m, "M_RockBlend_", "MB_"));
            m_seed.SetValueWithoutNotify(Settings.seed);
            m_matchTerrain.SetValueWithoutNotify(Settings.matchTerrain);
            SyncFeatureUI();
        }

        private void SyncFeatureUI()
        {
            DmRockFeatureSettings f = Settings.features;
            m_useStyleChances.SetValueWithoutNotify(f.useStyleChances);
            m_passage.SetValueWithoutNotify(f.passageChance);
            m_nook.SetValueWithoutNotify(f.nookChance);
            m_guaranteeNook.SetValueWithoutNotify(f.guaranteeNook);
            m_passage.SetEnabled(!f.useStyleChances);
            m_nook.SetEnabled(!f.useStyleChances);
        }

        // ------------------------------------------------------------------------------------------------
        // Spec edits (public: also used by tests / automation)

        public void SetPreset(DmRockPreset p)
        {
            Settings.preset = p;
            Settings.kit = null; Settings.style = null; Settings.material = null; Settings.blend = null;
            Settings.features = p != null && p.features != null ? p.features.Clone() : new DmRockFeatureSettings();
            SyncUI();
            OnSpecChanged();
        }

        public void SetStyle(DmRockStyle s) { Settings.style = s; SyncUI(); OnSpecChanged(); }
        public void SetSeed(int seed) { Settings.seed = seed; SyncUI(); OnSpecChanged(); }

        public void Reseed()
        {
            SetSeed(PcgSeededPasteWatcher.NewSeed());
            SetStatus($"New seed {Settings.seed}.");
        }

        private void OnSpecChanged()
        {
            if (CreatedRock != null && LiveEditCreated)
            {
                Configure(CreatedRock, Settings, null, false, true);
                CreatedRock.RebuildIfChanged();
            }
            UpdatePreview();
        }

        private void UpdatePreview() => m_icon.SetSubject(BuildPreviewRock);

        // ------------------------------------------------------------------------------------------------
        // Sources

        private static bool Accepts(Object o)
        {
            if (o is Mesh) return true;
            if (!(o is GameObject g)) return false;
            if (g.GetComponent<DmRockCombiner>() != null && !EditorUtility.IsPersistent(g)) return true; // scene rock = target
            return g.GetComponentsInChildren<MeshFilter>(true).Any(f => f.sharedMesh != null);
        }

        public void AddSources(IEnumerable<Object> objs)
        {
            int added = 0;
            foreach (Object o in objs ?? Enumerable.Empty<Object>())
            {
                if (o == null || !Accepts(o) || Sources.Any(s => s.obj == o)) continue;
                Sources.Add(new SourceItem { obj = o });
                added++;
                DmRockCombiner target = Sources[Sources.Count - 1].TargetRock;
                if (target != null && Sources.Count == 1) LoadFromRock(target, false);
            }
            if (BaseIndex >= Sources.Count) BaseIndex = 0;
            RebuildSourceList();
            UpdatePreview();
            SetStatus(added > 0 ? $"Added {added} source(s) ({Sources.Count} total)." : "Nothing usable dropped (prefabs / meshes / scene objects with meshes).");
        }

        public void RemoveSource(int i)
        {
            if (i < 0 || i >= Sources.Count) return;
            Sources.RemoveAt(i);
            if (BaseIndex >= Sources.Count) BaseIndex = Mathf.Max(0, Sources.Count - 1);
            else if (i < BaseIndex) BaseIndex--;
            RebuildSourceList();
            UpdatePreview();
        }

        public void SetBase(int i)
        {
            if (i < 0 || i >= Sources.Count) return;
            BaseIndex = i;
            RebuildSourceList();
            UpdatePreview();
        }

        public void ClearSources()
        {
            Sources.Clear();
            BaseIndex = 0;
            RebuildSourceList();
            UpdatePreview();
        }

        private void RebuildSourceList()
        {
            if (m_sourceList == null) return;
            m_sourceList.Clear();
            if (Sources.Count == 0)
            {
                var none = new Label("No sources: a kit-built rock in the chosen style.");
                none.AddToClassList("pcg-label-dim");
                m_sourceList.Add(none);
                return;
            }
            for (int i = 0; i < Sources.Count; i++)
            {
                int idx = i;
                SourceItem s = Sources[i];
                var row = new VisualElement();
                row.AddToClassList("pcg-source");
                row.EnableInClassList("pcg-source--base", i == BaseIndex);
                var radio = new Toggle { value = i == BaseIndex, tooltip = "Base / anchor (the target when it is a scene rock)." };
                radio.AddToClassList("pcg-source__base");
                radio.RegisterValueChangedCallback(e => { if (e.newValue) SetBase(idx); else radio.SetValueWithoutNotify(idx == BaseIndex); });
                row.Add(radio);
                Texture icon = s.obj != null ? AssetPreview.GetMiniThumbnail(s.obj) : null;
                if (icon != null)
                {
                    var img = new Image { image = icon };
                    img.AddToClassList("pcg-source__icon");
                    row.Add(img);
                }
                var name = new Label(s.Name);
                name.AddToClassList("pcg-source__name");
                name.RegisterCallback<ClickEvent>(_ => { if (s.obj != null) EditorGUIUtility.PingObject(s.obj); });
                row.Add(name);
                var kind = new Label(i == BaseIndex ? s.Kind + " · base" : s.Kind);
                kind.AddToClassList("pcg-source__kind");
                row.Add(kind);
                var x = new Button(() => RemoveSource(idx)) { text = "✕", tooltip = "Remove" };
                x.AddToClassList("pcg-source__remove");
                row.Add(x);
                m_sourceList.Add(row);
            }
        }

        public SourceItem BaseItem => BaseIndex >= 0 && BaseIndex < Sources.Count ? Sources[BaseIndex] : null;
        public DmRockCombiner TargetRock => BaseItem?.TargetRock;

        /// <summary>Base models (base first) relative to the base's position. Scene objects keep their layout; assets are ringed around the base.</summary>
        public List<DmRockCombiner.BaseModel> ComputeModels(out Vector3 anchor)
        {
            anchor = Vector3.zero;
            var models = new List<DmRockCombiner.BaseModel>();
            var order = new List<SourceItem>();
            if (BaseItem != null) order.Add(BaseItem);
            order.AddRange(Sources.Where((s, i) => i != BaseIndex));
            order = order.Where(s => s.TargetRock == null).ToList(); // a target rock is edited, not captured
            if (order.Count == 0) return models;

            SourceItem first = order[0];
            if (first.IsSceneObject) anchor = ((GameObject)first.obj).transform.position;
            GameObject frame = EditorUtility.CreateGameObjectWithHideFlags("PcgStudioFrame", HideFlags.HideAndDontSave);
            try
            {
                frame.transform.position = anchor;
                float baseRadius = 0f;
                int ring = 0, ringCount = order.Count(s => !s.IsSceneObject && s.model == null) - (first.IsSceneObject || first.model != null ? 0 : 1);
                for (int i = 0; i < order.Count; i++)
                {
                    SourceItem s = order[i];
                    if (s.model != null) { models.Add(Clone(s.model)); continue; }
                    if (s.IsSceneObject)
                    {
                        models.AddRange(DmRockCombinerEditor.CaptureBaseModels(frame.transform, new[] { (GameObject)s.obj }));
                        continue;
                    }
                    Bounds b = AssetBounds(s.obj);
                    float r = Mathf.Max(0.25f, Mathf.Max(b.extents.x, b.extents.z));
                    var m = new DmRockCombiner.BaseModel();
                    if (s.obj is Mesh mesh) { m.mesh = mesh; m.materials = MaterialsFor(mesh); }
                    else m.source = (GameObject)s.obj;
                    if (i == 0) baseRadius = r;
                    else
                    {
                        // Around the base, deterministic layout: golden-angle spacing, slight overlap so it reads as one formation.
                        float ang = (ring * 360f / Mathf.Max(1, ringCount)) + 18f;
                        float dist = (Mathf.Max(baseRadius, 0.5f) + r) * 0.72f;
                        Vector3 off = Quaternion.Euler(0f, ang, 0f) * Vector3.forward * dist;
                        m.localPosition = new Vector3(off.x - b.center.x, 0f, off.z - b.center.z);
                        m.localRotation = Quaternion.Euler(0f, ring * 137.5f, 0f);
                        ring++;
                    }
                    models.Add(m);
                }
            }
            finally { Object.DestroyImmediate(frame); }
            return models;
        }

        private static DmRockCombiner.BaseModel Clone(DmRockCombiner.BaseModel m) => new DmRockCombiner.BaseModel
        {
            source = m.source, mesh = m.mesh, materials = m.materials != null ? (Material[])m.materials.Clone() : null,
            localPosition = m.localPosition, localRotation = m.localRotation, localScale = m.localScale,
        };

        private static Bounds AssetBounds(Object o)
        {
            if (o is Mesh m) return m.bounds;
            var g = o as GameObject;
            bool any = false;
            var b = new Bounds();
            if (g == null) return b;
            Matrix4x4 inv = g.transform.worldToLocalMatrix;
            foreach (MeshFilter mf in g.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                Matrix4x4 mtx = inv * mf.transform.localToWorldMatrix;
                Bounds mb = mf.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 p = mtx.MultiplyPoint3x4(c);
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; } else b.Encapsulate(p);
                }
            }
            return b;
        }

        private static Material[] MaterialsFor(Mesh mesh)
        {
            string path = AssetDatabase.GetAssetPath(mesh);
            if (AssetDatabase.LoadMainAssetAtPath(path) is GameObject model)
                foreach (MeshFilter mf in model.GetComponentsInChildren<MeshFilter>(true))
                    if (mf.sharedMesh == mesh && mf.TryGetComponent(out MeshRenderer mr)) return mr.sharedMaterials;
            return null;
        }

        // ------------------------------------------------------------------------------------------------
        // Combiner setup (reuses DmRockCombiner; one rebuild on activation / OnValidate)

        public static void Configure(DmRockCombiner c, Spec s, List<DmRockCombiner.BaseModel> models, bool forPreview, bool undo)
        {
            if (c == null || s == null) return;
            if (undo) Undo.RecordObject(c, "Rock Studio Edit");
            DmRockFeatureSettings f = s.features ?? new DmRockFeatureSettings();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(f), c.Features);
            var so = new SerializedObject(c);
            so.FindProperty("preset").objectReferenceValue = s.preset;
            so.FindProperty("kit").objectReferenceValue = s.kit;
            so.FindProperty("style").objectReferenceValue = s.style;
            so.FindProperty("materialOverride").objectReferenceValue = s.material;
            so.FindProperty("blendMaterial").objectReferenceValue = s.blend;
            so.FindProperty("m_matchTerrain").boolValue = s.matchTerrain;
            so.FindProperty("seed").intValue = s.seed;
            // Same defaults as Rock Combiner (from Selection): the default recipe / kit when neither the rock nor the preset has one.
            SerializedProperty rp = so.FindProperty("recipe");
            DmRockCombineRecipe recipe = rp.objectReferenceValue as DmRockCombineRecipe;
            if (recipe == null && (s.preset == null || s.preset.recipe == null)) rp.objectReferenceValue = recipe = DmRockCombinerEditor.EnsureRecipe();
            if (recipe == null && s.preset != null) recipe = s.preset.recipe;
            if (s.kit == null && (s.preset == null || s.preset.kit == null) && (recipe == null || recipe.kit == null))
                so.FindProperty("kit").objectReferenceValue = DmRockCombinerEditor.EnsureKit(false);
            if (models != null)
            {
                SerializedProperty arr = so.FindProperty("baseModels");
                arr.arraySize = models.Count;
                for (int i = 0; i < models.Count; i++)
                {
                    SerializedProperty el = arr.GetArrayElementAtIndex(i);
                    DmRockCombiner.BaseModel m = models[i];
                    el.FindPropertyRelative("source").objectReferenceValue = m.source;
                    el.FindPropertyRelative("mesh").objectReferenceValue = m.mesh;
                    SerializedProperty mats = el.FindPropertyRelative("materials");
                    Material[] ma = m.materials ?? new Material[0];
                    mats.arraySize = ma.Length;
                    for (int k = 0; k < ma.Length; k++) mats.GetArrayElementAtIndex(k).objectReferenceValue = ma[k];
                    el.FindPropertyRelative("localPosition").vector3Value = m.localPosition;
                    el.FindPropertyRelative("localRotation").quaternionValue = m.localRotation;
                    el.FindPropertyRelative("localScale").vector3Value = m.localScale;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            if (forPreview) PcgStudioLibraryUtil.MakePreviewSafe(c, true);
            if (!forPreview) EditorUtility.SetDirty(c);
        }

        private GameObject BuildPreviewRock()
        {
            DmRockCombiner target = TargetRock;
            GameObject go = EditorUtility.CreateGameObjectWithHideFlags("PcgStudioPreviewRock", HideFlags.HideAndDontSave);
            go.SetActive(false);
            var c = go.AddComponent<DmRockCombiner>();
            c.flatGroundForPreview = true;
            List<DmRockCombiner.BaseModel> models = target != null ? target.BaseModels.Select(Clone).ToList() : ComputeModels(out _);
            Configure(c, Settings, models, true, false);
            return go;
        }

        public string AutoName()
        {
            if (m_name != null && !string.IsNullOrWhiteSpace(m_name.value)) return m_name.value.Trim();
            DmRockStyle st = Settings.style != null ? Settings.style : Settings.preset != null ? Settings.preset.style : null;
            string s = st != null ? st.name.Replace("DM_RockStyle_", "") : Settings.preset != null ? Settings.preset.name.Replace("DM_RockPreset_", "") : "Rock";
            return $"Rock_{s.Replace(" ", "")}_{Settings.seed & 0xFFFF:X4}";
        }

        // ------------------------------------------------------------------------------------------------
        // Actions

        /// <summary>
        /// Create Prefab: combines the sources + settings into a new rock in the active scene (at the base scene object, else
        /// the Scene View pivot, snapped), or updates the target rock. Undo-able; a bake is requested like the palette does.
        /// </summary>
        public DmRockCombiner Create()
        {
            DmRockCombiner target = TargetRock;
            if (target != null)
            {
                Configure(target, Settings, null, false, true);
                target.RebuildIfChanged();
                DmRockBakeScheduler.Request(target, 0.3f, false);
                CreatedRock = target;
                Selection.activeGameObject = target.gameObject;
                SetStatus($"Updated target {target.name} (seed {target.Seed}).");
                return target;
            }
            DmRockCombiner c = BuildSceneRock(false);
            if (c == null) return null;
            CreatedRock = c;
            Selection.activeGameObject = c.gameObject;
            SetStatus($"Created {c.name} in {c.gameObject.scene.name} (seed {c.Seed}). Not saved: save the scene yourself.");
            return c;
        }

        /// <summary>New combiner in the active scene. <paramref name="temporary"/>: flat ground, far below the world, no Undo (for prefab bakes).</summary>
        private DmRockCombiner BuildSceneRock(bool temporary)
        {
            List<DmRockCombiner.BaseModel> models = ComputeModels(out Vector3 anchor);
            var sceneSources = Sources.Where(s => s.IsSceneObject && s.TargetRock == null).Select(s => (GameObject)s.obj).ToList();
            Scene scene = SceneManager.GetActiveScene();
            var go = new GameObject(temporary ? "PcgStudio_TempBake_" + AutoName() : AutoName());
            go.SetActive(false);
            if (go.scene != scene) SceneManager.MoveGameObjectToScene(go, scene);
            bool sceneBase = BaseItem != null && BaseItem.IsSceneObject && BaseItem.TargetRock == null;
            if (temporary) go.transform.position = new Vector3(0f, -5000f, 0f);
            else if (sceneBase) go.transform.position = anchor;
            else go.transform.position = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
            var c = go.AddComponent<DmRockCombiner>();
            if (temporary) c.flatGroundForPreview = true;
            c.NewPlacementStamp();
            Configure(c, Settings, models, false, false);
            if (!temporary && !sceneBase) PcgSurfaceSnap.Snap(go.transform, c.SnapSettings);
            go.SetActive(true); // builds once at the final pose
            if (sceneBase && !temporary)
            {
                // The combined mesh is re-pivoted to the base's bottom center: move the object so nothing shifts visually.
                go.transform.position += go.transform.TransformVector(c.LastPivotOffset);
            }
            go.transform.hasChanged = false;
            if (temporary) return c;
            Undo.RegisterCreatedObjectUndo(go, "Rock Studio Create");
            if (HideSceneSources)
                foreach (GameObject g in sceneSources)
                {
                    Undo.RecordObject(g, "Rock Studio Create");
                    g.SetActive(false);
                }
            DmRockBakeScheduler.Request(c, 0.3f, false);
            EditorSceneManager.MarkSceneDirty(scene);
            return c;
        }

        private void PickFolder()
        {
            string abs = EditorUtility.OpenFolderPanel("Default Prefab Folder", DefaultPrefabFolder, "");
            if (string.IsNullOrEmpty(abs)) return;
            string proj = System.IO.Path.GetFullPath(".").Replace('\\', '/');
            abs = abs.Replace('\\', '/');
            if (!abs.StartsWith(proj + "/Assets", StringComparison.OrdinalIgnoreCase)) { SetStatus("Pick a folder inside Assets/."); return; }
            m_folder.value = abs.Substring(proj.Length + 1);
        }

        /// <summary>Save as Prefab with a save dialog (defaults to the prefab folder).</summary>
        public GameObject SaveAsPrefabInteractive()
        {
            PcgStudioExtensions.EnsureFolder(DefaultPrefabFolder);
            string path = EditorUtility.SaveFilePanelInProject("Save Rock Prefab", AutoName(), "prefab", "Bake the rock and save it as a prefab.", DefaultPrefabFolder);
            if (string.IsNullOrEmpty(path)) return null;
            return SavePrefabTo(path);
        }

        /// <summary>
        /// Bakes a clean temporary rock from the current settings (flat ground, terrain-independent) and writes the prefab
        /// to <paramref name="path"/>. The temporary rock and its scene-folder bake are removed afterwards.
        /// </summary>
        public GameObject SavePrefabTo(string path)
        {
            DmRockCombiner target = TargetRock;
            DmRockCombiner temp = null;
            string tempBaked = null;
            try
            {
                DmRockCombiner c = target;
                if (c == null)
                {
                    temp = BuildSceneRock(true);
                    c = temp;
                }
                GameObject prefab = PcgStudioLibraryUtil.SaveRockPrefab(c, path, out string error);
                if (temp != null && temp.IsBaked && temp.BakeData.mesh != null) tempBaked = AssetDatabase.GetAssetPath(temp.BakeData.mesh);
                if (prefab == null) { SetStatus("Save as Prefab failed: " + error); return null; }
                if (error != null) tempBaked = null; // the prefab still uses the scene bake: keep it
                LastSavedPrefab = prefab;
                m_lastSavedHash = SpecHash();
                EditorGUIUtility.PingObject(prefab);
                SetStatus($"Saved prefab {path}.");
                return prefab;
            }
            finally
            {
                if (temp != null)
                {
                    PcgSeededPasteWatcher.Suppress(temp.gameObject);
                    Object.DestroyImmediate(temp.gameObject);
                    if (!string.IsNullOrEmpty(tempBaked)) DmRockBaker.DeleteIfUnreferenced(tempBaked, null);
                }
            }
        }

        private string m_lastSavedHash;

        private string SpecHash()
        {
            string s = JsonUtility.ToJson(Settings.features) + "|" + Settings.seed + "|" + Settings.matchTerrain + "|" + BaseIndex;
            foreach (Object o in new Object[] { Settings.preset, Settings.kit, Settings.style, Settings.material, Settings.blend })
                s += "|" + (o != null ? o.GetEntityId().ToString() : "-");
            foreach (SourceItem it in Sources) s += "|" + (it.obj != null ? it.obj.GetEntityId().ToString() : "m");
            return s;
        }

        /// <summary>
        /// Save to Library: reuses the last saved prefab when the settings did not change, else saves a new one (unique name in
        /// the prefab folder), then adds / updates the library entry with a 512 icon from the icon window's current angle.
        /// </summary>
        public PcgAssetLibrary.Entry SaveToLibrary()
        {
            GameObject prefab = LastSavedPrefab != null && m_lastSavedHash == SpecHash() ? LastSavedPrefab : null;
            if (prefab == null)
            {
                PcgStudioExtensions.EnsureFolder(DefaultPrefabFolder);
                prefab = SavePrefabTo(AssetDatabase.GenerateUniqueAssetPath($"{DefaultPrefabFolder}/{AutoName()}.prefab"));
                if (prefab == null) return null;
            }
            DmRockStyle st = Settings.style != null ? Settings.style : Settings.preset != null ? Settings.preset.style : null;
            return SaveToLibrary(prefab, m_section == "Cliffs" ? "Cliffs" : "Rocks", Settings.preset, st, Settings.seed);
        }

        public PcgAssetLibrary.Entry SaveToLibrary(GameObject prefab, string category, DmRockPreset preset, DmRockStyle style, int seed)
        {
            PcgAssetLibrary lib = PcgStudioLibraryUtil.Load(true);
            PcgAssetLibrary.Entry e = PcgStudioLibraryUtil.AddOrUpdate(lib, prefab, category, preset, style, seed, m_icon.Yaw, m_icon.Pitch, m_icon.Zoom);
            if (e == null) { SetStatus("Save to Library failed."); return null; }
            CaptureIcon(lib, e);
            EditingEntry = e;
            RefreshEditing();
            m_library.Refresh();
            m_library.Select(e.id);
            SetStatus($"Saved {e.Name} to the library (icon yaw {e.iconYaw:0}°, pitch {e.iconPitch:0}°).");
            return e;
        }

        private void CaptureIcon(PcgAssetLibrary lib, PcgAssetLibrary.Entry e)
        {
            e.iconYaw = m_icon.Yaw; e.iconPitch = m_icon.Pitch; e.iconZoom = m_icon.Zoom;
            PcgStudioLibraryUtil.RerenderIcon(lib, e, 8, ok => { m_library.Refresh(); if (!ok) SetStatus("Icon saved while shaders were still compiling: right-click > Re-render Icon if it looks wrong."); });
        }

        /// <summary>Saves the edited library item's icon from the current orbit angle.</summary>
        public void RecaptureIcon()
        {
            if (EditingEntry == null) { SetStatus("Open a library item first (double-click or right-click > Open in Editor Panel), or Save to Library."); return; }
            PcgAssetLibrary lib = PcgStudioLibraryUtil.Load(true);
            Undo.RecordObject(lib, "Recapture PCG Icon");
            CaptureIcon(lib, EditingEntry);
            PcgStudioLibraryUtil.Save(lib);
            SetStatus($"Icon of {EditingEntry.Name} recaptured (yaw {EditingEntry.iconYaw:0}°, pitch {EditingEntry.iconPitch:0}°, zoom {EditingEntry.iconZoom:0.00}).");
        }

        private void RerenderEntry(PcgAssetLibrary.Entry e)
        {
            PcgStudioLibraryUtil.RerenderIcon(PcgStudioLibraryUtil.Load(false), e, 8, ok => { m_library.Refresh(); SetStatus(ok ? $"Icon of {e.Name} re-rendered." : $"Icon of {e.Name} re-rendered (shaders still compiling)."); });
        }

        /// <summary>Loads a library item into the panel (settings + sources from its rock, icon angle) for editing / recapture.</summary>
        public void OpenEntry(PcgAssetLibrary.Entry e)
        {
            if (e == null || e.prefab == null) return;
            ShowSection(string.Equals(e.category, "Cliffs", StringComparison.OrdinalIgnoreCase) ? "Cliffs" : "Rocks");
            var c = e.prefab.GetComponent<DmRockCombiner>();
            if (c != null) LoadFromRock(c, true);
            EditingEntry = e;
            LastSavedPrefab = e.prefab;
            m_lastSavedHash = SpecHash();
            CreatedRock = null;
            m_icon.SetView(e.iconYaw, e.iconPitch, e.iconZoom);
            RefreshEditing();
            m_library.Select(e.id);
            UpdatePreview();
            SetStatus($"Editing {e.Name}: orbit the icon window and Recapture Icon, or change settings and Save to Library.");
        }

        /// <summary>Copies a rock's own slots / seed / features (and optionally its base models as sources) into the panel.</summary>
        public void LoadFromRock(DmRockCombiner c, bool takeModels)
        {
            if (c == null) return;
            Settings.preset = c.Preset;
            Settings.kit = c.OwnKit;
            Settings.style = c.OwnStyle;
            Settings.material = c.OwnMaterialOverride;
            Material blend = c.OwnBlendMaterial;
            Settings.blend = blend != null && DmRockBlendMaterials.IsPerRock(blend) ? DmRockBlendMaterials.SourceTemplate(blend) : blend;
            Settings.matchTerrain = c.MatchTerrain;
            Settings.seed = c.Seed;
            Settings.features = c.Features.Clone();
            if (takeModels)
            {
                Sources.Clear();
                BaseIndex = 0;
                foreach (DmRockCombiner.BaseModel m in c.BaseModels)
                    if (m != null && (m.source != null || m.mesh != null))
                        Sources.Add(new SourceItem { obj = (Object)m.source ?? m.mesh, model = Clone(m) });
                RebuildSourceList();
            }
            SyncUI();
        }

        private void RefreshEditing()
        {
            if (m_editing == null) return;
            m_recapture.SetEnabled(EditingEntry != null);
            m_editing.text = EditingEntry != null ? $"Library item: {EditingEntry.Name}  (saved angle yaw {EditingEntry.iconYaw:0}°, pitch {EditingEntry.iconPitch:0}°)" : "Live preview of the current settings (flat ground).";
        }

        public void SetStatus(string message)
        {
            LastStatus = message ?? "";
            if (m_status != null) m_status.text = LastStatus;
        }

        public void SetPreviewSubject(Func<GameObject> build) => m_icon.SetSubject(build ?? BuildPreviewRock);

        // ------------------------------------------------------------------------------------------------
        // Cliffs (extension point)

        private readonly List<IPcgCliffBuilder> m_activeCliffBuilders = new List<IPcgCliffBuilder>();

        private void BuildCliffs()
        {
            m_cliffsBuilt = true;
            m_cliffsBody.Clear();
            IReadOnlyList<IPcgCliffBuilder> builders = PcgStudioExtensions.CliffBuilders;
            if (builders.Count == 0)
            {
                VisualElement card = Card(m_cliffsBody, "CLIFF BUILDER  ·  PLACEHOLDER");
                card.AddToClassList("pcg-card--placeholder");
                var l1 = new Label("The cliff builder is not built yet.");
                l1.AddToClassList("pcg-placeholder__title");
                card.Add(l1);
                var l2 = new Label("Extension point: implement GenesisPCG.RockCreation.Editor.IPcgCliffBuilder and register it with " +
                                   "PcgStudioExtensions.RegisterCliffBuilder(...) from an [InitializeOnLoad] class. It gets this section, " +
                                   "the shared 512 icon window (SetPreviewSubject) and the same library (SaveToLibrary, category \"Cliffs\").");
                l2.AddToClassList("pcg-label-dim");
                l2.AddToClassList("pcg-wrap");
                card.Add(l2);
                return;
            }
            foreach (IPcgCliffBuilder b in builders)
            {
                VisualElement card = Card(m_cliffsBody, b.DisplayName.ToUpperInvariant());
                try { VisualElement ui = b.CreateGUI(this); if (ui != null) card.Add(ui); m_activeCliffBuilders.Add(b); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
        }

        private void DisposeCliffBuilders()
        {
            foreach (IPcgCliffBuilder b in m_activeCliffBuilders)
            {
                try { b.Dispose(); } catch (Exception ex) { Debug.LogException(ex); }
            }
            m_activeCliffBuilders.Clear();
            m_cliffsBuilt = false;
        }
    }
}
#endif
