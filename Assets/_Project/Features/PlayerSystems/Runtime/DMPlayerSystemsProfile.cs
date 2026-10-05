using Project.Core;
using Project.World;
using Project.Features.Climb;
using Project.Features.Dash;
using Project.Features.Jetpack;
using Project.Progression;
using UnityEngine;

namespace Project.Player
{
    /// <summary>
    /// Live toggles for player movement systems. Sit this first on Player_v7.
    /// Leave everything on when testing is done.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-200)]
    [AddComponentMenu("Dark Matter/Player Systems Profile")]
    public sealed class DMPlayerSystemsProfile : MonoBehaviour
    {
        [Header("Systems")]
        [Tooltip("Free-climb cling (Space / E).")]
        public bool climb = true;

        [Tooltip("Double-tap WASD dash.")]
        public bool dash = true;

        [Tooltip("Jet boost on jump.")]
        public bool jetpack = true;

        [Tooltip("Hero land lock after a fall.")]
        public bool heroLand = true;

        [Header("Progression")]
        [Tooltip("When on, Climb / Dash / Jetpack require the matching Journal system anchor (Tier 0).")]
        public bool requireSkillTreeUnlocks = true;

        [Header("World")]
        [Tooltip("Final authority for Gaia terrain streaming, content scenes, expedition tile wait, and related world loaders. Off keeps Terrain Loader Manager disabled in Play and after returning to Edit.")]
        public bool terrainLoading = true;

        private bool _appliedClimb = true;
        private bool _appliedDash = true;
        private bool _appliedJetpack = true;
        private bool _appliedHeroLand = true;
        private bool _appliedTerrainLoading = true;

        public bool ClimbEnabled => isActiveAndEnabled && climb && PassesSkillGate(DMSkillMovementSystemGates.IsClimbUnlockedBySkills);
        public bool DashEnabled => isActiveAndEnabled && dash && PassesSkillGate(DMSkillMovementSystemGates.IsDashUnlockedBySkills);
        public bool JetpackEnabled => isActiveAndEnabled && jetpack && PassesSkillGate(DMSkillMovementSystemGates.IsJetpackUnlockedBySkills);
        public bool HeroLandEnabled => isActiveAndEnabled && heroLand;
        public bool TerrainLoadingEnabled => isActiveAndEnabled && terrainLoading;

        /// <summary>Authority for world terrain streaming (live player profile, else any active profile).</summary>
        public static bool IsWorldTerrainLoadingEnabled()
        {
            DMPlayerSystemsProfile profile = PlayerLocator.FindOnLivePlayer<DMPlayerSystemsProfile>();
            if (profile == null)
                profile = Object.FindFirstObjectByType<DMPlayerSystemsProfile>(FindObjectsInactive.Exclude);

            return profile == null || profile.TerrainLoadingEnabled;
        }

        /// <summary>Enables or pauses Gaia's Terrain Loader Manager when present.</summary>
        public static bool TryApplyGaiaTerrainLoader(bool on)
        {
            GameObject host = Gaia.GaiaUtils.GetTerrainLoaderManagerObject(false);
            Gaia.TerrainLoaderManager loader = host != null ? host.GetComponent<Gaia.TerrainLoaderManager>() : null;
            if (loader == null)
                return false;

            if (loader.enabled != on)
                loader.enabled = on;

            return true;
        }

        /// <summary>Re-applies Terrain Loading from the live scene profile (Edit or Play).</summary>
        public static void ApplyTerrainAuthorityFromProfiles()
        {
            DmGaiaTerrainStreamingGate.ApplyFromProfiles();
        }

        private bool PassesSkillGate(System.Func<bool> unlocked) =>
            !requireSkillTreeUnlocks || unlocked();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureOnPlayer()
        {
            if (!Application.isPlaying)
                return;

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player == null || player.GetComponent<DMPlayerSystemsProfile>() != null)
                return;

            player.AddComponent<DMPlayerSystemsProfile>();
        }

        private void Awake()
        {
            Apply(force: true);
        }

        private void OnValidate()
        {
            if (isActiveAndEnabled)
                Apply(force: true);
        }

        private void Update()
        {
            Apply(force: false);
        }

        private void Apply(bool force)
        {
            bool climbOn = ClimbEnabled;
            bool dashOn = DashEnabled;
            bool jetOn = JetpackEnabled;
            bool heroOn = HeroLandEnabled;
            bool terrainOn = TerrainLoadingEnabled;
            if (!force &&
                climbOn == _appliedClimb &&
                dashOn == _appliedDash &&
                jetOn == _appliedJetpack &&
                heroOn == _appliedHeroLand &&
                terrainOn == _appliedTerrainLoading)
                return;

            SetEnabled<DMClimbController>(climbOn);
            if (!climbOn)
            {
                var climbCtrl = GetComponent<DMClimbController>();
                if (climbCtrl != null)
                    climbCtrl.CancelClimb();
            }

            EnsureComponent<DMDashController>();
            EnsureComponent<DMHangLegOverlay>();
            EnsureComponent<DMLandingDirector>();
            SetEnabled<DMDashController>(dashOn);
            SetEnabled<DMJetpackController>(jetOn);
            SetEnabled<DMJetpackInputBridge>(jetOn);
            SetEnabled<DMLandingDirector>(heroOn);

            _appliedClimb = climbOn;
            _appliedDash = dashOn;
            _appliedJetpack = jetOn;
            _appliedHeroLand = heroOn;

            // Retry next frame if loading should be off but the loader isn't in the scene yet.
            _appliedTerrainLoading = ApplyTerrainLoading(terrainOn) ? terrainOn : !terrainOn;
        }

        private static bool ApplyTerrainLoading(bool on) =>
            TryApplyGaiaTerrainLoader(on) || !on;

        private void SetEnabled<T>(bool on) where T : Behaviour
        {
            T c = GetComponent<T>();
            if (c != null && c.enabled != on)
                c.enabled = on;
        }

        private void EnsureComponent<T>() where T : Component
        {
            if (GetComponent<T>() != null)
                return;
            // AddComponent SendMessages OnDidAddComponent; Unity forbids that in OnValidate.
            if (!Application.isPlaying)
                return;
            gameObject.AddComponent<T>();
        }
    }
}
