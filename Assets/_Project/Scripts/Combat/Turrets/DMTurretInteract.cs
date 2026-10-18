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

        [SerializeField] private float interactRange = 3.5f;
        [SerializeField] private Collider interactCollider;

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
            if (!IsAimedInRange(context, out float distance))
                return -1f;
            return 87f - distance;
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

        private bool IsAimedInRange(WorldUseContext context, out float distance)
        {
            Bounds bounds = interactCollider != null ? interactCollider.bounds : new Bounds(transform.position, Vector3.one * 2f);
            distance = PlayerInteractionUtility.DistanceToInteractable(
                context.PlayerPosition,
                interactCollider,
                transform.position);
            float range = DMBuildingGhostProfileRange();
            if (distance > range)
                return false;

            if (context.AimHit.HasValue && context.AimHit.Value.collider != null
                && context.AimHit.Value.collider.GetComponentInParent<DMTurretInteract>() == this)
                return true;

            Ray ray = context.ViewRay;
            Vector3 toCenter = bounds.center - ray.origin;
            float along = Vector3.Dot(toCenter, ray.direction);
            if (along < 0f)
                return false;
            float off = (toCenter - ray.direction * along).magnitude;
            return off <= Mathf.Max(1.4f, bounds.extents.magnitude * 0.7f);
        }

        private float DMBuildingGhostProfileRange()
        {
            float fromGhost = Project.Building.DMBuildingGhostProfile.TurretInteractRangeMeters;
            return fromGhost > 0.1f ? fromGhost : interactRange;
        }
    }
}
