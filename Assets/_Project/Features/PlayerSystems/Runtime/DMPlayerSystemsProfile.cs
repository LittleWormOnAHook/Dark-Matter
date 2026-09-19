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

        private bool _appliedClimb = true;
        private bool _appliedDash = true;
        private bool _appliedJetpack = true;
        private bool _appliedHeroLand = true;

        public bool ClimbEnabled => isActiveAndEnabled && climb && PassesSkillGate(DMSkillMovementSystemGates.IsClimbUnlockedBySkills);
        public bool DashEnabled => isActiveAndEnabled && dash && PassesSkillGate(DMSkillMovementSystemGates.IsDashUnlockedBySkills);
        public bool JetpackEnabled => isActiveAndEnabled && jetpack && PassesSkillGate(DMSkillMovementSystemGates.IsJetpackUnlockedBySkills);
        public bool HeroLandEnabled => isActiveAndEnabled && heroLand;

        private bool PassesSkillGate(System.Func<bool> unlocked) =>
            !requireSkillTreeUnlocks || unlocked();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureOnPlayer()
        {
            if (!Application.isPlaying)
                return;

            GameObject player = GameObject.Find("Player_v7");
            if (player == null)
                player = GameObject.Find("Player_v7 Variant");
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
            if (!force &&
                climbOn == _appliedClimb &&
                dashOn == _appliedDash &&
                jetOn == _appliedJetpack &&
                heroOn == _appliedHeroLand)
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
            SetEnabled<DMDashController>(dashOn);
            SetEnabled<DMJetpackController>(jetOn);
            SetEnabled<DMJetpackInputBridge>(jetOn);
            SetEnabled<DMLandingDirector>(heroOn);

            _appliedClimb = climbOn;
            _appliedDash = dashOn;
            _appliedJetpack = jetOn;
            _appliedHeroLand = heroOn;
        }

        private void SetEnabled<T>(bool on) where T : Behaviour
        {
            T c = GetComponent<T>();
            if (c != null && c.enabled != on)
                c.enabled = on;
        }

        private void EnsureComponent<T>() where T : Component
        {
            if (GetComponent<T>() == null)
                gameObject.AddComponent<T>();
        }
    }
}
