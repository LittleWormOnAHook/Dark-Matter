using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.EditorTools.Theme
{
    /// <summary>Label + segmented bar (click/drag to set) + numeric field. Raises ChangeEvent&lt;float&gt;.</summary>
    public class GSegmentSlider : VisualElement, INotifyValueChanged<float>
    {
        static readonly CustomStyleProperty<Color> s_On = new CustomStyleProperty<Color>("--g-seg-on");
        static readonly CustomStyleProperty<Color> s_Off = new CustomStyleProperty<Color>("--g-seg-off");
        static readonly CustomStyleProperty<Color> s_Line = new CustomStyleProperty<Color>("--g-seg-line");
        static readonly CustomStyleProperty<int> s_Count = new CustomStyleProperty<int>("--g-seg-count");

        readonly VisualElement m_Bar;
        readonly FloatField m_Field;
        readonly Label m_Label;
        float m_Value;
        Color m_On = GenesisTheme.Accent, m_Off = GenesisTheme.Bg0, m_Line = GenesisTheme.Line;
        int m_Count = 12;

        public float lowValue { get; set; }
        public float highValue { get; set; }
        /// <summary>Optional step; 0 = continuous.</summary>
        public float step { get; set; }
        public string label { get => m_Label.text; set => m_Label.text = value; }

        public GSegmentSlider(string label, float low, float high, float value = 0f, float step = 0f)
        {
            AddToClassList("g-seg");
            AddToClassList("g-row");
            lowValue = low; highValue = high; this.step = step;

            m_Label = new Label(label);
            m_Label.AddToClassList("unity-base-field__label");
            m_Label.style.minWidth = 120;
            Add(m_Label);

            m_Bar = new VisualElement();
            m_Bar.AddToClassList("g-seg__bar");
            m_Bar.generateVisualContent += DrawBar;
            m_Bar.RegisterCallback<CustomStyleResolvedEvent>(OnStyles);
            m_Bar.RegisterCallback<PointerDownEvent>(OnDown);
            m_Bar.RegisterCallback<PointerMoveEvent>(OnMove);
            m_Bar.RegisterCallback<PointerUpEvent>(OnUp);
            Add(m_Bar);

            m_Field = new FloatField();
            m_Field.AddToClassList("g-seg__value");
            m_Field.isDelayed = true;
            m_Field.RegisterValueChangedCallback(e => { value = e.newValue; e.StopPropagation(); });
            Add(m_Field);

            SetValueWithoutNotify(value);
        }

        public float value
        {
            get => m_Value;
            set
            {
                float v = Clean(value);
                if (Mathf.Approximately(v, m_Value)) { m_Field.SetValueWithoutNotify(v); return; }
                using (var evt = ChangeEvent<float>.GetPooled(m_Value, v))
                {
                    evt.target = this;
                    SetValueWithoutNotify(v);
                    SendEvent(evt);
                }
            }
        }

        public void SetValueWithoutNotify(float newValue)
        {
            m_Value = Clean(newValue);
            m_Field.SetValueWithoutNotify((float)Math.Round(m_Value, 3));
            m_Bar.MarkDirtyRepaint();
        }

        float Clean(float v)
        {
            v = Mathf.Clamp(v, Mathf.Min(lowValue, highValue), Mathf.Max(lowValue, highValue));
            if (step > 0f) v = lowValue + Mathf.Round((v - lowValue) / step) * step;
            return v;
        }

        void OnStyles(CustomStyleResolvedEvent e)
        {
            if (m_Bar.customStyle.TryGetValue(s_On, out var on)) m_On = on;
            if (m_Bar.customStyle.TryGetValue(s_Off, out var off)) m_Off = off;
            if (m_Bar.customStyle.TryGetValue(s_Line, out var line)) m_Line = line;
            if (m_Bar.customStyle.TryGetValue(s_Count, out var count)) m_Count = Mathf.Clamp(count, 2, 64);
            m_Bar.MarkDirtyRepaint();
        }

        void SetFromX(float x)
        {
            float w = m_Bar.layout.width;
            if (w <= 0f) return;
            value = Mathf.Lerp(lowValue, highValue, Mathf.Clamp01(x / w));
        }

        void OnDown(PointerDownEvent e)
        {
            if (e.button != 0) return;
            m_Bar.CapturePointer(e.pointerId);
            SetFromX(e.localPosition.x);
            e.StopPropagation();
        }

        void OnMove(PointerMoveEvent e)
        {
            if (!m_Bar.HasPointerCapture(e.pointerId)) return;
            SetFromX(e.localPosition.x);
            e.StopPropagation();
        }

        void OnUp(PointerUpEvent e)
        {
            if (!m_Bar.HasPointerCapture(e.pointerId)) return;
            m_Bar.ReleasePointer(e.pointerId);
            e.StopPropagation();
        }

        void DrawBar(MeshGenerationContext ctx)
        {
            float w = m_Bar.layout.width, h = m_Bar.layout.height;
            if (w < 4f || h < 2f || float.IsNaN(w)) return;
            const float gap = 2f;
            float segW = (w - gap * (m_Count - 1)) / m_Count;
            if (segW < 1f) return;
            float t = Mathf.Approximately(highValue, lowValue) ? 0f : Mathf.InverseLerp(lowValue, highValue, m_Value);
            float filled = t * m_Count;
            var p = ctx.painter2D;
            for (int i = 0; i < m_Count; i++)
            {
                float x = i * (segW + gap);
                bool on = i < Mathf.Round(filled);
                p.BeginPath();
                p.MoveTo(new Vector2(x + 0.5f, 0.5f));
                p.LineTo(new Vector2(x + segW - 0.5f, 0.5f));
                p.LineTo(new Vector2(x + segW - 0.5f, h - 0.5f));
                p.LineTo(new Vector2(x + 0.5f, h - 0.5f));
                p.ClosePath();
                p.fillColor = on ? m_On : m_Off;
                p.Fill();
                p.strokeColor = on ? m_On : m_Line;
                p.lineWidth = 1f;
                p.Stroke();
            }
        }
    }
}
