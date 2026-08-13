using Project.Core;
using Project.Data;
using Project.Inventory;
using Project.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Player.Invector
{
        /// <summary>
        /// Owns the R key (Input System): tap &lt;0.2s reloads, hold ≥0.2s never reloads,
        /// hold ≥2s opens Mode Switch (tap R again to close). Pref Laser / LaserSight only show while aiming.
        /// </summary>
        [DisallowMultipleComponent]
        [RequireComponent(typeof(PioneerInvectorBootstrap))]
        public class WeaponModeSwitchController : MonoBehaviour
        {
        private const float MaxReloadTapSeconds = 0.2f;
        private const float HoldSecondsToOpen = 2f;

        private const string DrawnSurvivalRifleName = "Drawn_Survival_Rifle";
        private const string DrawnSciFiPistolName = "Drawn_Sci_Fi_Pistol";

        private const string PrefRifleLaserSight = "DM.WeaponMode.SurvivalRifle.LaserSight.v2";
        private const string PrefRifleLaserBeam = "DM.WeaponMode.SurvivalRifle.Laser.v2";
        private const string PrefPistolLaserSight = "DM.WeaponMode.SciFiPistol.LaserSight.v2";
        private const string PrefPistolLaserBeam = "DM.WeaponMode.SciFiPistol.Laser.v2";

        private PioneerInvectorBootstrap bootstrap;
        private EquipmentController equipment;
        private PlayerController playerController;
        private PioneerShooterMeleeInput shooterInput;
        private bool holdingReload;
        private bool openedMenuThisHold;
        private float holdStartUnscaled;

        private bool rifleLaserSightEnabled;
        private bool rifleLaserBeamEnabled;
        private bool pistolLaserSightEnabled;
        private bool pistolLaserBeamEnabled;
        private bool lastAppliedAiming;

        /// <summary>Survival Rifle LaserSight (Rifles submenu).</summary>
        public bool LaserSightEnabled => rifleLaserSightEnabled;
        /// <summary>Survival Rifle Laser beam (Rifles submenu).</summary>
        public bool LaserBeamEnabled => rifleLaserBeamEnabled;

        public bool PistolLaserSightEnabled => pistolLaserSightEnabled;
        public bool PistolLaserBeamEnabled => pistolLaserBeamEnabled;

        private void Awake()
        {
            bootstrap = GetComponent<PioneerInvectorBootstrap>();
            equipment = GetComponent<EquipmentController>();
            playerController = GetComponent<PlayerController>();
            shooterInput = GetComponent<PioneerShooterMeleeInput>();
            // Default OFF — lasers start deactivated until the player enables them.
            rifleLaserSightEnabled = PlayerPrefs.GetInt(PrefRifleLaserSight, 0) != 0;
            rifleLaserBeamEnabled = PlayerPrefs.GetInt(PrefRifleLaserBeam, 0) != 0;
            pistolLaserSightEnabled = PlayerPrefs.GetInt(PrefPistolLaserSight, 0) != 0;
            pistolLaserBeamEnabled = PlayerPrefs.GetInt(PrefPistolLaserBeam, 0) != 0;
            // Do NOT read IsAiming here — shooterManager/cc are not ready during AddComponent Awake.
            ApplyLaserModes(forceAiming: false);
            lastAppliedAiming = false;
        }

        private void OnEnable()
        {
            if (equipment != null)
                equipment.OnSelectedHotbarChanged += HandleHotbarChanged;
        }

        private void OnDisable()
        {
            if (equipment != null)
                equipment.OnSelectedHotbarChanged -= HandleHotbarChanged;
            CancelHold();
        }

        private void Start()
        {
            ApplyLaserModes();
        }

        private void Update()
        {
            ProcessReloadKey();

            bool aiming = IsPlayerAiming();
            if (aiming == lastAppliedAiming)
                return;

            lastAppliedAiming = aiming;
            ApplyLaserModes();
        }

        private void HandleHotbarChanged(int _)
        {
            ApplyLaserModes();
        }

        private void ProcessReloadKey()
        {
            if (bootstrap == null || !bootstrap.IsActive)
            {
                CancelHold();
                return;
            }

            if (WeaponModeSwitchMenuUI.IsOpen)
            {
                // Ignore the same R hold that opened the menu; next press closes it.
                if (IsReloadHeld())
                    return;

                holdingReload = false;
                openedMenuThisHold = false;

                if (WasReloadPressedThisFrame())
                    WeaponModeSwitchMenuUI.HideAny();

                return;
            }

            if (playerController != null && playerController.BlocksCombatInput)
            {
                CancelHold();
                return;
            }

            bool pressed = WasReloadPressedThisFrame();
            bool held = IsReloadHeld();

            if (pressed)
            {
                holdingReload = true;
                openedMenuThisHold = false;
                holdStartUnscaled = Time.unscaledTime;
            }

            if (!holdingReload)
                return;

            float heldFor = Time.unscaledTime - holdStartUnscaled;

            if (held)
            {
                if (!openedMenuThisHold && heldFor >= HoldSecondsToOpen)
                {
                    openedMenuThisHold = true;
                    OpenModeSwitchMenu();
                }

                return;
            }

            holdingReload = false;
            if (!openedMenuThisHold && heldFor < MaxReloadTapSeconds)
                TryRequestManualReload();
        }

        private void CancelHold()
        {
            holdingReload = false;
            openedMenuThisHold = false;
        }

        private void TryRequestManualReload()
        {
            if (shooterInput == null)
                shooterInput = GetComponent<PioneerShooterMeleeInput>();

            if (shooterInput != null)
                shooterInput.RequestManualReloadFromModeSwitch();
        }

        private static bool WasReloadPressedThisFrame()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
                return true;

            Gamepad pad = Gamepad.current;
            return pad != null && pad.leftShoulder.wasPressedThisFrame;
        }

        private static bool IsReloadHeld()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.rKey.isPressed)
                return true;

            Gamepad pad = Gamepad.current;
            return pad != null && pad.leftShoulder.isPressed;
        }

        public void SetLaserSightEnabled(bool enabled)
        {
            rifleLaserSightEnabled = enabled;
            PlayerPrefs.SetInt(PrefRifleLaserSight, enabled ? 1 : 0);
            PlayerPrefs.Save();
            ApplyLaserModes();
        }

        public void SetLaserBeamEnabled(bool enabled)
        {
            rifleLaserBeamEnabled = enabled;
            PlayerPrefs.SetInt(PrefRifleLaserBeam, enabled ? 1 : 0);
            PlayerPrefs.Save();
            ApplyLaserModes();
        }

        public void SetPistolLaserSightEnabled(bool enabled)
        {
            pistolLaserSightEnabled = enabled;
            PlayerPrefs.SetInt(PrefPistolLaserSight, enabled ? 1 : 0);
            PlayerPrefs.Save();
            ApplyLaserModes();
        }

        public void SetPistolLaserBeamEnabled(bool enabled)
        {
            pistolLaserBeamEnabled = enabled;
            PlayerPrefs.SetInt(PrefPistolLaserBeam, enabled ? 1 : 0);
            PlayerPrefs.Save();
            ApplyLaserModes();
        }

        public void ApplyLaserModes()
        {
            ApplyLaserModes(forceAiming: null);
        }

        private void ApplyLaserModes(bool? forceAiming)
        {
            bool aiming = forceAiming ?? IsPlayerAiming();
            lastAppliedAiming = aiming;

            ApplyLaserStack(
                FindDrawnWeapon(DrawnSurvivalRifleName, IsSurvivalRifle),
                rifleLaserBeamEnabled && aiming,
                rifleLaserSightEnabled && aiming);

            ApplyLaserStack(
                FindDrawnWeapon(DrawnSciFiPistolName, IsSciFiPistol),
                pistolLaserBeamEnabled && aiming,
                pistolLaserSightEnabled && aiming);
        }

        private bool IsPlayerAiming()
        {
            if (shooterInput == null)
                shooterInput = GetComponent<PioneerShooterMeleeInput>();

            // IsAiming touches cc / shooterManager — unsafe until Invector has finished Init.
            return shooterInput != null && shooterInput.IsAimingActive;
        }

        private static void ApplyLaserStack(Transform drawn, bool beamEnabled, bool sightEnabled)
        {
            if (drawn == null)
                return;

            if (!TryResolveLaserStack(drawn, out LineRenderer laserLine, out SpriteRenderer laserSight))
                return;

            if (laserLine != null)
                laserLine.enabled = beamEnabled;

            if (laserSight != null)
                laserSight.enabled = sightEnabled;
        }

        private void OpenModeSwitchMenu()
        {
            UIManager uiManager = Object.FindAnyObjectByType<UIManager>();
            Transform canvasRoot = uiManager != null ? uiManager.transform : null;
            WeaponModeSwitchMenuUI menu = WeaponModeSwitchMenuUI.EnsureExists(canvasRoot);
            menu.Show(this);
        }

        private Transform FindDrawnWeapon(string drawnObjectName, System.Func<ItemData, bool> itemMatch)
        {
            if (equipment != null && itemMatch != null)
            {
                ItemData active = equipment.DrawnWeaponItem ?? equipment.EquippedItem;
                if (active != null && itemMatch(active))
                {
                    GameObject drawnSlot = PioneerInvectorWeaponBridge.FindPreloadedDrawnSlot(transform, active);
                    if (drawnSlot != null)
                        return drawnSlot.transform;
                }
            }

            Transform[] all = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t != null && t.name.Equals(drawnObjectName, System.StringComparison.OrdinalIgnoreCase))
                    return t;
            }

            return null;
        }

        private static bool TryResolveLaserStack(
            Transform drawn,
            out LineRenderer laserLine,
            out SpriteRenderer laserSight)
        {
            laserLine = null;
            laserSight = null;
            if (drawn == null)
                return false;

            Transform laser = FindChildRecursive(drawn, "Laser");
            if (laser != null)
                laserLine = laser.GetComponent<LineRenderer>();

            Transform sight = FindChildRecursive(drawn, "laserSight");
            if (sight != null)
                laserSight = sight.GetComponent<SpriteRenderer>();

            return laserLine != null || laserSight != null;
        }

        private static bool IsSurvivalRifle(ItemData item)
        {
            if (item == null)
                return false;

            if (!string.IsNullOrEmpty(item.itemName) &&
                item.itemName.IndexOf("Survival Rifle", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return item.name.IndexOf("survival_rifle", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsSciFiPistol(ItemData item)
        {
            if (item == null)
                return false;

            if (!string.IsNullOrEmpty(item.itemName) &&
                item.itemName.IndexOf("Sci-Fi Pistol", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (!string.IsNullOrEmpty(item.itemName) &&
                item.itemName.IndexOf("Scifi Pistol", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return item.name.IndexOf("sci_fi_pistol", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null || string.IsNullOrEmpty(childName))
                return null;

            Transform direct = root.Find(childName);
            if (direct != null)
                return direct;

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child != null && child.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase))
                    return child;
            }

            return null;
        }
    }
}
