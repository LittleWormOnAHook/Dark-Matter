using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Project.Pet;

namespace Project.UI
{
    public class PetEntryUI : MonoBehaviour
    {
        private PetController _pet;
        private bool _suppressEvents;
        private float _scale = 1f;
        private bool _compact;

        private TMP_InputField _nameField;
        private TextMeshProUGUI _statusText;
        private Toggle _activeToggle;
        private Toggle _followToggle;
        private Toggle _wanderToggle;
        private Toggle _fetchToggle;
        private Button _callButton;

        public void SetFont(TMP_FontAsset font)
        {
            // Kept for compatibility; all pet UI uses LiberationSans SDF - Fallback via TmpUiHelper.
        }

        public void SetScale(float scale)
        {
            _scale = Mathf.Max(0.5f, scale);
        }

        public void SetCompact(bool compact)
        {
            _compact = compact;
        }

        private float S(float value) => value * _scale;
        private int Si(float value) => Mathf.RoundToInt(value * _scale);

        public void Build()
        {
            Image rowBg = gameObject.AddComponent<Image>();
            rowBg.color = new Color(0.16f, 0.17f, 0.2f, 0.95f);

            HorizontalLayoutGroup layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(Si(12f), Si(12f), Si(10f), Si(10f));
            layout.spacing = Si(12f);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            LayoutElement rowLayout = gameObject.AddComponent<LayoutElement>();
            rowLayout.minHeight = S(_compact ? 88f : 110f);
            rowLayout.preferredHeight = S(_compact ? 88f : 110f);

            CreatePortraitColumn();
            CreateDetailsColumn();
            CreateControlsColumn();
        }

        public void Bind(PetController pet)
        {
            _pet = pet;
            RefreshFromPet();
            HookEvents(true);
        }

        private void Update()
        {
            if (_pet != null)
                UpdateStatusText();
        }

        private void OnDestroy()
        {
            HookEvents(false);
        }

        public void RefreshFromPet()
        {
            if (_pet == null) return;

            _suppressEvents = true;

            if (_nameField != null)
                _nameField.text = _pet.DisplayName;

            if (_activeToggle != null)
                _activeToggle.isOn = _pet.CompanionActive;

            if (_followToggle != null)
                _followToggle.isOn = _pet.FollowEnabled;

            if (_wanderToggle != null)
                _wanderToggle.isOn = _pet.WanderEnabled;

            if (_fetchToggle != null)
                _fetchToggle.isOn = _pet.FetchEnabled;

            UpdateStatusText();
            _suppressEvents = false;
        }

        private void CreatePortraitColumn()
        {
            float portraitSize = S(_compact ? 72f : 84f);
            GameObject column = CreateChild("PortraitColumn", new Vector2(portraitSize, portraitSize));
            LayoutElement layout = column.AddComponent<LayoutElement>();
            layout.minWidth = portraitSize;
            layout.preferredWidth = portraitSize;

            Image portrait = column.AddComponent<Image>();
            portrait.color = new Color(0.24f, 0.25f, 0.28f, 1f);
        }

        private void CreateDetailsColumn()
        {
            GameObject column = CreateChild("DetailsColumn", Vector2.zero);
            LayoutElement layout = column.AddComponent<LayoutElement>();
            layout.flexibleWidth = 1;
            layout.minWidth = S(_compact ? 140f : 220f);

            VerticalLayoutGroup vLayout = column.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = Si(6f);
            vLayout.childAlignment = TextAnchor.UpperLeft;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;

            _nameField = CreateInputField(column.transform, "PetNameField", "Pet Name");
            _statusText = CreateLabel(column.transform, "StatusText", "Status: Following", S(16f), FontStyles.Normal);
        }

