using System.Collections.Generic;
using Invector.vCharacterController;
using Project.AI;
using Project.Features.Climb;
using Project.Inventory;
using Project.Player.Invector;
using Project.UI;
using UnityEngine;

namespace Project.Features.Locomotion
{
    public enum DMStrafeLocomotionMode
    {
        [Tooltip("Director does nothing (legacy free locomotion + S backpedal helper).")]
        Off = 0,
        [Tooltip("Strafe only in combat context (weapon drawn + enemy engaged); free otherwise.")]
        CombatContext = 1,
        [Tooltip("Free inside the forward cone, strafe outside it (side steps, diagonals, backpedal). Combat context = full strafe.")]
        Hybrid = 2,
        [Tooltip("Always strafe while moving (burst sprint stays free).")]
        AlwaysStrafe = 3
    }

    /// <summary>
    /// 2026-10-20c (Strafe plan P1): single owner of <c>cc.isStrafing</c> for non-aim / non-lock-on cases.
    /// Assigns (never toggles) once per frame from <see cref="DMLocomotionGaitController.TickLocomotion"/>, which runs
    /// after Invector's InputHandle, so Invector's Strafe() toggles earlier in the frame are overwritten before FixedUpdate.
    /// Settings: DM_ClimbDashProfile "Locomotion - strafe / backpedal" (Genesis Studio > Survival > Walk / Run / Sprint).
    /// No physics / capsule / rigidbody / root-motion changes (player-physics rule).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMStrafeLocomotionDirector : MonoBehaviour
    {
        private vThirdPersonController _cc;
        private PioneerShooterMeleeInput _input;
        private EquipmentController _equipment;
        private DMClimbController _climb;
        private bool _strafeWanted;
        private bool _owning;
        private float _modeChangedAt = -99f;
        private float _nextCombatScan;
        private bool _combatContext;
        private float _lastAngle;
        private string _lastReason = "";
        private static readonly List<EnemyHealth> s_enemies = new List<EnemyHealth>(64);

        public static DMStrafeLocomotionDirector Instance { get; private set; }

        /// <summary>True while the director owns the strafe flag (PSMI's legacy backpedal helper stands down).</summary>
        public bool IsActive => isActiveAndEnabled && Mode != DMStrafeLocomotionMode.Off;
        public bool StrafeWanted => _strafeWanted;
        public bool CombatContextActive => _combatContext;

        private static DM_ClimbDashProfile P => DM_ClimbDashProfile.Live;
        private static DMStrafeLocomotionMode Mode => P != null ? P.strafeLocoMode : DMStrafeLocomotionMode.Hybrid;

        public static DMStrafeLocomotionDirector Ensure(GameObject player)
        {
            if (player == null) return null;
            var d = player.GetComponent<DMStrafeLocomotionDirector>();
            return d != null ? d : player.AddComponent<DMStrafeLocomotionDirector>();
        }

        private void Awake()
        {
            _cc = GetComponent<vThirdPersonController>();
            _input = GetComponent<PioneerShooterMeleeInput>();
            _equipment = GetComponent<EquipmentController>();
            _climb = GetComponent<DMClimbController>();
        }

        private void OnEnable() => Instance = this;
        private void OnDisable() { if (Instance == this) Instance = null; }

        /// <summary>Camera-relative input angle in degrees (0 = forward, +-180 = back). Raw stick/keys (rotateByWorld off).</summary>
        public static float InputAngle(vThirdPersonController cc)
        {
            if (cc == null) return 0f;
            return Mathf.Atan2(cc.input.x, cc.input.z) * Mathf.Rad2Deg;
        }

        /// <summary>Burst sprint gate: input must be within sprintForwardConeHalfAngle of camera forward (Off = always allowed).</summary>
        public static bool SprintConeAllows(vThirdPersonController cc)
        {
            var p = P;
            if (p == null || p.strafeLocoMode == DMStrafeLocomotionMode.Off || cc == null) return true;
            Vector2 v = new Vector2(cc.input.x, cc.input.z);
            if (v.sqrMagnitude < 0.0001f) return true; // standing: allow arming; checked again on move
            return Mathf.Abs(InputAngle(cc)) <= p.strafeLocoSprintConeHalfAngle;
        }

        /// <summary>Called once per frame after Invector input (DMLocomotionGaitController.TickLocomotion).</summary>
        public void Tick()
        {
            if (_cc == null || Mode == DMStrafeLocomotionMode.Off)
            {
                ReleaseOwnership();
                return;
            }

            if (ShouldYield(out string why))
            {
                _lastReason = "yield: " + why;
                _owning = false; // other systems own the flag; re-resolve when they let go
                return;
            }

            var p = P;
            UpdateCombatContext(p);

            Vector2 move = new Vector2(_cc.input.x, _cc.input.z);
            float deadzone = p != null ? p.strafeLocoPadDeadzoneMagnitude : 0.2f;
            bool moving = move.magnitude > deadzone;
            float angle = moving ? Mathf.Abs(InputAngle(_cc)) : _lastAngle;
            _lastAngle = angle;

            bool want = Resolve(p, moving, angle);

            float hold = p != null ? p.strafeLocoMinHoldSeconds : 0.15f;
            if (want != _strafeWanted && Time.time - _modeChangedAt >= hold)
            {
                _strafeWanted = want;
                _modeChangedAt = Time.time;
            }

            // Mode switches only on the ground (no mid-roll / mid-air flips; yields cover those).
            _cc.isStrafing = _strafeWanted;
            _owning = true;
            _lastReason = (_strafeWanted ? "strafe" : "free") + (_combatContext ? " (combat)" : "") + $" angle {angle:0}";
        }

        private bool Resolve(DM_ClimbDashProfile p, bool moving, float angle)
        {
            DMStrafeLocomotionMode mode = p != null ? p.strafeLocoMode : DMStrafeLocomotionMode.Hybrid;
            if (_cc.isSprinting)
                return false; // burst sprint is always free (sprintOnlyFree)

            bool combatFull = _combatContext && (p == null || p.strafeLocoCombatFullStrafe);
            switch (mode)
            {
                case DMStrafeLocomotionMode.AlwaysStrafe:
                    return moving || _strafeWanted;
                case DMStrafeLocomotionMode.CombatContext:
                    return _combatContext;
                case DMStrafeLocomotionMode.Hybrid:
                default:
                    // 2026-10-20e: combat context keeps the forward cone (W / W+A / W+D stay free); it only adds strafe while idle.
                    if (combatFull && p != null && p.strafeLocoCombatIgnoresForwardCone)
                        return true; // legacy: full strafe in combat
                    if (!moving)
                        return combatFull;
                    float cone = p != null ? p.strafeLocoForwardConeHalfAngle : 67f;
                    float hyst = p != null ? p.strafeLocoHysteresis : 10f;
                    // Hysteresis: once strafing, need to come back inside (cone - hyst) to go free; once free, leave (cone + hyst).
                    return _strafeWanted ? angle > cone - hyst : angle > cone + hyst;
            }
        }

        private bool ShouldYield(out string why)
        {
            why = null;
            if (_cc.isDead) why = "dead";
            else if (_cc.ragdolled) why = "ragdoll";
            else if (_cc.lockInStrafe) why = "lock-on";
            else if (_cc.isRolling) why = "roll";
            else if (!_cc.isGrounded || _cc.isJumping) why = "air";
            else if (_cc.customAction) why = "customAction";
            else if (_input != null && _input.IsAimingActive) why = "aim";
            else if (_input != null && _input.IsMeleeComboActive) why = "melee combo";
            else if (_climb != null && _climb.IsClimbing) why = "climb";
            else if (GameplayKeyboardShortcuts.IsGameplayInputLockedByUi()) why = "ui";
            return why != null;
        }

        private void UpdateCombatContext(DM_ClimbDashProfile p)
        {
            if (Time.time < _nextCombatScan) return;
            _nextCombatScan = Time.time + 0.25f;
            _combatContext = false;
            if (_equipment == null || !_equipment.IsWeaponDrawn) return;
            float r = p != null ? p.strafeLocoCombatEngageRadius : 12f;
            float r2 = r * r;
            Vector3 me = transform.position;
            s_enemies.Clear();
            EnemyHealthSceneRegistry.CopyTo(s_enemies);
            for (int i = 0; i < s_enemies.Count; i++)
            {
                EnemyHealth h = s_enemies[i];
                if (h == null || h.IsDead) continue;
                if ((h.transform.position - me).sqrMagnitude <= r2) { _combatContext = true; break; }
            }
        }

        private void ReleaseOwnership()
        {
            if (_owning && _cc != null && _strafeWanted)
                _cc.isStrafing = false;
            _owning = false;
            _strafeWanted = false;
        }

        private void OnGUI()
        {
            var p = P;
            if (p == null || !p.strafeLocoDebugOverlay) return;
            GUI.Label(new Rect(12, 140, 420, 22), "Strafe director: " + _lastReason + "  isStrafing=" + (_cc != null && _cc.isStrafing));
        }
    }
}
