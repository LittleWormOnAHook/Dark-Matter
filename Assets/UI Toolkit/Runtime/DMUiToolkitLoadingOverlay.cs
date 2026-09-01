using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Toolkit port of the Loading Genesis overlay visuals.
    /// Sequence, camera gate, audio, and early blackout stay on LoadingOverlayController.
    /// uGUI overlay code is not deleted — this is the dual-run visual path.
    /// </summary>
    public static class DMUiToolkitLoadingOverlay
    {
        private const int FlyingStarCount = 48;
        private const float StarTravelRadius = 1180f;
        private const float BlackholeApproachScale = 0.02f;

        private struct FlyingStar
        {
            public VisualElement Element;
            public Vector2 Direction;
            public float Depth;
            public float Speed;
            public float BaseSize;
            public float TwinklePhase;
            public bool Accent;
        }

        private static bool showing;
        private static UIDocument document;
        private static VisualElement root;
        private static VisualElement veil;
        private static VisualElement content;
        private static VisualElement starsRoot;
        private static VisualElement blackhole;
        private static VisualElement progressFill;
        private static Label statusLabel;
        private static Label percentLabel;
        private static FlyingStar[] flyingStars;
        private static int shownPercent = -1;
        private static Vector2 blackholeBaseSize = new Vector2(150f, 150f);

        public static bool IsShowing => showing;

        public static bool IsVeilOpaque
        {
            get
            {
                if (veil == null)
                    return showing;

                float opacity = veil.resolvedStyle.opacity;
                return opacity > 0.99f;
            }
        }

        public static void Show(string statusText)
        {
            if (!DMUiToolkitBootstrap.EnsureExists())
                return;

            DMUiToolkitBootstrap bootstrap = DMUiToolkitBootstrap.Instance;
            if (bootstrap == null)
                return;

            bootstrap.EnsureDocuments();
            document = bootstrap.LoadingDocument;
            if (document == null)
                return;

            GameObject host = document.gameObject;
            host.SetActive(true);
            document.enabled = true;

            root = document.rootVisualElement;
            if (root == null)
            {
                BuildFallbackTree(document);
                root = document.rootVisualElement;
            }

            if (root == null)
            {
                Debug.LogWarning(DMUiToolkitConfig.LogStamp + " loading overlay UXML failed; uGUI path should still run.");
                showing = false;
                return;
            }

            Bind(root);
            if (content == null || progressFill == null)
            {
                BuildFallbackTree(document);
                root = document.rootVisualElement;
                Bind(root);
            }

            DMUiToolkitBootstrap.ApplyTheme(document, DMUiToolkitBootstrap.ThemeUssPath);
            DMUiToolkitBootstrap.ApplyTheme(document, DMUiToolkitBootstrap.LoadingUssPath);
            RepairRuntimeLayout();

            if (statusLabel != null)
                statusLabel.text = string.IsNullOrEmpty(statusText) ? "Loading Genesis..." : statusText;

            SetContentOpacity(1f);
            SetVeilOpacity(1f);
            SetProgress(0f);
            shownPercent = -1;
            SetPercent(0);
            BuildStars();
            showing = true;
            DMUiToolkitBootstrap.Stamp("loading overlay active (panel-sibling)");
        }

        public static void Hide()
        {
            showing = false;
            flyingStars = null;

            CollapseOverlayTree(document != null ? document.rootVisualElement : root);
            ReleaseLoadingDocument(document);

            DMUiToolkitBootstrap bootstrap = DMUiToolkitBootstrap.Instance;
            if (bootstrap != null)
            {
                UIDocument fromBootstrap = bootstrap.LoadingDocument;
                if (fromBootstrap != null && fromBootstrap != document)
                    ReleaseLoadingDocument(fromBootstrap);

                // Shell stays alive and transparent. Never deactivate UITK_Root / MainCanvas.
                UIDocument shell = bootstrap.ShellDocument;
                if (shell != null && shell.panelSettings != null)
                {
                    shell.panelSettings.clearColor = false;
                    shell.panelSettings.colorClearValue = Color.clear;
                }

                VisualElement shellRoot = shell != null ? shell.rootVisualElement : null;
                VisualElement stray = shellRoot != null ? shellRoot.Q("loading-root") : null;
                if (stray != null)
                {
                    stray.style.display = DisplayStyle.None;
                    stray.style.backgroundColor = Color.clear;
                    stray.pickingMode = PickingMode.Ignore;
                }
            }

            DMUiToolkitBootstrap.ReleaseLoadingClearColor();
            DMUiToolkitBootstrap.DeactivateLoadingHosts();

            root = null;
            veil = null;
            content = null;
            starsRoot = null;
            blackhole = null;
            progressFill = null;
            statusLabel = null;
            percentLabel = null;
            document = null;

            DMUiToolkitBootstrap.Stamp("loading overlay hidden after load");
        }

        /// <summary>
        /// Drop PanelSettings black-clear so the veil fade can actually reveal the menu/world.
        /// Call under the still-opaque veil after cameras restore.
        /// </summary>
        public static void BeginReveal()
        {
            ReleasePanelClear(document);
            DMUiToolkitBootstrap.ReleaseLoadingClearColor();

            VisualElement panelRoot = document != null ? document.rootVisualElement : null;
            if (panelRoot != null)
                panelRoot.style.backgroundColor = Color.clear;
            if (root != null)
                root.style.backgroundColor = Color.clear;
        }

        private static void CollapseOverlayTree(VisualElement tree)
        {
            if (tree == null)
                return;

            tree.style.opacity = 0f;
            tree.style.backgroundColor = Color.clear;
            tree.style.display = DisplayStyle.None;
            tree.pickingMode = PickingMode.Ignore;

            VisualElement loadingRoot = tree.Q("loading-root");
            if (loadingRoot != null && loadingRoot != tree)
            {
                loadingRoot.style.opacity = 0f;
                loadingRoot.style.backgroundColor = Color.clear;
                loadingRoot.style.display = DisplayStyle.None;
                loadingRoot.pickingMode = PickingMode.Ignore;
            }
        }

        private static void ReleaseLoadingDocument(UIDocument doc)
        {
            if (doc == null)
                return;

            ReleasePanelClear(doc);
            CollapseOverlayTree(doc.rootVisualElement);
            doc.panelSettings = null;
            doc.enabled = false;

            GameObject host = doc.gameObject;
            if (host == null)
                return;
            if (host.name == DMUiToolkitBootstrap.RootName
                || host.name == "MainCanvas"
                || host.name == "OpticsOverlayCanvas")
                return;

            host.SetActive(false);
        }

        private static void ReleasePanelClear(UIDocument doc)
        {
            if (doc == null || doc.panelSettings == null)
                return;

            doc.panelSettings.clearColor = false;
            doc.panelSettings.colorClearValue = Color.clear;
        }

        public static void SetProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            if (progressFill == null)
                return;

            progressFill.style.width = Length.Percent(progress * 100f);
        }

        public static void SetPercent(int percent)
        {
            if (percentLabel == null)
                return;

            percentLabel.text = percent + "%";
        }

        public static void SetContentOpacity(float opacity)
        {
            if (content != null)
                content.style.opacity = Mathf.Clamp01(opacity);
        }

        public static void SetVeilOpacity(float opacity)
        {
            if (veil != null)
                veil.style.opacity = Mathf.Clamp01(opacity);
        }

        public static void ForceOpaque()
        {
            SetContentOpacity(1f);
            SetVeilOpacity(1f);
        }

        public static void Tick(float unscaledDelta)
        {
            if (!showing)
                return;

            UpdateFlyingStars(unscaledDelta);

            if (blackhole != null)
            {
                float approach = 1f + BlackholeApproachScale
                    * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 0.55f));
                blackhole.style.width = blackholeBaseSize.x * approach;
                blackhole.style.height = blackholeBaseSize.y * approach;
            }
        }

        private static void Bind(VisualElement tree)
        {
            if (tree == null)
                return;

            root = tree.Q("loading-root") ?? tree;
            veil = tree.Q("veil");
            content = tree.Q("content");
            starsRoot = tree.Q("stars");
            blackhole = tree.Q("blackhole");
            progressFill = tree.Q("progress-fill");
            statusLabel = tree.Q<Label>("status");
            percentLabel = tree.Q<Label>("percent");
        }

        /// <summary>
        /// Visibility only while showing. USS / UXML own fonts, colors, and element layout.
        /// Hide() is what removes the overlay after load.
        /// </summary>
        private static void RepairRuntimeLayout()
        {
            VisualElement panelRoot = document != null ? document.rootVisualElement : null;
            if (panelRoot != null)
            {
                panelRoot.style.flexGrow = 1;
                panelRoot.style.width = Length.Percent(100);
                panelRoot.style.height = Length.Percent(100);
                panelRoot.style.backgroundColor = Color.black;
                panelRoot.style.display = DisplayStyle.Flex;
                panelRoot.style.opacity = 1f;
            }

            StretchFull(root, Color.black);
            StretchFull(veil, Color.black);
            StretchFull(content, null);
            StretchFull(starsRoot, null);

            VisualElement space = root != null ? root.Q("space") : null;
            StretchFull(space, new Color(3f / 255f, 3f / 255f, 5f / 255f, 1f));

            if (root != null)
            {
                root.style.display = DisplayStyle.Flex;
                root.style.opacity = 1f;
            }

            if (veil != null)
            {
                veil.style.display = DisplayStyle.Flex;
                veil.style.opacity = 1f;
            }

            if (content != null)
            {
                content.style.display = DisplayStyle.Flex;
                content.style.opacity = 1f;
            }

            if (blackhole == null && content != null)
            {
                blackhole = new VisualElement { name = "blackhole" };
                blackhole.AddToClassList("dmg-blackhole");
                blackhole.pickingMode = PickingMode.Ignore;
                Label title = root != null ? root.Q<Label>("title") : null;
                if (title != null)
                    content.Insert(content.IndexOf(title), blackhole);
                else
                    content.Add(blackhole);
            }

            EnsureBlackholeSprite();
        }

        private static void EnsureBlackholeSprite()
        {
            if (blackhole == null)
                return;

            Sprite holeSprite = Resources.Load<Sprite>(LoadingOverlayController.BlackholeResourcePath);
            if (holeSprite != null)
            {
                blackhole.style.backgroundImage = new StyleBackground(holeSprite);
                return;
            }

            Texture2D holeTexture = Resources.Load<Texture2D>(LoadingOverlayController.BlackholeResourcePath);
            if (holeTexture != null)
                blackhole.style.backgroundImage = new StyleBackground(holeTexture);
        }

        private static void StretchFull(VisualElement element, Color? background)
        {
            if (element == null)
                return;

            element.style.position = Position.Absolute;
            element.style.left = 0;
            element.style.top = 0;
            element.style.right = 0;
            element.style.bottom = 0;
            element.style.width = Length.Percent(100);
            element.style.height = Length.Percent(100);
            if (background.HasValue)
                element.style.backgroundColor = background.Value;
        }

        private static void BuildFallbackTree(UIDocument doc)
        {
            if (doc == null)
                return;

            VisualElement host = doc.rootVisualElement;
            if (host == null)
                return;

            host.Clear();

            VisualElement loadingRoot = new VisualElement { name = "loading-root" };
            loadingRoot.AddToClassList("dmg-loading-root");
            loadingRoot.pickingMode = PickingMode.Position;

            VisualElement veilElement = new VisualElement { name = "veil" };
            veilElement.AddToClassList("dmg-veil");
            veilElement.pickingMode = PickingMode.Ignore;

            VisualElement contentElement = new VisualElement { name = "content" };
            contentElement.AddToClassList("dmg-loading-content");
            contentElement.pickingMode = PickingMode.Ignore;

            VisualElement space = new VisualElement { name = "space" };
            space.AddToClassList("dmg-space");
            space.pickingMode = PickingMode.Ignore;

            VisualElement stars = new VisualElement { name = "stars" };
            stars.AddToClassList("dmg-stars");
            stars.pickingMode = PickingMode.Ignore;

            VisualElement hole = new VisualElement { name = "blackhole" };
            hole.AddToClassList("dmg-blackhole");
            hole.pickingMode = PickingMode.Ignore;

            Label title = new Label("DARK MATTER : GENESIS") { name = "title" };
            title.AddToClassList("dmg-title");
            title.pickingMode = PickingMode.Ignore;

            VisualElement progressBlock = new VisualElement { name = "progress-block" };
            progressBlock.AddToClassList("dmg-progress-block");
            progressBlock.pickingMode = PickingMode.Ignore;

            Label status = new Label("Loading Genesis...") { name = "status" };
            status.AddToClassList("dmg-status");
            status.pickingMode = PickingMode.Ignore;

            Label percent = new Label("0%") { name = "percent" };
            percent.AddToClassList("dmg-percent");
            percent.pickingMode = PickingMode.Ignore;

            VisualElement track = new VisualElement { name = "progress-track" };
            track.AddToClassList("dmg-progress-track");
            track.pickingMode = PickingMode.Ignore;

            VisualElement fill = new VisualElement { name = "progress-fill" };
            fill.AddToClassList("dmg-progress-fill");
            fill.pickingMode = PickingMode.Ignore;

            track.Add(fill);
            progressBlock.Add(status);
            progressBlock.Add(percent);
            progressBlock.Add(track);

            contentElement.Add(space);
            contentElement.Add(stars);
            contentElement.Add(hole);
            contentElement.Add(title);
            contentElement.Add(progressBlock);

            loadingRoot.Add(veilElement);
            loadingRoot.Add(contentElement);
            host.Add(loadingRoot);

            DMUiToolkitBootstrap.ApplyTheme(doc, DMUiToolkitBootstrap.ThemeUssPath);
            DMUiToolkitBootstrap.ApplyTheme(doc, DMUiToolkitBootstrap.LoadingUssPath);
        }

        private static void BuildStars()
        {
            if (starsRoot == null)
                return;

            starsRoot.Clear();
            flyingStars = new FlyingStar[FlyingStarCount];
            for (int i = 0; i < FlyingStarCount; i++)
            {
                VisualElement star = new VisualElement { name = "star-" + i };
                star.AddToClassList("dmg-star");
                star.pickingMode = PickingMode.Ignore;
                starsRoot.Add(star);

                FlyingStar data = new FlyingStar { Element = star };
                RespawnFlyingStar(ref data, randomizeDepth: true);
                if (data.Accent)
                    star.AddToClassList("dmg-star--accent");
                flyingStars[i] = data;
            }

            UpdateFlyingStars(0f);
        }

        private static void UpdateFlyingStars(float unscaledDelta)
        {
            if (flyingStars == null || flyingStars.Length == 0 || starsRoot == null)
                return;

            float width = starsRoot.resolvedStyle.width;
            float height = starsRoot.resolvedStyle.height;
            if (width <= 1f || height <= 1f)
                return;

            float dt = Mathf.Max(0f, unscaledDelta);
            Vector2 center = new Vector2(width * 0.5f, height * 0.5f);

            for (int i = 0; i < flyingStars.Length; i++)
            {
                FlyingStar star = flyingStars[i];
                if (star.Element == null)
                    continue;

                star.Depth += star.Speed * dt;
                if (star.Depth >= 1f)
                    RespawnFlyingStar(ref star, randomizeDepth: false);

                float travel = star.Depth * star.Depth;
                float radius = Mathf.Lerp(36f, StarTravelRadius, travel);
                // uGUI +y is up; Toolkit +y is down — flip Y so the field still expands from center.
                Vector2 pos = center + new Vector2(star.Direction.x, -star.Direction.y) * radius;
                float size = star.BaseSize * Mathf.Lerp(0.55f, 3.4f, travel);
                float twinkle = 0.72f + 0.28f * Mathf.Sin(Time.unscaledTime * (1.6f + star.TwinklePhase) + star.TwinklePhase);
                float alpha = Mathf.Clamp01(0.18f + travel * 0.85f) * twinkle;

                star.Element.style.left = pos.x - size * 0.5f;
                star.Element.style.top = pos.y - size * 0.5f;
                star.Element.style.width = size;
                star.Element.style.height = size;
                star.Element.style.opacity = alpha;
                if (star.Accent)
                    star.Element.EnableInClassList("dmg-star--accent", true);

                flyingStars[i] = star;
            }
        }

        private static void RespawnFlyingStar(ref FlyingStar star, bool randomizeDepth)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float ellipticity = Random.Range(0.82f, 1.18f);
            star.Direction = new Vector2(Mathf.Cos(angle) * ellipticity, Mathf.Sin(angle) / ellipticity).normalized;
            star.Depth = randomizeDepth ? Random.Range(0f, 0.82f) : Random.Range(0f, 0.08f);
            star.Speed = Random.Range(0.085f, 0.22f);
            star.BaseSize = Random.Range(1.4f, 3.6f);
            star.TwinklePhase = Random.Range(0f, Mathf.PI * 2f);
            star.Accent = Random.value < 0.08f;
        }

        public static IEnumerator FadeContent(float from, float to, float durationSeconds)
        {
            yield return Fade(setContent: true, from, to, durationSeconds);
        }

        public static IEnumerator FadeVeil(float from, float to, float durationSeconds)
        {
            yield return Fade(setContent: false, from, to, durationSeconds);
        }

        private static IEnumerator Fade(bool setContent, float from, float to, float durationSeconds)
        {
            float duration = Mathf.Max(0.05f, durationSeconds);
            float startedAt = Time.realtimeSinceStartup;
            if (setContent)
                SetContentOpacity(from);
            else
                SetVeilOpacity(from);

            while (true)
            {
                float elapsed = Time.realtimeSinceStartup - startedAt;
                float t = Mathf.Clamp01(elapsed / duration);
                float alpha = Mathf.Lerp(from, to, t);
                if (setContent)
                    SetContentOpacity(alpha);
                else
                    SetVeilOpacity(alpha);

                if (t >= 1f)
                    break;

                yield return null;
            }

            if (setContent)
                SetContentOpacity(to);
            else
                SetVeilOpacity(to);
        }
    }
}