        private void CreateControlsColumn()
        {
            float columnWidth = S(_compact ? 190f : 260f);
            GameObject column = CreateChild("ControlsColumn", new Vector2(columnWidth, 0f));
            LayoutElement layout = column.AddComponent<LayoutElement>();
            layout.minWidth = columnWidth;
            layout.preferredWidth = columnWidth;

            VerticalLayoutGroup vLayout = column.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = Si(4f);
            vLayout.childAlignment = TextAnchor.UpperLeft;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = true;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;

            _activeToggle = CreateToggle(column.transform, "ActiveToggle", "Active");
            _followToggle = CreateToggle(column.transform, "FollowToggle", "Follow");
            _wanderToggle = CreateToggle(column.transform, "WanderToggle", "Wander");
            _fetchToggle = CreateToggle(column.transform, "FetchToggle", "Fetch Items");
            _callButton = CreateButton(column.transform, "CallButton", "Call Here");
        }

        private void HookEvents(bool hook)
        {
            if (_nameField != null)
            {
                _nameField.onEndEdit.RemoveListener(OnNameEdited);
                if (hook) _nameField.onEndEdit.AddListener(OnNameEdited);
            }

            if (_activeToggle != null)
            {
                _activeToggle.onValueChanged.RemoveListener(OnActiveChanged);
                if (hook) _activeToggle.onValueChanged.AddListener(OnActiveChanged);
            }

            if (_followToggle != null)
            {
                _followToggle.onValueChanged.RemoveListener(OnFollowChanged);
                if (hook) _followToggle.onValueChanged.AddListener(OnFollowChanged);
            }

            if (_wanderToggle != null)
            {
                _wanderToggle.onValueChanged.RemoveListener(OnWanderChanged);
                if (hook) _wanderToggle.onValueChanged.AddListener(OnWanderChanged);
            }

            if (_fetchToggle != null)
            {
                _fetchToggle.onValueChanged.RemoveListener(OnFetchChanged);
                if (hook) _fetchToggle.onValueChanged.AddListener(OnFetchChanged);
            }

            if (_callButton != null)
            {
                _callButton.onClick.RemoveListener(OnCallClicked);
                if (hook) _callButton.onClick.AddListener(OnCallClicked);
            }
        }

        private void OnNameEdited(string value)
        {
            if (_suppressEvents || _pet == null) return;
            _pet.DisplayName = string.IsNullOrWhiteSpace(value) ? _pet.DefaultDisplayName : value.Trim();
            UpdateStatusText();
            PetManager.Instance?.NotifyPetChanged();
        }

        private void OnActiveChanged(bool value)
        {
            if (_suppressEvents || _pet == null) return;
            _pet.CompanionActive = value;
            UpdateStatusText();
        }

        private void OnFollowChanged(bool value)
        {
            if (_suppressEvents || _pet == null) return;
            _pet.FollowEnabled = value;
            UpdateStatusText();
        }

        private void OnWanderChanged(bool value)
        {
            if (_suppressEvents || _pet == null) return;
            _pet.WanderEnabled = value;
            UpdateStatusText();
        }

        private void OnFetchChanged(bool value)
        {
            if (_suppressEvents || _pet == null) return;
            _pet.FetchEnabled = value;
            UpdateStatusText();
        }

        private void OnCallClicked()
        {
            _pet?.CallToOwner();
            UpdateStatusText();
        }

        private void UpdateStatusText()
        {
            if (_statusText == null || _pet == null) return;
            _statusText.text = _pet.CompanionActive ? $"Status: {_pet.CurrentBehaviorLabel}" : "Status: Dismissed";
        }

        private GameObject CreateChild(string name, Vector2 size)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(transform, false);
            RectTransform rt = child.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            return child;
        }

