using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Top-half companion health arc using a feathered ring sprite and radial Image fill.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class HalfCircleHealthBarImage : MonoBehaviour
    {
        private const float HalfArcFill = 0.5f;

        private Image image;

        public float FillAmount
        {
            get
            {
                EnsureImage();
                return image.fillAmount / HalfArcFill;
            }
            set
            {
                EnsureImage();
                image.fillAmount = HalfArcFill * Mathf.Clamp01(value);
            }
        }

        public Color Color
        {
            get
            {
                EnsureImage();
                return image.color;
            }
            set
            {
                EnsureImage();
                image.color = value;
            }
        }

        private void Awake()
        {
            EnsureImage();
            if (image.sprite == null)
                Configure(Color.white, 1f);
        }

        public void Configure(Color color, float normalizedFill)
        {
            EnsureImage();
            image.sprite = MapUiSprites.HudHealthRing;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Left;
            image.fillClockwise = true;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = color;
            FillAmount = normalizedFill;
        }

        private void EnsureImage()
        {
            if (image != null)
                return;

            image = GetComponent<Image>();
        }
    }
}
