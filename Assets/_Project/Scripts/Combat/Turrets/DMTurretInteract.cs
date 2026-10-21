using System.Collections.Generic;
using Project.Core;
using Project.Interaction;
using Project.Player;
using Project.UI;
using UnityEngine;

namespace Project.Combat.Turrets
{
    /// <summary>Press E on a player-owned turret to open the UITK Auto / Enter menu.</summary>
    [DisallowMultipleComponent]
    public sealed class DMTurretInteract : MonoBehaviour, IWorldUsable
    {
        private static readonly List<DMTurretInteract> Active = new List<DMTurretInteract>(8);

        [Tooltip("Body collider: physics / aim-hit preference only. Interaction is decided by the InteractZone trigger.")]
        [SerializeField] private Collider interactCollider;

        public const string InteractZoneName = "InteractZone";
        private SphereCollider interactZone;
        public SphereCollider InteractZone => interactZone;

        private DMTurretRuntime runtime;

        public DMTurretRuntime Runtime => runtime;
        public static IReadOnlyList<DMTurretInteract> All => Active;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Active.Clear();

        private void Awake()
        {
            runtime = GetComponent<DMTurretRuntime>();
            if (interactCollider == null)
                interactCollider = GetComponent<Collider>();
            if (interactCollider == null)
                interactCollider = GetComponentInChildren<Collider>();
            EnsureInteractZone();
        }

        /// <summary>2026-10-20f: dedicated child trigger, sole interact authority. Built at runtime if missing (no prefab save).</summary>
        private void EnsureInteractZone()
        {
            Transform zt = transform.Find(InteractZoneName);
            if (zt == null)
            {
                zt = new GameObject(InteractZoneName).transform;
                zt.SetParent(transform, false);
            }

            // Ignore Raycast layer: excluded from default raycasts / aim; trigger never pushes physics or the player.
            zt.gameObject.layer = 2;
            interactZone = zt.GetComponent<SphereCollider>();
            if (interactZone == null)
                interactZone = zt.gameObject.AddComponent<SphereCollider>();
            interactZone.isTrigger = true;
            ApplyZoneRadius();
        }

        private void ApplyZoneRadius()
        {
            if (interactZone == null)
                return;
            Vector3 s = interactZone.transform.lossyScale;
            float scale = Mathf.Max(0.0001f, Mathf.Max(Mathf.Abs(s.x), Mathf.Max(Mathf.Abs(s.y), Mathf.Abs(s.z))));
            float r = ResolveInteractRange() / scale;
            if (!Mathf.Approximately(interactZone.radius, r))
                interactZone.radius = r;
        }

        private bool PlayerInsideZone(Vector3 playerPosition, out float distance)
        {
            ApplyZoneRadius(); // follows Studio edits live
            Vector3 c = interactZone != null ? interactZone.transform.position : transform.position;
            distance = Vector3.Distance(playerPosition, c);
            return distance <= ResolveInteractRange();
        }

        private bool IsAimedAt(WorldUseContext context)
        {
            if (context.AimHit.HasValue && context.AimHit.Value.collider != null
                && context.AimHit.Value.collider.GetComponentInParent<DMTurretInteract>() == this)
                return true;
            Bounds bounds = interactCollider != null ? interactCollider.bounds : new Bounds(transform.position, Vector3.one * 2f);
            Ray ray = context.ViewRay;
            Vector3 toCenter = bounds.center - ray.origin;
            float along = Vector3.Dot(toCenter, ray.direction);
            if (along < 0f)
                return false;
            float off = (toCenter - ray.direction * along).magnitude;
            return off <= Mathf.Max(1.4f, bounds.extents.magnitude * 0.7f);
        }

        private void OnDrawGizmosSelected()
        {
            float r = Application.isPlaying ? ResolveInteractRange()
                : Project.Building.DMBuildingGhostProfile.TurretInteractRangeMeters;
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, r);
        }

        private void OnEnable()
        {
            if (!Active.Contains(this))
                Active.Add(this);
            WorldUseController.Register(this);
        }

        private void OnDisable()
        {
            Active.Remove(this);
            WorldUseController.Unregister(this);
        }

        public float GetUsePriority(WorldUseContext context)
        {
            if (!GameSession.HasStarted || runtime == null || runtime.IsDestroyed)
                return -1f;
            if (runtime.Faction != DMTurretFaction.Player && !runtime.PlacedByPlayer)
                return -1f;
            // 2026-10-20f: InteractZone is the sole authority. Aimed-at turret wins; otherwise nearest (stays below close loot/crates).
            if (!PlayerInsideZone(context.PlayerPosition, out float distance))
                return -1f;
            return IsAimedAt(context) ? 87f - distance * 0.1f : 60f - distance;
        }

        public bool TryUse(WorldUseContext context)
        {
            if (GetUsePriority(context) < 0f)
                return false;
            if (DMUiToolkitTurretMenu.IsOpen)
                return DMUiToolkitTurretMenu.TryHide();
            if (DMTurretPlayerControl.IsControlling(runtime))
            {
                DMTurretPlayerControl.Exit();
                return true;
            }

            return DMUiToolkitTurretMenu.TryShow(this);
        }

        public string GetInteractionPromptMessage()
        {
            if (runtime == null)
                return "Press E — Turret";
            if (DMTurretPlayerControl.IsControlling(runtime))
                return "Press E — Exit turret";
            return "Press E — Turret";
        }

        public static string TryGetPrompt(WorldUseContext context)
        {
            DMTurretInteract best = null;
            float bestPriority = -1f;
            for (int i = 0; i < Active.Count; i++)
            {
                DMTurretInteract turret = Active[i];
                if (turret == null)
                    continue;
                float priority = turret.GetUsePriority(context);
                if (priority <= bestPriority)
                    continue;
                best = turret;
                bestPriority = priority;
            }

            return best != null && bestPriority >= 0f ? best.GetInteractionPromptMessage() : null;
        }

        private float ResolveInteractRange()
        {
            return runtime != null ? runtime.InteractRangeMeters : 3.5f;
        }
    }
}
