using System.Collections;
using Project.Combat;
using Project.Data;
using Project.Core;
using Project.Inventory;
using Project.Player;
using Project.Player.Invector;
using Project.Shelter;
using Project.Survival;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Leftover gameplay HUD chrome: ranged crosshair, oxygen deprivation FX, Quora shelter timer.
    /// Ammo readout stays in DMUiToolkitHud.XpAmmoEnemy. Hide unused hosts in C#, not USS.
    /// </summary>
    public partial class DMUiToolkitHud
    {
        private const float OxygenCriticalThreshold = 0.15f;
        private const float OxygenFlashPeak = 0.22f;
        private const float OxygenFlashDuration = 0.35f;
        private const int OxygenFlashCount = 3;
        private const float OxygenVignetteAlpha = 0.45f;

        private VisualElement crosshairRoot;
        private VisualElement oxygenFlash;
        private VisualElement oxygenVignette;
        private VisualElement shelterTimerRoot;
        private Label shelterTimerCaption;
        private Label shelterTimerValue;
        private bool leftoverBound;
        private bool oxygenWasCritical;
        private Coroutine oxygenFlashRoutine;
        private static Texture2D oxygenVignetteTexture;
        private int lastShelterSecond = -1;

        private void BindLeftoverChrome(VisualElement root)
        {
            if (root == null)
                return;

            crosshairRoot = root.Q<VisualElement>("crosshair");
            oxygenFlash = root.Q<VisualElement>("oxygen-flash");
            oxygenVignette = root.Q<VisualElement>("oxygen-vignette");
            shelterTimerRoot = root.Q<VisualElement>("shelter-timer");
            shelterTimerCaption = root.Q<Label>("shelter-timer-caption");
            shelterTimerValue = root.Q<Label>("shelter-timer-value");

            leftoverBound = crosshairRoot != null || oxygenFlash != null || shelterTimerRoot != null;
            ApplyOxygenVignetteTexture();
            HideLeftoverPreviewHosts();
        }

        private void HideLeftoverPreviewHosts()
        {
            if (crosshairRoot != null)
                crosshairRoot.style.display = DisplayStyle.None;
            if (oxygenFlash != null)
            {
                oxygenFlash.style.opacity = 0f;
                oxygenFlash.style.display = DisplayStyle.None;
            }
            if (oxygenVignette != null)
            {
                oxygenVignette.style.opacity = 0f;
                oxygenVignette.style.display = DisplayStyle.None;
            }
            if (shelterTimerRoot != null)
                shelterTimerRoot.style.display = DisplayStyle.None;
        }

        private void TickLeftoverChrome()
        {
            if (!leftoverBound)
                return;

            if (!gameplayVisible)
            {
                HideLeftoverPreviewHosts();
                oxygenWasCritical = false;
                lastShelterSecond = -1;
                return;
            }

            TickCrosshair();
            TickOxygenFx();
            TickShelterTimer();
        }

        private void TickCrosshair()
        {
            if (crosshairRoot == null)
                return;

            bool show = false;
            if (!DMUiToolkitMenus.IsOpen && !DMUiToolkitOpticsOverlay.IsShowing)
            {
                if (equipmentController == null)
                    BindInventoryEvents();

                EquipmentController equipment = equipmentController;
                if (equipment == null)
                    equipment = FindAnyObjectByType<EquipmentController>();

                if (equipment != null && equipment.HasActiveRangedWeapon())
                {
                    ItemData weapon = equipment.DrawnWeaponItem;
                    PlayerController player = equipment.GetComponent<PlayerController>();
                    show = weapon != null && weapon.IsRangedWeapon
                        && (player == null || !player.BlocksCombatInput);
                }
            }

            crosshairRoot.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show)
                return;

            float scale = IsHudAiming() ? 0.75f : 1f;
            crosshairRoot.style.scale = new Scale(new Vector3(scale, scale, 1f));
        }

        private bool IsHudAiming()
        {
            if (equipmentController == null)
                return false;

            PioneerInvectorInputBridge invector = equipmentController.GetComponent<PioneerInvectorInputBridge>();
            if (invector != null && PioneerInvectorBootstrap.IsInvectorPlayer(invector))
                return invector.IsAiming;

            RangedCombatController ranged = equipmentController.GetComponent<RangedCombatController>();
            return ranged != null && ranged.IsAiming;
        }

        private void TickOxygenFx()
        {
            if (oxygenFlash == null && oxygenVignette == null)
                return;

            if (survivalStats == null)
                BindSurvival();

            bool critical = false;
            if (survivalStats != null && GameSession.HasStarted && !survivalStats.IsDead)
                critical = survivalStats.GetOxygenNormalized() <= OxygenCriticalThreshold;

            if (critical && !oxygenWasCritical)
                BeginOxygenFlash();
            else if (!critical && oxygenWasCritical)
                StopOxygenFlash();

            if (oxygenVignette != null)
            {
                oxygenVignette.style.display = critical ? DisplayStyle.Flex : DisplayStyle.None;
                oxygenVignette.style.opacity = critical ? OxygenVignetteAlpha : 0f;
            }

            if (!critical && oxygenFlash != null && oxygenFlashRoutine == null)
            {
                oxygenFlash.style.opacity = 0f;
                oxygenFlash.style.display = DisplayStyle.None;
            }

            oxygenWasCritical = critical;
        }

        private void BeginOxygenFlash()
        {
            if (!isActiveAndEnabled)
                return;
            if (oxygenFlashRoutine != null)
                StopCoroutine(oxygenFlashRoutine);
            oxygenFlashRoutine = StartCoroutine(RunOxygenFlash());
        }

        private void StopOxygenFlash()
        {
            if (oxygenFlashRoutine != null)
            {
                StopCoroutine(oxygenFlashRoutine);
                oxygenFlashRoutine = null;
            }

            if (oxygenFlash != null)
            {
                oxygenFlash.style.opacity = 0f;
                oxygenFlash.style.display = DisplayStyle.None;
            }
        }

        private IEnumerator RunOxygenFlash()
        {
            if (oxygenFlash == null)
            {
                oxygenFlashRoutine = null;
                yield break;
            }

            oxygenFlash.style.display = DisplayStyle.Flex;
            for (int i = 0; i < OxygenFlashCount; i++)
            {
                yield return FadeOxygenFlash(0f, OxygenFlashPeak, OxygenFlashDuration * 0.4f);
                yield return FadeOxygenFlash(OxygenFlashPeak, 0f, OxygenFlashDuration * 0.6f);
            }

            oxygenFlash.style.opacity = 0f;
            oxygenFlash.style.display = DisplayStyle.None;
            oxygenFlashRoutine = null;
        }

        private IEnumerator FadeOxygenFlash(float from, float to, float duration)
        {
            if (oxygenFlash == null)
                yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration <= 0f ? 1f : elapsed / duration;
                oxygenFlash.style.opacity = Mathf.Lerp(from, to, t);
                yield return null;
            }

            oxygenFlash.style.opacity = to;
        }

        private void ApplyOxygenVignetteTexture()
        {
            if (oxygenVignette == null)
                return;
            Texture2D texture = EnsureOxygenVignetteTexture();
            if (texture == null)
                return;
            oxygenVignette.style.backgroundImage = new StyleBackground(Background.FromTexture2D(texture));
            oxygenVignette.style.unityBackgroundScaleMode = ScaleMode.StretchToFill;
        }

        private static Texture2D EnsureOxygenVignetteTexture()
        {
            if (oxygenVignetteTexture != null)
                return oxygenVignetteTexture;

            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "DM_UITK_OxygenVignette",
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float maxDist = center.magnitude;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                    float alpha = Mathf.SmoothStep(0.2f, 1f, dist);
                    pixels[y * size + x] = new Color(0.55f, 0f, 0f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            oxygenVignetteTexture = texture;
            return oxygenVignetteTexture;
        }

        private void TickShelterTimer()
        {
            if (shelterTimerRoot == null)
                return;

            QuoraShelterController shelter = QuoraShelterController.ActiveOccupiedShelter;
            bool show = shelter != null && shelter.IsOccupied && !DMUiToolkitMenus.IsShelterOpen;
            if (!show)
            {
                shelterTimerRoot.style.display = DisplayStyle.None;
                lastShelterSecond = -1;
                return;
            }

            shelterTimerRoot.style.display = DisplayStyle.Flex;
            float remaining = Mathf.Max(0f, shelter.RemainingLifetimeSeconds);
            int wholeSeconds = Mathf.FloorToInt(remaining);
            if (wholeSeconds == lastShelterSecond)
                return;

            lastShelterSecond = wholeSeconds;
            int minutes = wholeSeconds / 60;
            int seconds = wholeSeconds % 60;
            if (shelterTimerValue != null)
            {
                shelterTimerValue.text = $"{minutes:00}:{seconds:00}";
                if (wholeSeconds <= 60)
                    shelterTimerValue.style.color = DarkMatterGenesisUiPalette.DeepMagenta;
                else if (wholeSeconds <= 120)
                    shelterTimerValue.style.color = DarkMatterGenesisUiPalette.Gold;
                else
                    shelterTimerValue.style.color = DarkMatterGenesisUiPalette.WarmOffWhite;
            }
        }
    }
}
