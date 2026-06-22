using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public enum SurvivalArcFillMode
    {
        FullCircle = 0,
        HalfMoon = 1,
        QuarterArc = 2
    }

    [RequireComponent(typeof(Slider))]
    public class CircularProgressBar : MonoBehaviour
    {
        [Tooltip("The Image (Filled) that reflects slider value.")]
        public Image filledImage;

        [SerializeField] private SurvivalArcFillMode fillMode = SurvivalArcFillMode.FullCircle;

        private Slider slider;
        private Image trackImage;

        private void Awake()
        {
            slider = GetComponent<Slider>();
            if (slider != null)
                slider.onValueChanged.AddListener(UpdateRadialFill);
        }

        private void Start()
        {
            if (slider != null)
                UpdateRadialFill(slider.value);
        }

        public void SetFillMode(SurvivalArcFillMode mode, bool applyVisuals = true)
        {
            fillMode = mode;
            if (!applyVisuals)
            {
                if (slider != null)
                    UpdateRadialFill(slider.value);
                return;
            }

            if (filledImage != null)
                ApplyFillSettings(filledImage, isFill: true);

            if (trackImage != null)
                ApplyFillSettings(trackImage, isFill: false);

            if (slider != null)
                UpdateRadialFill(slider.value);
        }

        public void SetTrackImage(Image track)
        {
            trackImage = track;
            if (trackImage != null)
                ApplyFillSettings(trackImage, isFill: false);
        }

        public void UpdateRadialFill(float value)
        {
            if (filledImage == null || slider == null)
                return;

            float range = slider.maxValue - slider.minValue;
            float normalized = range > 0f ? (value - slider.minValue) / range : 0f;
            normalized = Mathf.Clamp01(normalized);

            filledImage.fillAmount = fillMode switch
            {
                SurvivalArcFillMode.QuarterArc => normalized * 0.25f,
                _ => normalized
            };
        }

        public static void ApplyArcVisuals(Image image, SurvivalArcFillMode mode, bool isFillLayer)
        {
            if (image == null)
                return;

            image.type = Image.Type.Filled;
            image.preserveAspect = true;
            image.raycastTarget = false;

            switch (mode)
            {
                case SurvivalArcFillMode.HalfMoon:
                    image.fillMethod = Image.FillMethod.Radial180;
                    image.fillOrigin = (int)Image.Origin180.Bottom;
                    image.fillClockwise = true;
                    image.fillAmount = isFillLayer ? 0f : 1f;
                    break;

                case SurvivalArcFillMode.QuarterArc:
                    image.fillMethod = Image.FillMethod.Radial360;
                    image.fillOrigin = (int)Image.Origin360.Right;
                    image.fillClockwise = true;
                    image.fillAmount = isFillLayer ? 0f : 0.25f;
                    break;

                default:
                    image.fillMethod = Image.FillMethod.Radial360;
                    image.fillOrigin = (int)Image.Origin360.Top;
                    image.fillClockwise = true;
                    image.fillAmount = isFillLayer ? 0f : 1f;
                    break;
            }
        }

        private void ApplyFillSettings(Image image, bool isFill)
        {
            ApplyArcVisuals(image, fillMode, isFill);
        }

        private void OnDestroy()
        {
            if (slider != null)
                slider.onValueChanged.RemoveListener(UpdateRadialFill);
        }
    }
}
