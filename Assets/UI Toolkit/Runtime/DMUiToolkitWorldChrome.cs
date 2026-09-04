using System.Collections.Generic;
using Project.AI;
using Project.Building;
using Project.Combat;
using Project.Companions;
using Project.Core;
using Project.Crafting;
using Project.Data;
using Project.Echoes;
using Project.Interaction;
using Project.Player;
using Project.Quests;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// World-to-screen proximity dots and per-NPC health bars on the Damage overlay.
    /// Dual-run: hides uGUI PickupProximityDotUI / WorldInteractionDotUI / FloatingTargetHealthBar chrome.
    /// </summary>
    [DefaultExecutionOrder(-366)]
    [DisallowMultipleComponent]
    public class DMUiToolkitWorldChrome : MonoBehaviour
    {
        private const float PickupConeFov = PickupProximityDotUI.PickupConeFovDegrees;
        /// <summary>Pickup stem length at lock-on / far planar range (world-up meters).</summary>
        private const float PickupStemFarMeters = 0.25f;
        /// <summary>Pickup stem length at near planar reach; tip-locked dot is largest here.</summary>
        private const float PickupStemNearMeters = 0.5f;
        /// <summary>Proximity reaches 1 (max stem + largest tip dot) at this planar XZ distance.</summary>
        private const float StemNearReachMeters = 0.5f;
        /// <summary>Fallback fixed stem for non-pickup interaction chrome (quest/craft/loot/etc).</summary>
        private const float InteractStemMeters = 0.75f;
        private const float DotSizeFarPx = 10f;
        private const float DotSizeNearPx = 22f;
        private const float InteractionScanInterval = 1f / 12f;
        private const int MaxDots = 24;
        private const int MaxBars = 16;

        private static DMUiToolkitWorldChrome instance;

        private UIDocument document;
        private VisualElement root;
        private VisualElement dotsLayer;
        private VisualElement barsLayer;
        private bool bound;
        private bool uguiHidden;
        private bool lastGameplayWant;
        private float nextInteractScan;
        private Camera worldCamera;
        private Transform playerTransform;
        private PlayerController cachedPlayer;

        private readonly List<VisualElement> dotPool = new List<VisualElement>();
        private readonly List<VisualElement> liveDots = new List<VisualElement>();
        private readonly List<VisualElement> barPool = new List<VisualElement>();
        private readonly List<VisualElement> liveBars = new List<VisualElement>();
        private readonly HashSet<FloatingTargetHealthBar> hiddenBars = new HashSet<FloatingTargetHealthBar>();
        private readonly List<WorldDot> pendingDots = new List<WorldDot>(32);
        private readonly List<WorldDot> cachedInteractDots = new List<WorldDot>(24);

        private struct WorldDot
        {
            /// <summary>Prefab-centered world anchor (line base / item bounds center).</summary>
            public Vector3 Anchor;
            public Color Color;
            public bool DrawStem;
            /// <summary>World-up stem length at proximity 0 (far / lock-on).</summary>
            public float StemMinHeight;
            /// <summary>World-up stem length at proximity 1 (planar near). Dot always on tip.</summary>
            public float StemMaxHeight;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying || !DMUiToolkitConfig.IsEnabled)
                return;
            EnsureHost();
        }

        public static DMUiToolkitWorldChrome EnsureHost()
        {
            if (instance != null)
                return instance;

            DMUiToolkitDamage.EnsureHost();
            GameObject host = DMUiToolkitOverlayDocument.FindNamed(DMUiToolkitOverlayDocument.DamageName);
            if (host == null)
                return null;

            DMUiToolkitWorldChrome chrome = host.GetComponent<DMUiToolkitWorldChrome>();
            if (chrome == null)
                chrome = host.AddComponent<DMUiToolkitWorldChrome>();

            chrome.document = host.GetComponent<UIDocument>();
            chrome.BindTree();
            return chrome;
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

            bool want = DMUiToolkitOverlayDocument.GameplayHudWanted();
            bool show = want && !GameplayHudVisibility.CinematicChromeHidden;
            if (show != lastGameplayWant)
            {
                lastGameplayWant = show;
                DMUiToolkitOverlayDocument.SetShown(root, show);
                DMUiToolkitOverlayDocument.SetShown(dotsLayer, show);
                DMUiToolkitOverlayDocument.SetShown(barsLayer, show);
            }

            if (!show)
            {
                DMUiToolkitOverlayDocument.SetShown(root, false);
                DMUiToolkitOverlayDocument.SetShown(dotsLayer, false);
                DMUiToolkitOverlayDocument.SetShown(barsLayer, false);
                RecycleDots(0);
                RecycleBars(0);
                if (want)
                    HideUguiCounterparts();
                return;
            }

            HideUguiCounterparts();
            CollectDots();
            PaintDots();
            PaintBars();
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
            dotsLayer = tree.Q<VisualElement>("world-dots-layer");
            barsLayer = tree.Q<VisualElement>("world-bars-layer");
            if (dotsLayer == null)
            {
                dotsLayer = new VisualElement { name = "world-dots-layer", pickingMode = PickingMode.Ignore };
                dotsLayer.AddToClassList("dmg-world-dots");
                root.Add(dotsLayer);
            }

            if (barsLayer == null)
            {
                barsLayer = new VisualElement { name = "world-bars-layer", pickingMode = PickingMode.Ignore };
                barsLayer.AddToClassList("dmg-world-bars");
                root.Add(barsLayer);
            }

            bound = root != null;
        }

        private void CollectDots()
        {
            if (!ResolvePlayer(out Transform player, out Camera camera))
            {
                pendingDots.Clear();
                cachedInteractDots.Clear();
                return;
            }

            PlayerController pc = cachedPlayer;
            if (pc != null && pc.BlocksCombatInput)
            {
                pendingDots.Clear();
                cachedInteractDots.Clear();
                return;
            }

            // Exclusive pickup stem tracks every frame from each pickup's own anchor.
            // Other interaction dots stay throttled.
            if (Time.unscaledTime >= nextInteractScan)
            {
                nextInteractScan = Time.unscaledTime + InteractionScanInterval;
                cachedInteractDots.Clear();
                CollectInteractionDots(player, cachedInteractDots);
            }

            pendingDots.Clear();
            CollectExclusivePickupDot(player, camera);
            for (int i = 0; i < cachedInteractDots.Count; i++)
                pendingDots.Add(cachedInteractDots[i]);
        }

        private void CollectExclusivePickupDot(Transform player, Camera camera)
        {
            float nearR = WorldUseController.MaxPickupDistance;
            float nearSqr = nearR * nearR;
            float halfCone = Mathf.Clamp(PickupConeFov, 1f, 179f) * 0.5f;
            Vector3 origin = camera != null ? camera.transform.position : player.position;
            Vector3 forward = camera != null ? camera.transform.forward : player.forward;

            float bestDist = float.MaxValue;
            Vector3 bestWorld = Vector3.zero;
            Color bestColor = Color.white;
            float bestStemMin = PickupStemFarMeters;
            float bestStemMax = PickupStemNearMeters;
            bool found = false;

            ItemPickup[] pickups = SceneComponentCache.GetAll<ItemPickup>(FindObjectsInactive.Exclude, refreshInterval: 0.05f);
            for (int i = 0; i < pickups.Length; i++)
            {
                ItemPickup pickup = pickups[i];
                if (pickup == null || !pickup.IsIndicatorAvailable || pickup.itemData == null)
                    continue;
                if (!WorldUseController.IsCollectiblePickup(pickup))
                    continue;
                Vector3 anchor = pickup.GetIndicatorWorldAnchor();
                if (!TryQualify(anchor, player.position, origin, forward, nearSqr, halfCone, out float dist))
                    continue;
                if (dist >= bestDist)
                    continue;
                bestDist = dist;
                bestWorld = anchor;
                bestColor = ProximityDotStyle.PickupColor(pickup.itemData.itemType);
                bestStemMin = pickup.IndicatorStemMinHeight;
                bestStemMax = pickup.IndicatorStemMaxHeight;
                found = true;
            }

            RecipePickup[] recipes = SceneComponentCache.GetAll<RecipePickup>(FindObjectsInactive.Exclude, refreshInterval: 0.05f);
            for (int i = 0; i < recipes.Length; i++)
            {
                RecipePickup recipe = recipes[i];
                if (recipe == null || !recipe.IsIndicatorAvailable)
                    continue;
                Vector3 anchor = recipe.GetIndicatorWorldAnchor();
                if (!TryQualify(anchor, player.position, origin, forward, nearSqr, halfCone, out float dist))
                    continue;
                if (dist >= bestDist)
                    continue;
                bestDist = dist;
                bestWorld = anchor;
                bestColor = ProximityDotStyle.RecipeColor;
                bestStemMin = recipe.IndicatorStemMinHeight;
                bestStemMax = recipe.IndicatorStemMaxHeight;
                found = true;
            }

            ResourceNode[] nodes = SceneComponentCache.GetAll<ResourceNode>(FindObjectsInactive.Exclude, refreshInterval: 0.05f);
            for (int i = 0; i < nodes.Length; i++)
            {
                ResourceNode node = nodes[i];
                if (node == null
                    || node.interactionMode != ResourceNodeInteractionMode.HoldHarvest
                    || node.resourceItem == null
                    || node.IsHoldActive)
                    continue;
                Vector3 pos = node.GetNodeCenter();
                if (!TryQualify(pos, player.position, origin, forward, nearSqr, halfCone, out float dist))
                    continue;
                if (dist >= bestDist)
                    continue;
                bestDist = dist;
                bestWorld = pos;
                bestColor = ProximityDotStyle.PickupColor(node.resourceItem.itemType);
                found = true;
            }

            if (found)
                pendingDots.Add(MakeStemDot(bestWorld, bestColor, bestStemMin, bestStemMax));
        }

        private static bool TryQualify(
            Vector3 world,
            Vector3 playerPos,
            Vector3 origin,
            Vector3 forward,
            float nearSqr,
            float halfCone,
            out float dist)
        {
            dist = 0f;
            float sqr = (world - playerPos).sqrMagnitude;
            if (sqr > nearSqr)
                return false;
            Vector3 toTarget = world - origin;
            if (toTarget.sqrMagnitude >= 0.0001f && Vector3.Angle(forward, toTarget) > halfCone)
                return false;
            dist = Mathf.Sqrt(sqr);
            return true;
        }

        private void CollectInteractionDots(Transform player, List<WorldDot> into)
        {
            if (into == null)
                return;

            QuestGiverNpc[] givers = SceneComponentCache.GetAll<QuestGiverNpc>();
            for (int i = 0; i < givers.Length; i++)
            {
                QuestGiverNpc giver = givers[i];
                if (giver == null || !giver.IsWithinInteractRange(player.position))
                    continue;
                into.Add(MakeStemDot(giver.transform.position, ProximityDotStyle.QuestGiverColor, InteractStemMeters));
            }

            CraftingStation[] stations = SceneComponentCache.GetAll<CraftingStation>();
            for (int i = 0; i < stations.Length; i++)
            {
                CraftingStation station = stations[i];
                if (station == null || !station.IsWithinInteractRange(player.position))
                    continue;
                into.Add(MakeStemDot(station.transform.position, ProximityDotStyle.CraftingColor, InteractStemMeters));
            }

            BuildingControlPanel[] panels = SceneComponentCache.GetAll<BuildingControlPanel>();
            for (int i = 0; i < panels.Length; i++)
            {
                BuildingControlPanel panel = panels[i];
                if (panel == null || !panel.IsWithinInteractRange(player.position))
                    continue;
                into.Add(MakeStemDot(panel.transform.position, ProximityDotStyle.BuildingColor, 0.9f));
            }

            EnemyLootBag[] bags = SceneComponentCache.GetAll<EnemyLootBag>();
            for (int i = 0; i < bags.Length; i++)
            {
                EnemyLootBag bag = bags[i];
                if (bag == null || !bag.CanPlayerLoot(player.position))
                    continue;
                into.Add(MakeStemDot(bag.transform.position, ProximityDotStyle.LootColor, InteractStemMeters));
            }


            InjuredPioneerLabRecoverable[] recoverables = SceneComponentCache.GetAll<InjuredPioneerLabRecoverable>();
            for (int i = 0; i < recoverables.Length; i++)
            {
                InjuredPioneerLabRecoverable recoverable = recoverables[i];
                if (recoverable == null || !recoverable.CanShowInteractionHint())
                    continue;
                if ((recoverable.transform.position - player.position).sqrMagnitude > recoverable.InteractRange * recoverable.InteractRange)
                    continue;
                into.Add(MakeStemDot(recoverable.transform.position, ProximityDotStyle.ScienceLabColor, 0.85f));
            }

            EchoWorldEntity[] echoes = SceneComponentCache.GetAll<EchoWorldEntity>();
            for (int i = 0; i < echoes.Length; i++)
            {
                EchoWorldEntity echo = echoes[i];
                if (echo == null || !echo.IsInteractable)
                    continue;
                if ((echo.transform.position - player.position).sqrMagnitude > echo.InteractRange * echo.InteractRange)
                    continue;
                into.Add(MakeStemDot(echo.transform.position, ProximityDotStyle.EchoColor, InteractStemMeters));
            }
        }

        /// <summary>
        /// Fixed-height interact stem (min==max). Pickup exclusive path passes growing min/max.
        /// Tip is computed in PaintDots from proximity — never slide the dot along a fixed stem.
        /// </summary>
        private static WorldDot MakeStemDot(Vector3 anchor, Color color, float stemHeight)
        {
            float h = Mathf.Max(0.05f, stemHeight);
            return MakeStemDot(anchor, color, h, h);
        }

        private static WorldDot MakeStemDot(Vector3 anchor, Color color, float stemMinHeight, float stemMaxHeight)
        {
            float minH = Mathf.Max(0.05f, stemMinHeight);
            float maxH = Mathf.Max(minH, stemMaxHeight);
            return new WorldDot
            {
                Anchor = anchor,
                Color = color,
                DrawStem = true,
                StemMinHeight = minH,
                StemMaxHeight = maxH
            };
        }

        private void PaintDots()
        {
            if (dotsLayer == null || dotsLayer.panel == null)
                return;

            Camera camera = worldCamera;
            Transform player = playerTransform;
            float maxRange = WorldUseController.MaxPickupDistance;
            int shown = 0;
            int limit = Mathf.Min(pendingDots.Count, MaxDots);
            for (int i = 0; i < limit; i++)
            {
                WorldDot pending = pendingDots[i];
                if (camera == null)
                    continue;

                // Proximity 0 at max range, 1 when planar (XZ) dist within StemNearReachMeters.
                // Full 3D Distance never hits 0.5m: player root vs ground item has a Y delta.
                float dist = maxRange;
                if (player != null)
                {
                    Vector3 delta = player.position - pending.Anchor;
                    delta.y = 0f;
                    dist = delta.magnitude;
                }
                float reach = Mathf.Clamp(StemNearReachMeters, 0f, maxRange * 0.85f);
                float span = Mathf.Max(0.01f, maxRange - reach);
                float proximity = 1f - Mathf.Clamp01(Mathf.Max(0f, dist - reach) / span);

                // Growing stem: far/lock-on ~0.25m, near (<=0.5m planar) ~0.5m. Dot always on tip.
                float stemHeight = Mathf.Lerp(pending.StemMinHeight, pending.StemMaxHeight, proximity);
                Vector3 tipWorld = pending.Anchor + Vector3.up * stemHeight;

                Vector3 tipScreen = camera.WorldToScreenPoint(tipWorld);
                Vector3 anchorScreen = camera.WorldToScreenPoint(pending.Anchor);
                // Both endpoints must be in front; otherwise screen-lerp of the tip is bogus.
                if (tipScreen.z <= 0f || anchorScreen.z <= 0f)
                    continue;

                Vector2 tipPanel = RuntimePanelUtils.ScreenToPanel(
                    dotsLayer.panel, new Vector2(tipScreen.x, tipScreen.y));
                Vector2 anchorPanel = RuntimePanelUtils.ScreenToPanel(
                    dotsLayer.panel, new Vector2(anchorScreen.x, anchorScreen.y));

                float size = Mathf.Lerp(DotSizeFarPx, DotSizeNearPx, proximity);
                float half = size * 0.5f;
                float coreSize = size * 0.5f;

                VisualElement host = AcquireDot(shown);
                VisualElement stem = host.childCount > 0 ? host[0] : null;
                VisualElement glow = host.childCount > 1 ? host[1] : null;

                // Screen segment from world-projected anchor -> growing tip (world-up).
                // Both ends track the pickup every frame so orbit stays world-locked.
                float dx = tipPanel.x - anchorPanel.x;
                float dy = tipPanel.y - anchorPanel.y;
                float len = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;

                // Dot is always glued to the tip (no along-stem sliding).
                Vector2 alongPanel = tipPanel;

                if (pending.DrawStem && stem != null && tipScreen.z > 0f && anchorScreen.z > 0f && len > 0.5f)
                {
                    // Pivot at box center: left/top place the unrotated bar so its midpoint
                    // sits on the screen midpoint of (anchor, tip). Explicit 50%/50% origin --
                    // left-at-anchor + 0% origin drifts when orbiting if origin fails to apply
                    // to style.rotate.
                    float midX = (anchorPanel.x + tipPanel.x) * 0.5f;
                    float midY = (anchorPanel.y + tipPanel.y) * 0.5f;
                    stem.style.left = midX - len * 0.5f;
                    stem.style.top = midY - 1f;
                    stem.style.width = Mathf.Max(2f, len);
                    stem.style.height = 2f;
                    stem.style.transformOrigin = new TransformOrigin(Length.Percent(50f), Length.Percent(50f));
                    stem.style.rotate = new StyleRotate(new UnityEngine.UIElements.Rotate(Angle.Degrees(angle)));
                    Color stemColor = pending.Color;
                    stemColor.a = Mathf.Clamp01(pending.Color.a * 0.85f);
                    stem.style.backgroundColor = stemColor;
                    DMUiToolkitOverlayDocument.SetShown(stem, true);
                }
                else if (stem != null)
                {
                    DMUiToolkitOverlayDocument.SetShown(stem, false);
                }

                if (glow != null)
                {
                    glow.style.width = size;
                    glow.style.height = size;
                    glow.style.left = alongPanel.x - half;
                    glow.style.top = alongPanel.y - half;
                    glow.style.borderTopLeftRadius = half;
                    glow.style.borderTopRightRadius = half;
                    glow.style.borderBottomLeftRadius = half;
                    glow.style.borderBottomRightRadius = half;
                    glow.style.backgroundColor = DarkMatterGenesisUiPalette.WithAlpha(pending.Color, 0.28f);
                    VisualElement core = glow.childCount > 0 ? glow[0] : null;
                    if (core != null)
                    {
                        core.style.width = coreSize;
                        core.style.height = coreSize;
                        core.style.borderTopLeftRadius = coreSize * 0.5f;
                        core.style.borderTopRightRadius = coreSize * 0.5f;
                        core.style.borderBottomLeftRadius = coreSize * 0.5f;
                        core.style.borderBottomRightRadius = coreSize * 0.5f;
                        core.style.backgroundColor = pending.Color;
                    }
                }

                DMUiToolkitOverlayDocument.SetShown(host, true);
                shown++;
            }

            RecycleDots(shown);
        }

        private void PaintBars()
        {
            if (barsLayer == null || barsLayer.panel == null)
                return;

            Camera camera = worldCamera;
            FloatingTargetHealthBar[] bars = SceneComponentCache.GetAll<FloatingTargetHealthBar>(
                FindObjectsInactive.Exclude,
                refreshInterval: 0.12f);
            int shown = 0;
            for (int i = 0; i < bars.Length && shown < MaxBars; i++)
            {
                FloatingTargetHealthBar bar = bars[i];
                if (bar == null)
                    continue;
                HideBarGraphics(bar);
                if (camera == null)
                    continue;
                if (!bar.TryGetWorldPresentation(out Vector3 world, out float normalized, out string hpText))
                    continue;

                Vector3 screen = camera.WorldToScreenPoint(world);
                if (screen.z <= 0f)
                    continue;

                VisualElement host = AcquireBar(shown);
                Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(
                    barsLayer.panel,
                    new Vector2(screen.x, screen.y + 28f));
                host.style.left = panelPos.x - 40f;
                host.style.top = panelPos.y - 8f;
                VisualElement fill = null;
                Label label = null;
                if (host.childCount > 0)
                {
                    VisualElement track = host[0];
                    if (track.childCount > 0)
                        fill = track[0];
                }
                if (host.childCount > 1)
                    label = host[1] as Label;
                if (fill != null)
                    fill.style.width = Length.Percent(Mathf.Clamp01(normalized) * 100f);
                if (label != null)
                    label.text = hpText;
                DMUiToolkitOverlayDocument.SetShown(host, true);
                shown++;
            }

            RecycleBars(shown);
        }

        private VisualElement AcquireDot(int index)
        {
            while (dotPool.Count <= index)
            {
                VisualElement host = new VisualElement { pickingMode = PickingMode.Ignore };
                host.AddToClassList("dmg-world-dot-host");

                VisualElement stem = new VisualElement { pickingMode = PickingMode.Ignore };
                stem.AddToClassList("dmg-world-dot-stem");
                stem.style.transformOrigin = new TransformOrigin(Length.Percent(50f), Length.Percent(50f));
                host.Add(stem);

                VisualElement glow = new VisualElement { pickingMode = PickingMode.Ignore };
                glow.AddToClassList("dmg-world-dot");
                VisualElement core = new VisualElement { pickingMode = PickingMode.Ignore };
                core.AddToClassList("dmg-world-dot-core");
                glow.Add(core);
                host.Add(glow);

                dotsLayer.Add(host);
                dotPool.Add(host);
            }

            VisualElement dot = dotPool[index];
            if (index >= liveDots.Count)
                liveDots.Add(dot);
            return dot;
        }

        private VisualElement AcquireBar(int index)
        {
            while (barPool.Count <= index)
            {
                VisualElement host = new VisualElement { pickingMode = PickingMode.Ignore };
                host.AddToClassList("dmg-world-bar");
                VisualElement track = new VisualElement { pickingMode = PickingMode.Ignore };
                track.AddToClassList("dmg-world-bar-track");
                VisualElement fill = new VisualElement { name = "fill", pickingMode = PickingMode.Ignore };
                fill.AddToClassList("dmg-world-bar-fill");
                track.Add(fill);
                Label hp = new Label { name = "hp", pickingMode = PickingMode.Ignore };
                hp.AddToClassList("dmg-world-bar-hp");
                host.Add(track);
                host.Add(hp);
                barsLayer.Add(host);
                barPool.Add(host);
            }

            VisualElement bar = barPool[index];
            if (index >= liveBars.Count)
                liveBars.Add(bar);
            return bar;
        }

        private void RecycleDots(int keep)
        {
            for (int i = keep; i < liveDots.Count; i++)
                DMUiToolkitOverlayDocument.SetShown(liveDots[i], false);
            if (keep < liveDots.Count)
                liveDots.RemoveRange(keep, liveDots.Count - keep);
        }

        private void RecycleBars(int keep)
        {
            for (int i = keep; i < liveBars.Count; i++)
                DMUiToolkitOverlayDocument.SetShown(liveBars[i], false);
            if (keep < liveBars.Count)
                liveBars.RemoveRange(keep, liveBars.Count - keep);
        }

        private bool ResolvePlayer(out Transform player, out Camera camera)
        {
            if (cachedPlayer == null)
                cachedPlayer = PlayerLocator.FindPlayerController();
            playerTransform = PlayerReference.Transform ?? (cachedPlayer != null ? cachedPlayer.transform : null);
            worldCamera = PlayerReference.ResolveCamera();
            player = playerTransform;
            camera = worldCamera;
            return player != null && camera != null;
        }

        private void HideUguiCounterparts()
        {
            // Keep retrying: PickupProximityDotUI / WorldInteractionDotUI may create their
            // layers lazily after our first LateUpdate, which previously left uguiHidden=true
            // with the old floating-dot painter still running (felt like stem patches did nothing).
            bool pickupGone = HideNamedLayer("PickupProximityDots");
            bool worldGone = HideNamedLayer("WorldInteractionDots");
            if (pickupGone && worldGone)
                uguiHidden = true;
        }

        /// <returns>True when the named layer is absent or its painters are disabled.</returns>
        private static bool HideNamedLayer(string objectName)
        {
            GameObject layer = DMUiToolkitOverlayDocument.FindNamed(objectName);
            if (layer == null)
                return true;

            DMUiToolkitOverlayDocument.DisableUguiVisuals(layer);

            PickupProximityDotUI pickupDots = layer.GetComponent<PickupProximityDotUI>();
            if (pickupDots != null)
                pickupDots.enabled = false;

            WorldInteractionDotUI worldDots = layer.GetComponent<WorldInteractionDotUI>();
            if (worldDots != null)
                worldDots.enabled = false;

            return (pickupDots == null || !pickupDots.enabled)
                && (worldDots == null || !worldDots.enabled);
        }

        private void HideBarGraphics(FloatingTargetHealthBar bar)
        {
            if (bar == null)
                return;

            if (!hiddenBars.Add(bar))
                return;

            DMUiToolkitOverlayDocument.DisableUguiVisuals(bar.gameObject);
            bar.enabled = false;
        }
    }
}
