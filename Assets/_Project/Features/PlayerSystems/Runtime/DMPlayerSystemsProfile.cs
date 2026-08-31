using Project.Features.Climb;
using Project.Features.Dash;
using Project.Features.Jetpack;
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

        private bool _appliedClimb = true;
        private bool _appliedDash = true;
        private bool _appliedJetpack = true;
        private bool _appliedHeroLand = true;

        public bool ClimbEnabled => isActiveAndEnabled && climb;
        public bool DashEnabled => isActiveAndEnabled && dash;
        public bool JetpackEnabled => isActiveAndEnabled && jetpack;
        public bool HeroLandEnabled => isActiveAndEnabled && heroLand;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureOnPlayer()
        {
            if (!Application.isPlaying)
                return;

            GameObject player = GameObject.Find("Player_v7");
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
            if (!force &&
                climb == _appliedClimb &&
                dash == _appliedDash &&
                jetpack == _appliedJetpack &&
                heroLand == _appliedHeroLand)
                return;

            SetEnabled<DMClimbController>(climb);
            if (!climb)
            {
                var climbCtrl = GetComponent<DMClimbController>();
                if (climbCtrl != null)
                    climbCtrl.CancelClimb();
            }

            SetEnabled<DMDashController>(dash);
            SetEnabled<DMJetpackController>(jetpack);
            SetEnabled<DMJetpackInputBridge>(jetpack);
            SetEnabled<DMLandingDirector>(heroLand);

            _appliedClimb = climb;
            _appliedDash = dash;
            _appliedJetpack = jetpack;
            _appliedHeroLand = heroLand;
        }

        private void SetEnabled<T>(bool on) where T : Behaviour
        {
            T c = GetComponent<T>();
            if (c != null && c.enabled != on)
                c.enabled = on;
        }
    }
}
