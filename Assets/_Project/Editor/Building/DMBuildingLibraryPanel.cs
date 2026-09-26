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
    /// Genesis Studio &gt; Building &gt; Library.
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

            if (selected == null)
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
                foreach (DMBuildingPartEntry p in style.parts)
                    if (p != null) openParts.Add(style.styleId + "/" + p.id);
            if (GUILayout.Button("Collapse", GUILayout.Width(64f)))
                openParts.Clear();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2f);

            int remove = -1;
            for (int i = 0; i < style.parts.Count; i++)
            {
                DMBuildingPartEntry part = style.parts[i];
                if (part == null)
                    continue;
                if (!string.IsNullOrEmpty(partFilter)
                    && part.id.IndexOf(partFilter, System.StringComparison.OrdinalIgnoreCase) < 0
                    && (part.displayName ?? string.Empty).IndexOf(partFilter, System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                if (DrawPart(style, part))
                    remove = i;
            }

            if (remove >= 0 && EditorUtility.DisplayDialog("Remove part", "Remove '" + style.parts[remove].displayName + "' from " + style.displayName + "? The prefab is kept.", "Remove", "Cancel"))
            {
                style.parts.RemoveAt(remove);
                GUI.changed = true;
            }

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
        bool DrawPart(DMBuildingStyleLibrary style, DMBuildingPartEntry part)
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
            DMStudioStyles.DrawBadge(part.cost + " " + CostName(style), DarkMatterGenesisUiPalette.SlateGray);
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
                part.applyStyleFinish = EditorGUILayout.Toggle(
                    new GUIContent("Apply style finish", "Off keeps the prefab's own materials (lights, decorations)."),
                    part.applyStyleFinish);
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

        static string CostName(DMBuildingStyleLibrary style)
        {
            string name = style.CostItemName;
            return string.IsNullOrEmpty(name) ? "?" : name;
        }
    }
}