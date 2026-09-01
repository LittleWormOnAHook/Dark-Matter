using Project.Pet;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// UITK pet context menu + hover tooltip. Forwards PetContextMenu / PetHoverTooltip Show/Hide/HideAny.
    /// DMUiToolkit 0901-finish
    /// </summary>
    [DefaultExecutionOrder(-368)]
    [DisallowMultipleComponent]
    public class DMUiToolkitPetChrome : MonoBehaviour
    {
        private static DMUiToolkitPetChrome instance;

        private UIDocument document;
        private VisualElement root;
        private VisualElement dismiss;
        private VisualElement menu;
        private Button assignButton;
        private Button clearButton;
        private VisualElement tooltip;
        private Label tipTitle;
        private Label tipBody;
        private bool bound;
        private bool wired;
        private bool menuOpen;
        private bool tipOpen;
        private PetController activePet;
        private int openedOnFrame = -1;

        public static bool IsMenuOpen => instance != null && instance.menuOpen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying || !DMUiToolkitConfig.IsEnabled)
                return;

            EnsureHost();
        }

        public static DMUiToolkitPetChrome EnsureHost()
        {
            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitOverlayDocument.Ensure(
                DMUiToolkitOverlayDocument.PetChromeName,
                DMUiToolkitOverlayDocument.PetChromeUxml,
                DMUiToolkitOverlayDocument.PetChromeUss,
                DMUiToolkitOverlayDocument.PetChromeSort);
            if (doc == null)
                return null;

            DMUiToolkitPetChrome host = doc.GetComponent<DMUiToolkitPetChrome>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitPetChrome>();

            host.document = doc;
            host.BindTree();
            return host;
        }

        public static bool TryShowMenu(PetController pet, Vector2 screenPosition)
        {
            if (!DMUiToolkitHud.IsDriving)
                return false;

            if (pet == null || !pet.IsOwned)
                return false;

            DMUiToolkitPetChrome host = EnsureHost();
            if (host == null)
                return false;

            host.ShowMenuInternal(pet, screenPosition);
            return true;
        }

        public static bool TryShowTooltip(PetController pet, Vector2 screenPosition)
        {
            if (!DMUiToolkitHud.IsDriving)
                return false;

            DMUiToolkitPetChrome host = EnsureHost();
            if (host == null)
                return false;

            host.ShowTipInternal(pet, screenPosition);
            return true;
        }

        public static void HideMenu()
        {
            instance?.HideMenuInternal();
        }

        public static void HideTooltip()
        {
            instance?.HideTipInternal();
        }

        public static void HideAny()
        {
            instance?.HideMenuInternal();
            instance?.HideTipInternal();
        }

        private void Awake()
        {
            instance = this;
            if (document == null)
                document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            instance = this;
            BindTree();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void Update()
        {
            if (!menuOpen)
                return;

            if (UiEscapeGate.TryConsumeEscape())
            {
                HideMenuInternal();
                return;
            }

            if (Time.frameCount == openedOnFrame)
                return;

            if (UnityEngine.InputSystem.Mouse.current != null
                && UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame)
                HideMenuInternal();
        }

        private void LateUpdate()
        {
            if (!bound)
                BindTree();

            if (menuOpen || tipOpen)
                HideUgui();
        }

        internal void BindTree()
        {
            if (document == null)
                document = GetComponent<UIDocument>();
            if (document == null)
                return;

            VisualElement tree = document.rootVisualElement;
            if (tree == null)
                return;

            root = tree.Q<VisualElement>("pet-root") ?? tree;
            dismiss = tree.Q<VisualElement>("pet-dismiss");
            menu = tree.Q<VisualElement>("pet-menu");
            assignButton = tree.Q<Button>("pet-assign");
            clearButton = tree.Q<Button>("pet-clear");
            tooltip = tree.Q<VisualElement>("pet-tooltip");
            tipTitle = tree.Q<Label>("pet-tip-title");
            tipBody = tree.Q<Label>("pet-tip-body");
            Wire();
            if (!menuOpen && !tipOpen)
                DMUiToolkitOverlayDocument.SetShown(root, false);
            DMUiToolkitOverlayDocument.SetShown(menu, menuOpen);
            DMUiToolkitOverlayDocument.SetShown(dismiss, menuOpen);
            DMUiToolkitOverlayDocument.SetShown(tooltip, tipOpen);
            bound = root != null;
        }

        private void Wire()
        {
            if (wired)
                return;

            if (dismiss != null)
                dismiss.RegisterCallback<ClickEvent>(_ => HideMenuInternal());
            if (assignButton != null)
                assignButton.clicked += AssignActive;
            if (clearButton != null)
                clearButton.clicked += ClearActive;
            wired = true;
        }

        private void ShowMenuInternal(PetController pet, Vector2 screenPosition)
        {
            BindTree();
            HideTipInternal();
            activePet = pet;
            openedOnFrame = Time.frameCount;
            menuOpen = true;
            DMUiToolkitOverlayDocument.SetShown(root, true);
            DMUiToolkitOverlayDocument.SetShown(dismiss, true);
            DMUiToolkitOverlayDocument.SetShown(menu, true);

            bool assigned = PetManager.Instance != null && PetManager.Instance.ToolbarPet == pet;
            DMUiToolkitOverlayDocument.SetShown(assignButton, !assigned);
            DMUiToolkitOverlayDocument.SetShown(clearButton, assigned);
            DMUiToolkitOverlayDocument.PositionAtScreen(menu, screenPosition);
        }

        private void HideMenuInternal()
        {
            menuOpen = false;
            activePet = null;
            DMUiToolkitOverlayDocument.SetShown(menu, false);
            DMUiToolkitOverlayDocument.SetShown(dismiss, false);
            if (!tipOpen)
                DMUiToolkitOverlayDocument.SetShown(root, false);
        }

        private void ShowTipInternal(PetController pet, Vector2 screenPosition)
        {
            BindTree();
            if (pet == null || tipTitle == null)
                return;

            tipOpen = true;
            DMUiToolkitOverlayDocument.SetShown(root, true);
            DMUiToolkitOverlayDocument.SetShown(tooltip, true);
            tipTitle.text = pet.DisplayName;
            bool assigned = PetManager.Instance != null && PetManager.Instance.ToolbarPet == pet;
            string status = assigned ? "Active companion" : pet.IsOwned ? "Owned" : "Wild";
            if (tipBody != null)
            {
                tipBody.text = string.IsNullOrWhiteSpace(pet.Description)
                    ? status
                    : $"{pet.Description}\n\n{status}";
            }

            Vector2 offset = screenPosition + new Vector2(18f, 18f);
            DMUiToolkitOverlayDocument.PositionAtScreen(tooltip, offset);
        }

        private void HideTipInternal()
        {
            tipOpen = false;
            DMUiToolkitOverlayDocument.SetShown(tooltip, false);
            if (!menuOpen)
                DMUiToolkitOverlayDocument.SetShown(root, false);
        }

        private void AssignActive()
        {
            if (activePet != null && PetManager.Instance != null)
                PetManager.Instance.TryAssignToolbarPet(activePet);
            HideMenuInternal();
        }

        private void ClearActive()
        {
            if (PetManager.Instance != null && activePet != null && PetManager.Instance.ToolbarPet == activePet)
                PetManager.Instance.ClearToolbarPet();
            HideMenuInternal();
        }

        private static void HideUgui()
        {
            PetContextMenu menuUi = Object.FindAnyObjectByType<PetContextMenu>(FindObjectsInactive.Include);
            if (menuUi != null)
                DMUiToolkitOverlayDocument.HideGameObject(menuUi.gameObject);

            PetHoverTooltip tipUi = Object.FindAnyObjectByType<PetHoverTooltip>(FindObjectsInactive.Include);
            if (tipUi != null)
                DMUiToolkitOverlayDocument.HideGameObject(tipUi.gameObject);
        }
    }
}
