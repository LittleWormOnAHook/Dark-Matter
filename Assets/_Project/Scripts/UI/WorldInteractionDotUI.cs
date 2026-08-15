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
    /// Blueprint scroll dots are owned by PickupProximityDotUI.
    /// </summary>
    public class WorldInteractionDotUI : MonoBehaviour
    {
        public static WorldInteractionDotUI Instance { get; private set; }

        private const float ScanInterval = 0.12f;

        private struct DotAnchor
        {
            public Transform Transform;
            public float HeightOffset;
            public Color Color;
        }

        [SerializeField] private float verticalWorldOffset = ProximityDotStyle.DefaultWorldOffset;

        private readonly Dictionary<Object, RectTransform> activeDots = new Dictionary<Object, RectTransform>();
        private readonly Dictionary<Object, DotAnchor> activeAnchors = new Dictionary<Object, DotAnchor>();
        private readonly HashSet<Object> visibleThisFrame = new HashSet<Object>();
        private readonly Stack<RectTransform> dotPool = new Stack<RectTransform>();
        private readonly List<Object> staleScratch = new List<Object>(16);

        private RectTransform dotLayer;
        private Canvas rootCanvas;
        private Camera worldCamera;
        private Transform playerTransform;
        private float nextScanTime;

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

            if (Time.unscaledTime >= nextScanTime)
            {
                nextScanTime = Time.unscaledTime + ScanInterval;
                visibleThisFrame.Clear();
                ScanInteractables();
                HideStaleDots();
            }

            RepositionActiveDots();
        }

        private void ScanInteractables()
        {
            ScanQuestGivers();
            ScanCraftingStations();
            ScanBuildingPanels();
            ScanLootBags();
            ScanPetAdoptables();
            ScanInjuredRecoverables();
            ScanEchoEntities();
        }

        private void RepositionActiveDots()
        {
            foreach (KeyValuePair<Object, RectTransform> pair in activeDots)
            {
                if (!activeAnchors.TryGetValue(pair.Key, out DotAnchor anchor) || anchor.Transform == null)
                    continue;

                PositionDot(pair.Value, anchor.Transform.position, anchor.HeightOffset);
            }
        }

        private void ScanQuestGivers()
        {
            QuestGiverNpc[] givers = SceneComponentCache.GetAll<QuestGiverNpc>();
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
            CraftingStation[] stations = SceneComponentCache.GetAll<CraftingStation>();
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
            BuildingControlPanel[] panels = SceneComponentCache.GetAll<BuildingControlPanel>();
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
            EnemyLootBag[] bags = SceneComponentCache.GetAll<EnemyLootBag>();
            for (int i = 0; i < bags.Length; i++)
            {
                EnemyLootBag bag = bags[i];
                if (bag == null || !bag.CanPlayerLoot(playerTransform.position))
                    continue;

                ShowDot(bag, bag.transform, verticalWorldOffset, ProximityDotStyle.LootColor);
            }
        }

        private void ScanPetAdoptables()
        {
            PetWorldAdoptable[] adoptables = SceneComponentCache.GetAll<PetWorldAdoptable>();
            for (int i = 0; i < adoptables.Length; i++)
            {
                PetWorldAdoptable adoptable = adoptables[i];
                if (adoptable == null)
                    continue;

                if (!IsWithinRange(adoptable.transform.position, adoptable.InteractRange))
                    continue;

                ShowDot(adoptable, adoptable.transform, 0.55f, ProximityDotStyle.PetColor);
            }
        }

        private void ScanInjuredRecoverables()
        {
            InjuredPioneerLabRecoverable[] recoverables =
                SceneComponentCache.GetAll<InjuredPioneerLabRecoverable>();
            for (int i = 0; i < recoverables.Length; i++)
            {
                InjuredPioneerLabRecoverable recoverable = recoverables[i];
                if (recoverable == null || !recoverable.CanShowInteractionHint())
                    continue;

                if (!IsWithinRange(recoverable.transform.position, recoverable.InteractRange))
                    continue;

                ShowDot(recoverable, recoverable.transform, 0.85f, ProximityDotStyle.ScienceLabColor);
            }
        }

        private void ScanEchoEntities()
        {
            EchoWorldEntity[] echoes = SceneComponentCache.GetAll<EchoWorldEntity>();
            for (int i = 0; i < echoes.Length; i++)
            {
                EchoWorldEntity echo = echoes[i];
                if (echo == null || !echo.IsInteractable)
                    continue;

                if (!IsWithinRange(echo.transform.position, echo.InteractRange))
                    continue;

                ShowDot(echo, echo.transform, 1f, ProximityDotStyle.EchoColor);
            }
        }

        private bool IsWithinRange(Vector3 worldPosition, float range)
        {
            float rangeSqr = range * range;
            return (worldPosition - playerTransform.position).sqrMagnitude <= rangeSqr;
        }

        private void ShowDot(Object owner, Transform anchor, float heightOffset, Color color)
        {
            if (owner == null || anchor == null)
                return;

            visibleThisFrame.Add(owner);
            activeAnchors[owner] = new DotAnchor
            {
                Transform = anchor,
                HeightOffset = heightOffset,
                Color = color
            };

            if (!activeDots.TryGetValue(owner, out RectTransform dotRect))
            {
                dotRect = AcquireDot(color);
                activeDots[owner] = dotRect;
            }
            else
            {
                ProximityDotStyle.ApplyColor(dotRect, color);
            }
        }

        private void PositionDot(RectTransform dotRect, Vector3 worldPosition, float heightOffset)
        {
            Vector3 worldPoint = worldPosition + Vector3.up * heightOffset;
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

            staleScratch.Clear();
            foreach (KeyValuePair<Object, RectTransform> pair in activeDots)
            {
                if (visibleThisFrame.Contains(pair.Key))
                    continue;

                staleScratch.Add(pair.Key);
            }

            for (int i = 0; i < staleScratch.Count; i++)
            {
                Object key = staleScratch[i];
                if (activeDots.TryGetValue(key, out RectTransform dotRect))
                    ReleaseDot(dotRect);

                activeDots.Remove(key);
                activeAnchors.Remove(key);
            }
        }

        private void HideAllDots()
        {
            foreach (KeyValuePair<Object, RectTransform> pair in activeDots)
                ReleaseDot(pair.Value);

            activeDots.Clear();
            activeAnchors.Clear();
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
                worldCamera = PlayerReference.ResolveCamera();

            if (playerTransform == null)
                playerTransform = PlayerReference.Transform ?? PlayerReference.ResolveTransform();

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
