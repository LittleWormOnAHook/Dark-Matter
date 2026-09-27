using System.Collections.Generic;
using System.Linq;
using Project.Building;
using Project.Data;
using Project.EditorTools.GenesisStudio;
using Project.UI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Building
{
    /// <summary>
    /// Building Library: one subtab per style (Stone, Iron, Silicate, ...). Each style is a kit with its own cost item,
    /// finishes, kit shape settings and parts (prefab, icon, shape, category, cost). Shared by Building Studio and
    /// Genesis Studio &gt; Building &gt; Library. Extra Equipment and Placements subtabs list those parts from every style.
    /// Parts are shown grouped by type and sorted by name; the serialized order is never changed.
    /// </summary>
    public sealed class DMBuildingLibraryPanel
    {
        string selectedStyleId;
        string newStyleName = string.Empty;
        string partFilter = string.Empty;
        readonly HashSet<string> openParts = new HashSet<string>();
        bool showStyle = true;
        bool showFinishes = true;
        bool showKit;
        bool showParts = true;
        bool showCollection = true;
        readonly HashSet<string> closedGroups = new HashSet<string>();

        // 0927-library-tabs: sub-tabs that list parts from every style instead of one style.
        const string EquipmentTabId = "__equipment";
        const string PlacementsTabId = "__placements";

        static bool IsCollectionTab(string id) => id == EquipmentTabId || id == PlacementsTabId;

        public void Draw()
        {
            IReadOnlyList<DMBuildingStyleLibrary> styles = DMBuildingStyles.All;
            if (styles.Count == 0)
            {
                DMStudioStyles.DrawSection("Library", DMStudioStyles.ContentPanel, () =>
                {
                    EditorGUILayout.HelpBox(
                        "No building styles yet. This creates Stone, Iron and Silicate in " + DMBuildingStyleLibrary.AssetFolder
                        + " and moves the old DM_BuildingLibrary / DM_BuildingMaterialLibrary data into Stone.",
                        MessageType.Info);
                    if (GUILayout.Button("Create Styles (Stone, Iron, Silicate)", GUILayout.Height(26f)))
                        EditorApplication.delayCall += () => DMBuildingStyleLibraryBuilder.EnsureStyles();
                });
                return;
            }

            DMBuildingStyleLibrary style = DrawStyleTabs(styles);
            if (IsCollectionTab(selectedStyleId))
            {
                EditorGUILayout.Space(4f);
                DrawCollectionTab(styles, selectedStyleId == EquipmentTabId);
                return;
            }

            if (style == null)
                return;

            EditorGUILayout.Space(4f);
            Undo.RecordObject(style, "Edit Building Style");
            EditorGUI.BeginChangeCheck();

            DrawFoldoutSection(ref showStyle, style.displayName + " kit", style.accent, () => DrawStyleSettings(style));
            DrawFoldoutSection(ref showFinishes, "Finishes (M key)", style.accent, () => DrawFinishes(style));
            DrawFoldoutSection(ref showKit, "Kit shapes (ProBuilder)", style.accent, () => DrawKitSettings(style));
            DrawFoldoutSection(ref showParts, "Parts (" + (style.parts != null ? style.parts.Count : 0) + ")", style.accent, () => DrawParts(style));

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(style);
                DMBuildingStyles.Invalidate();
            }
        }

        DMBuildingStyleLibrary DrawStyleTabs(IReadOnlyList<DMBuildingStyleLibrary> styles)
        {
            DMBuildingStyleLibrary selected = null;
            for (int i = 0; i < styles.Count; i++)
            {
                if (styles[i] != null && styles[i].styleId == selectedStyleId)
                    selected = styles[i];
            }

            if (selected == null && !IsCollectionTab(selectedStyleId))
            {
                selected = styles[0];
                selectedStyleId = selected.styleId;
            }

            EditorGUILayout.BeginHorizontal(DMStudioStyles.HeaderPanel);
            for (int i = 0; i < styles.Count; i++)
            {
                DMBuildingStyleLibrary s = styles[i];
                if (s == null)
                    continue;
                bool on = DMStudioStyles.DrawSubTab(s.displayName, s == selected);
                if (on && s != selected)
                {
                    selectedStyleId = s.styleId;
                    GUI.FocusControl(null);
                }
            }

            if (DMStudioStyles.DrawSubTab("Equipment", selectedStyleId == EquipmentTabId) && selectedStyleId != EquipmentTabId)
            {
                selectedStyleId = EquipmentTabId;
                GUI.FocusControl(null);
            }

            if (DMStudioStyles.DrawSubTab("Placements", selectedStyleId == PlacementsTabId) && selectedStyleId != PlacementsTabId)
            {
                selectedStyleId = PlacementsTabId;
                GUI.FocusControl(null);
            }

            GUILayout.FlexibleSpace();
            newStyleName = EditorGUILayout.TextField(newStyleName, GUILayout.Width(120f));
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newStyleName)))
            {
                if (GUILayout.Button("+ New Style", GUILayout.Width(92f)))
                {
                    string name = newStyleName;
                    newStyleName = string.Empty;
                    EditorApplication.delayCall += () =>
                    {
                        DMBuildingStyleLibrary created = DMBuildingStyleLibraryBuilder.CreateStyle(name);
                        if (created != null)
                            selectedStyleId = created.styleId;
                        else
                            Debug.LogWarning("[DM Building Library] Could not create style '" + name + "' (empty or id already used).");
                    };
                }
            }

            if (GUILayout.Button("Save", GUILayout.Width(52f)))
                AssetDatabase.SaveAssets();
            EditorGUILayout.EndHorizontal();
            return selected;
        }

        static void DrawFoldoutSection(ref bool open, string title, Color accent, System.Action draw)
        {
            EditorGUILayout.BeginVertical(DMStudioStyles.ContentPanel);
            Rect r = GUILayoutUtility.GetRect(0f, 22f, GUILayout.ExpandWidth(true));
            open = EditorGUI.Foldout(r, open, title, true, FoldoutStyle);
            DMStudioStyles.DrawAccentLine(r, accent, 1.5f);
            if (open)
            {
                EditorGUILayout.Space(2f);
                draw();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        static GUIStyle foldoutStyle;

        static GUIStyle FoldoutStyle
        {
            get
            {
                if (foldoutStyle == null)
                {
                    foldoutStyle = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };
                    foldoutStyle.normal.textColor = DMStudioStyles.SectionTitle.normal.textColor;
                    foldoutStyle.onNormal.textColor = DMStudioStyles.SectionTitle.normal.textColor;
                }

                return foldoutStyle;
            }
        }

        // ---------------------------------------------------------------- style settings

        static void DrawStyleSettings(DMBuildingStyleLibrary style)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.ObjectField("Asset", style, typeof(DMBuildingStyleLibrary), false);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Style id", style.styleId);
                EditorGUILayout.TextField("Part id prefix", DMBuildingStyleLibraryBuilder.PrefixOf(style));
            }

            style.displayName = EditorGUILayout.TextField("Display name", style.displayName);
            style.order = EditorGUILayout.IntField("Order (hotbar Tab list)", style.order);
            style.accent = EditorGUILayout.ColorField("Accent", style.accent);
            style.resourceTextColor = EditorGUILayout.ColorField("Resource text colour", style.resourceTextColor);
            EditorGUILayout.EndVertical();
            style.icon = (Texture2D)EditorGUILayout.ObjectField(style.icon, typeof(Texture2D), false, GUILayout.Width(64f), GUILayout.Height(64f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Cost", EditorStyles.miniBoldLabel);
            style.costItem = (ItemData)EditorGUILayout.ObjectField("Paid with item", style.costItem, typeof(ItemData), false);
            string names = style.costItemNames != null ? string.Join(", ", style.costItemNames) : string.Empty;
            string edited = EditorGUILayout.TextField(new GUIContent("Also accept names", "Comma list of item names that pay for this style (legacy items)."), names);
            if (edited != names)
                style.costItemNames = edited.Split(',').Select(n => n.Trim()).Where(n => n.Length > 0).ToList();
            style.costMultiplier = Mathf.Max(0f, EditorGUILayout.FloatField("Cost multiplier", style.costMultiplier));
        }

        static void DrawFinishes(DMBuildingStyleLibrary style)
        {
            EditorGUILayout.HelpBox("The first finish is the default for new pieces. M cycles through them on a built piece.", MessageType.None);
            if (style.finishes == null)
                style.finishes = new List<DMBuildingMaterialVariant>();

            int remove = -1;
            int moveUp = -1;
            for (int i = 0; i < style.finishes.Count; i++)
            {
                DMBuildingMaterialVariant finish = style.finishes[i];
                if (finish == null)
                    continue;
                EditorGUILayout.BeginHorizontal();
                finish.finishedMaterial = (Material)EditorGUILayout.ObjectField(finish.finishedMaterial, typeof(Material), false, GUILayout.Width(170f));
                finish.displayName = EditorGUILayout.TextField(finish.displayName);
                finish.id = EditorGUILayout.TextField(finish.id, GUILayout.Width(130f));
                finish.overrideGhostTint = GUILayout.Toggle(finish.overrideGhostTint, new GUIContent("Tint", "Override the ghost tint for this finish."), GUILayout.Width(40f));
                using (new EditorGUI.DisabledScope(!finish.overrideGhostTint))
                    finish.ghostTint = EditorGUILayout.ColorField(GUIContent.none, finish.ghostTint, false, true, false, GUILayout.Width(44f));
                using (new EditorGUI.DisabledScope(i == 0))
                {
                    if (GUILayout.Button("\u25B2", GUILayout.Width(24f)))
                        moveUp = i;
                }

                if (GUILayout.Button("X", GUILayout.Width(24f)))
                    remove = i;
                EditorGUILayout.EndHorizontal();
            }

            if (moveUp > 0)
            {
                DMBuildingMaterialVariant f = style.finishes[moveUp];
                style.finishes.RemoveAt(moveUp);
                style.finishes.Insert(moveUp - 1, f);
                GUI.changed = true;
            }

            if (remove >= 0)
            {
                style.finishes.RemoveAt(remove);
                GUI.changed = true;
            }

            if (GUILayout.Button("+ Finish (new HDRP/Lit material)", GUILayout.Width(230f)))
            {
                DMBuildingStyleLibraryBuilder.AddFinish(style);
                GUI.changed = true;
            }

            EditorGUILayout.Space(4f);
            style.doorMaterial = (Material)EditorGUILayout.ObjectField("Door material", style.doorMaterial, typeof(Material), false);
            style.glassMaterial = (Material)EditorGUILayout.ObjectField("Window glass material", style.glassMaterial, typeof(Material), false);
        }

        static void DrawKitSettings(DMBuildingStyleLibrary style)
        {
            if (style.kit == null)
                style.kit = new DMBuildingKitSettings();
            DMBuildingKitSettings kit = style.kit;
            EditorGUILayout.HelpBox(
                "Footprints are fixed: 4 m module, 4 m story, 0.3 m walls, 0.4 m foundation, 0.2 m slabs, 2 m roof rise. "
                + "These shape openings and details. Rebuild Kit writes meshes and prefabs to "
                + DMBuildingStyleLibraryBuilder.StyleRoot(style) + " and links them to this style's parts.",
                MessageType.None);
            kit.windowWidthMeters = EditorGUILayout.FloatField("Window width (m)", kit.windowWidthMeters);
            kit.windowHeightMeters = EditorGUILayout.FloatField("Window height (m)", kit.windowHeightMeters);
            kit.windowSillMeters = EditorGUILayout.FloatField("Window sill (m)", kit.windowSillMeters);
            kit.glassThicknessMeters = EditorGUILayout.FloatField("Glass thickness (m)", kit.glassThicknessMeters);
            kit.passageWidthMeters = EditorGUILayout.FloatField("Passage width (m)", kit.passageWidthMeters);
            kit.passageHeightMeters = EditorGUILayout.FloatField("Passage height (m)", kit.passageHeightMeters);
            kit.hatchOpeningMeters = EditorGUILayout.FloatField("Hatch opening (m)", kit.hatchOpeningMeters);
            kit.stairSteps = EditorGUILayout.IntSlider("Stair steps", kit.stairSteps, 4, 32);
            kit.railingPosts = EditorGUILayout.IntSlider("Railing posts", kit.railingPosts, 2, 8);

            // Kit phase 2 (0926).
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Phase 2 pieces", EditorStyles.boldLabel);
            kit.wideWindowWidthMeters = EditorGUILayout.FloatField("Wide window width (m)", kit.wideWindowWidthMeters);
            kit.wideWindowHeightMeters = EditorGUILayout.FloatField("Wide window height (m)", kit.wideWindowHeightMeters);
            kit.wideWindowSillMeters = EditorGUILayout.FloatField("Wide window sill (m)", kit.wideWindowSillMeters);
            kit.slitWidthMeters = EditorGUILayout.FloatField("Slit window width (m)", kit.slitWidthMeters);
            kit.slitHeightMeters = EditorGUILayout.FloatField("Slit window height (m)", kit.slitHeightMeters);
            kit.slitSillMeters = EditorGUILayout.FloatField("Slit window sill (m)", kit.slitSillMeters);
            kit.archWidthMeters = EditorGUILayout.FloatField("Archway width (m)", kit.archWidthMeters);
            kit.archSpringMeters = EditorGUILayout.FloatField("Archway spring height (m)", kit.archSpringMeters);
            kit.archSegments = EditorGUILayout.IntSlider("Archway segments", kit.archSegments, 4, 32);
            kit.ventWidthMeters = EditorGUILayout.FloatField("Vent width (m)", kit.ventWidthMeters);
            kit.ventHeightMeters = EditorGUILayout.FloatField("Vent height (m)", kit.ventHeightMeters);
            kit.ventSillMeters = EditorGUILayout.FloatField("Vent sill (m)", kit.ventSillMeters);
            kit.ventSlats = EditorGUILayout.IntSlider("Vent slats", kit.ventSlats, 1, 12);
            kit.foundationStepCount = EditorGUILayout.IntSlider("Foundation steps", kit.foundationStepCount, 2, 12);
            kit.stairwellOpeningMeters = EditorGUILayout.FloatField("Stairwell opening length (m)", kit.stairwellOpeningMeters);
            kit.spiralSteps = EditorGUILayout.IntSlider("Spiral stair steps", kit.spiralSteps, 8, 32);
            kit.ladderRungs = EditorGUILayout.IntSlider("Ladder rungs", kit.ladderRungs, 4, 24);
            kit.rooftopEdgeHeightMeters = EditorGUILayout.FloatField("Rooftop edge height (m)", kit.rooftopEdgeHeightMeters);

            // Kit phase 3 (0926).
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Phase 3 pieces (double doors, 8 m gates, half-cell foundations)", EditorStyles.boldLabel);
            kit.doorLeafGapMeters = EditorGUILayout.FloatField("Door / gate leaf gap (m)", kit.doorLeafGapMeters);
            kit.gateCrossbars = EditorGUILayout.IntSlider("Gate crossbars", kit.gateCrossbars, 0, 8);

            EditorGUILayout.Space(4f);
            if (GUILayout.Button("Rebuild " + style.displayName + " Kit (ProBuilder)", GUILayout.Height(26f)))
            {
                DMBuildingStyleLibrary target = style;
                AssetDatabase.SaveAssets();
                EditorApplication.delayCall += () => DMBuildingKitBuilder.RebuildStyle(target);
            }
        }

        // ---------------------------------------------------------------- parts

        void DrawParts(DMBuildingStyleLibrary style)
        {
            if (style.parts == null)
                style.parts = new List<DMBuildingPartEntry>();

            EditorGUILayout.BeginHorizontal();
            partFilter = EditorGUILayout.TextField("Filter", partFilter);
            if (GUILayout.Button("Expand", GUILayout.Width(60f)))
            {
                closedGroups.Clear();
                foreach (DMBuildingPartEntry p in style.parts)
                    if (p != null) openParts.Add(style.styleId + "/" + p.id);
            }
            if (GUILayout.Button("Collapse", GUILayout.Width(64f)))
                openParts.Clear();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2f);

            // 0927-library-tabs: display only - grouped by type and sorted by name. The serialized part order is untouched.
            var rows = new List<PartRow>();
            for (int i = 0; i < style.parts.Count; i++)
            {
                DMBuildingPartEntry part = style.parts[i];
                if (part != null && PassesFilter(part))
                    rows.Add(new PartRow(style, part));
            }

            ConfirmRemove(DrawGroupedRows(style.styleId, rows, style.accent, false));

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Part"))
                AddPart(style, DMBuildingShape.Custom, null);
            if (GUILayout.Button(new GUIContent("+ Surface Item", "Lights and decorations that stick to walls, floors and ceilings of built pieces.")))
                AddPart(style, DMBuildingShape.SurfaceItem, null);
            if (GUILayout.Button(new GUIContent("+ From Selected Prefabs", "Adds a part for every prefab selected in the Project window.")))
            {
                foreach (GameObject go in Selection.GetFiltered<GameObject>(SelectionMode.Assets))
                {
                    if (PrefabUtility.IsPartOfPrefabAsset(go))
                        AddPart(style, DMBuildingShape.Custom, go);
                }
            }

            if (GUILayout.Button(new GUIContent("Add Missing Kit Shapes", "Adds a row for every standard kit shape this style is missing.")))
            {
                DMBuildingStyleLibraryBuilder.EnsureKitParts(style);
                GUI.changed = true;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Bake Missing Icons"))
            {
                int n = DMBuildingStyleLibraryBuilder.BakeIcons(style, false);
                Debug.Log("[DM Building Library] Queued " + n + " " + style.displayName + " icons to bake.");
            }

            if (GUILayout.Button("Rebake All Icons"))
            {
                int n = DMBuildingStyleLibraryBuilder.BakeIcons(style, true);
                Debug.Log("[DM Building Library] Queued " + n + " " + style.displayName + " icons to rebake.");
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>Returns true when the user pressed remove.</summary>
        bool DrawPart(DMBuildingStyleLibrary style, DMBuildingPartEntry part, bool showStyle = false)
        {
            string key = style.styleId + "/" + part.id;
            bool open = openParts.Contains(key);
            bool remove = false;

            EditorGUILayout.BeginVertical(DMStudioStyles.SidebarPanel);
            EditorGUILayout.BeginHorizontal();
            Rect iconRect = GUILayoutUtility.GetRect(36f, 36f, GUILayout.Width(36f), GUILayout.Height(36f));
            Texture preview = part.icon != null ? part.icon : (part.prefab != null ? AssetPreview.GetAssetPreview(part.prefab) : null);
            if (preview != null)
                GUI.DrawTexture(iconRect, preview, ScaleMode.ScaleToFit);
            else
                EditorGUI.DrawRect(iconRect, DarkMatterGenesisUiPalette.WithAlpha(style.accent, 0.25f));

            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            part.enabled = EditorGUILayout.Toggle(part.enabled, GUILayout.Width(16f));
            bool nextOpen = EditorGUILayout.Foldout(open, part.displayName, true);
            GUILayout.FlexibleSpace();
            DMStudioStyles.DrawBadge(part.shape.ToString(), DarkMatterGenesisUiPalette.WithAlpha(style.accent, 0.35f));
            if (showStyle)
                DMStudioStyles.DrawBadge(style.displayName, DarkMatterGenesisUiPalette.WithAlpha(style.accent, 0.6f));
            DMStudioStyles.DrawBadge(CostLabel(style, part), DarkMatterGenesisUiPalette.SlateGray);
            if (part.prefab == null)
                DMStudioStyles.DrawBadge("fallback mesh", DarkMatterGenesisUiPalette.DeepMagenta);
            if (GUILayout.Button("X", GUILayout.Width(22f)))
                remove = true;
            EditorGUILayout.EndHorizontal();
            part.prefab = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Prefab", "Drop a prefab here to replace the part's mesh."), part.prefab, typeof(GameObject), false);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            if (nextOpen != open)
            {
                if (nextOpen)
                    openParts.Add(key);
                else
                    openParts.Remove(key);
            }

            if (nextOpen)
            {
                EditorGUI.indentLevel++;
                part.displayName = EditorGUILayout.TextField("Display name", part.displayName);
                string id = EditorGUILayout.DelayedTextField("Id", part.id);
                if (id != part.id && !string.IsNullOrWhiteSpace(id) && style.FindPart(id) == null)
                {
                    openParts.Remove(key);
                    part.id = id.Trim();
                    openParts.Add(style.styleId + "/" + part.id);
                }

                DMBuildingShape shape = (DMBuildingShape)EditorGUILayout.EnumPopup("Shape (snap rules)", part.shape);
                if (shape != part.shape)
                {
                    part.shape = shape;
                    part.category = DMBuildingCatalog.DefaultCategory(shape);
                }

                part.category = (DMBuildingCategory)EditorGUILayout.EnumPopup("Hotbar category", part.category);
                part.icon = (Texture2D)EditorGUILayout.ObjectField("Icon", part.icon, typeof(Texture2D), false);
                part.cost = Mathf.Max(0, EditorGUILayout.IntField("Cost (" + CostName(style) + ")", part.cost));
                if (part.category == DMBuildingCategory.Equipment || (part.customCost != null && part.customCost.Count > 0))
                    DrawCustomCost(part);
                part.applyStyleFinish = EditorGUILayout.Toggle(
                    new GUIContent("Apply style finish", "Off keeps the prefab's own materials (lights, decorations)."),
                    part.applyStyleFinish);
                Material pickedOverride = (Material)EditorGUILayout.ObjectField(
                    new GUIContent("Material override", "When set, the built piece uses this material on every mesh (glass panes keep glass), replacing the prefab's materials and the style finish. The icon rebakes with it."),
                    part.materialOverride, typeof(Material), false);
                if (pickedOverride != part.materialOverride)
                {
                    part.materialOverride = pickedOverride;
                    part.icon = null;
                    EditorUtility.SetDirty(style);
                    DMBuildingStyles.Invalidate();
                    DMBuildingStyleLibraryBuilder.BakeIcons(style, false);
                }
                if (part.shape == DMBuildingShape.SurfaceItem)
                    part.surfaceOffsetMeters = EditorGUILayout.FloatField(new GUIContent("Surface offset (m)", "Gap between the item and the face it sticks to."), part.surfaceOffsetMeters);
                part.sizeOverride = EditorGUILayout.Vector3Field(new GUIContent("Size override (m)", "Zero uses the shape's grid size or the prefab mesh bounds."), part.sizeOverride);
                if (part.shape == DMBuildingShape.SurfaceItem || part.shape == DMBuildingShape.Custom)
                    part.modelRotation = EditorGUILayout.Vector3Field(new GUIContent("Model rotation (deg)", "Turns the model inside the piece, for prefabs authored facing the wrong way. Wall items face +Z out of the wall; floor and ceiling items point +Y away from the face."), part.modelRotation);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            return remove;
        }

        void AddPart(DMBuildingStyleLibrary style, DMBuildingShape shape, GameObject prefab)
        {
            string prefix = DMBuildingStyleLibraryBuilder.PrefixOf(style);
            string stem = prefab != null
                ? prefab.name.ToLowerInvariant().Replace(' ', '_')
                : (shape == DMBuildingShape.SurfaceItem ? "surface_item" : "part");
            string id = stem.StartsWith(prefix) ? stem : prefix + stem;
            string unique = id;
            for (int n = 2; style.FindPart(unique) != null; n++)
                unique = id + "_" + n;

            var part = new DMBuildingPartEntry
            {
                id = unique,
                displayName = prefab != null ? prefab.name : (shape == DMBuildingShape.SurfaceItem ? "Surface Item" : "New Part"),
                shape = shape,
                category = DMBuildingCatalog.DefaultCategory(shape),
                prefab = prefab,
                cost = 1,
                enabled = true,
                applyStyleFinish = shape != DMBuildingShape.SurfaceItem,
            };
            style.parts.Add(part);
            openParts.Add(style.styleId + "/" + part.id);
            GUI.changed = true;
        }

        // ---------------------------------------------------------------- 0927-library-tabs: Equipment / Placements

        readonly struct PartRow
        {
            public readonly DMBuildingStyleLibrary style;
            public readonly DMBuildingPartEntry part;

            public PartRow(DMBuildingStyleLibrary style, DMBuildingPartEntry part)
            {
                this.style = style;
                this.part = part;
            }
        }

        /// <summary>Equipment tab: parts whose hotbar category is Equipment (the in-game Equipment Tab entry).</summary>
        static bool IsEquipmentPart(DMBuildingPartEntry part)
        {
            return part != null && part.category == DMBuildingCategory.Equipment;
        }

        /// <summary>Placements tab: parts that stick to the face of built pieces (Surface snap: lights, signs...), minus Equipment.</summary>
        static bool IsPlacementPart(DMBuildingPartEntry part)
        {
            return part != null
                && part.category != DMBuildingCategory.Equipment
                && DMBuildingCatalog.SnapFor(part.shape) == DMBuildingSnap.Surface;
        }

        void DrawCollectionTab(IReadOnlyList<DMBuildingStyleLibrary> styles, bool equipment)
        {
            Color accent = equipment ? new Color(0.95f, 0.62f, 0.2f, 1f) : new Color(0.35f, 0.75f, 0.95f, 1f);
            var rows = new List<PartRow>();
            var owners = new List<DMBuildingStyleLibrary>();
            int total = 0;
            for (int s = 0; s < styles.Count; s++)
            {
                DMBuildingStyleLibrary style = styles[s];
                if (style == null || style.parts == null)
                    continue;
                for (int i = 0; i < style.parts.Count; i++)
                {
                    DMBuildingPartEntry part = style.parts[i];
                    if (!(equipment ? IsEquipmentPart(part) : IsPlacementPart(part)))
                        continue;
                    total++;
                    if (!owners.Contains(style))
                        owners.Add(style);
                    if (PassesFilter(part))
                        rows.Add(new PartRow(style, part));
                }
            }

            string title = equipment ? "Equipment" : "Placements";
            string scope = equipment ? EquipmentTabId : PlacementsTabId;
            DrawFoldoutSection(ref showCollection, title + " (" + total + ")", accent, () =>
            {
                EditorGUILayout.HelpBox(equipment
                        ? "Every part with Hotbar category = Equipment, from all styles. In game these go to the Equipment Tab entry instead of a style hotbar. Edits save to the owning style asset."
                        : "Every surface part (lights, signs, banners, posters...) from all styles, except Equipment. Add new ones with + Surface Item on a style's Parts. Edits save to the owning style asset.",
                    MessageType.None);

                EditorGUILayout.BeginHorizontal();
                partFilter = EditorGUILayout.TextField("Filter", partFilter);
                if (GUILayout.Button("Expand", GUILayout.Width(60f)))
                {
                    closedGroups.Clear();
                    foreach (PartRow r in rows)
                        openParts.Add(r.style.styleId + "/" + r.part.id);
                }

                if (GUILayout.Button("Collapse", GUILayout.Width(64f)))
                    openParts.Clear();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2f);

                for (int i = 0; i < owners.Count; i++)
                    Undo.RecordObject(owners[i], "Edit Building Part");
                ConfirmRemove(DrawGroupedRows(scope, rows, accent, true));

                EditorGUILayout.Space(4f);
                if (GUILayout.Button(new GUIContent("Bake Missing Icons", "Bakes missing icons in every style that owns one of these parts.")))
                {
                    int n = 0;
                    for (int i = 0; i < owners.Count; i++)
                        n += DMBuildingStyleLibraryBuilder.BakeIcons(owners[i], false);
                    Debug.Log("[DM Building Library] Queued " + n + " icons to bake.");
                }
            });
        }

        static readonly string[] GroupNames =
        {
            "Foundations", "Floors & Ceilings", "Walls", "Windows", "Doors & Gates", "Roofs",
            "Stairs & Ramps", "Structure", "Surface Items", "Decor & Custom", "Equipment", "Other"
        };

        /// <summary>Display group from the part's shape (piece kind); Custom and unknown shapes use the hotbar category.</summary>
        static int GroupOf(DMBuildingPartEntry part)
        {
            if (part.category == DMBuildingCategory.Equipment)
                return 10;
            switch (part.shape)
            {
                case DMBuildingShape.Foundation:
                case DMBuildingShape.TriFoundation:
                case DMBuildingShape.FoundationSteps:
                case DMBuildingShape.SupportPillar:
                case DMBuildingShape.HalfFoundation:
                case DMBuildingShape.QuarterFoundation:
                    return 0;
                case DMBuildingShape.Floor:
                case DMBuildingShape.TriFloor:
                case DMBuildingShape.Ceiling:
                case DMBuildingShape.Hatch:
                case DMBuildingShape.StairwellFloor:
                case DMBuildingShape.Balcony:
                    return 1;
                case DMBuildingShape.Wall:
                case DMBuildingShape.HalfWall:
                case DMBuildingShape.QuarterWall:
                case DMBuildingShape.TriWallLeft:
                case DMBuildingShape.TriWallRight:
                case DMBuildingShape.InvTriWallLeft:
                case DMBuildingShape.InvTriWallRight:
                case DMBuildingShape.VentWall:
                case DMBuildingShape.Passage:
                case DMBuildingShape.Archway:
                    return 2;
                case DMBuildingShape.Window:
                case DMBuildingShape.WideWindow:
                case DMBuildingShape.SlitWindow:
                    return 3;
                case DMBuildingShape.DoorFrame:
                case DMBuildingShape.Door:
                case DMBuildingShape.DoubleDoorFrame:
                case DMBuildingShape.DoubleDoor:
                case DMBuildingShape.GateFrame:
                case DMBuildingShape.Gate:
                case DMBuildingShape.HatchLid:
                    return 4;
                case DMBuildingShape.Roof:
                case DMBuildingShape.RoofCorner:
                case DMBuildingShape.RoofInner:
                case DMBuildingShape.SteepRoof:
                case DMBuildingShape.RidgeCap:
                case DMBuildingShape.Rooftop:
                    return 5;
                case DMBuildingShape.Stairs:
                case DMBuildingShape.HalfStairs:
                case DMBuildingShape.SpiralStairs:
                case DMBuildingShape.Ramp:
                case DMBuildingShape.HalfRamp:
                case DMBuildingShape.Ladder:
                    return 6;
                case DMBuildingShape.Column:
                case DMBuildingShape.HalfColumn:
                case DMBuildingShape.Beam:
                case DMBuildingShape.Brace:
                case DMBuildingShape.Railing:
                case DMBuildingShape.HalfRailing:
                    return 7;
                case DMBuildingShape.SurfaceItem:
                    return 8;
            }

            switch (part.category)
            {
                case DMBuildingCategory.Foundations:
                    return 0;
                case DMBuildingCategory.FloorsAndRoofs:
                    return 1;
                case DMBuildingCategory.Walls:
                    return 2;
                case DMBuildingCategory.Doors:
                    return 4;
                case DMBuildingCategory.StructureAndStairs:
                    return 7;
                case DMBuildingCategory.Decor:
                    return 9;
            }

            return GroupNames.Length - 1;
        }

        static string NameOf(DMBuildingPartEntry part)
        {
            return string.IsNullOrEmpty(part.displayName) ? (part.id ?? string.Empty) : part.displayName;
        }

        static int CompareRows(PartRow a, PartRow b)
        {
            int c = GroupOf(a.part).CompareTo(GroupOf(b.part));
            if (c != 0)
                return c;
            c = EditorUtility.NaturalCompare(NameOf(a.part), NameOf(b.part));
            if (c != 0)
                return c;
            c = a.style.order.CompareTo(b.style.order);
            if (c != 0)
                return c;
            c = string.CompareOrdinal(a.style.styleId, b.style.styleId);
            return c != 0 ? c : string.CompareOrdinal(a.part.id, b.part.id);
        }

        bool PassesFilter(DMBuildingPartEntry part)
        {
            if (string.IsNullOrEmpty(partFilter))
                return true;
            return (part.id ?? string.Empty).IndexOf(partFilter, System.StringComparison.OrdinalIgnoreCase) >= 0
                || (part.displayName ?? string.Empty).IndexOf(partFilter, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>Draws rows under one foldout header per group. Returns the row whose X was pressed (part null if none).</summary>
        PartRow DrawGroupedRows(string scope, List<PartRow> rows, Color accent, bool acrossStyles)
        {
            rows.Sort(CompareRows);
            var counts = new int[GroupNames.Length];
            for (int i = 0; i < rows.Count; i++)
                counts[GroupOf(rows[i].part)]++;

            PartRow remove = default;
            int current = -1;
            bool open = true;
            for (int i = 0; i < rows.Count; i++)
            {
                PartRow row = rows[i];
                int group = GroupOf(row.part);
                if (group != current)
                {
                    current = group;
                    open = DrawGroupHeader(scope + "/" + GroupNames[group], GroupNames[group] + " (" + counts[group] + ")", accent);
                }

                if (!open)
                    continue;

                bool pressed;
                if (acrossStyles)
                {
                    EditorGUI.BeginChangeCheck();
                    pressed = DrawPart(row.style, row.part, true);
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(row.style);
                        DMBuildingStyles.Invalidate();
                    }
                }
                else
                {
                    pressed = DrawPart(row.style, row.part);
                }

                if (pressed)
                    remove = row;
            }

            if (rows.Count == 0)
                EditorGUILayout.LabelField("No parts match.", EditorStyles.miniLabel);
            return remove;
        }

        bool DrawGroupHeader(string key, string label, Color accent)
        {
            bool open = !closedGroups.Contains(key);
            EditorGUILayout.Space(2f);
            Rect r = GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true));
            bool next = EditorGUI.Foldout(r, open, label, true, FoldoutStyle);
            DMStudioStyles.DrawAccentLine(r, DarkMatterGenesisUiPalette.WithAlpha(accent, 0.6f), 1f);
            if (next != open)
            {
                if (next)
                    closedGroups.Remove(key);
                else
                    closedGroups.Add(key);
            }

            return next;
        }

        static void ConfirmRemove(PartRow row)
        {
            if (row.part == null || row.style == null || row.style.parts == null)
                return;
            if (!EditorUtility.DisplayDialog("Remove part", "Remove '" + row.part.displayName + "' from " + row.style.displayName + "? The prefab is kept.", "Remove", "Cancel"))
                return;
            Undo.RecordObject(row.style, "Remove Building Part");
            row.style.parts.Remove(row.part);
            EditorUtility.SetDirty(row.style);
            DMBuildingStyles.Invalidate();
            GUI.changed = true;
        }

        /// <summary>Equipment and other parts paid with several items (replaces the style cost when any line is valid).</summary>
        static void DrawCustomCost(DMBuildingPartEntry part)
        {
            if (part.customCost == null)
                part.customCost = new List<DMBuildingCostLine>();
            EditorGUILayout.LabelField(new GUIContent("Custom cost", "When any line is set it replaces the style cost."), EditorStyles.miniBoldLabel);
            int remove = -1;
            for (int i = 0; i < part.customCost.Count; i++)
            {
                DMBuildingCostLine line = part.customCost[i];
                if (line == null)
                    continue;
                EditorGUILayout.BeginHorizontal();
                line.item = (ItemData)EditorGUILayout.ObjectField(line.item, typeof(ItemData), false);
                line.amount = Mathf.Max(1, EditorGUILayout.IntField(line.amount, GUILayout.Width(80f)));
                if (GUILayout.Button("X", GUILayout.Width(22f)))
                    remove = i;
                EditorGUILayout.EndHorizontal();
            }

            if (remove >= 0)
            {
                part.customCost.RemoveAt(remove);
                GUI.changed = true;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUI.indentLevel * 15f);
            if (GUILayout.Button("+ Cost Line", GUILayout.Width(100f)))
            {
                part.customCost.Add(new DMBuildingCostLine());
                GUI.changed = true;
            }

            EditorGUILayout.EndHorizontal();
        }

        static string CostLabel(DMBuildingStyleLibrary style, DMBuildingPartEntry part)
        {
            if (part.customCost != null && part.customCost.Count > 0)
            {
                var bits = new List<string>();
                for (int i = 0; i < part.customCost.Count; i++)
                {
                    DMBuildingCostLine line = part.customCost[i];
                    if (line != null && line.item != null && line.amount > 0)
                        bits.Add(line.amount + " " + (string.IsNullOrEmpty(line.item.itemName) ? line.item.name : line.item.itemName));
                }

                if (bits.Count > 0)
                    return string.Join(", ", bits);
            }

            return part.cost + " " + CostName(style);
        }

        static string CostName(DMBuildingStyleLibrary style)
        {
            string name = style.CostItemName;
            return string.IsNullOrEmpty(name) ? "?" : name;
        }
    }
}