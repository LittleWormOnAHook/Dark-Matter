using System.Collections.Generic;
using Project.Building;
using Project.Companions;
using Project.Combat;
using Project.Core;
using Project.Crafting;
using Project.Echoes;
using Project.Pet;
using Project.Quests;
using UnityEngine;

namespace Project.UI
{
    /// <summary>
    /// Colored proximity dots for Press-E interactables (NPCs, stations, loot, pets, etc.).
    /// </summary>
    public class WorldInteractionDotUI : MonoBehaviour
    {
        public static WorldInteractionDotUI Instance { get; private set; }

        [SerializeField] private float verticalWorldOffset = ProximityDotStyle.DefaultWorldOffset;

        private readonly Dictionary<Object, RectTransform> activeDots = new Dictionary<Object, RectTransform>();
        private readonly HashSet<Object> visibleThisFrame = new HashSet<Object>();
        private readonly Stack<RectTransform> dotPool = new Stack<RectTransform>();

        private RectTransform dotLayer;
        private Canvas rootCanvas;
        private Camera worldCamera;
        private Transform playerTransform;

        private void Awake()
        {
            Instance = this;
            BuildDotLayer();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void LateUpdate()
        {
            if (!GameSession.HasStarted)
            {
                HideAllDots();
                return;
            }

            if (!ResolveReferences())
                return;

            visibleThisFrame.Clear();
            ScanInteractables();
            HideStaleDots();
        }

        private void ScanInteractables()
        {
            ScanQuestGivers();
            ScanCraftingStations();
            ScanBuildingPanels();
            ScanLootBags();
            ScanRecipePickups();
            ScanPetAdoptables();
            ScanInjuredRecoverables();
            ScanEchoEntities();
        }

        private void ScanQuestGivers()
        {
            QuestGiverNpc[] givers = FindObjectsByType<QuestGiverNpc>(FindObjectsInactive.Exclude);
            for (int i = 0; i < givers.Length; i++)
            {
                QuestGiverNpc giver = givers[i];
                if (giver == null || !giver.IsWithinInteractRange(playerTransform.position))
                    continue;

                ShowDot(giver, giver.transform, 1.15f, ProximityDotStyle.QuestGiverColor);
            }
        }

        private void ScanCraftingStations()
        {
            CraftingStation[] stations = FindObjectsByType<CraftingStation>(FindObjectsInactive.Exclude);
            for (int i = 0; i < stations.Length; i++)
            {
                CraftingStation station = stations[i];
                if (station == null || !station.IsWithinInteractRange(playerTransform.position))
                    continue;

                ShowDot(station, station.transform, 0.75f, ProximityDotStyle.CraftingColor);
            }
        }

        private void ScanBuildingPanels()
        {
            BuildingControlPanel[] panels = FindObjectsByType<BuildingControlPanel>(FindObjectsInactive.Exclude);
            for (int i = 0; i < panels.Length; i++)
            {
                BuildingControlPanel panel = panels[i];
                if (panel == null || !panel.IsWithinInteractRange(playerTransform.position))
                    continue;

                ShowDot(panel, panel.transform, 0.9f, ProximityDotStyle.BuildingColor);
            }
        }

        private void ScanLootBags()
        {
            EnemyLootBag[] bags = FindObjectsByType<EnemyLootBag>(FindObjectsInactive.Exclude);
            for (int i = 0; i < bags.Length; i++)
            {
                EnemyLootBag bag = bags[i];
                if (bag == null || !bag.CanPlayerLoot(playerTransform.position))
                    continue;

                ShowDot(bag, bag.transform, verticalWorldOffset, ProximityDotStyle.LootColor);
            }
        }

        private void ScanRecipePickups()
        {
            RecipePickup[] pickups = FindObjectsByType<RecipePickup>(FindObjectsInactive.Exclude);
            for (int i = 0; i < pickups.Length; i++)
            {
                RecipePickup pickup = pickups[i];
                if (pickup == null || pickup.IsLearned)
                    continue;

                float distance = Vector3.Distance(playerTransform.position, pickup.transform.position);
                if (distance > pickup.InteractRange)
                    continue;

                ShowDot(pickup, pickup.transform, verticalWorldOffset, ProximityDotStyle.RecipeColor);
            }
        }

        private void ScanPetAdoptables()
        {
            PetWorldAdoptable[] adoptables = FindObjectsByType<PetWorldAdoptable>(FindObjectsInactive.Exclude);
            for (int i = 0; i < adoptables.Length; i++)
            {
                PetWorldAdoptable adoptable = adoptables[i];
                if (adoptable == null)
                    continue;

                float distance = Vector3.Distance(playerTransform.position, adoptable.transform.position);
                if (distance > adoptable.InteractRange)
                    continue;

                ShowDot(adoptable, adoptable.transform, 0.55f, ProximityDotStyle.PetColor);
            }
        }

        private void ScanInjuredRecoverables()
        {
            InjuredPioneerLabRecoverable[] recoverables =
                FindObjectsByType<InjuredPioneerLabRecoverable>(FindObjectsInactive.Exclude);
            for (int i = 0; i < recoverables.Length; i++)
            {
                InjuredPioneerLabRecoverable recoverable = recoverables[i];
                if (recoverable == null || string.IsNullOrEmpty(recoverable.GetPromptText()))
                    continue;

                float distance = Vector3.Distance(playerTransform.position, recoverable.transform.position);
                if (distance > recoverable.InteractRange)
                    continue;

                ShowDot(recoverable, recoverable.transform, 0.85f, ProximityDotStyle.ScienceLabColor);
            }
        }

        private void ScanEchoEntities()
        {
            EchoWorldEntity[] echoes = FindObjectsByType<EchoWorldEntity>(FindObjectsInactive.Exclude);
            for (int i = 0; i < echoes.Length; i++)
            {
                EchoWorldEntity echo = echoes[i];
                if (echo == null || !echo.IsInteractable)
                    continue;

                float distance = Vector3.Distance(playerTransform.position, echo.transform.position);
                if (distance > echo.InteractRange)
                    continue;

                ShowDot(echo, echo.transform, 1f, ProximityDotStyle.EchoColor);
            }
        }

        private void ShowDot(Object owner, Transform anchor, float heightOffset, Color color)
        {
            if (owner == null || anchor == null)
                return;

            visibleThisFrame.Add(owner);

            if (!activeDots.TryGetValue(owner, out RectTransform dotRect))
            {
                dotRect = AcquireDot(color);
                activeDots[owner] = dotRect;
            }
            else
            {
                ProximityDotStyle.ApplyColor(dotRect, color);
            }

            Vector3 worldPoint = anchor.position + Vector3.up * heightOffset;
            Vector3 screenPoint = worldCamera.WorldToScreenPoint(worldPoint);
            if (screenPoint.z <= 0f)
            {
                dotRect.gameObject.SetActive(false);
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    dotLayer,
                    screenPoint,
                    rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : worldCamera,
                    out Vector2 localPoint))
            {
                dotRect.gameObject.SetActive(true);
                dotRect.anchoredPosition = localPoint;
            }
        }

