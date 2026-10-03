using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.EditorTools.Theme
{
    /// <summary>Shows every themed control in one place. Use it to judge and tune the Genesis "Frontier" theme.</summary>
    public class GenesisThemePreviewWindow : EditorWindow
    {
        enum DemoElement { Kinetic, Plasma, Cryo, Energy, Toxic }

        [MenuItem(GenesisTheme.MenuRoot + "Theme Preview", false, 20)]
        public static void Open()
        {
            var w = GetWindow<GenesisThemePreviewWindow>();
            w.titleContent = new GUIContent("Genesis Theme");
            w.minSize = new Vector2(900, 620);
        }

        void OnEnable() => GenesisTheme.Changed += Rebuild;
        void OnDisable() => GenesisTheme.Changed -= Rebuild;

        void Rebuild()
        {
            rootVisualElement.Clear();
            CreateGUI();
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            bool themed = GenesisTheme.Apply(root);
            root.style.flexDirection = FlexDirection.Column;

            root.Add(GWidgets.HeaderBar("Genesis Theme Preview", themed ? "Frontier" : "Theme off (Unity default)"));

            var tabs = new TabView();
            tabs.style.flexGrow = 1;
            tabs.Add(BuildControlsTab());
            tabs.Add(BuildListsTab());
            tabs.Add(BuildInspectorTab());
            root.Add(tabs);

            root.Add(GWidgets.Footer(out _, themed ? "Genesis Theme active" : "Genesis Theme disabled (Tools > Dark Matter Genesis > Theme)"));
        }

        Tab BuildControlsTab()
        {
            var tab = new Tab("Controls");
            var body = new VisualElement();
            body.style.flexDirection = FlexDirection.Row;
            body.style.flexGrow = 1;

            var nav = new VisualElement();
            nav.AddToClassList("g-nav");
            nav.Add(GWidgets.NavItem("Combat Core", true, null));
            nav.Add(GWidgets.NavItem("Momentum", false, null));
            nav.Add(GWidgets.NavItem("Poise & Stagger", false, null));
            nav.Add(GWidgets.NavItem("Status Effects", false, null));
            nav.Add(GWidgets.NavItem("Dodge / Dash", false, null));
            body.Add(nav);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            scroll.Add(grid);
            body.Add(scroll);

            // Panel 1: sliders and switches
            var p1 = Panel("Sliders & Switches  (sample values)");
            p1.Add(new GSegmentSlider("Segmented slider", 0f, 1f, 0.5f, 0.05f));
            p1.Add(new GSegmentSlider("Stepped (int)", 0f, 12f, 7f, 1f));
            p1.Add(new Slider("Standard slider", 0f, 10f) { value = 6f, showInputField = true });
            p1.Add(new SliderInt("Int slider", 0, 100) { value = 40, showInputField = true });
            p1.Add(new MinMaxSlider("Min / max", 2f, 7f, 0f, 10f));
            p1.Add(new GSwitch("Pill switch (on)") { value = true });
            p1.Add(new GSwitch("Pill switch (off)") { value = false });
            p1.Add(new Toggle("Tick box (on)") { value = true });
            p1.Add(new Toggle("Tick box (off)") { value = false });
            grid.Add(p1);

            // Panel 2: fields and dropdowns
            var p2 = Panel("Fields & Dropdowns");
            p2.Add(new TextField("Text field") { value = "Burning_Plasma_T2" });
            p2.Add(new FloatField("Float field") { value = 0.25f });
            p2.Add(new IntegerField("Integer field") { value = 3 });
            p2.Add(new Vector3Field("Vector3") { value = new Vector3(0f, 1.5f, -2f) });
            p2.Add(new DropdownField("Dropdown", new List<string> { "Stone", "Iron", "Silicate" }, 0));
            p2.Add(new EnumField("Enum", DemoElement.Plasma));
            p2.Add(new ColorField("Colour") { value = GenesisTheme.Accent });
            p2.Add(new ObjectField("Object") { objectType = typeof(Material), allowSceneObjects = false });
            var tip = new TextField("With tooltip") { value = "hover me", tooltip = "Tooltips use Unity's own popup, so they keep Unity's look." };
            p2.Add(tip);
            grid.Add(p2);

            // Panel 3: buttons, progress, help, toast
            var p3 = Panel("Buttons & Feedback");
            var row = new VisualElement();
            row.AddToClassList("g-row");
            row.Add(GWidgets.ActionButton("Apply", () => GWidgets.Toast(rootVisualElement, "Applied (preview only, nothing saved)"), primary: true));
            row.Add(GWidgets.ActionButton("Revert", () => GWidgets.Toast(rootVisualElement, "Reverted (preview only)")));
            row.Add(GWidgets.ActionButton("Reset Tab", () => GWidgets.Toast(rootVisualElement, "Reset (preview only)"), danger: true));
            var disabled = GWidgets.ActionButton("Disabled", null);
            disabled.SetEnabled(false);
            row.Add(disabled);
            p3.Add(row);
            var pb = new UnityEngine.UIElements.ProgressBar { title = "Baking icons 68%", lowValue = 0f, highValue = 100f, value = 68f };
            pb.style.marginTop = 8;
            p3.Add(pb);
            var pills = new VisualElement();
            pills.AddToClassList("g-row");
            pills.Add(GWidgets.Pill("Active", GWidgets.PillKind.Ok));
            pills.Add(new VisualElement { style = { width = 6 } });
            pills.Add(GWidgets.Pill("Tune", GWidgets.PillKind.Warn));
            pills.Add(new VisualElement { style = { width = 6 } });
            pills.Add(GWidgets.Pill("Conflict", GWidgets.PillKind.Bad));
            pills.style.marginTop = 8;
            p3.Add(pills);
            p3.Add(new HelpBox("Help boxes pick up the accent edge.", HelpBoxMessageType.Info));
            grid.Add(p3);

            // Panel 4: foldouts
            var p4 = Panel("Foldouts");
            var fo = new Foldout { text = "Dodge / Dash", value = true };
            fo.Add(new FloatField("I-frame window (s)") { value = 0.25f });
            fo.Add(new FloatField("Dash distance (m)") { value = 6f });
            p4.Add(fo);
            var fo2 = new Foldout { text = "Advanced", value = false };
            fo2.Add(new Toggle("Cancel recovery"));
            p4.Add(fo2);
            grid.Add(p4);

            tab.Add(body);
            return tab;
        }

        Tab BuildListsTab()
        {
            var tab = new Tab("Lists");
            var p = Panel("List view");
            p.style.flexGrow = 1;
            var items = new List<(string name, GWidgets.PillKind kind, string state)>
            {
                ("Burning", GWidgets.PillKind.Ok, "Active"), ("Frozen", GWidgets.PillKind.Ok, "Active"),
                ("Shocked", GWidgets.PillKind.Warn, "Tune"), ("Corroded", GWidgets.PillKind.Bad, "Conflict"),
                ("Bleeding", GWidgets.PillKind.Ok, "Active"),
            };
            var list = new ListView
            {
                itemsSource = items,
                fixedItemHeight = 26,
                selectionType = SelectionType.Single,
                makeItem = () =>
                {
                    var r = new VisualElement();
                    r.AddToClassList("g-row");
                    r.style.justifyContent = Justify.SpaceBetween;
                    r.style.flexGrow = 1;
                    r.Add(new Label());
                    return r;
                },
                bindItem = (e, i) =>
                {
                    ((Label)e[0]).text = items[i].name;
                    while (e.childCount > 1) e.RemoveAt(1);
                    e.Add(GWidgets.Pill(items[i].state, items[i].kind));
                },
            };
            list.style.height = 160;
            list.selectedIndex = 0;
            p.Add(list);
            tab.Add(p);
            return tab;
        }

        Tab BuildInspectorTab()
        {
            var tab = new Tab("Inspector look");
            var p = Panel("Material  ·  Genesis Rock Blend  (sample)");
            var row = new VisualElement();
            row.AddToClassList("g-row");
            foreach (var n in new[] { "Albedo", "Normal", "Mask", "Height" })
            {
                var col = new VisualElement();
                col.style.alignItems = Align.Center;
                col.style.marginRight = 10;
                var slot = new GChamferPanel { ShowStrip = false };
                slot.style.width = 52; slot.style.height = 52; slot.style.marginLeft = 0; slot.style.marginRight = 0;
                col.Add(slot);
                var cap = new Label(n);
                cap.AddToClassList("g-dim");
                col.Add(cap);
                row.Add(col);
            }
            p.Add(row);
            p.Add(new GSegmentSlider("Blend sharpness", 0f, 1f, 0.62f, 0.01f));
            p.Add(new GSegmentSlider("Height contrast", 0f, 4f, 1.5f, 0.1f));
            tab.Add(p);
            return tab;
        }

        static GChamferPanel Panel(string title)
        {
            var p = new GChamferPanel(title);
            p.style.width = 380;
            return p;
        }
    }
}
