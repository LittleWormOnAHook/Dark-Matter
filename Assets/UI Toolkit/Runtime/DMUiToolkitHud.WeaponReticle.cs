using Invector.vCharacterController;
using Project.Combat;
using Project.Data;
using Project.Interaction;
using Project.Inventory;
using Project.Player;
using Project.Player.Invector;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Center-screen UITK weapon reticle: four arms + dot, spread via translate (no world projection).
    /// Hidden unless ADS with a drawn ranged weapon.
    /// </summary>
    public partial class DMUiToolkitHud
    {
        private const float ReticleBaseGapPx = 6f;
        private const float ReticleArmHalfLengthPx = 5f;
        private const float ReticleMaxSpreadPx = 22f;
        private const float ReticleMovementSpreadPx = 6f;
        private const float ReticleSpreadSmoothLambda = 16f;
        private const float ReticleHitFlashSeconds = 0.1f;

        private VisualElement weaponReticleRoot;
        private VisualElement reticleSpreadRoot;
        private VisualElement reticleDot;
        private VisualElement reticleArmLeft;
        private VisualElement reticleArmRight;
        private VisualElement reticleArmTop;
        private VisualElement reticleArmBottom;

        private bool weaponReticleBound;
        private bool lastReticleShown;
        private int lastSpreadPx = -1;
        private float smoothedSpreadPx;
        private float reticleHitFlashUntil;

        private EquipmentController reticleEquipment;
        private PioneerInvectorInputBridge reticleInvectorBridge;
        private RangedCombatController reticleRangedCombat;
        private PlayerController reticlePlayerController;
        private vThirdPersonMotor reticleMotor;
        private WeaponAmmoState reticleAmmoState;

        private void BindWeaponReticle(VisualElement root)
        {
            if (root == null)
                return;

            weaponReticleRoot = root.Q<VisualElement>("weapon-reticle");
            if (weaponReticleRoot == null)
                return;

            reticleSpreadRoot = weaponReticleRoot.Q<VisualElement>("reticle-spread");
            reticleDot = weaponReticleRoot.Q<VisualElement>("reticle-dot");
            reticleArmLeft = weaponReticleRoot.Q<VisualElement>("reticle-arm-left");
            reticleArmRight = weaponReticleRoot.Q<VisualElement>("reticle-arm-right");
            reticleArmTop = weaponReticleRoot.Q<VisualElement>("reticle-arm-top");
            reticleArmBottom = weaponReticleRoot.Q<VisualElement>("reticle-arm-bottom");

            weaponReticleBound = reticleSpreadRoot != null
                && reticleArmLeft != null
                && reticleArmRight != null
                && reticleArmTop != null
                && reticleArmBottom != null;

            HideWeaponReticlePreview();
        }

        private void HideWeaponReticlePreview()
        {
            if (weaponReticleRoot != null)
                weaponReticleRoot.style.display = DisplayStyle.None;

            lastReticleShown = false;
            lastSpreadPx = -1;
            smoothedSpreadPx = 0f;
        }

        private void TickWeaponReticle()
        {
            if (!weaponReticleBound || weaponReticleRoot == null)
                return;

            bool show = ShouldShowWeaponReticle();
            if (show != lastReticleShown)
            {
                lastReticleShown = show;
                weaponReticleRoot.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                if (!show)
                {
                    lastSpreadPx = -1;
                    smoothedSpreadPx = 0f;
                    return;
                }
            }

            if (!show)
                return;

            float targetSpread = ComputeTargetSpreadPx();
            float dt = Time.deltaTime;
            if (dt <= 0f)
                dt = 0.02f;
            smoothedSpreadPx = Mathf.Lerp(
                smoothedSpreadPx,
                targetSpread,
                1f - Mathf.Exp(-ReticleSpreadSmoothLambda * dt));

            int spreadPx = Mathf.RoundToInt(smoothedSpreadPx);
            if (spreadPx != lastSpreadPx)
            {
                lastSpreadPx = spreadPx;
                ApplyReticleSpreadPx(spreadPx);
            }

            bool hitFlash = Time.unscaledTime < reticleHitFlashUntil;
            weaponReticleRoot.EnableInClassList("dmg-weapon-reticle-hit", hitFlash);
        }

        /// <summary>Call from combat feedback when a shot connects (optional).</summary>
        public static void PulseWeaponReticleHit()
        {
            if (InstanceOrNull == null)
                return;

            InstanceOrNull.reticleHitFlashUntil = Time.unscaledTime + ReticleHitFlashSeconds;
        }

        private bool ShouldShowWeaponReticle()
        {
            if (!gameplayVisible || GameplayHudVisibility.CinematicChromeHidden)
                return false;
            if (DMUiToolkitMenus.IsOpen || DMUiToolkitOpticsOverlay.IsShowing)
                return false;

            if (equipmentController == null)
                BindInventoryEvents();

            if (equipmentController == null || !equipmentController.HasActiveRangedWeapon())
                return false;

            EnsureReticleRefs();
            ItemData weapon = equipmentController.DrawnWeaponItem;
            if (weapon == null || !weapon.IsRangedWeapon)
                return false;

            if (reticlePlayerController != null && reticlePlayerController.BlocksCombatInput)
                return false;

            return IsReticleAiming();
        }

        private bool IsReticleAiming()
        {
            EnsureReticleRefs();
            if (reticleInvectorBridge != null && PioneerInvectorBootstrap.IsInvectorPlayer(reticleInvectorBridge))
                return reticleInvectorBridge.IsAiming;

            return reticleRangedCombat != null && reticleRangedCombat.IsAiming;
        }

        private float ComputeTargetSpreadPx()
        {
            EnsureReticleRefs();
            ItemData weapon = equipmentController != null ? equipmentController.DrawnWeaponItem : null;
            bool aiming = IsReticleAiming();

            float spreadPx = ReticleBaseGapPx;
            if (weapon != null && reticleAmmoState != null && equipmentController != null)
            {
                int slot = equipmentController.ActiveWeaponHotbarSlot;
                ItemData ammo = reticleAmmoState.GetLoadedAmmoItem(slot);
                float degrees = RangedFireSolver.ResolveEffectiveSpreadDegrees(
                    weapon,
                    ammo,
                    aiming,
                    RangedFireSolver.DefaultLookAtConvergeDistance,
                    applyPlayerSkillBonus: true);
                spreadPx += Mathf.Clamp(degrees * 0.55f, 0f, ReticleMaxSpreadPx);
            }

            if (!aiming)
                spreadPx = Mathf.Max(spreadPx, ReticleBaseGapPx + 4f);

            if (reticleMotor != null)
            {
                float move01 = Mathf.Clamp01(reticleMotor.inputMagnitude);
                spreadPx += move01 * ReticleMovementSpreadPx;
            }

            return Mathf.Clamp(spreadPx, ReticleBaseGapPx, ReticleMaxSpreadPx + ReticleMovementSpreadPx);
        }

        private void ApplyReticleSpreadPx(int spreadPx)
        {
            // Gap is measured from the dot edge to the arm center; arms are centered on their anchor.
            float offset = ReticleBaseGapPx + ReticleArmHalfLengthPx + spreadPx;
            SetArmTranslate(reticleArmLeft, new Vector2(-offset, 0f));
            SetArmTranslate(reticleArmRight, new Vector2(offset, 0f));
            SetArmTranslate(reticleArmTop, new Vector2(0f, -offset));
            SetArmTranslate(reticleArmBottom, new Vector2(0f, offset));
        }

        private static void SetArmTranslate(VisualElement arm, Vector2 px)
        {
            if (arm == null)
                return;

            arm.style.translate = new Translate(
                new Length(px.x, LengthUnit.Pixel),
                new Length(px.y, LengthUnit.Pixel));
        }

        private void EnsureReticleRefs()
        {
            if (equipmentController == reticleEquipment)
                return;

            reticleEquipment = equipmentController;
            if (reticleEquipment == null)
            {
                reticleInvectorBridge = null;
                reticleRangedCombat = null;
                reticlePlayerController = null;
                reticleMotor = null;
                reticleAmmoState = null;
                return;
            }

            reticleInvectorBridge = reticleEquipment.GetComponent<PioneerInvectorInputBridge>();
            reticleRangedCombat = reticleEquipment.GetComponent<RangedCombatController>();
            reticlePlayerController = reticleEquipment.GetComponent<PlayerController>();
            reticleMotor = reticleEquipment.GetComponent<vThirdPersonMotor>();
            reticleAmmoState = reticleEquipment.GetComponent<WeaponAmmoState>();
        }
    }
}
