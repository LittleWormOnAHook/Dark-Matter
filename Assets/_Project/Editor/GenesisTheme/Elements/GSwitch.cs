using UnityEngine.UIElements;

namespace Project.EditorTools.Theme
{
    /// <summary>Toggle drawn as a pill switch. Same API as Toggle.</summary>
    public class GSwitch : Toggle
    {
        public GSwitch() : this(null) { }

        public GSwitch(string label) : base(label)
        {
            AddToClassList("g-switch");
            var mark = this.Q(className: "unity-toggle__checkmark");
            if (mark != null)
            {
                var knob = new VisualElement { pickingMode = PickingMode.Ignore };
                knob.AddToClassList("g-switch__knob");
                mark.Add(knob);
            }
        }
    }
}
