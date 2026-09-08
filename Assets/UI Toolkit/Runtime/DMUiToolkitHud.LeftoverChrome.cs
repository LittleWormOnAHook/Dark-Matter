using System.Collections;
using Project.Core;
using Project.Interaction;
using Project.Shelter;
using Project.Survival;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Leftover gameplay HUD chrome: oxygen deprivation FX, Quora shelter timer.
    /// Weapon reticle: <see cref="DMUiToolkitHud.WeaponReticle"/> partial.
    /// </summary>
    public partial class DMUiToolkitHud
    {
        private const float OxygenCriticalThreshold = 0.15f;
        private const float OxygenFlashPeak = 0.22f;
        private const float OxygenFlashDuration = 0.35f;
        private const int OxygenFlashCount = 3;
        private const float OxygenVignetteAlpha = 0.45f;

        private VisualElement oxygenFlash;
        private VisualElement oxygenVignette;
        private VisualElement shelterTimerRoot;
        private Label shelterTimerCaption;
        private Label shelterTimerValue;

        private bool leftoverBound;
        private bool leftoverPreviewHidden;
        private bool oxygenWasCritical;
        private Coroutine oxygenFlashRoutine;
        private static Texture2D oxygenVignetteTexture;
        private int lastShelterSecond = -1;

        private void BindLeftoverChrome(VisualElement root)
        {
            if (root == null)
                return;

            BindWeaponReticle(root);

            oxygenFlash = root.Q<VisualElement>("oxygen-flash");
            oxygenVignette = root.Q<VisualElement>("oxygen-vignette");
            shelterTimerRoot = root.Q<VisualElement>("shelter-timer");
            shelterTimerCaption = root.Q<Label>("shelter-timer-caption");
            shelterTimerValue = root.Q<Label>("shelter-timer-value");

            leftoverBound = weaponReticleBound || oxygenFlash != null || shelterTimerRoot != null;
            ApplyOxygenVignetteTexture();
            HideLeftoverPreviewHosts();
        }

        private void HideLeftoverPreviewHosts()
        {
            if (leftoverPreviewHidden)
                return;

            HideWeaponReticlePreview();

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

            leftoverPreviewHidden = true;
        }

        private void TickLeftoverChrome()
        {
            if (!leftoverBound)
                return;

            if (!gameplayVisible || GameplayHudVisibility.CinematicChromeHidden)
            {
                HideLeftoverPreviewHosts();
                oxygenWasCritical = false;
                lastShelterSecond = -1;
                return;
            }

            leftoverPreviewHidden = false;
            TickWeaponReticle();
            TickOxygenFx();
            TickShelterTimer();
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
            DMUiToolkitStyle.TrySetTextureBackground(oxygenVignette, texture, ScaleMode.StretchToFill);
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
            bool show = shelter != null && shelter.IsOccupied && !DMUiToolkitWorldMenus.IsShelterOpen;
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
