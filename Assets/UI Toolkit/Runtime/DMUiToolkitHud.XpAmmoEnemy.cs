using Project.AI;
using Project.Combat;
using Project.Core;
using Project.Data;
using Project.Inventory;
using Project.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// UITK ammo label + focused enemy health. XP lives in Journal; old XP bar host removed.
    /// </summary>
    public partial class DMUiToolkitHud
    {
        private const string XpAmmoLogStamp = "DMUiToolkit 0901-xpammo";

        private static bool xpAmmoStamped;

        private Label ammoLabel;
        private VisualElement enemyFocusRoot;
        private Label enemyNameLabel;
        private VisualElement enemyTrack;
        private VisualElement enemyFill;
        private bool xpAmmoHostsReady;
        private bool xpAmmoUguiHidden;
        private bool hudAmmoHidden;
        private EngagedEnemyHealthHud cachedEnemyHud;
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
                if (ammoLabel != null)
                    ammoLabel.style.display = DisplayStyle.None;
                if (enemyFocusRoot != null && lastEnemyShown)
                {
                    lastEnemyShown = false;
                    enemyFocusRoot.style.display = DisplayStyle.None;
                }
            }
            else
            {
                HideHudAmmoLabel();
                PullEnemyFocus();
            }

            HideXpAmmoEnemyUgui();
        }

        private void EnsureXpAmmoEnemyBound()
        {
            if (xpAmmoHostsReady && (ammoLabel != null || enemyFocusRoot != null))
                return;

            if (document == null)
                document = GetComponent<UIDocument>();
            if (document == null)
                return;

            VisualElement root = document.rootVisualElement;
            if (root == null)
                return;

            VisualElement hostParent = hudRoot != null ? hudRoot : root.Q<VisualElement>("hud-root") ?? root;

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

            HideHudAmmoLabel();

            xpAmmoHostsReady = ammoLabel != null || enemyFocusRoot != null;
            if (xpAmmoHostsReady && !xpAmmoStamped)
                xpAmmoStamped = true;
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

        private void HideHudAmmoLabel()
        {
            if (hudAmmoHidden || ammoLabel == null)
                return;

            ammoLabel.style.display = DisplayStyle.None;
            hudAmmoHidden = true;
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
                    if (enemyFill != null)
                        enemyFill.style.width = Length.Percent(Mathf.Clamp01(normalized) * 100f);
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
                EngagedEnemyHealthHud enemyHud = EngagedEnemyHealthHud.Instance
                    ?? FindAnyObjectByType<EngagedEnemyHealthHud>(FindObjectsInactive.Include);
                enemyHud?.ApplyToolkitVisibility();
            }

            xpAmmoUguiHidden = false;
        }
    }
}
