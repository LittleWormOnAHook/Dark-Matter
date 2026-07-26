using System.Collections;
using System.Collections.Generic;
using Project.Core;
using Project.Map;
using Project.Player;
using Project.Vehicles;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Project.UI
{
    // WorldMapProvider binding, minimap auto-scale, player/vehicle world-position and camera-facing
    // resolution, and applying the resolved map texture to both display surfaces.
    // Split out of MapUI.cs.
    public partial class MapUI
    {
        private void HandleMapTextureReady()
        {
            ApplyMapTexture();
        }

        public void SyncMinimapSpanFromWorldBounds()
        {
            if (!autoScaleMinimapToTerrain)
                return;

            if (mapProvider == null)
                EnsureMapProvider();

            if (mapProvider == null)
                return;

            mapProvider.RefreshWorldBounds();
            float worldSpan = mapProvider.GetPlayableWorldSpan();
            if (worldSpan <= 0.01f)
                return;

            float scaledSpan = DefaultMinimapWorldSpan * (worldSpan / ReferenceTerrainSpan);
            minimapWorldSpan = Mathf.Clamp(scaledSpan, MinMinimapSpan, MaxMinimapSpan);
            UpdateMinimapInfoPanel();
        }

        private void EnsureMapProvider()
        {
            mapProvider = WorldMapProvider.Instance;
            if (mapProvider != null)
                return;

            mapProvider = FindAnyObjectByType<WorldMapProvider>();
            if (mapProvider == null)
                mapProvider = EnsureWorldMapProviderExists();
        }

        private static WorldMapProvider EnsureWorldMapProviderExists()
        {
            WorldMapProvider existing = WorldMapProvider.Instance ?? FindAnyObjectByType<WorldMapProvider>();
            if (existing != null)
                return existing;

            // Never spawn providers during play-mode teardown — that leaves orphan
            // "WorldMapProvider" objects Unity reports as scene cleanup leftovers.
            if (!Application.isPlaying || MapRuntimeCleanup.IsQuittingPlayMode)
                return null;

            Terrain terrain = FindAnyObjectByType<Terrain>();
            GameObject host = terrain != null ? terrain.gameObject : new GameObject("WorldMapProvider");
            if (terrain == null)
                host.hideFlags = HideFlags.None;

            return host.GetComponent<WorldMapProvider>() ?? host.AddComponent<WorldMapProvider>();
        }

        private void SetMapProviderActive(bool active)
        {
            if (mapProvider == null)
                EnsureMapProvider();

            if (mapProvider != null)
                mapProvider.ApplySystemEnabled(active);
        }

        private void BindPlayer()
        {
            if (playerTransform != null && playerController != null)
                return;

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player == null)
                return;

            if (playerTransform == null)
                playerTransform = player.transform;

            if (playerController == null)
                playerController = player.GetComponent<PlayerController>();
        }

        private float GetMapFacingYaw()
        {
            Camera facingCamera = ResolveMapFacingCamera();
            if (facingCamera != null)
                return facingCamera.transform.eulerAngles.y;

            if (playerController != null)
                return playerController.CameraYaw;

            return playerTransform != null ? playerTransform.eulerAngles.y : 0f;
        }

        private Camera ResolveMapFacingCamera()
        {
            Camera mainCamera = Camera.main;
            if (PlayerVehicleState.IsMounted && mainCamera != null)
                return mainCamera;

            if (playerController != null)
            {
                Camera gameplayCamera = playerController.GameplayCamera;
                if (gameplayCamera != null)
                    return gameplayCamera;
            }

            return mainCamera;
        }

        private bool HasMapWorldPosition()
        {
            if (PlayerVehicleState.IsMounted && PlayerVehicleState.ActiveCraft != null)
                return true;

            return playerTransform != null;
        }

        private Vector3 GetMapWorldPosition()
        {
            if (PlayerVehicleState.IsMounted && PlayerVehicleState.ActiveCraft != null)
            {
                HovercraftController craft = PlayerVehicleState.ActiveCraft;
                Transform craftTransform = craft.transform;
                Rigidbody body = craft.GetComponent<Rigidbody>();
                float speed = body != null ? body.linearVelocity.magnitude : 0f;
                bool moving = speed > VehicleMapPositionFreezeSpeed;

                if (!moving && hasStableMapWorldPosition)
                    return stableMapWorldPosition;

                Vector3 position = craftTransform.position;
                stableMapWorldPosition = position;
                hasStableMapWorldPosition = true;
                return position;
            }

            hasStableMapWorldPosition = false;
            return playerTransform != null ? playerTransform.position : Vector3.zero;
        }

        private void ApplyPlayerArrowRotation(RectTransform playerIconRect)
        {
            if (playerIconRect == null)
                return;

            playerIconRect.localEulerAngles = new Vector3(0f, 0f, -GetMapFacingYaw());
        }

        private void ApplyMapTexture()
        {
            Texture mapTexture = ResolveMapTexture();
            if (mapTexture == null)
                return;

            if (minimapImage != null)
            {
                minimapImage.texture = mapTexture;
                minimapImage.color = Color.white;
            }

            if (fullMapImage != null)
            {
                fullMapImage.texture = mapTexture;
                fullMapImage.color = Color.white;
            }

            SyncMapContentLayout();
        }

        private Texture ResolveMapTexture()
        {
            if (mapProvider == null)
                EnsureMapProvider();

            if (mapProvider != null && mapProvider.MapTexture != null)
                return mapProvider.MapTexture;

            return WorldMapProvider.CreateDisplayFallback();
        }

        private void SyncMapContentLayout()
        {
            if (!uiBuilt)
                return;

            UpdateMinimap();
            if (fullMapOpen)
                UpdateFullMap();
        }
    }
}
