using System.Collections.Generic;
using Project.Core;
using Project.Interaction;
using Project.Map;
using Project.UI;
using UnityEngine;

namespace Project.Vendor
{
    [RequireComponent(typeof(BoxCollider))]
    public class DMVendorNpc : MonoBehaviour, IVendor, IWorldUsable
    {
        public const float AimRadius = 1.25f;

        private static readonly List<DMVendorNpc> active = new List<DMVendorNpc>(4);

        [SerializeField] private DMVendorProfile profile;
        [SerializeField] private float interactRange = 3.5f;

        private Collider interactCollider;

        public static IReadOnlyList<DMVendorNpc> Active => active;

        public DMVendorProfile Profile => profile;
        public string VendorId => profile != null ? profile.vendorId : string.Empty;
        public string DisplayName => profile != null ? profile.displayName : name;
        public Collider InteractCollider => interactCollider;

        private void Awake()
        {
            interactCollider = GetComponent<Collider>();
            if (interactCollider == null)
                interactCollider = GetComponentInChildren<Collider>();
            if (interactCollider != null)
                interactCollider.isTrigger = true;
            if (profile != null && interactRange <= 0.01f)
                interactRange = profile.interactRange;
        }

        private void OnEnable()
        {
            if (!active.Contains(this))
                active.Add(this);
            WorldUseController.Register(this);
            DMVendorRuntime.EnsureHooked();
            if (profile != null)
                DMVendorRuntime.GetOrCreate(profile);
            ConfigureMapPoi();
        }

        private void ConfigureMapPoi()
        {
            string poiName = !string.IsNullOrWhiteSpace(DisplayName) ? DisplayName : "Vendor";
            ScannableTarget scan = GetComponent<ScannableTarget>();
            if (scan == null)
                scan = gameObject.AddComponent<ScannableTarget>();
            scan.Configure(poiName, DarkMatterGenesisUiPalette.MapPoiBlue, ScannerTargetCategory.Interactable, true, false, true);

            MapMarker marker = GetComponent<MapMarker>();
            if (marker == null)
                marker = gameObject.AddComponent<MapMarker>();
            marker.ConfigureScannedPoi(poiName, DarkMatterGenesisUiPalette.MapPoiBlue);
        }

        private void OnDisable()
        {
            active.Remove(this);
            WorldUseController.Unregister(this);
        }

        public bool CanOpenShop(WorldUseContext context)
        {
            return profile != null && GameSession.HasStarted && IsWithinInteractRange(context.PlayerPosition);
        }

        public bool TryOpenShop(WorldUseContext context)
        {
            if (!CanOpenShop(context))
                return false;
            return DMUiToolkitVendor.TryShow(this);
        }

        public float GetUsePriority(WorldUseContext context)
        {
            if (!WorldUseController.IsAimedAtVendor(context, this, interactCollider))
                return -1f;
            if (!CanOpenShop(context))
                return -1f;

            float distance = PlayerInteractionUtility.DistanceToInteractable(
                context.PlayerPosition,
                interactCollider,
                transform.position);
            return 90f - distance;
        }

        public bool TryUse(WorldUseContext context)
        {
            return TryOpenShop(context);
        }

        public bool IsWithinInteractRange(Vector3 playerPosition)
        {
            float range = profile != null ? profile.interactRange : interactRange;
            return PlayerInteractionUtility.DistanceToInteractable(
                playerPosition,
                interactCollider,
                transform.position) <= range;
        }

        public string GetInteractionPromptMessage()
        {
            if (profile != null && !string.IsNullOrWhiteSpace(profile.promptText))
                return profile.promptText;
            return "Press E — " + DisplayName;
        }

        public void Configure(DMVendorProfile vendorProfile)
        {
            profile = vendorProfile;
            if (profile != null)
                interactRange = profile.interactRange;
        }
    }
}
