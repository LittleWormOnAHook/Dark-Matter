using System.Collections.Generic;
using Project.Audio;
using Project.Combat;
using Project.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Screen-space UITK damage numbers tracking world positions.
    /// DMUiToolkit 0919-dot-rise
    /// </summary>
    [DefaultExecutionOrder(-367)]
    [DisallowMultipleComponent]
    public class DMUiToolkitDamage : MonoBehaviour
    {
        private const float Lifetime = 1.4f;
        private const float FadeDuration = 0.45f;
        private const float FloatSpeed = 0.85f;
        private const float StartScale = 0.7f;
        private const float EndScale = 1.55f;

        private static DMUiToolkitDamage instance;

        private UIDocument document;
        private VisualElement root;
        private VisualElement layer;
        private bool bound;
        private readonly List<Popup> popups = new List<Popup>();
        private VisualElement hitDirectionHost;
        private HitDirectionRing hitDirectionRing;
        private GameAudioProfile feedbackProfile;
        private readonly HitDirection[] hitDirections = new HitDirection[8];
        private bool hitDirectionsVisible;

        private struct HitDirection
        {
            public Vector3 Direction;
            public float Until;
        }

        private sealed class HitDirectionRing : VisualElement
        {
            public HitDirectionRing(DMUiToolkitDamage owner)
            {
                pickingMode = PickingMode.Ignore;
                style.position = Position.Absolute;
                style.left = style.top = style.right = style.bottom = 0f;
                generateVisualContent += owner.PaintHitDirections;
            }
        }

        private class Popup
        {
            public Label Label;
            public Vector3 World;
            public Vector3 Drift;
            public float Elapsed;
            public bool Crit;
            public float SizeScale = 1f;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying || !DMUiToolkitConfig.IsEnabled)
                return;

            EnsureHost();
        }

        public static DMUiToolkitDamage EnsureHost()
        {
            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitOverlayDocument.Ensure(
                DMUiToolkitOverlayDocument.DamageName,
                DMUiToolkitOverlayDocument.DamageUxml,
                DMUiToolkitOverlayDocument.DamageUss,
                DMUiToolkitOverlayDocument.DamageSort);
            if (doc == null)
                return null;

            DMUiToolkitDamage host = doc.GetComponent<DMUiToolkitDamage>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitDamage>();

            host.document = doc;
            host.BindTree();
            return host;
        }

        public static bool TrySpawn(float damage, Vector3 worldPosition, Color? color = null, float sizeScale = 1f, bool? crit = null)
        {
            if (!DMUiToolkitHud.IsDriving)
                return false;

            if (damage <= 0f)
                return true;

            DMUiToolkitDamage host = EnsureHost();
            if (host == null)
                return false;

            host.SpawnInternal(damage, worldPosition, color, sizeScale, crit);
            return true;
        }

        private void Awake()
        {
            instance = this;
            if (document == null)
                document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            instance = this;
            BindTree();
            CombatEvents.PlayerDamaged -= HandlePlayerDamaged;
            CombatEvents.PlayerDamaged += HandlePlayerDamaged;
        }

        private void OnDisable()
        {
            CombatEvents.PlayerDamaged -= HandlePlayerDamaged;
            System.Array.Clear(hitDirections, 0, hitDirections.Length);
            hitDirectionHost?.SetEnabled(false);
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void LateUpdate()
        {
            if (!bound)
                BindTree();

            TickPopups();
            TickHitDirections();
            HideUgui();
        }

        internal void BindTree()
        {
            if (document == null)
                document = GetComponent<UIDocument>();
            if (document == null)
                return;

            VisualElement tree = document.rootVisualElement;
            if (tree == null)
                return;

            root = tree.Q<VisualElement>("damage-root") ?? tree;
            DMUiToolkitOverlayDocument.ApplyIgnorePicking(root);
            layer = tree.Q<VisualElement>("damage-layer") ?? root;
            VisualElement directionHost = tree.Q<VisualElement>("hit-direction-host");
            if (directionHost != null && hitDirectionHost != directionHost)
            {
                hitDirectionHost = directionHost;
                hitDirectionRing = new HitDirectionRing(this);
                hitDirectionHost.Add(hitDirectionRing);
                hitDirectionHost.style.display = DisplayStyle.None;
            }
            bound = root != null;
        }

        private GameAudioProfile FeedbackProfile()
        {
            if (GameAudioManager.Instance != null) return GameAudioManager.Instance.Profile;
            if (feedbackProfile == null) feedbackProfile = Resources.Load<GameAudioProfile>("GameAudioProfile");
            return feedbackProfile;
        }

        private void HandlePlayerDamaged(GameObject player, GameObject source, float amount)
        {
            GameAudioProfile profile = FeedbackProfile();
            if (profile == null || !profile.playerHitIndicatorEnabled || player == null || source == null || amount <= 0f) return;
            Vector3 direction = source.transform.position - player.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return; // Unknown bearing: do not invent a direction.
            direction.Normalize();
            int slot = 0;
            float oldest = float.MaxValue;
            for (int i = 0; i < hitDirections.Length; i++)
            {
                if (hitDirections[i].Until > Time.unscaledTime && Vector3.Dot(direction, hitDirections[i].Direction) > 0.92f)
                { slot = i; break; }
                if (hitDirections[i].Until < oldest) { oldest = hitDirections[i].Until; slot = i; }
            }
            hitDirections[slot] = new HitDirection { Direction = direction, Until = Time.unscaledTime + profile.playerHitIndicatorSeconds };
        }

        private void TickHitDirections()
        {
            if (hitDirectionHost == null) return;
            GameAudioProfile profile = FeedbackProfile();
            bool visible = false;
            if (profile != null && profile.playerHitIndicatorEnabled && GameSession.HasStarted &&
                !MainMenuController.BlocksGameplayHud && !GameplayWorldSimulation.IsFrozen)
                for (int i = 0; i < hitDirections.Length; i++) visible |= hitDirections[i].Until > Time.unscaledTime;
            if (visible != hitDirectionsVisible)
            {
                hitDirectionsVisible = visible;
                hitDirectionHost.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                hitDirectionHost.SetEnabled(true);
            }
            if (visible) hitDirectionRing.MarkDirtyRepaint();
        }

        private void PaintHitDirections(MeshGenerationContext context)
        {
            GameAudioProfile profile = FeedbackProfile();
            Camera camera = Camera.main;
            if (profile == null || camera == null || !hitDirectionsVisible) return;
            Vector3 forward = camera.transform.forward; forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) { forward = camera.transform.up; forward.y = 0f; }
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Rect rect = hitDirectionRing.contentRect;
            Vector2 center = rect.center;
            float radius = Mathf.Clamp(profile.playerHitIndicatorRadius, 25f, 750f);
            float width = Mathf.Clamp(profile.playerHitIndicatorThickness, 1f, 16f);
            var painter = context.painter2D;
            for (int i = 0; i < hitDirections.Length; i++)
            {
                float remaining = hitDirections[i].Until - Time.unscaledTime;
                if (remaining <= 0f) continue;
                float angle = Mathf.Atan2(Vector3.Dot(hitDirections[i].Direction, right), Vector3.Dot(hitDirections[i].Direction, forward)) * Mathf.Rad2Deg - 90f;
                float half = Mathf.Clamp(profile.playerHitIndicatorArc, 15f, 100f) * 0.5f;
                float alpha = Mathf.Clamp01(remaining / Mathf.Max(0.01f, profile.playerHitIndicatorSeconds) * 2f);
                painter.lineWidth = width + 3f;
                painter.strokeColor = new Color(0f, 0f, 0f, alpha * 0.75f);
                painter.BeginPath(); painter.Arc(center, radius, Angle.Degrees(angle - half), Angle.Degrees(angle + half)); painter.Stroke();
                Color color = profile.playerHitIndicatorColor; color.a *= alpha;
                painter.lineWidth = width; painter.strokeColor = color;
                painter.BeginPath(); painter.Arc(center, radius, Angle.Degrees(angle - half), Angle.Degrees(angle + half)); painter.Stroke();
            }
        }

        private void SpawnInternal(float damage, Vector3 worldPosition, Color? color, float sizeScale = 1f, bool? critOverride = null)
        {
            BindTree();
            if (layer == null)
                return;

            Label label = new Label(Mathf.RoundToInt(damage).ToString());
            label.AddToClassList("dmg-dmg-num");
            label.pickingMode = PickingMode.Ignore;
            bool crit = critOverride ?? (color.HasValue && color.Value.g > 0.5f);
            if (crit)
                label.AddToClassList("dmg-dmg-crit");
            if (color.HasValue)
                label.style.color = color.Value;
            layer.Add(label);

            popups.Add(new Popup
            {
                Label = label,
                World = worldPosition,
                Drift = new Vector3(Random.Range(-0.08f, 0.08f), 0f, Random.Range(-0.08f, 0.08f)),
                Elapsed = 0f,
                Crit = crit,
                SizeScale = Mathf.Max(0.1f, sizeScale)
            });
        }

        private void TickPopups()
        {
            Camera camera = Camera.main;
            for (int i = popups.Count - 1; i >= 0; i--)
            {
                Popup popup = popups[i];
                popup.Elapsed += Time.deltaTime;
                // Rise in world space; panel Y is top-down so we map via viewport (not ScreenToPanel).
                popup.World += Vector3.up * FloatSpeed * Time.deltaTime;

                if (popup.Elapsed >= Lifetime || popup.Label == null)
                {
                    popup.Label?.RemoveFromHierarchy();
                    popups.RemoveAt(i);
                    continue;
                }

                if (camera == null || layer == null)
                    continue;

                Vector3 viewport = camera.WorldToViewportPoint(popup.World + popup.Drift);
                if (viewport.z <= 0.05f)
                {
                    popup.Label.style.opacity = 0f;
                    continue;
                }

                Rect layout = layer.contentRect;
                float w = layout.width;
                float h = layout.height;
                if (w < 1f || h < 1f)
                {
                    w = layer.resolvedStyle.width;
                    h = layer.resolvedStyle.height;
                }

                if (w >= 1f && h >= 1f)
                {
                    // Viewport Y is bottom-up; UITK Y is top-down — rising world => smaller top.
                    float left = viewport.x * w - 60f;
                    float top = (1f - viewport.y) * h - 20f;
                    popup.Label.style.left = left;
                    popup.Label.style.top = top;
                }

                float lifeT = Mathf.Clamp01(popup.Elapsed / Lifetime);
                // Grow steadily while rising; keep large through fade-out.
                float scale = Mathf.Lerp(StartScale, EndScale, lifeT);
                scale *= popup.SizeScale;
                popup.Label.style.scale = new Scale(new Vector3(scale, scale, 1f));

                float fadeStart = Lifetime - FadeDuration;
                popup.Label.style.opacity = popup.Elapsed >= fadeStart
                    ? 1f - ((popup.Elapsed - fadeStart) / FadeDuration)
                    : 1f;
            }
        }

        private static bool uguiHidden;

        private static void HideUgui()
        {
            if (!DMUiToolkitHud.IsDriving || uguiHidden)
                return;

            WorldFloatingDamageNumber[] world = Object.FindObjectsByType<WorldFloatingDamageNumber>(FindObjectsInactive.Include);
            for (int i = 0; i < world.Length; i++)
            {
                if (world[i] == null)
                    continue;
                Canvas canvas = world[i].GetComponent<Canvas>();
                if (canvas != null)
                    DMUiToolkitOverlayDocument.HideCanvas(canvas);
                else
                    DMUiToolkitOverlayDocument.HideGameObject(world[i].gameObject);
            }

            FloatingDamageNumber[] screen = Object.FindObjectsByType<FloatingDamageNumber>(FindObjectsInactive.Include);
            for (int i = 0; i < screen.Length; i++)
            {
                if (screen[i] != null)
                    DMUiToolkitOverlayDocument.HideGameObject(screen[i].gameObject);
            }

            uguiHidden = true;
        }
    }
}
