using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Shared UITK style helpers (background sizing, etc.).
    /// </summary>
    internal static class DMUiToolkitStyle
    {
        private static readonly System.Collections.Generic.Dictionary<VisualElement, object> AppliedBackgrounds =
            new System.Collections.Generic.Dictionary<VisualElement, object>();

        private static bool BackgroundUnchanged(VisualElement element, object key)
        {
            return element != null
                && AppliedBackgrounds.TryGetValue(element, out object existing)
                && ReferenceEquals(existing, key);
        }

        private static void RememberBackground(VisualElement element, object key)
        {
            if (element == null)
                return;

            if (key == null)
                AppliedBackgrounds.Remove(element);
            else
                AppliedBackgrounds[element] = key;
        }
        public static void ApplyBackgroundScale(IStyle style, ScaleMode mode)
        {
            style.backgroundSize = mode switch
            {
                ScaleMode.ScaleToFit => new BackgroundSize(BackgroundSizeType.Contain),
                ScaleMode.ScaleAndCrop => new BackgroundSize(BackgroundSizeType.Cover),
                ScaleMode.StretchToFill => new BackgroundSize(Length.Percent(100), Length.Percent(100)),
                _ => new BackgroundSize(BackgroundSizeType.Contain)
            };
        }

        public static void ApplyBackgroundScale(VisualElement element, ScaleMode mode)
        {
            if (element != null)
                ApplyBackgroundScale(element.style, mode);
        }

        public static void ClearBackgroundImage(VisualElement element)
        {
            if (element == null)
                return;

            if (BackgroundUnchanged(element, null))
                return;

            element.style.backgroundImage = StyleKeyword.None;
            RememberBackground(element, null);
        }

        /// <summary>Avoid UITK "Invalid value for image texture" when sprite/texture is missing.</summary>
        public static bool TrySetSpriteBackground(VisualElement element, Sprite sprite, ScaleMode mode = ScaleMode.ScaleToFit)
        {
            if (element == null)
                return false;

            if (!IsValidSprite(sprite))
            {
                ClearBackgroundImage(element);
                return false;
            }

            if (BackgroundUnchanged(element, sprite))
                return true;

            Background background = Background.FromSprite(sprite);
            if (background.sprite == null && background.texture == null)
            {
                ClearBackgroundImage(element);
                return false;
            }

            element.style.backgroundImage = new StyleBackground(background);
            ApplyBackgroundScale(element, mode);
            RememberBackground(element, sprite);
            return true;
        }

        public static bool TrySetTextureBackground(VisualElement element, Texture texture, ScaleMode mode = ScaleMode.ScaleToFit)
        {
            if (element == null)
                return false;

            if (!IsValidTexture(texture))
            {
                ClearBackgroundImage(element);
                return false;
            }

            if (BackgroundUnchanged(element, texture))
                return true;

            if (texture is RenderTexture renderTexture)
            {
                element.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(renderTexture));
            }
            else if (texture is Texture2D texture2D)
            {
                element.style.backgroundImage = new StyleBackground(Background.FromTexture2D(texture2D));
            }
            else
            {
                ClearBackgroundImage(element);
                return false;
            }
            ApplyBackgroundScale(element, mode);
            RememberBackground(element, texture);
            return true;
        }

        public static bool TrySetRenderTextureBackground(
            VisualElement element,
            RenderTexture renderTexture,
            ScaleMode mode = ScaleMode.ScaleAndCrop)
        {
            if (element == null)
                return false;

            if (!IsValidRenderTexture(renderTexture))
            {
                ClearBackgroundImage(element);
                return false;
            }

            if (BackgroundUnchanged(element, renderTexture))
                return true;

            element.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(renderTexture));
            ApplyBackgroundScale(element, mode);
            RememberBackground(element, renderTexture);
            return true;
        }

        private static bool IsValidSprite(Sprite sprite)
        {
            if (sprite == null)
                return false;

            Texture2D texture = sprite.texture;
            if (!IsValidTexture(texture))
                return false;

            Rect rect = sprite.rect;
            return rect.width > 0f && rect.height > 0f;
        }

        private static bool IsValidTexture(Texture texture)
        {
            if (texture == null)
                return false;

            return texture.width > 0 && texture.height > 0;
        }

        private static bool IsValidRenderTexture(RenderTexture renderTexture)
        {
            if (renderTexture == null)
                return false;

            if (!renderTexture.IsCreated())
                return false;

            return renderTexture.width > 0 && renderTexture.height > 0;
        }
    }
}
