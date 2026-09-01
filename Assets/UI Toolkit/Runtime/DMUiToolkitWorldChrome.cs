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
using Project.Pet;
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
        private const float VerticalWorldOffset = 0.45f;
        private const float InteractionScanInterval = 0.12f;
        private const int MaxDots = 24;
        private const int MaxBars = 16;

        private static DMUiToolkitWorldChrome instance;

        private UIDocument document;
        private VisualElement root;
        private VisualElement dotsLayer;
        private VisualElement barsLayer;
        private bool bound;
        private bool uguiHidden;
        private float nextInteractScan;
        private Camera worldCamera;
        private Transform playerTransform;
        private PlayerController cachedPlayer;

        private readonly List<VisualElement> dotPool = new List<VisualElement>();
        private readonly List<VisualElement> liveDots = new List<VisualElement>();
        private readonly List<VisualElement> barPool = new List<VisualElement>();
        private readonly List<VisualElement> liveBars = new List<VisualElement>();
        private readonly List<WorldDot> pendingDots = new List<WorldDot>(32);

        private struct WorldDot
        {
            public Vector3 World;
            public Color Color;
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
            DMUiToolkitOverlayDocument.SetShown(root, want);
            DMUiToolkitOverlayDocument.SetShown(dotsLayer, want);
            DMUiToolkitOverlayDocument.SetShown(barsLayer, want);

            if (!want)
            {
                RecycleDots(0);
                RecycleBars(0);
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
            pendingDots.Clear();
            if (!ResolvePlayer(out Transform player, out Camera camera))
                return;

            PlayerController pc = cachedPlayer;
            if (pc != null && pc.BlocksCombatInput)
                return;

            CollectExclusivePickupDot(player, camera);
            if (Time.unscaledTime >= nextInteractScan)
                nextInteractScan = Time.unscaledTime + InteractionScanInterval;
            CollectInteractionDots(player);
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
            bool found = false;

            ItemPickup[] pickups = SceneComponentCache.GetAll<ItemPickup>(FindObjectsInactive.Exclude, refreshInterval: 0.05f);
            for (int i = 0; i < pickups.Length; i++)
            {
                ItemPickup pickup = pickups[i];
                if (pickup == null || pickup.IsPickedUp || pickup.itemData == null)
                    continue;
                if (!WorldUseController.IsCollectiblePickup(pickup))
                    continue;
                if (!TryQualify(pickup.transform.position, player.position, origin, forward, nearSqr, halfCone, out float dist))
                    continue;
                if (dist >= bestDist)
                    continue;
                bestDist = dist;
                bestWorld = pickup.transform.position;
                bestColor = ProximityDotStyle.PickupColor(pickup.itemData.itemType);
                found = true;
            }

            RecipePickup[] recipes = SceneComponentCache.GetAll<RecipePickup>(FindObjectsInactive.Exclude, refreshInterval: 0.05f);
            for (int i = 0; i < recipes.Length; i++)
            {
                RecipePickup recipe = recipes[i];
                if (recipe == null || recipe.IsLearned)
                    continue;
                if (!TryQualify(recipe.transform.position, player.position, origin, forward, nearSqr, halfCone, out float dist))
                    continue;
                if (dist >= bestDist)
                    continue;
                bestDist = dist;
                bestWorld = recipe.transform.position;
                bestColor = ProximityDotStyle.RecipeColor;
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
                pendingDots.Add(new WorldDot { World = bestWorld + Vector3.up * VerticalWorldOffset, Color = bestColor });
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

        private void CollectInteractionDots(Transform player)
        {
            QuestGiverNpc[] givers = SceneComponentCache.GetAll<QuestGiverNpc>();
            for (int i = 0; i < givers.Length; i++)
            {
                QuestGiverNpc giver = givers[i];
                if (giver == null || !giver.IsWithinInteractRange(player.position))
                    continue;
                pendingDots.Add(new WorldDot { World = giver.transform.position + Vector3.up * 1.15f, Color = ProximityDotStyle.QuestGiverColor });
            }

            CraftingStation[] stations = SceneComponentCache.GetAll<CraftingStation>();
            for (int i = 0; i < stations.Length; i++)
            {
                CraftingStation station = stations[i];
                if (station == null || !station.IsWithinInteractRange(player.position))
                    continue;
                pendingDots.Add(new WorldDot { World = station.transform.position + Vector3.up * 0.75f, Color = ProximityDotStyle.CraftingColor });
            }

            BuildingControlPanel[] panels = SceneComponentCache.GetAll<BuildingControlPanel>();
            for (int i = 0; i < panels.Length; i++)
            {
                BuildingControlPanel panel = panels[i];
                if (panel == null || !panel.IsWithinInteractRange(player.position))
                    continue;
                pendingDots.Add(new WorldDot { World = panel.transform.position + Vector3.up * 0.9f, Color = ProximityDotStyle.BuildingColor });
            }

            EnemyLootBag[] bags = SceneComponentCache.GetAll<EnemyLootBag>();
            for (int i = 0; i < bags.Length; i++)
            {
                EnemyLootBag bag = bags[i];
                if (bag == null || !bag.CanPlayerLoot(player.position))
                    continue;
                pendingDots.Add(new WorldDot { World = bag.transform.position + Vector3.up * VerticalWorldOffset, Color = ProximityDotStyle.LootColor });
            }

            PetWorldAdoptable[] adoptables = SceneComponentCache.GetAll<PetWorldAdoptable>();
            for (int i = 0; i < adoptables.Length; i++)
            {
                PetWorldAdoptable adoptable = adoptables[i];
                if (adoptable == null)
                    continue;
                if ((adoptable.transform.position - player.position).sqrMagnitude > adoptable.InteractRange * adoptable.InteractRange)
                    continue;
                pendingDots.Add(new WorldDot { World = adoptable.transform.position + Vector3.up * 0.55f, Color = ProximityDotStyle.PetColor });
            }

            InjuredPioneerLabRecoverable[] recoverables = SceneComponentCache.GetAll<InjuredPioneerLabRecoverable>();
            for (int i = 0; i < recoverables.Length; i++)
            {
                InjuredPioneerLabRecoverable recoverable = recoverables[i];
                if (recoverable == null || !recoverable.CanShowInteractionHint())
                    continue;
                if ((recoverable.transform.position - player.position).sqrMagnitude > recoverable.InteractRange * recoverable.InteractRange)
                    continue;
                pendingDots.Add(new WorldDot { World = recoverable.transform.position + Vector3.up * 0.85f, Color = ProximityDotStyle.ScienceLabColor });
            }

            EchoWorldEntity[] echoes = SceneComponentCache.GetAll<EchoWorldEntity>();
            for (int i = 0; i < echoes.Length; i++)
            {
                EchoWorldEntity echo = echoes[i];
                if (echo == null || !echo.IsInteractable)
                    continue;
                if ((echo.transform.position - player.position).sqrMagnitude > echo.InteractRange * echo.InteractRange)
                    continue;
                pendingDots.Add(new WorldDot { World = echo.transform.position + Vector3.up * 1f, Color = ProximityDotStyle.EchoColor });
            }
        }

        private void PaintDots()
        {
            if (dotsLayer == null || dotsLayer.panel == null)
                return;

            Camera camera = worldCamera;
            int shown = 0;
            int limit = Mathf.Min(pendingDots.Count, MaxDots);
            for (int i = 0; i < limit; i++)
            {
                WorldDot pending = pendingDots[i];
                if (camera == null)
                    continue;
                Vector3 screen = camera.WorldToScreenPoint(pending.World);
                if (screen.z <= 0f)
                    continue;

                VisualElement dot = AcquireDot(shown);
                Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(dotsLayer.panel, new Vector2(screen.x, screen.y));
                dot.style.left = panelPos.x - 8f;
                dot.style.top = panelPos.y - 8f;
                dot.style.backgroundColor = pending.Color;
                VisualElement core = dot.childCount > 0 ? dot[0] : null;
                if (core != null)
                    core.style.backgroundColor = pending.Color;
                DMUiToolkitOverlayDocument.SetShown(dot, true);
                shown++;
            }

            RecycleDots(shown);
        }

        private void PaintBars()
        {
            if (barsLayer == null || barsLayer.panel == null)
                return;

            Camera camera = worldCamera;
            FloatingTargetHealthBar[] bars = Object.FindObjectsByType<FloatingTargetHealthBar>(FindObjectsInactive.Exclude);
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
                VisualElement fill = host.Q<VisualElement>("fill");
                Label label = host.Q<Label>("hp");
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
                VisualElement glow = new VisualElement { pickingMode = PickingMode.Ignore };
                glow.AddToClassList("dmg-world-dot");
                VisualElement core = new VisualElement { pickingMode = PickingMode.Ignore };
                core.AddToClassList("dmg-world-dot-core");
                glow.Add(core);
                dotsLayer.Add(glow);
                dotPool.Add(glow);
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
            HideNamedLayer("PickupProximityDots");
            HideNamedLayer("WorldInteractionDots");
            uguiHidden = true;
        }

        private static void HideNamedLayer(string objectName)
        {
            GameObject layer = DMUiToolkitOverlayDocument.FindNamed(objectName);
            if (layer == null)
                return;
            global::UnityEngine.CanvasGroup group = layer.GetComponent<global::UnityEngine.CanvasGroup>();
            if (group == null)
                group = layer.AddComponent<global::UnityEngine.CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        private static void HideBarGraphics(FloatingTargetHealthBar bar)
        {
            if (bar == null)
                return;
            global::UnityEngine.CanvasGroup group = bar.GetComponent<global::UnityEngine.CanvasGroup>();
            if (group == null)
                group = bar.gameObject.AddComponent<global::UnityEngine.CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }
    }
}
