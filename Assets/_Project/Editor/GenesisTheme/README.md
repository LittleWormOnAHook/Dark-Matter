# Genesis Theme ("Frontier")

Shared look for every Genesis editor window and inspector. Picked Oct 2, 2026.

- **Colours, sizes:** `USS/GenesisTheme.uss` (variables only)
- **Control restyles:** `USS/GenesisControls.uss` (scoped to `.genesis-root`, so Unity's own windows are untouched)
- **Header font:** `Fonts/ChakraPetch-SemiBold.ttf` (SIL Open Font Licence, see `Fonts/ChakraPetch-OFL.txt`). Body text keeps Unity's Inter.
- **Turn on/off:** Tools > Dark Matter Genesis > Theme > Genesis Theme Enabled
- **See every control:** Tools > Dark Matter Genesis > Theme > Theme Preview

## UI Toolkit windows / inspectors
```csharp
public void CreateGUI() {
    GenesisTheme.Apply(rootVisualElement);
    rootVisualElement.Add(GWidgets.HeaderBar("My Tool", "Subtitle"));
    var panel = new GChamferPanel("Section");
    panel.Add(new GSegmentSlider("Strength", 0f, 1f, 0.5f));
    panel.Add(new GSwitch("Enabled"));
    panel.Add(GWidgets.ActionButton("Apply", DoApply, primary: true));
    rootVisualElement.Add(panel);
}
```
Classes: `g-primary` / `g-danger` on Buttons, `g-h` for header text, `g-row`, `g-dim`, `g-pill--ok|warn|bad`.

## Old IMGUI (OnGUI) windows, until converted
```csharp
void OnGUI() {
    GenesisStyles.WindowBackground(position);
    GenesisStyles.Section("Dodge / Dash");
    GenesisStyles.BeginPanel();
    // ... existing fields ...
    if (GUILayout.Button("Apply", GenesisStyles.PrimaryButton)) DoApply();
    GenesisStyles.EndPanel();
}
```

## Limits
Unity's own windows (Hierarchy, Project, Console, toolbars), built-in component inspectors and Unity tooltips keep Unity's look.
