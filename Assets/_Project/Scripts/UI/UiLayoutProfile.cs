using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    [Serializable]
    public sealed class UiLayoutNodeEntry
    {
        public string relativePath = string.Empty;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector2 offsetMin;
        public Vector2 offsetMax;
        public Vector3 localScale = Vector3.one;
        public bool activeSelf = true;

        public bool hasImageStyle;
        public Sprite imageSprite;
        public Material imageMaterial;
        public Color imageColor = Color.white;
        public bool imagePreserveAspect;
        public Image.Type imageType = Image.Type.Simple;

        public bool hasRawImageStyle;
        public Texture rawTexture;
        public Color rawImageColor = Color.white;

        public bool hasLayoutElement;
        public float layoutMinWidth = -1f;
        public float layoutMinHeight = -1f;
        public float layoutPreferredWidth = -1f;
        public float layoutPreferredHeight = -1f;
        public float layoutFlexibleWidth = -1f;
        public float layoutFlexibleHeight = -1f;

        public bool hasGridLayout;
        public Vector2 gridCellSize = new Vector2(64f, 64f);
        public Vector2 gridSpacing = new Vector2(8f, 8f);
        public int gridPaddingLeft;
        public int gridPaddingRight;
        public int gridPaddingTop;
        public int gridPaddingBottom;
        public GridLayoutGroup.Constraint gridConstraint = GridLayoutGroup.Constraint.Flexible;
        public int gridConstraintCount = 1;
        public GridLayoutGroup.Corner gridStartCorner = GridLayoutGroup.Corner.UpperLeft;
        public GridLayoutGroup.Axis gridStartAxis = GridLayoutGroup.Axis.Horizontal;
        public TextAnchor gridChildAlignment = TextAnchor.UpperLeft;

        public bool hasButtonStyle;
        public bool buttonInteractable = true;
        public Selectable.Transition buttonTransition = Selectable.Transition.ColorTint;
        public string buttonTargetGraphicPath = string.Empty;
        public Color buttonNormalColor = Color.white;
        public Color buttonHighlightedColor = Color.white;
        public Color buttonPressedColor = Color.white;
        public Color buttonSelectedColor = Color.white;
        public Color buttonDisabledColor = Color.white;
        public float buttonColorMultiplier = 1f;
        public float buttonFadeDuration = 0.1f;
        public Sprite buttonHighlightedSprite;
        public Sprite buttonPressedSprite;
        public Sprite buttonSelectedSprite;
        public Sprite buttonDisabledSprite;
        public string buttonNormalTrigger = "Normal";
        public string buttonHighlightedTrigger = "Highlighted";
        public string buttonPressedTrigger = "Pressed";
        public string buttonSelectedTrigger = "Selected";
        public string buttonDisabledTrigger = "Disabled";

        public bool hasTextStyle;
        public string textContent = string.Empty;
        public TMP_FontAsset textFont;
        public Material textFontMaterial;
        public float textFontSize = 14f;
        public FontStyles textFontStyle;
        public Color textColor = Color.white;
        public TextAlignmentOptions textAlignment = TextAlignmentOptions.Center;
        public bool textAutoSize;
        public float textFontSizeMin = 8f;
        public float textFontSizeMax = 72f;
        public bool textWordWrap = true;
        public bool textRichText = true;
        public bool textRaycastTarget;
        public Vector4 textMargin;
        public float textCharacterSpacing;
        public float textLineSpacing;
        public float textParagraphSpacing;
        public TextOverflowModes textOverflowMode = TextOverflowModes.Overflow;

        public bool hasLegacyTextStyle;
        public string legacyTextContent = string.Empty;
        public Font legacyFont;
        public int legacyFontSize = 14;
        public Color legacyTextColor = Color.white;
        public TextAnchor legacyAlignment = TextAnchor.MiddleCenter;
        public FontStyle legacyFontStyle = FontStyle.Normal;
        public float legacyLineSpacing = 1f;
        public bool legacyRichText = true;
        public bool legacyRaycastTarget;
        public bool legacyBestFit;
        public int legacyMinSize = 10;
        public int legacyMaxSize = 40;
        public HorizontalWrapMode legacyHorizontalOverflow = HorizontalWrapMode.Wrap;
        public VerticalWrapMode legacyVerticalOverflow = VerticalWrapMode.Truncate;
    }

    [CreateAssetMenu(menuName = "Project/UI/Layout Profile", fileName = "UiLayoutProfile")]
    public sealed class UiLayoutProfile : ScriptableObject
    {
        [Tooltip("Matches UiPanelIds (e.g. inventory_panel).")]
        public string panelId;

        public List<UiLayoutNodeEntry> nodes = new List<UiLayoutNodeEntry>();

        public UiLayoutNodeEntry FindNode(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                relativePath = string.Empty;

            for (int i = 0; i < nodes.Count; i++)
            {
                UiLayoutNodeEntry node = nodes[i];
                if (node != null && node.relativePath == relativePath)
                    return node;
            }

            return null;
        }
    }
}
