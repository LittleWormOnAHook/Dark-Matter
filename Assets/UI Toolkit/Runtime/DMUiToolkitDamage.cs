using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Screen-space UITK damage numbers tracking world positions.
    /// DMUiToolkit 0901-finish
    /// </summary>
    [DefaultExecutionOrder(-367)]
    [DisallowMultipleComponent]
    public class DMUiToolkitDamage : MonoBehaviour
    {
        private const float Lifetime = 1.4f;
        private const float FadeDuration = 0.45f;
        private const float FloatSpeed = 0.75f;

        private static DMUiToolkitDamage instance;

        private UIDocument document;
        private VisualElement root;
        private VisualElement layer;
        private bool bound;
        private readonly List<Popup> popups = new List<Popup>();

        private class Popup
        {
            public Label Label;
            public Vector3 World;
            public Vector3 Drift;
            public float Elapsed;
            public bool Crit;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying || !DMUiToolkitConfig.IsEnabled)
                return;

            EnsureHost();
        }

        public static DMUiToolkitDamage EnsureHost()
        {
            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitOverlayDocument.Ensure(
                DMUiToolkitOverlayDocument.DamageName,
                DMUiToolkitOverlayDocument.DamageUxml,
                DMUiToolkitOverlayDocument.DamageUss,
                DMUiToolkitOverlayDocument.DamageSort);
            if (doc == null)
                return null;

            DMUiToolkitDamage host = doc.GetComponent<DMUiToolkitDamage>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitDamage>();

            host.document = doc;
            host.BindTree();
            return host;
        }

        public static bool TrySpawn(float damage, Vector3 worldPosition, Color? color = null)
        {
            if (!DMUiToolkitHud.IsDriving)
                return false;

            if (damage <= 0f)
                return true;

            DMUiToolkitDamage host = EnsureHost();
            if (host == null)
                return false;

            host.SpawnInternal(damage, worldPosition, color);
            return true;
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

        private void LateUpdate()
        {
            if (!bound)
                BindTree();

            TickPopups();
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

            root = tree.Q<VisualElement>("damage-root") ?? tree;
            layer = tree.Q<VisualElement>("damage-layer") ?? root;
            bound = root != null;
        }

        private void SpawnInternal(float damage, Vector3 worldPosition, Color? color)
        {
            BindTree();
            if (layer == null)
                return;

            Label label = new Label(Mathf.RoundToInt(damage).ToString());
            label.AddToClassList("dmg-dmg-num");
            label.pickingMode = PickingMode.Ignore;
            bool crit = color.HasValue && color.Value.g > 0.5f;
            if (crit)
                label.AddToClassList("dmg-dmg-crit");
            if (color.HasValue)
                label.style.color = color.Value;
            layer.Add(label);

            popups.Add(new Popup
            {
                Label = label,
                World = worldPosition,
                Drift = new Vector3(Random.Range(-0.08f, 0.08f), 0f, Random.Range(-0.08f, 0.08f)),
                Elapsed = 0f,
                Crit = crit
            });
        }

        private void TickPopups()
        {
            Camera camera = Camera.main;
            for (int i = popups.Count - 1; i >= 0; i--)
            {
                Popup popup = popups[i];
                popup.Elapsed += Time.deltaTime;
                popup.World += Vector3.up * FloatSpeed * Time.deltaTime;

                if (popup.Elapsed >= Lifetime || popup.Label == null)
                {
                    popup.Label?.RemoveFromHierarchy();
                    popups.RemoveAt(i);
                    continue;
                }

                if (camera == null)
                    continue;

                Vector3 screen = camera.WorldToScreenPoint(popup.World + popup.Drift);
                if (screen.z <= 0f)
                {
                    popup.Label.style.opacity = 0f;
                    continue;
                }

                if (popup.Label.panel != null)
                {
                    Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(
                        popup.Label.panel,
                        new Vector2(screen.x, screen.y));
                    popup.Label.style.left = panelPos.x - 60f;
                    popup.Label.style.top = panelPos.y - 20f;
                }

                float fadeStart = Lifetime - FadeDuration;
                popup.Label.style.opacity = popup.Elapsed >= fadeStart
                    ? 1f - ((popup.Elapsed - fadeStart) / FadeDuration)
                    : 1f;

                float lifeT = Mathf.Clamp01(popup.Elapsed / Lifetime);
                float scale = Mathf.Lerp(0.85f, 1.15f, 1f - Mathf.Abs(lifeT - 0.18f));
                popup.Label.style.scale = new Scale(new Vector3(scale, scale, 1f));
            }
        }

        private static bool uguiHidden;

        private static void HideUgui()
        {
            if (!DMUiToolkitHud.IsDriving || uguiHidden)
                return;

            WorldFloatingDamageNumber[] world = Object.FindObjectsByType<WorldFloatingDamageNumber>(FindObjectsInactive.Include);
            for (int i = 0; i < world.Length; i++)
            {
                if (world[i] == null)
                    continue;
                Canvas canvas = world[i].GetComponent<Canvas>();
                if (canvas != null)
                    DMUiToolkitOverlayDocument.HideCanvas(canvas);
                else
                    DMUiToolkitOverlayDocument.HideGameObject(world[i].gameObject);
            }

            FloatingDamageNumber[] screen = Object.FindObjectsByType<FloatingDamageNumber>(FindObjectsInactive.Include);
            for (int i = 0; i < screen.Length; i++)
            {
                if (screen[i] != null)
                    DMUiToolkitOverlayDocument.HideGameObject(screen[i].gameObject);
            }

            uguiHidden = true;
        }
    }
}
