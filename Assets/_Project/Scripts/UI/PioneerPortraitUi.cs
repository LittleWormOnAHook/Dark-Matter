using Project.Pioneers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Shared helpers for companion/Echo ID portraits in HUD and journal lists.
    /// Structure: fixed-size root → circular <see cref="Mask"/> clip → <see cref="RawImage"/> photo.
    /// Source PNGs bake the ring border; corners are opaque square plates, so the mask hides them.
    /// No programmatic ring overlay — baked art is the only border.
    /// Photos bind static <see cref="Texture2D"/> via RawImage + explicit UI/Default material on
    /// Screen Space Overlay canvases. This differs from OpticsCameraRig, where an HDRP camera
    /// RenderTexture fed into RawImage triggers DrawRawMesh / D3D12 crashes — static portrait
    /// textures on overlay UI are safe (same pattern as MapUI baked terrain RawImages).
    /// </summary>
    public static class PioneerPortraitUi
    {
        private static Material sharedPhotoMaterial;

        public static RawImage CreateCircularPortrait(
            Transform parent,
            float diameter,
            bool raycastTarget = false)
        {
            GameObject root = new GameObject("Portrait", typeof(RectTransform), typeof(LayoutElement));
            root.transform.SetParent(parent, false);

            LayoutElement layout = root.GetComponent<LayoutElement>();
            layout.minWidth = diameter;
            layout.preferredWidth = diameter;
            layout.minHeight = diameter;
            layout.preferredHeight = diameter;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(diameter, diameter);

            GameObject maskObject = new GameObject("MaskClip", typeof(RectTransform), typeof(Image), typeof(Mask));
            maskObject.transform.SetParent(root.transform, false);
            RectTransform maskRect = maskObject.GetComponent<RectTransform>();
            maskRect.anchorMin = Vector2.zero;
            maskRect.anchorMax = Vector2.one;
            maskRect.offsetMin = Vector2.zero;
            maskRect.offsetMax = Vector2.zero;

            Image maskImage = maskObject.GetComponent<Image>();
            maskImage.sprite = MapUiSprites.PortraitCircleMask;
            maskImage.type = Image.Type.Simple;
            maskImage.preserveAspect = true;
            maskImage.raycastTarget = false;
            maskImage.color = Color.white;

            Mask mask = maskObject.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject photoObject = new GameObject("Photo", typeof(RectTransform), typeof(RawImage));
            photoObject.transform.SetParent(maskObject.transform, false);
            RectTransform photoRect = photoObject.GetComponent<RectTransform>();
            photoRect.anchorMin = Vector2.zero;
            photoRect.anchorMax = Vector2.one;
            photoRect.offsetMin = Vector2.zero;
            photoRect.offsetMax = Vector2.zero;

            RawImage photo = photoObject.GetComponent<RawImage>();
            ApplyHdrpSafePhotoMaterial(photo);
            photo.raycastTarget = raycastTarget;
            photo.enabled = false;

            WarnIfNotOverlayCanvas(root.transform);

            return photo;
        }

        public static Image GetMaskImage(RawImage photoImage) =>
            photoImage != null ? photoImage.transform.parent?.GetComponent<Image>() : null;

        /// <summary>Legacy hook — portraits no longer use a programmatic ring overlay.</summary>
        public static Image GetRingImage(RawImage photoImage) => null;

        public static void ApplyPortrait(
            Image maskOrFrame,
            RawImage photoImage,
            TextMeshProUGUI initialsLabel,
            SkilledPioneerRecord record,
            bool preferEchoSpirit = false)
        {
            if (photoImage == null)
                return;

            Sprite sourceSprite = preferEchoSpirit
                ? PioneerPortraitResolver.ResolveEchoSpirit()
                : PioneerPortraitResolver.Resolve(record);

            if (record == null)
            {
                ClearPhoto(photoImage);
                if (initialsLabel != null)
                    initialsLabel.text = string.Empty;
                return;
            }

            if (sourceSprite != null)
            {
                SetPhotoFromSource(photoImage, sourceSprite);

                if (initialsLabel != null)
                    initialsLabel.text = string.Empty;
                return;
            }

            ClearPhoto(photoImage);
            if (initialsLabel != null)
                initialsLabel.text = BuildInitials(PioneerUiLabels.GetDisplayName(record));
        }

        /// <summary>
        /// Binds a portrait photo without resolving a roster record. Accepts a source Sprite asset
        /// only to read its underlying Texture2D + atlas rect — never assigns Image.sprite on the photo.
        /// </summary>
        public static void ApplySpriteOnly(Image maskOrFrame, RawImage photoImage, Sprite sourceSprite)
        {
            if (photoImage == null)
                return;

            if (sourceSprite == null)
            {
                ClearPhoto(photoImage);
                return;
            }

            SetPhotoFromSource(photoImage, sourceSprite);
        }

        public static string BuildInitials(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return string.Empty;

            string trimmed = displayName.Trim();
            if (trimmed.Length == 1)
                return trimmed.ToUpperInvariant();

            int spaceIndex = trimmed.IndexOf(' ');
            if (spaceIndex > 0 && spaceIndex < trimmed.Length - 1)
                return $"{char.ToUpperInvariant(trimmed[0])}{char.ToUpperInvariant(trimmed[spaceIndex + 1])}";

            return trimmed.Length >= 2
                ? $"{char.ToUpperInvariant(trimmed[0])}{char.ToUpperInvariant(trimmed[1])}"
                : char.ToUpperInvariant(trimmed[0]).ToString();
        }

        internal static void ClearDisplaySpriteCache()
        {
            // Legacy no-op — portraits no longer cache runtime Sprite wrappers.
        }

        /// <summary>
        /// Shared UI/Default material for portrait RawImages. Never assign RenderTextures here.
        /// </summary>
        internal static void ApplyHdrpSafePhotoMaterial(RawImage photo)
        {
            if (photo == null)
                return;

            photo.material = GetSharedPhotoMaterial();
            photo.color = Color.white;
        }

        private static Material GetSharedPhotoMaterial()
        {
            if (sharedPhotoMaterial != null)
                return sharedPhotoMaterial;

            Material defaultGraphic = Graphic.defaultGraphicMaterial;
            Shader shader = defaultGraphic != null ? defaultGraphic.shader : null;
            if (shader == null)
                shader = Shader.Find("UI/Default");

            sharedPhotoMaterial = shader != null
                ? new Material(shader) { name = "PioneerPortraitPhoto (Shared)", hideFlags = HideFlags.HideAndDontSave }
                : defaultGraphic;

            return sharedPhotoMaterial;
        }

        /// <summary>
        /// Binds a static sprite atlas region to RawImage. Safe on Screen Space Overlay — not a live camera RT.
        /// </summary>
        private static void SetPhotoFromSource(RawImage photoImage, Sprite sourceSprite)
        {
            if (photoImage == null || sourceSprite == null)
                return;

            Texture2D texture = sourceSprite.texture;
            if (texture == null)
            {
                ClearPhoto(photoImage);
                return;
            }

            ApplyHdrpSafePhotoMaterial(photoImage);
            photoImage.texture = texture;
            photoImage.uvRect = BuildUvRect(sourceSprite, texture);
            photoImage.color = Color.white;
            photoImage.enabled = true;
        }

        private static Rect BuildUvRect(Sprite sourceSprite, Texture2D texture)
        {
            Rect rect = sourceSprite.textureRect;
            if (rect.width <= 0f || rect.height <= 0f)
                return new Rect(0f, 0f, 1f, 1f);

            return new Rect(
                rect.x / texture.width,
                rect.y / texture.height,
                rect.width / texture.width,
                rect.height / texture.height);
        }

        private static void ClearPhoto(RawImage photoImage)
        {
            if (photoImage == null)
                return;

            photoImage.enabled = false;
            photoImage.texture = null;
            photoImage.uvRect = new Rect(0f, 0f, 1f, 1f);
        }

        private static void WarnIfNotOverlayCanvas(Transform portraitRoot)
        {
            Canvas canvas = portraitRoot.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                return;

            Debug.LogWarning(
                $"PioneerPortraitUi: portrait under '{canvas.name}' uses {canvas.renderMode}. " +
                "Prefer Screen Space Overlay for HDRP-safe RawImage portraits.");
        }
    }
}
