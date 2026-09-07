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
using Project.Inventory;
using Project.Map;
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
    [DefaultExecutionOrder(1100)]
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
        private const float CloseCirclePx = 28f;
        private const float CloseRowGapPx = 6f;
        private const float CloseRingThickness = 3f;
        private const float CloseKeyHostPx = CloseCirclePx + CloseRingThickness * 2f + 2f;
        private const float CloseKeyInsetPx = (CloseKeyHostPx - CloseCirclePx) * 0.5f;
        private const float InteractionScanInterval = 1f / 12f;
        private const float ExclusivePickupScanInterval = 0.1f;
        private const int MaxDots = 24;
        private const int MaxBars = 16;

        private static DMUiToolkitWorldChrome instance;

        private UIDocument document;
        private VisualElement root;
        private VisualElement dotsLayer;
        private VisualElement barsLayer;
        private bool bound;
        private bool lastGameplayWant;
        private bool uguiHidden;
        private float nextInteractScan;
        private float nextExclusivePickupScan;
        private bool hasExclusiveDot;
        private WorldDot exclusiveDot;
        private bool panelYFlipResolved;
        private bool panelYIncreasesWithScreenY;
        private float panelFlipHeight;
        private Camera worldCamera;
        private Transform playerTransform;
        private PlayerController cachedPlayer;
        private InventorySystem cachedInventory;

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
            /// <summary>True for exclusive item/blueprint pickup stem (supports close Hold-E chrome).</summary>
            public bool IsPickupPrompt;
            public bool ClosePrompt;
            public string KeyLabel;
            public string ActionLabel;
            public string ItemLabel;
            public bool ItemKnown;
            public Sprite ItemIcon;
            public float HoldProgress01;
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
                RecycleDots(0);
                RecycleBars(0);
                hasExclusiveDot = false;
                WorldPickupFocus.Clear();
                if (want && !uguiHidden)
                    HideUguiCounterparts();
                return;
            }

            if (!uguiHidden)
                HideUguiCounterparts();

            // Paint after final camera pose (see DefaultExecutionOrder 1100).
            panelYFlipResolved = false;
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
                hasExclusiveDot = false;
                WorldPickupFocus.Clear();
                return;
            }

            PlayerController pc = cachedPlayer;
            if (pc != null && pc.BlocksCombatInput)
            {
                pendingDots.Clear();
                cachedInteractDots.Clear();
                hasExclusiveDot = false;
                WorldPickupFocus.Clear();
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
            if (Time.unscaledTime >= nextExclusivePickupScan)
            {
                nextExclusivePickupScan = Time.unscaledTime + ExclusivePickupScanInterval;
                CollectExclusivePickupDot(player, camera);
            }
            else if (hasExclusiveDot)
            {
                RefreshExclusiveAnchor();
                RefreshExclusiveHoldProgress();
            }

            if (hasExclusiveDot)
                pendingDots.Add(exclusiveDot);
            for (int i = 0; i < cachedInteractDots.Count; i++)
                pendingDots.Add(cachedInteractDots[i]);
        }

        private void CollectExclusivePickupDot(Transform player, Camera camera)
        {
            WorldPickupFocus.Clear();

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
            ItemPickup bestItem = null;
            RecipePickup bestRecipe = null;
            ResourceNode bestHarvest = null;
            bool found = false;

            ItemPickup[] pickups = SceneComponentCache.GetAll<ItemPickup>(FindObjectsInactive.Exclude, refreshInterval: 0.2f);
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
                bestItem = pickup;
                bestRecipe = null;
                bestHarvest = null;
                found = true;
            }

            RecipePickup[] recipes = SceneComponentCache.GetAll<RecipePickup>(FindObjectsInactive.Exclude, refreshInterval: 0.2f);
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
                bestItem = null;
                bestRecipe = recipe;
                bestHarvest = null;
                found = true;
            }

            ResourceNode[] nodes = SceneComponentCache.GetAll<ResourceNode>(FindObjectsInactive.Exclude, refreshInterval: 0.2f);
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
                bestStemMin = PickupStemFarMeters;
                bestStemMax = PickupStemNearMeters;
                bestItem = null;
                bestRecipe = null;
                bestHarvest = node;
                found = true;
            }

            if (!found)
            {
                hasExclusiveDot = false;
                return;
            }

            if (bestItem != null)
                WorldPickupFocus.SetItem(bestItem);
            else if (bestRecipe != null)
                WorldPickupFocus.SetRecipe(bestRecipe);
            else if (bestHarvest != null)
                WorldPickupFocus.SetHarvest(bestHarvest);

            WorldDot dot = MakeStemDot(bestWorld, bestColor, bestStemMin, bestStemMax);
            bool isPickup = bestItem != null || bestRecipe != null;
            dot.IsPickupPrompt = isPickup;

            if (isPickup)
            {
                bool close = WorldPickupFocus.IsWithinClosePromptRange(player.position, bestWorld);
                dot.ClosePrompt = close;
                dot.KeyLabel = "E";
                dot.ActionLabel = "Take";
                FillPickupIdentity(bestItem, bestRecipe, ref dot);
                if (bestItem != null && bestItem.IsHoldActive)
                    dot.HoldProgress01 = bestItem.HoldProgress01;
                else if (bestRecipe != null && bestRecipe.IsHoldActive)
                    dot.HoldProgress01 = bestRecipe.HoldProgress01;
                else
                    dot.HoldProgress01 = 0f;
            }

            exclusiveDot = dot;
            hasExclusiveDot = true;
        }

        private void RefreshExclusiveAnchor()
        {
            if (!hasExclusiveDot)
                return;

            if (WorldPickupFocus.Item != null)
                exclusiveDot.Anchor = WorldPickupFocus.Item.GetIndicatorWorldAnchor();
            else if (WorldPickupFocus.Recipe != null)
                exclusiveDot.Anchor = WorldPickupFocus.Recipe.GetIndicatorWorldAnchor();
            else if (WorldPickupFocus.Harvest != null)
                exclusiveDot.Anchor = WorldPickupFocus.Harvest.GetNodeCenter();
        }

        private void RefreshExclusiveHoldProgress()
        {
            if (!hasExclusiveDot || !exclusiveDot.IsPickupPrompt)
                return;

            if (WorldPickupFocus.Item != null && WorldPickupFocus.Item.IsHoldActive)
                exclusiveDot.HoldProgress01 = WorldPickupFocus.Item.HoldProgress01;
            else if (WorldPickupFocus.Recipe != null && WorldPickupFocus.Recipe.IsHoldActive)
                exclusiveDot.HoldProgress01 = WorldPickupFocus.Recipe.HoldProgress01;
            else
                exclusiveDot.HoldProgress01 = 0f;
        }

        private void FillPickupIdentity(ItemPickup item, RecipePickup recipe, ref WorldDot dot)
        {
            InventorySystem inventory = cachedInventory;

            if (item != null && item.itemData != null)
            {
                ItemData data = item.itemData;
                bool known = ResourceIdentificationRegistry.IsIdentified(data)
                    || (inventory != null && inventory.CountItem(data) > 0);
                dot.ItemKnown = known;
                if (known)
                {
                    dot.ItemLabel = string.IsNullOrEmpty(data.itemName) ? data.name : data.itemName;
                    dot.ItemIcon = data.icon;
                }
                else
                {
                    dot.ItemLabel = string.Empty;
                    dot.ItemIcon = null;
                }
                return;
            }

            if (recipe != null)
            {
                RecipeDefinition def = RecipeRegistry.Resolve(recipe.RecipeId);
                if (def != null)
                {
                    dot.ItemKnown = true;
                    dot.ItemLabel = !string.IsNullOrEmpty(def.displayName) ? def.displayName : "Blueprint";
                    // RecipeDefinition may not expose an icon; leave null for letter fallback.
                    dot.ItemIcon = null;
                }
                else
                {
                    dot.ItemKnown = false;
                    dot.ItemLabel = string.Empty;
                    dot.ItemIcon = null;
                }
            }
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
        /// Tip is computed in PaintDots from proximity - never slide the dot along a fixed stem.
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
                StemMaxHeight = maxH,
                IsPickupPrompt = false,
                ClosePrompt = false,
                KeyLabel = null,
                ActionLabel = null,
                ItemLabel = null,
                ItemKnown = false,
                ItemIcon = null,
                HoldProgress01 = 0f
            };
        }

        /// <summary>
        /// dotsLayer panel position for a world point. Y is flipped when the panel uses Y-up coords.
        /// Do not round here — callers lock world-vertical stems to one screen X before rounding.
        /// </summary>
        private bool TryWorldToPanel(Camera camera, Vector3 world, out Vector2 panelPos)
        {
            panelPos = default;
            if (camera == null || dotsLayer == null || dotsLayer.panel == null)
                return false;

            Vector3 screen = camera.WorldToScreenPoint(world);
            if (screen.z <= 0f)
                return false;

            IPanel panel = dotsLayer.panel;
            panelPos = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screen.x, screen.y));
            ResolvePanelYFlip(panel);
            if (panelYIncreasesWithScreenY)
                panelPos.y = panelFlipHeight - panelPos.y;
            return true;
        }

        private void ResolvePanelYFlip(IPanel panel)
        {
            if (panelYFlipResolved)
                return;

            Vector2 screenBottom = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(0f, 0f));
            Vector2 screenTop = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(0f, Screen.height));
            panelYIncreasesWithScreenY = screenTop.y > screenBottom.y + 0.5f;

            panelFlipHeight = dotsLayer != null ? dotsLayer.layout.height : 0f;
            if (panelFlipHeight <= 1f)
                panelFlipHeight = Screen.height;

            panelYFlipResolved = true;
        }

        private static Vector3 ResolveLiveAnchor(in WorldDot pending)
        {
            if (pending.IsPickupPrompt)
            {
                if (WorldPickupFocus.Item != null)
                    return WorldPickupFocus.Item.GetIndicatorWorldAnchor();
                if (WorldPickupFocus.Recipe != null)
                    return WorldPickupFocus.Recipe.GetIndicatorWorldAnchor();
            }

            return pending.Anchor;
        }

        private static Vector2 StabilizeAnchorPanel(Vector2 raw, Vector2 lastWritten)
        {
            raw.x = Mathf.Round(raw.x);
            raw.y = Mathf.Round(raw.y);
            if (float.IsNaN(lastWritten.x))
                return raw;

            // Low-FPS camera micro-step can wobble screen X; ignore 2px anchor drift.
            if (Mathf.Abs(raw.x - lastWritten.x) < 2f)
                raw.x = lastWritten.x;
            if (Mathf.Abs(raw.y - lastWritten.y) < 2f)
                raw.y = lastWritten.y;
            return raw;
        }

        private static int ResolvePickupAnchorId()
        {
            if (WorldPickupFocus.Item != null)
                return WorldPickupFocus.Item.GetEntityId().GetHashCode();
            if (WorldPickupFocus.Recipe != null)
                return WorldPickupFocus.Recipe.GetEntityId().GetHashCode();
            if (WorldPickupFocus.Harvest != null)
                return WorldPickupFocus.Harvest.GetEntityId().GetHashCode();
            return 0;
        }

        /// <summary>
        /// World-to-panel stem/dot layout. Called from LateUpdate after camera
        /// (execution order 1100) so WorldToScreen matches the same-frame pose --
        /// avoids classic 1-frame look/move wiggle. Tip stays locked to item tip;
        /// stem base stays on world anchor (do not invert).
        /// </summary>
        private void PaintDots()
        {
            if (dotsLayer == null || dotsLayer.panel == null)
                return;

            Camera camera = worldCamera;
            Transform player = playerTransform;
            float maxRange = WorldUseController.MaxPickupDistance;
            const float stemThickness = 2f;
            int shown = 0;
            int limit = Mathf.Min(pendingDots.Count, MaxDots);
            for (int i = 0; i < limit; i++)
            {
                WorldDot pending = pendingDots[i];
                if (camera == null)
                    continue;

                DotVisuals visuals = AcquireDot(shown);
                Vector3 anchorWorld = ResolveLiveAnchor(pending);
                if (pending.IsPickupPrompt)
                {
                    int anchorId = ResolvePickupAnchorId();
                    if (!visuals.HasLockedWorldAnchor
                        || visuals.LockedAnchorId != anchorId
                        || (anchorWorld - visuals.LockedWorldAnchor).sqrMagnitude > 0.0025f)
                    {
                        visuals.LockedWorldAnchor = anchorWorld;
                        visuals.HasLockedWorldAnchor = true;
                        visuals.LockedAnchorId = anchorId;
                        visuals.LastAnchor = new Vector2(float.NaN, float.NaN);
                    }
                    else
                    {
                        anchorWorld = visuals.LockedWorldAnchor;
                    }
                }

                float dist = maxRange;
                if (player != null)
                {
                    Vector3 delta = player.position - anchorWorld;
                    delta.y = 0f;
                    dist = delta.magnitude;
                }
                float reach = Mathf.Clamp(StemNearReachMeters, 0f, maxRange * 0.85f);
                float span = Mathf.Max(0.01f, maxRange - reach);
                float proximity = 1f - Mathf.Clamp01(Mathf.Max(0f, dist - reach) / span);

                float stemHeight = Mathf.Lerp(pending.StemMinHeight, pending.StemMaxHeight, proximity);
                Vector3 tipWorld = anchorWorld + Vector3.up * stemHeight;

                VisualElement host = visuals.Host;
                if (host == null
                    || !TryWorldToPanel(camera, anchorWorld, out Vector2 anchorRaw)
                    || !TryWorldToPanel(camera, tipWorld, out Vector2 tipRaw))
                {
                    DMUiToolkitOverlayDocument.SetShown(visuals.Host, false);
                    continue;
                }

                // World-up stems must share one screen column; separate projection + rounding skews X.
                tipRaw.x = anchorRaw.x;
                Vector2 anchorPanel = StabilizeAnchorPanel(anchorRaw, visuals.LastAnchor);
                Vector2 tipPanel = new Vector2(anchorPanel.x, Mathf.Round(tipRaw.y));
                float relDy = tipPanel.y - anchorPanel.y;

                bool moved = (anchorPanel - visuals.LastAnchor).sqrMagnitude >= 0.01f
                    || Mathf.Abs(relDy - (visuals.LastTip.y - visuals.LastAnchor.y)) >= 0.01f;
                visuals.LastAnchor = anchorPanel;
                visuals.LastTip = tipPanel;

                if (moved || !visuals.LastStemShown)
                {
                    host.style.left = anchorPanel.x;
                    host.style.top = anchorPanel.y;
                }

                VisualElement stem = visuals.Stem;
                VisualElement glow = visuals.Glow;
                VisualElement closeCluster = visuals.CloseCluster;

                float len = Mathf.Abs(relDy);
                float angle = relDy >= 0f ? 90f : -90f;

                if (pending.DrawStem && stem != null && len > 0.5f)
                {
                    if (moved || !visuals.LastStemShown)
                    {
                        stem.style.left = 0f;
                        stem.style.top = -stemThickness * 0.5f;
                        stem.style.right = StyleKeyword.Auto;
                        stem.style.bottom = StyleKeyword.Auto;
                        stem.style.width = Mathf.Max(stemThickness, len);
                        stem.style.height = stemThickness;
                        stem.style.rotate = new StyleRotate(new UnityEngine.UIElements.Rotate(Angle.Degrees(angle)));
                        Color stemColor = pending.Color;
                        stemColor.a = Mathf.Clamp01(pending.Color.a * 0.85f);
                        stem.style.backgroundColor = stemColor;
                    }

                    DMUiToolkitOverlayDocument.SetShown(stem, true);
                    visuals.LastStemShown = true;
                }
                else if (stem != null)
                {
                    DMUiToolkitOverlayDocument.SetShown(stem, false);
                    visuals.LastStemShown = false;
                }

                bool showClose = pending.IsPickupPrompt && pending.ClosePrompt;
                if (showClose)
                {
                    if (glow != null)
                        DMUiToolkitOverlayDocument.SetShown(glow, false);
                    PaintClosePrompt(visuals, relDy, pending, moved);
                }
                else
                {
                    if (closeCluster != null)
                    {
                        DMUiToolkitOverlayDocument.SetShown(closeCluster, false);
                        visuals.LastCloseShown = false;
                    }

                    float size = Mathf.Lerp(DotSizeFarPx, DotSizeNearPx, proximity);
                    float half = size * 0.5f;
                    float coreSize = size * 0.5f;
                    if (glow != null)
                    {
                        if (moved || Mathf.Abs(size - visuals.LastGlowSize) >= 0.5f)
                        {
                            visuals.LastGlowSize = size;
                            glow.style.width = size;
                            glow.style.height = size;
                            glow.style.left = -half;
                            glow.style.top = relDy - half;
                            glow.style.right = StyleKeyword.Auto;
                            glow.style.bottom = StyleKeyword.Auto;
                            glow.style.borderTopLeftRadius = half;
                            glow.style.borderTopRightRadius = half;
                            glow.style.borderBottomLeftRadius = half;
                            glow.style.borderBottomRightRadius = half;
                            glow.style.backgroundColor = DarkMatterGenesisUiPalette.WithAlpha(pending.Color, 0.28f);
                            VisualElement core = visuals.Core;
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

                        DMUiToolkitOverlayDocument.SetShown(glow, true);
                    }
                }

                DMUiToolkitOverlayDocument.SetShown(visuals.Host, true);
                shown++;
            }

            RecycleDots(shown);
        }

        private void PaintClosePrompt(DotVisuals visuals, float relDy, WorldDot pending, bool moved)
        {
            VisualElement cluster = visuals != null ? visuals.CloseCluster : null;
            if (cluster == null)
                return;

            DMUiToolkitOverlayDocument.SetShown(cluster, true);

            // Host sits on the pickup anchor; cluster is positioned relative to the stem tip.
            float keyHalf = CloseKeyHostPx * 0.5f;
            if (moved || !visuals.LastCloseShown)
            {
                cluster.style.left = -keyHalf;
                cluster.style.top = relDy - (CloseCirclePx + CloseRowGapPx + keyHalf);
            }

            visuals.LastCloseShown = true;

            if (visuals.InfoCircle != null)
                visuals.InfoCircle.style.backgroundColor = pending.Color;

            bool known = pending.ItemKnown;
            if (visuals.InfoIcon != null)
            {
                if (known && pending.ItemIcon != null)
                {
                    if (visuals.InfoIcon.sprite != pending.ItemIcon)
                        visuals.InfoIcon.sprite = pending.ItemIcon;
                    DMUiToolkitOverlayDocument.SetShown(visuals.InfoIcon, true);
                }
                else
                {
                    if (visuals.InfoIcon.sprite != null)
                        visuals.InfoIcon.sprite = null;
                    DMUiToolkitOverlayDocument.SetShown(visuals.InfoIcon, false);
                }
            }

            if (visuals.InfoUnknown != null)
            {
                if (!known)
                {
                    if (visuals.InfoUnknown.text != "?")
                        visuals.InfoUnknown.text = "?";
                    DMUiToolkitOverlayDocument.SetShown(visuals.InfoUnknown, true);
                }
                else
                {
                    DMUiToolkitOverlayDocument.SetShown(visuals.InfoUnknown, false);
                }
            }

            if (visuals.InfoName != null)
            {
                if (known)
                {
                    string name = pending.ItemLabel ?? string.Empty;
                    if (visuals.InfoName.text != name)
                        visuals.InfoName.text = name;
                    DMUiToolkitOverlayDocument.SetShown(visuals.InfoName, true);
                }
                else
                {
                    DMUiToolkitOverlayDocument.SetShown(visuals.InfoName, false);
                }
            }

            if (visuals.ActionLabel != null)
            {
                string action = string.IsNullOrEmpty(pending.ActionLabel) ? "Take" : pending.ActionLabel;
                if (visuals.ActionLabel.text != action)
                    visuals.ActionLabel.text = action;
            }

            if (visuals.KeyLabel != null)
            {
                string key = string.IsNullOrEmpty(pending.KeyLabel) ? "E" : pending.KeyLabel;
                if (visuals.KeyLabel.text != key)
                    visuals.KeyLabel.text = key;
            }

            if (visuals.HoldRing != null)
            {
                float progress = Mathf.Clamp01(pending.HoldProgress01);
                bool progressChanged = !(visuals.HoldRing.userData is float last)
                    || Mathf.Abs(last - progress) > 0.001f;
                if (progressChanged)
                {
                    visuals.HoldRing.userData = progress;
                    visuals.HoldRing.MarkDirtyRepaint();
                }
            }
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
                float left = panelPos.x - 40f;
                float top = panelPos.y - 8f;
                float fillPct = Mathf.Clamp01(normalized) * 100f;

                BarVisuals barVisuals = GetBarVisuals(host);
                bool moved = float.IsNaN(barVisuals.LastLeft)
                    || Mathf.Abs(left - barVisuals.LastLeft) >= 0.5f
                    || Mathf.Abs(top - barVisuals.LastTop) >= 0.5f;
                bool fillChanged = Mathf.Abs(fillPct - barVisuals.LastFillPct) >= 0.5f;
                bool textChanged = hpText != barVisuals.LastHpText;

                if (moved)
                {
                    barVisuals.LastLeft = left;
                    barVisuals.LastTop = top;
                    host.style.left = left;
                    host.style.top = top;
                }

                VisualElement fill = barVisuals.Fill;
                Label label = barVisuals.Label;
                if (fill != null && fillChanged)
                {
                    barVisuals.LastFillPct = fillPct;
                    fill.style.width = Length.Percent(fillPct);
                }

                if (label != null && textChanged)
                {
                    barVisuals.LastHpText = hpText;
                    label.text = hpText;
                }

                DMUiToolkitOverlayDocument.SetShown(host, true);
                shown++;
            }

            RecycleBars(shown);
        }

        private static BarVisuals GetBarVisuals(VisualElement host)
        {
            if (host.userData is BarVisuals cached)
                return cached;

            var visuals = new BarVisuals();
            if (host.childCount > 0)
            {
                VisualElement track = host[0];
                if (track.childCount > 0)
                    visuals.Fill = track[0];
            }

            if (host.childCount > 1)
                visuals.Label = host[1] as Label;

            host.userData = visuals;
            return visuals;
        }

        private sealed class BarVisuals
        {
            public VisualElement Fill;
            public Label Label;
            public float LastLeft = float.NaN;
            public float LastTop = float.NaN;
            public float LastFillPct = -1f;
            public string LastHpText;
        }

        private sealed class DotVisuals
        {
            public VisualElement Host;
            public VisualElement Stem;
            public VisualElement Glow;
            public VisualElement Core;
            public VisualElement CloseCluster;
            public VisualElement InfoRow;
            public VisualElement InfoCircle;
            public VisualElement PickRow;
            public Image InfoIcon;
            public Label InfoUnknown;
            public Label InfoName;
            public Label KeyLabel;
            public Label ActionLabel;
            public VisualElement HoldRing;
            public Vector2 LastTip = new Vector2(float.NaN, float.NaN);
            public Vector2 LastAnchor = new Vector2(float.NaN, float.NaN);
            public Vector3 LockedWorldAnchor;
            public bool HasLockedWorldAnchor;
            public int LockedAnchorId;
            public float LastGlowSize = -1f;
            public bool LastStemShown;
            public bool LastCloseShown;
        }

        private DotVisuals AcquireDot(int index)
        {
            while (dotPool.Count <= index)
            {
                VisualElement host = new VisualElement { pickingMode = PickingMode.Ignore };
                host.AddToClassList("dmg-world-dot-host");

                VisualElement stem = new VisualElement { name = "stem", pickingMode = PickingMode.Ignore };
                stem.AddToClassList("dmg-world-dot-stem");
                stem.style.transformOrigin = new TransformOrigin(Length.Percent(0f), Length.Percent(50f));
                host.Add(stem);

                VisualElement glow = new VisualElement { name = "far-glow", pickingMode = PickingMode.Ignore };
                glow.AddToClassList("dmg-world-dot");
                VisualElement core = new VisualElement { name = "core", pickingMode = PickingMode.Ignore };
                core.AddToClassList("dmg-world-dot-core");
                glow.Add(core);
                host.Add(glow);

                VisualElement closeCluster = BuildCloseCluster();
                host.Add(closeCluster);

                host.userData = new DotVisuals
                {
                    Host = host,
                    Stem = stem,
                    Glow = glow,
                    Core = core,
                    CloseCluster = closeCluster,
                    InfoRow = closeCluster.Q<VisualElement>("info-row"),
                    InfoCircle = closeCluster.Q<VisualElement>("info-circle"),
                    PickRow = closeCluster.Q<VisualElement>("pick-row"),
                    InfoIcon = closeCluster.Q<Image>("info-icon"),
                    InfoUnknown = closeCluster.Q<Label>("info-unknown"),
                    InfoName = closeCluster.Q<Label>("info-name"),
                    KeyLabel = closeCluster.Q<Label>("key-label"),
                    ActionLabel = closeCluster.Q<Label>("action-label"),
                    HoldRing = closeCluster.Q<VisualElement>("hold-ring")
                };

                dotsLayer.Add(host);
                dotPool.Add(host);
            }

            VisualElement dot = dotPool[index];
            if (index >= liveDots.Count)
                liveDots.Add(dot);
            return (DotVisuals)dot.userData;
        }

        private static VisualElement BuildCloseCluster()
        {
            VisualElement cluster = new VisualElement { name = "close-cluster", pickingMode = PickingMode.Ignore };
            cluster.AddToClassList("dmg-world-close");
            cluster.style.display = DisplayStyle.None;

            VisualElement infoRow = new VisualElement { name = "info-row", pickingMode = PickingMode.Ignore };
            infoRow.AddToClassList("dmg-world-close-row");

            VisualElement infoCircle = new VisualElement { name = "info-circle", pickingMode = PickingMode.Ignore };
            infoCircle.AddToClassList("dmg-world-close-info");
            Image infoIcon = new Image { name = "info-icon", pickingMode = PickingMode.Ignore };
            infoIcon.AddToClassList("dmg-world-close-icon");
            infoCircle.Add(infoIcon);
            Label infoUnknown = new Label("?") { name = "info-unknown", pickingMode = PickingMode.Ignore };
            infoUnknown.AddToClassList("dmg-world-close-unknown");
            infoCircle.Add(infoUnknown);
            infoRow.Add(infoCircle);

            Label infoName = new Label { name = "info-name", pickingMode = PickingMode.Ignore };
            infoName.AddToClassList("dmg-world-close-name");
            infoRow.Add(infoName);
            cluster.Add(infoRow);

            VisualElement pickRow = new VisualElement { name = "pick-row", pickingMode = PickingMode.Ignore };
            pickRow.AddToClassList("dmg-world-close-row");
            pickRow.AddToClassList("dmg-world-close-row-last");

            VisualElement keyHost = new VisualElement { name = "key-host", pickingMode = PickingMode.Ignore };
            keyHost.AddToClassList("dmg-world-close-keyhost");
            keyHost.style.width = CloseKeyHostPx;
            keyHost.style.height = CloseKeyHostPx;

            VisualElement keyCircle = new VisualElement { name = "key-circle", pickingMode = PickingMode.Ignore };
            keyCircle.AddToClassList("dmg-world-close-key");
            keyCircle.style.left = CloseKeyInsetPx;
            keyCircle.style.top = CloseKeyInsetPx;
            Label keyLabel = new Label("E") { name = "key-label", pickingMode = PickingMode.Ignore };
            keyLabel.AddToClassList("dmg-world-close-keylabel");
            keyCircle.Add(keyLabel);
            keyHost.Add(keyCircle);

            VisualElement ring = new VisualElement { name = "hold-ring", pickingMode = PickingMode.Ignore };
            ring.AddToClassList("dmg-world-close-ring");
            ring.style.width = CloseKeyHostPx;
            ring.style.height = CloseKeyHostPx;
            ring.generateVisualContent += PaintHoldRing;
            keyHost.Add(ring);
            pickRow.Add(keyHost);

            Label actionLabel = new Label("Take") { name = "action-label", pickingMode = PickingMode.Ignore };
            actionLabel.AddToClassList("dmg-world-close-action");
            pickRow.Add(actionLabel);
            cluster.Add(pickRow);

            return cluster;
        }

        private static void PaintHoldRing(MeshGenerationContext ctx)
        {
            VisualElement ve = ctx.visualElement;
            float progress = 0f;
            if (ve.userData is float f)
                progress = Mathf.Clamp01(f);
            else if (ve.userData is double d)
                progress = Mathf.Clamp01((float)d);

            Rect r = ve.contentRect;
            if (r.width < 2f || r.height < 2f)
                return;

            Vector2 center = new Vector2(r.width * 0.5f, r.height * 0.5f);
            float ringRadius = (CloseCirclePx * 0.5f) + (CloseRingThickness * 0.5f) + 1f;
            Painter2D p = ctx.painter2D;

            // Faded track around the E circle.
            p.strokeColor = new Color(1f, 1f, 1f, 0.22f);
            p.lineWidth = CloseRingThickness;
            p.lineCap = LineCap.Round;
            p.BeginPath();
            p.Arc(center, ringRadius, Angle.Degrees(0f), Angle.Degrees(360f));
            p.Stroke();

            if (progress <= 0.001f)
                return;

            // Gold fill arc, clockwise from top.
            Color gold = DarkMatterGenesisUiPalette.Gold;
            gold.a = 0.95f;
            p.strokeColor = gold;
            p.lineWidth = CloseRingThickness;
            p.BeginPath();
            float sweep = 360f * progress;
            p.Arc(center, ringRadius, Angle.Degrees(-90f), Angle.Degrees(-90f + sweep), ArcDirection.Clockwise);
            p.Stroke();
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
            {
                VisualElement el = liveDots[i];
                DMUiToolkitOverlayDocument.SetShown(el, false);
                if (el.userData is DotVisuals visuals)
                {
                    visuals.LastTip = new Vector2(float.NaN, float.NaN);
                    visuals.LastAnchor = new Vector2(float.NaN, float.NaN);
                    visuals.HasLockedWorldAnchor = false;
                    visuals.LockedAnchorId = 0;
                    visuals.LastGlowSize = -1f;
                    visuals.LastStemShown = false;
                    visuals.LastCloseShown = false;
                }
            }

            if (keep < liveDots.Count)
                liveDots.RemoveRange(keep, liveDots.Count - keep);
        }

        private void RecycleBars(int keep)
        {
            for (int i = keep; i < liveBars.Count; i++)
            {
                VisualElement el = liveBars[i];
                DMUiToolkitOverlayDocument.SetShown(el, false);
                if (el.userData is BarVisuals visuals)
                {
                    visuals.LastLeft = float.NaN;
                    visuals.LastTop = float.NaN;
                    visuals.LastFillPct = -1f;
                    visuals.LastHpText = null;
                }
            }

            if (keep < liveBars.Count)
                liveBars.RemoveRange(keep, liveBars.Count - keep);
        }

        private bool ResolvePlayer(out Transform player, out Camera camera)
        {
            if (cachedPlayer == null)
                cachedPlayer = PlayerLocator.FindPlayerController();
            playerTransform = PlayerReference.Transform ?? (cachedPlayer != null ? cachedPlayer.transform : null);
            worldCamera = PlayerReference.ResolveCamera();
            if (cachedInventory == null && cachedPlayer != null)
                cachedInventory = cachedPlayer.GetComponent<InventorySystem>();
            player = playerTransform;
            camera = worldCamera;
            return player != null && camera != null;
        }

        private void HideUguiCounterparts()
        {
            if (uguiHidden)
                return;

            HideNamedLayer("PickupProximityDots");
            HideNamedLayer("WorldInteractionDots");
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