        private void HideStaleDots()
        {
            if (activeDots.Count == 0)
                return;

            List<Object> stale = null;
            foreach (KeyValuePair<Object, RectTransform> pair in activeDots)
            {
                if (visibleThisFrame.Contains(pair.Key))
                    continue;

                stale ??= new List<Object>();
                stale.Add(pair.Key);
            }

            if (stale == null)
                return;

            for (int i = 0; i < stale.Count; i++)
            {
                Object key = stale[i];
                if (activeDots.TryGetValue(key, out RectTransform dotRect))
                    ReleaseDot(dotRect);

                activeDots.Remove(key);
            }
        }

        private void HideAllDots()
        {
            foreach (KeyValuePair<Object, RectTransform> pair in activeDots)
                ReleaseDot(pair.Value);

            activeDots.Clear();
            visibleThisFrame.Clear();
        }

        private RectTransform AcquireDot(Color color)
        {
            RectTransform dotRect = dotPool.Count > 0 ? dotPool.Pop() : ProximityDotStyle.CreateDotWidget(dotLayer);
            ProximityDotStyle.ApplyColor(dotRect, color);
            dotRect.gameObject.SetActive(true);
            return dotRect;
        }

        private void ReleaseDot(RectTransform dotRect)
        {
            if (dotRect == null)
                return;

            dotRect.gameObject.SetActive(false);
            dotPool.Push(dotRect);
        }

        private bool ResolveReferences()
        {
            if (rootCanvas == null)
                rootCanvas = GetComponentInParent<Canvas>();

            if (worldCamera == null)
                worldCamera = Camera.main;

            if (playerTransform == null)
            {
                GameObject player = PlayerLocator.FindPlayerObject();
                if (player != null)
                    playerTransform = player.transform;
            }

            return rootCanvas != null && worldCamera != null && playerTransform != null;
        }

        private void BuildDotLayer()
        {
            GameObject layerObject = new GameObject("WorldInteractionDots", typeof(RectTransform));
            layerObject.transform.SetParent(transform, false);

            dotLayer = layerObject.GetComponent<RectTransform>();
            dotLayer.anchorMin = Vector2.zero;
            dotLayer.anchorMax = Vector2.one;
            dotLayer.offsetMin = Vector2.zero;
            dotLayer.offsetMax = Vector2.zero;
            dotLayer.SetAsFirstSibling();
        }
    }
}
