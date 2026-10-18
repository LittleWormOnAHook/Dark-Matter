using System.Collections.Generic;
using Project.Inventory;
using Project.Player;
using Project.Survival;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.Combat.Turrets
{
    /// <summary>
    /// Hides Kade's vis and pauses combat/locomotion ticks while seated.
    /// Does not destroy the player, retune physics, or disable HUD / inventory.
    /// </summary>
    static class DMTurretPlayerSeatHider
    {
        struct RendererSnap
        {
            public Renderer Renderer;
            public bool Enabled;
        }

        struct BehaviourSnap
        {
            public Behaviour Behaviour;
            public bool Enabled;
        }

        static readonly List<RendererSnap> Renderers = new List<RendererSnap>(64);
        static readonly List<BehaviourSnap> Behaviours = new List<BehaviourSnap>(32);
        static bool captured;

        public static void Hide(Transform player)
        {
            Restore();
            if (player == null)
                return;

            CaptureRenderers(player);
            CaptureBehaviours(player);
            captured = true;

            for (int i = 0; i < Renderers.Count; i++)
            {
                Renderer renderer = Renderers[i].Renderer;
                if (renderer != null)
                    renderer.enabled = false;
            }

            for (int i = 0; i < Behaviours.Count; i++)
            {
                Behaviour behaviour = Behaviours[i].Behaviour;
                if (behaviour != null)
                    behaviour.enabled = false;
            }
        }

        public static void Restore()
        {
            if (!captured)
            {
                Renderers.Clear();
                Behaviours.Clear();
                return;
            }

            for (int i = 0; i < Behaviours.Count; i++)
            {
                BehaviourSnap snap = Behaviours[i];
                if (snap.Behaviour != null)
                    snap.Behaviour.enabled = snap.Enabled;
            }

            for (int i = 0; i < Renderers.Count; i++)
            {
                RendererSnap snap = Renderers[i];
                if (snap.Renderer != null)
                    snap.Renderer.enabled = snap.Enabled;
            }

            ClearWithoutRestore();
        }

        public static void ClearWithoutRestore()
        {
            captured = false;
            Renderers.Clear();
            Behaviours.Clear();
        }

        static void CaptureRenderers(Transform player)
        {
            Renderers.Clear();
            Renderer[] found = player.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < found.Length; i++)
            {
                Renderer renderer = found[i];
                if (renderer == null)
                    continue;
                Renderers.Add(new RendererSnap
                {
                    Renderer = renderer,
                    Enabled = renderer.enabled,
                });
            }
        }

        static void CaptureBehaviours(Transform player)
        {
            Behaviours.Clear();
            Behaviour[] found = player.GetComponentsInChildren<Behaviour>(true);
            for (int i = 0; i < found.Length; i++)
            {
                Behaviour behaviour = found[i];
                if (behaviour == null || !ShouldDisable(behaviour))
                    continue;
                Behaviours.Add(new BehaviourSnap
                {
                    Behaviour = behaviour,
                    Enabled = behaviour.enabled,
                });
            }
        }

        static bool ShouldDisable(Behaviour behaviour)
        {
            if (behaviour is Camera || behaviour is AudioListener)
                return false;
            if (behaviour is PlayerController)
                return false;
            if (behaviour is InventorySystem || behaviour is EquipmentController)
                return false;
            if (behaviour is SurvivalStats)
                return false;
            if (behaviour is UIDocument)
                return false;

            string typeName = behaviour.GetType().Name;
            if (typeName.StartsWith("DMUiToolkit", System.StringComparison.Ordinal))
                return false;

            switch (typeName)
            {
                case "PioneerShooterMeleeInput":
                case "PioneerShooterManager":
                case "vShooterMeleeInput":
                case "vShooterManager":
                case "vMeleeManager":
                case "WeaponModeSwitchController":
                case "MeleeCombatController":
                case "DMJetpackController":
                case "DMJetpackThrusterVfx":
                case "DMJetpackInputBridge":
                case "DMClimbController":
                case "DMLandingDirector":
                case "DMLocomotionGaitController":
                case "vThirdPersonController":
                case "vHeadTrack":
                case "vWeaponIK":
                case "Animator":
                    return true;
                default:
                    return false;
            }
        }
    }
}
