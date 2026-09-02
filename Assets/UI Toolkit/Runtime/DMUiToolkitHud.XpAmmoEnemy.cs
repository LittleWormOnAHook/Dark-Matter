using Project.AI;
using Project.Combat;
using Project.Core;
using Project.Data;
using Project.Inventory;
using Project.Player;
using Project.Progression;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// UITK cutover for remaining live uGUI HUD pieces: bottom XP bar, ammo above stats,
    /// and top focused enemy health. Bind/update lives here so popup/rolodex/hotbar
    /// work in DMUiToolkitHud.cs is not rewritten.
    /// </summary>
    public partial class DMUiToolkitHud
    {
        private const string XpAmmoLogStamp = "DMUiToolkit 0901-xpammo";

        private static bool xpAmmoStamped;

        private VisualElement xpRoot;
        private VisualElement xpTrack;
        private VisualElement xpFill;
        private Label xpLabel;
        private Label ammoLabel;
        private VisualElement enemyFocusRoot;
        private Label enemyNameLabel;
        private VisualElement enemyTrack;
        private VisualElement enemyFill;
        private bool xpAmmoHostsReady;
        private bool xpAmmoUguiHidden;
        private PlayerProgressionManager xpProgression;
        private WeaponAmmoState xpAmmoState;
        private PlayerController cachedEquipmentPlayer;
        private EngagedEnemyHealthHud cachedEnemyHud;
        private string lastXpText;
        private float lastXpFill = -1f;
        private bool lastXpShown;
        private string lastAmmoText;
        private bool lastAmmoShown;
        private string lastEnemyName;
        private float lastEnemyFill = -1f;
        private bool lastEnemyShown;

        private void Update()
        {
            EnsureXpAmmoEnemyBound();
            if (!gameplayVisible)
            {
                RestoreXpAmmoEnemyUgui();
                return;
            }

            if (GameplayHudVisibility.CinematicChromeHidden)
            {
                if (xpRoot != null)
                    xpRoot.style.display = DisplayStyle.None;
                if (ammoLabel != null)
                {
                    lastAmmoShown = false;
                    lastAmmoText = null;
                    ammoLabel.style.display = DisplayStyle.None;
                }
                if (enemyFocusRoot != null && lastEnemyShown)
                {
                    lastEnemyShown = false;
                    enemyFocusRoot.style.display = DisplayStyle.None;
                }
            }
            else
            {
                PullXpBar();
                PullAmmoReadout();
                PullEnemyFocus();
            }

            HideXpAmmoEnemyUgui();
        }

        private void EnsureXpAmmoEnemyBound()
        {
            // Once bound, keep cached VisualElement refs. Do not Q() every Update.
            if (xpAmmoHostsReady && (xpRoot != null || ammoLabel != null || enemyFocusRoot != null))
                return;

            if (document == null)
                document = GetComponent<UIDocument>();
            if (document == null)
                return;

            VisualElement root = document.rootVisualElement;
            if (root == null)
                return;

            VisualElement hostParent = hudRoot != null ? hudRoot : root.Q<VisualElement>("hud-root") ?? root;

            xpRoot = root.Q<VisualElement>("xp");
            if (xpRoot == null)
            {
                CreateXpHost(hostParent);
                xpRoot = hostParent.Q<VisualElement>("xp") ?? root.Q<VisualElement>("xp");
            }

            xpTrack = root.Q<VisualElement>("xp-track");
            xpFill = root.Q<VisualElement>("xp-fill");
            xpLabel = root.Q<Label>("xp-label");
            if (xpRoot != null)
            {
                if (xpTrack == null)
                    xpTrack = xpRoot.Q<VisualElement>("xp-track");
                if (xpFill == null)
                    xpFill = xpRoot.Q<VisualElement>("xp-fill");
                if (xpLabel == null)
                    xpLabel = xpRoot.Q<Label>("xp-label");
            }
            HintDynamicFill(xpFill);

            ammoLabel = root.Q<Label>("ammo");
            if (ammoLabel == null)
            {
                CreateAmmoHost(hostParent);
                ammoLabel = hostParent.Q<Label>("ammo") ?? root.Q<Label>("ammo");
            }

            enemyFocusRoot = root.Q<VisualElement>("enemy-focus");
            if (enemyFocusRoot == null)
            {
                CreateEnemyFocusHost(hostParent);
                enemyFocusRoot = hostParent.Q<VisualElement>("enemy-focus") ?? root.Q<VisualElement>("enemy-focus");
            }

            enemyNameLabel = root.Q<Label>("enemy-name");
            enemyTrack = root.Q<VisualElement>("enemy-track");
            enemyFill = root.Q<VisualElement>("enemy-fill");
            if (enemyFocusRoot != null)
            {
                if (enemyNameLabel == null)
                    enemyNameLabel = enemyFocusRoot.Q<Label>("enemy-name");
                if (enemyTrack == null)
                    enemyTrack = enemyFocusRoot.Q<VisualElement>("enemy-track");
                if (enemyFill == null)
                    enemyFill = enemyFocusRoot.Q<VisualElement>("enemy-fill");
            }
            HintDynamicFill(enemyFill);

            if (enemyFocusRoot != null && !lastEnemyShown)
                enemyFocusRoot.style.display = DisplayStyle.None;

            xpAmmoHostsReady = xpRoot != null || ammoLabel != null || enemyFocusRoot != null;
            if (xpAmmoHostsReady && !xpAmmoStamped)
            {
                xpAmmoStamped = true;
                Debug.Log(XpAmmoLogStamp);
            }
        }

        private static void CreateXpHost(VisualElement parent)
        {
            if (parent == null)
                return;

            VisualElement xp = new VisualElement { name = "xp" };
            xp.AddToClassList("dmg-hud-xp");
            xp.pickingMode = PickingMode.Ignore;

            VisualElement track = new VisualElement { name = "xp-track" };
            track.AddToClassList("dmg-hud-xp-track");
            track.pickingMode = PickingMode.Ignore;

            VisualElement fill = new VisualElement { name = "xp-fill" };
            fill.AddToClassList("dmg-hud-xp-fill");
            fill.pickingMode = PickingMode.Ignore;
            fill.style.width = Length.Percent(62f);
            fill.usageHints = UsageHints.DynamicTransform;
            track.Add(fill);

            Label label = new Label("Lv 5    120 / 400 XP") { name = "xp-label" };
            label.AddToClassList("dmg-hud-xp-label");
            label.pickingMode = PickingMode.Ignore;

            xp.Add(track);
            xp.Add(label);
            parent.Add(xp);
        }

        private static void CreateAmmoHost(VisualElement parent)
        {
            if (parent == null)
                return;

            Label ammo = new Label("STANDARD 12/30  (+48)") { name = "ammo" };
            ammo.AddToClassList("dmg-hud-ammo");
            ammo.pickingMode = PickingMode.Ignore;
            parent.Add(ammo);
        }

        private static void CreateEnemyFocusHost(VisualElement parent)
        {
            if (parent == null)
                return;

            VisualElement focus = new VisualElement { name = "enemy-focus" };
            focus.AddToClassList("dmg-hud-enemy-focus");
            focus.pickingMode = PickingMode.Ignore;

            Label name = new Label("Enemy Name") { name = "enemy-name" };
            name.AddToClassList("dmg-hud-enemy-name");
            name.pickingMode = PickingMode.Ignore;

            VisualElement track = new VisualElement { name = "enemy-track" };
            track.AddToClassList("dmg-hud-enemy-track");
            track.pickingMode = PickingMode.Ignore;

            VisualElement fill = new VisualElement { name = "enemy-fill" };
            fill.AddToClassList("dmg-hud-enemy-fill");
            fill.pickingMode = PickingMode.Ignore;
            fill.style.width = Length.Percent(70f);
            fill.usageHints = UsageHints.DynamicTransform;
            track.Add(fill);

            focus.Add(name);
            focus.Add(track);
            parent.Add(focus);
        }

        private void PullXpBar()
        {
            if (xpFill == null && xpLabel == null)
                return;

            if (xpProgression == null)
                xpProgression = PlayerProgressionManager.EnsureExists();

            int level = xpProgression != null ? xpProgression.Level : 1;
            int xpIntoLevel = xpProgression != null ? xpProgression.GetXpProgressInCurrentLevel() : 0;
            int xpToNext = xpProgression != null ? xpProgression.GetXpRequiredForNextLevel() : 0;
            float fill = xpProgression != null ? xpProgression.GetXpProgressNormalized() : 0f;

            string text = xpToNext > 0
                ? $"Lv {level}    {xpIntoLevel} / {xpToNext} XP"
                : $"Lv {level}    MAX";

            bool fillChanged = !Mathf.Approximately(fill, lastXpFill);
            bool textChanged = !string.Equals(text, lastXpText, System.StringComparison.Ordinal);
            if (fillChanged || textChanged)
            {
                lastXpFill = fill;
                lastXpText = text;
                SetFill(xpFill, xpLabel, fill, text);
            }

            if (xpRoot != null && !lastXpShown)
            {
                lastXpShown = true;
                xpRoot.style.display = DisplayStyle.Flex;
            }
        }

        private void PullAmmoReadout()
        {
            if (ammoLabel == null)
                return;

            if (equipmentController == null)
                BindInventoryEvents();

            EquipmentController equipment = equipmentController;
            if (equipment == null && (Time.frameCount & 31) == 0)
                equipment = FindAnyObjectByType<EquipmentController>();

            if (xpAmmoState == null && equipment != null)
                xpAmmoState = equipment.GetComponent<WeaponAmmoState>();
            if (xpAmmoState == null && equipment != null && (Time.frameCount & 31) == 0)
                xpAmmoState = FindAnyObjectByType<WeaponAmmoState>();

            if (equipment != null && cachedEquipmentPlayer == null)
                cachedEquipmentPlayer = equipment.GetComponent<PlayerController>();

            ItemData weapon = null;
            bool show = false;
            if (equipment != null)
            {
                PlayerController player = cachedEquipmentPlayer;
                if (player == null || !player.BlocksCombatInput)
                {
                    show = equipment.HasActiveRangedWeapon();
                    if (show)
                    {
                        weapon = equipment.DrawnWeaponItem;
                        show = weapon != null && weapon.IsRangedWeapon;
                    }
                }
            }

            if (!show)
            {
                if (lastAmmoShown)
                {
                    lastAmmoShown = false;
                    lastAmmoText = null;
                    ammoLabel.style.display = DisplayStyle.None;
                }

                return;
            }

            if (!lastAmmoShown)
            {
                lastAmmoShown = true;
                ammoLabel.style.display = DisplayStyle.Flex;
            }

            string nextText;
            if (weapon.isMiningTool)
            {
                int percent = xpAmmoState != null
                    ? xpAmmoState.GetMiningChargePercent(equipment.ActiveWeaponHotbarSlot)
                    : 0;
                nextText = $"CHARGE {percent}%";
                SetAmmoText(nextText);

                return;
            }

            int weaponHotbarSlot = equipment.ActiveWeaponHotbarSlot;
            int loaded = xpAmmoState != null ? xpAmmoState.GetActiveLoadedAmmo() : 0;
            int magazineSize = WeaponAmmoState.GetMagazineCapacity(weapon);
            bool infiniteReserve = xpAmmoState != null && xpAmmoState.IsInfiniteAmmoForSlot(weaponHotbarSlot);
            int reserve = !infiniteReserve && xpAmmoState != null
                ? xpAmmoState.GetReserveAmmoCount(weaponHotbarSlot)
                : 0;

            if (!infiniteReserve && loaded <= 0 && reserve <= 0)
            {
                SetAmmoText("Empty 0/0");
                return;
            }

            string ammoName = ResolveToolkitAmmoLabelName(weaponHotbarSlot);
            if (infiniteReserve)
                SetAmmoText($"{ammoName} {loaded}/{magazineSize}  (\u221e)");
            else
                SetAmmoText(reserve > 0
                    ? $"{ammoName} {loaded}/{magazineSize}  (+{reserve})"
                    : $"{ammoName} {loaded}/{magazineSize}");
        }

        private void SetAmmoText(string text)
        {
            if (string.Equals(text, lastAmmoText, System.StringComparison.Ordinal))
                return;

            lastAmmoText = text;
            ammoLabel.text = text;
        }

        private string ResolveToolkitAmmoLabelName(int weaponHotbarSlot)
        {
            if (xpAmmoState == null)
                return "STANDARD";

            ItemData loadedAmmoItem = xpAmmoState.GetLoadedAmmoItem(weaponHotbarSlot);
            if (loadedAmmoItem != null && !string.IsNullOrWhiteSpace(loadedAmmoItem.itemName))
                return loadedAmmoItem.itemName.ToUpperInvariant();

            AmmoType loadedType = xpAmmoState.GetLoadedAmmoType(weaponHotbarSlot);
            return loadedType == AmmoType.Gunpowder ? "STANDARD" : loadedType.ToString().ToUpperInvariant();
        }

        private void PullEnemyFocus()
        {
            if (enemyFocusRoot == null)
                return;

            if (cachedEnemyHud == null && (Time.frameCount & 31) == 0)
                cachedEnemyHud = EngagedEnemyHealthHud.Instance
                    ?? FindAnyObjectByType<EngagedEnemyHealthHud>(FindObjectsInactive.Include);

            EngagedEnemyHealthHud enemyHud = cachedEnemyHud;
            string displayName = null;
            float normalized = 0f;
            bool show = enemyHud != null
                && !DMUiToolkitMenus.IsOpen
                && enemyHud.TryGetFocus(out displayName, out normalized);

            if (show)
            {
                if (!lastEnemyShown)
                {
                    lastEnemyShown = true;
                    enemyFocusRoot.style.display = DisplayStyle.Flex;
                }

                if (enemyNameLabel != null
                    && !string.Equals(displayName, lastEnemyName, System.StringComparison.Ordinal))
                {
                    lastEnemyName = displayName;
                    enemyNameLabel.text = displayName;
                }

                if (!Mathf.Approximately(normalized, lastEnemyFill))
                {
                    lastEnemyFill = normalized;
                    SetFill(enemyFill, null, normalized, null);
                }

                return;
            }

            if (lastEnemyShown)
            {
                lastEnemyShown = false;
                lastEnemyName = null;
                lastEnemyFill = -1f;
                enemyFocusRoot.style.display = DisplayStyle.None;
            }
        }

        private void HideXpAmmoEnemyUgui()
        {
            if (xpAmmoUguiHidden)
                return;

            HotbarXpHud xpHud = FindAnyObjectByType<HotbarXpHud>(FindObjectsInactive.Include);
            if (xpHud != null && xpHud.gameObject.activeSelf)
                xpHud.SetVisible(false);

            EngagedEnemyHealthHud enemyHud = EngagedEnemyHealthHud.Instance
                ?? FindAnyObjectByType<EngagedEnemyHealthHud>(FindObjectsInactive.Include);
            enemyHud?.ApplyToolkitVisibility();

            xpAmmoUguiHidden = true;
        }

        private void RestoreXpAmmoEnemyUgui()
        {
            if (!xpAmmoUguiHidden)
                return;

            if (!DMUiToolkitConfig.IsEnabled || !DMUiToolkitBootstrap.IsRootActive)
            {
                HotbarXpHud xpHud = FindAnyObjectByType<HotbarXpHud>(FindObjectsInactive.Include);
                if (xpHud != null && GameSession.HasStarted && !MainMenuController.BlocksGameplayHud)
                    xpHud.SetVisible(true);

                EngagedEnemyHealthHud enemyHud = EngagedEnemyHealthHud.Instance
                    ?? FindAnyObjectByType<EngagedEnemyHealthHud>(FindObjectsInactive.Include);
                enemyHud?.ApplyToolkitVisibility();
            }

            xpAmmoUguiHidden = false;
        }
    }
}