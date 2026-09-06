using Project.Combat;
using Project.Core;
using Project.Data;
using Project.Inventory;
using Project.UI;
using Invector.vShooter;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Player.Invector
{
    /// <summary>
    /// Owns the R key (Input System): tap &lt;0.2s reloads, hold ≥0.2s never reloads,
    /// hold ≥1.2s opens Mode Switch (tap R again to close). Pref Laser / LaserSight only show while aiming.
    /// Aim lasers are driven muzzle→reticle (world space) so they match the crosshair — never barrel-only vLaserSight.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PioneerInvectorBootstrap))]
    [DefaultExecutionOrder(620)]
    public class WeaponModeSwitchController : MonoBehaviour
    {
        private const float MaxReloadTapSeconds = 0.2f;
        private const float HoldSecondsToOpen = 1.2f;
        private const float AimLaserMaxRange = 80f;

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
        private PioneerInvectorWeaponBridge weaponBridge;
        private bool holdingReload;
        private bool openedMenuThisHold;
        private float holdStartUnscaled;

        private bool rifleLaserSightEnabled;
        private bool rifleLaserBeamEnabled;
        private bool pistolLaserSightEnabled;
        private bool pistolLaserBeamEnabled;
        private bool lastAppliedAiming;

        private LineRenderer activeAimLaserLine;
        private SpriteRenderer activeAimLaserSight;
        private Transform activeAimLaserRoot;
        private Transform cachedPulseRoot;
        private HitscanBeamMuzzleFollow cachedPulse;

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
            weaponBridge = GetComponent<PioneerInvectorWeaponBridge>();
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
            // Do NOT walk the full player hierarchy here — Player_Invector is huge and
            // domain reload OnDisable would freeze the editor for minutes.
            ClearActiveAimLaser();
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

        private void LateUpdate()
        {
            if (!lastAppliedAiming || activeAimLaserRoot == null)
                return;

            // Hitscan pulse owns the stack briefly while firing — don't fight it.
            if (cachedPulseRoot != activeAimLaserRoot)
            {
                cachedPulseRoot = activeAimLaserRoot;
                cachedPulse = activeAimLaserRoot.GetComponent<HitscanBeamMuzzleFollow>();
            }

            if (cachedPulse != null && cachedPulse.enabled)
                return;

            UpdateActiveAimLaserToReticle();
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

        /// <summary>
        /// Clears hold-R Mode Switch prefs and runtime laser/hold state for a fresh expedition.
        /// </summary>
        public static void ClearPersistedStatesForNewGame()
        {
            PlayerPrefs.DeleteKey(PrefRifleLaserSight);
            PlayerPrefs.DeleteKey(PrefRifleLaserBeam);
            PlayerPrefs.DeleteKey(PrefPistolLaserSight);
            PlayerPrefs.DeleteKey(PrefPistolLaserBeam);
            PlayerPrefs.Save();

            WeaponModeSwitchMenuUI.HideAny();

            WeaponModeSwitchController[] controllers =
                Object.FindObjectsByType<WeaponModeSwitchController>(FindObjectsInactive.Include);
            for (int i = 0; i < controllers.Length; i++)
                controllers[i]?.ResetForNewGame();
        }

        private void ResetForNewGame()
        {
            CancelHold();
            rifleLaserSightEnabled = false;
            rifleLaserBeamEnabled = false;
            pistolLaserSightEnabled = false;
            pistolLaserBeamEnabled = false;
            lastAppliedAiming = false;
            ClearActiveAimLaser();
            ApplyLaserModes(forceAiming: false);
        }

        public void ApplyLaserModes()
        {
            ApplyLaserModes(forceAiming: null);
        }

        private void ApplyLaserModes(bool? forceAiming)
        {
            bool aiming = forceAiming ?? IsPlayerAiming();
            lastAppliedAiming = aiming;

            // Prefer scoped disables on known drawn weapon roots — never scan the full
            // Player_Invector hierarchy (domain reload Awake would freeze the editor).
            DisableLaserStacksOnKnownWeapons();
            ClearActiveAimLaser();

            if (!aiming)
                return;

            ItemData drawn = equipment != null ? equipment.DrawnWeaponItem : null;
            if (drawn == null)
                return;

            if (IsSciFiPistol(drawn))
            {
                BindActiveAimLaser(
                    FindDrawnWeapon(DrawnSciFiPistolName, IsSciFiPistol),
                    pistolLaserBeamEnabled,
                    pistolLaserSightEnabled);
            }
            else if (IsSurvivalRifle(drawn))
            {
                BindActiveAimLaser(
                    FindDrawnWeapon(DrawnSurvivalRifleName, IsSurvivalRifle),
                    rifleLaserBeamEnabled,
                    rifleLaserSightEnabled);
            }
        }

        private bool IsPlayerAiming()
        {
            if (shooterInput == null)
                shooterInput = GetComponent<PioneerShooterMeleeInput>();

            // IsAiming touches cc / shooterManager — unsafe until Invector has finished Init.
            return shooterInput != null && shooterInput.IsAimingActive;
        }

        private void BindActiveAimLaser(Transform drawn, bool beamEnabled, bool sightEnabled)
        {
            if (drawn == null || (!beamEnabled && !sightEnabled))
                return;

            if (!TryResolveLaserStack(drawn, out LineRenderer laserLine, out SpriteRenderer laserSight, out Transform laserRoot))
                return;

            activeAimLaserRoot = laserRoot;
            activeAimLaserLine = laserLine;
            activeAimLaserSight = laserSight;

            // Barrel-forward Invector sight fights reticle alignment — keep it off while we own aim.
            vLaserSight laserSightDriver = laserRoot != null ? laserRoot.GetComponent<vLaserSight>() : null;
            if (laserSightDriver != null)
                laserSightDriver.enabled = false;

            if (activeAimLaserLine != null)
            {
                activeAimLaserLine.useWorldSpace = true;
                activeAimLaserLine.positionCount = 2;
                activeAimLaserLine.enabled = beamEnabled;
            }

            if (activeAimLaserSight != null)
                activeAimLaserSight.enabled = sightEnabled;

            if (beamEnabled || sightEnabled)
                UpdateActiveAimLaserToReticle();
        }

        private void UpdateActiveAimLaserToReticle()
        {
            if (activeAimLaserRoot == null)
                return;

            Camera cam = playerController != null ? playerController.GameplayCamera : null;
            if (cam == null)
                cam = Camera.main;
            if (cam == null)
                return;

            Vector3 origin = activeAimLaserRoot.position;
            Vector3 direction = RangedFireSolver.ResolveMuzzleToReticleDirection(
                cam,
                origin,
                AimLaserMaxRange,
                out float aimDistance);
            Vector3 endPoint = origin + direction * Mathf.Max(aimDistance, 0.5f);

            if (Physics.Raycast(origin, direction, out RaycastHit hit, AimLaserMaxRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                endPoint = hit.point;

            if (activeAimLaserLine != null && activeAimLaserLine.enabled)
            {
                activeAimLaserLine.useWorldSpace = true;
                activeAimLaserLine.positionCount = 2;
                activeAimLaserLine.SetPosition(0, origin);
                activeAimLaserLine.SetPosition(1, endPoint);
            }

            if (activeAimLaserSight != null && activeAimLaserSight.enabled)
            {
                activeAimLaserSight.transform.position = endPoint;
                Vector3 toCam = cam.transform.position - endPoint;
                if (toCam.sqrMagnitude > 0.0001f)
                    activeAimLaserSight.transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
            }
        }

        private void ClearActiveAimLaser()
        {
            if (activeAimLaserLine != null)
                activeAimLaserLine.enabled = false;
            if (activeAimLaserSight != null)
                activeAimLaserSight.enabled = false;

            activeAimLaserLine = null;
            activeAimLaserSight = null;
            activeAimLaserRoot = null;
        }

        private void DisableLaserStacksOnKnownWeapons()
        {
            DisableLaserStacksUnder(FindNamedChild(DrawnSciFiPistolName));
            DisableLaserStacksUnder(FindNamedChild(DrawnSurvivalRifleName));

            if (equipment == null)
                return;

            ItemData drawn = equipment.DrawnWeaponItem ?? equipment.EquippedItem;
            if (drawn == null)
                return;

            GameObject slot = PioneerInvectorWeaponBridge.FindPreloadedDrawnSlot(transform, drawn);
            if (slot != null)
                DisableLaserStacksUnder(slot.transform);

            if (weaponBridge != null)
            {
                GameObject visual = weaponBridge.TryGetWeaponInstance(drawn);
                if (visual != null)
                    DisableLaserStacksUnder(visual.transform);
            }
        }

        private void DisableLaserStacksUnder(Transform root)
        {
            if (root == null)
                return;

            LineRenderer[] lines = root.GetComponentsInChildren<LineRenderer>(true);
            for (int i = 0; i < lines.Length; i++)
            {
                LineRenderer line = lines[i];
                if (line == null || line.gameObject == null)
                    continue;
                if (!line.gameObject.name.Equals("Laser", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                line.enabled = false;
                vLaserSight sight = line.GetComponent<vLaserSight>();
                if (sight != null)
                    sight.enabled = false;
            }

            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform t = transforms[i];
                if (t == null || !t.name.Equals("laserSight", System.StringComparison.OrdinalIgnoreCase))
                    continue;
                if (t.TryGetComponent(out SpriteRenderer sr))
                    sr.enabled = false;
            }
        }

        private Transform FindNamedChild(string exactName)
        {
            if (string.IsNullOrEmpty(exactName))
                return null;

            // Shallow-first search without allocating every Transform under the player.
            Transform direct = transform.Find(exactName);
            if (direct != null)
                return direct;

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child != null && child.name.Equals(exactName, System.StringComparison.OrdinalIgnoreCase))
                    return child;

                if (child == null)
                    continue;

                Transform nested = child.Find(exactName);
                if (nested != null)
                    return nested;
            }

            return null;
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

                    if (weaponBridge != null)
                    {
                        GameObject visual = weaponBridge.TryGetWeaponInstance(active);
                        if (visual != null)
                            return visual.transform;
                    }
                }
            }

            return FindNamedChild(drawnObjectName);
        }

        private static bool TryResolveLaserStack(
            Transform drawn,
            out LineRenderer laserLine,
            out SpriteRenderer laserSight,
            out Transform laserRoot)
        {
            laserLine = null;
            laserSight = null;
            laserRoot = null;
            if (drawn == null)
                return false;

            Transform laser = FindChildRecursive(drawn, "Laser");
            if (laser != null)
            {
                laserRoot = laser;
                laserLine = laser.GetComponent<LineRenderer>();
            }

            Transform sight = FindChildRecursive(drawn, "laserSight");
            if (sight != null)
            {
                sight.gameObject.SetActive(true);
                laserSight = sight.GetComponent<SpriteRenderer>();
            }

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