        private TextMeshProUGUI CreateLabel(Transform parent, string name, string text, float fontSize, FontStyles style)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            TextMeshProUGUI label = obj.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            TmpUiHelper.ApplyDefaultFont(label);
            LayoutElement layout = obj.AddComponent<LayoutElement>();
            layout.minHeight = S(fontSize + 8f);
            return label;
        }

        private TMP_InputField CreateInputField(Transform parent, string name, string placeholder)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);

            Image bg = obj.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.13f, 0.16f, 1f);

            TMP_InputField input = obj.AddComponent<TMP_InputField>();

            GameObject textArea = new GameObject("Text Area", typeof(RectTransform));
            textArea.transform.SetParent(obj.transform, false);
            RectTransform textAreaRt = textArea.GetComponent<RectTransform>();
            textAreaRt.anchorMin = Vector2.zero;
            textAreaRt.anchorMax = Vector2.one;
            textAreaRt.offsetMin = new Vector2(S(10f), S(4f));
            textAreaRt.offsetMax = new Vector2(S(-10f), S(-4f));

            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(textArea.transform, false);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.fontSize = S(20f);
            text.color = Color.white;
            TmpUiHelper.ApplyDefaultFont(text);

            GameObject placeholderObj = new GameObject("Placeholder", typeof(RectTransform));
            placeholderObj.transform.SetParent(textArea.transform, false);
            TextMeshProUGUI placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholderText.text = placeholder;
            placeholderText.fontSize = S(20f);
            placeholderText.color = new Color(1f, 1f, 1f, 0.35f);
            TmpUiHelper.ApplyDefaultFont(placeholderText);

            input.textViewport = textAreaRt;
            input.textComponent = text;
            input.placeholder = placeholderText;

            LayoutElement layout = obj.AddComponent<LayoutElement>();
            layout.minHeight = S(34f);
            layout.preferredHeight = S(34f);

            return input;
        }

        private Toggle CreateToggle(Transform parent, string name, string label)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);

            Toggle toggle = obj.AddComponent<Toggle>();

            GameObject bgObj = new GameObject("Background", typeof(RectTransform));
            bgObj.transform.SetParent(obj.transform, false);
            Image bg = bgObj.AddComponent<Image>();
            bg.color = new Color(0.24f, 0.25f, 0.28f, 1f);
            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.sizeDelta = new Vector2(S(20f), S(20f));
            bgRt.anchorMin = new Vector2(0, 0.5f);
            bgRt.anchorMax = new Vector2(0, 0.5f);
            bgRt.pivot = new Vector2(0, 0.5f);
            bgRt.anchoredPosition = Vector2.zero;

            GameObject checkObj = new GameObject("Checkmark", typeof(RectTransform));
            checkObj.transform.SetParent(bgObj.transform, false);
            Image check = checkObj.AddComponent<Image>();
            check.color = new Color(0.35f, 0.85f, 0.45f, 1f);
            RectTransform checkRt = checkObj.GetComponent<RectTransform>();
            checkRt.anchorMin = Vector2.zero;
            checkRt.anchorMax = Vector2.one;
            checkRt.offsetMin = new Vector2(S(4f), S(4f));
            checkRt.offsetMax = new Vector2(S(-4f), S(-4f));

            GameObject labelObj = new GameObject("Label", typeof(RectTransform));
            labelObj.transform.SetParent(obj.transform, false);
            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = S(16f);
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            TmpUiHelper.ApplyDefaultFont(labelText);
            RectTransform labelRt = labelObj.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(S(28f), 0f);
            labelRt.offsetMax = Vector2.zero;

            toggle.targetGraphic = bg;
            toggle.graphic = check;
            toggle.isOn = true;

            LayoutElement layout = obj.AddComponent<LayoutElement>();
            layout.minHeight = S(24f);
            layout.preferredHeight = S(24f);

            return toggle;
        }

        private Button CreateButton(Transform parent, string name, string label)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);

            Image bg = obj.AddComponent<Image>();
            bg.color = new Color(0.22f, 0.35f, 0.55f, 1f);
            Button button = obj.AddComponent<Button>();

            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(obj.transform, false);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 16;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            TmpUiHelper.ApplyDefaultFont(text);
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            LayoutElement layout = obj.AddComponent<LayoutElement>();
            layout.minHeight = S(30f);
            layout.preferredHeight = S(30f);

            return button;
        }
    }
}
