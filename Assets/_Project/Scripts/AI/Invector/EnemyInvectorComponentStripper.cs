using System;
using Invector;
using Invector.vCharacterController;
using Invector.vCharacterController.vActions;
using Invector.vItemManager;
using Invector.vMelee;
using Invector.vShooter;
using Project.Combat;
using Project.Interaction;
using Project.Inventory;
using Project.Player;
using Project.Player.Invector;
using Project.Survival;
using Project.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.AI.Invector
{
    /// <summary>
    /// Removes player-only Pioneer and Invector components from humanoid enemy prefabs.
    /// Keeps locomotion, melee/shooter managers, animator, weapon hitboxes, and enemy bridges.
    /// </summary>
    public static class EnemyInvectorComponentStripper
    {
        private static readonly string[] PlayerOnlyChildRootNames =
        {
            "InvectorComponents",
        };

        public static void StripRuntime(GameObject root)
        {
            if (root == null)
                return;

            EnemyInvectorBodySnapSetup.PreserveBeforeStrip(root);
            Strip(root, component =>
            {
                if (component != null)
                    UnityEngine.Object.Destroy(component);
            });

            DestroyPlayerOnlyChildRoots(root, go =>
            {
                if (go != null)
                    UnityEngine.Object.Destroy(go);
            });
        }

#if UNITY_EDITOR
        public static void StripEditor(GameObject root)
        {
            if (root == null)
                return;

            EnemyInvectorBodySnapSetup.PreserveBeforeStrip(root);
            Strip(root, component =>
            {
                if (component != null)
                    UnityEngine.Object.DestroyImmediate(component, true);
            });

            DestroyPlayerOnlyChildRoots(root, go =>
            {
                if (go != null)
                    UnityEngine.Object.DestroyImmediate(go, true);
            });
        }
#endif

        public static void StripAfterBodySnap(GameObject root, Action<Component> destroyComponent)
        {
            // Body snaps are preserved on humanoid enemies — no post-snap stripping.
        }

        private static void Strip(GameObject root, Action<Component> destroyComponent)
        {
            DestroyThrowManagerRoots(root);
            StripPioneerPlayerStack(root, destroyComponent);
            StripInvectorPlayerStack(root, destroyComponent);
        }

        private static void DestroyThrowManagerRoots(GameObject root)
        {
            global::Invector.Throw.vThrowManagerBase[] throwManagers =
                root.GetComponentsInChildren<global::Invector.Throw.vThrowManagerBase>(true);
            for (int i = 0; i < throwManagers.Length; i++)
            {
                if (throwManagers[i] != null)
                    UnityEngine.Object.Destroy(throwManagers[i].gameObject);
            }
        }

        private static void StripPioneerPlayerStack(GameObject root, Action<Component> destroyComponent)
        {
            // Dependents before RequireComponent parents.
            DestroyAll<PioneerInvectorInputBridge>(root, destroyComponent);
            DestroyAll<PioneerInvectorWeaponBridge>(root, destroyComponent);
            DestroyAll<PioneerInvectorDamageBridge>(root, destroyComponent);
            DestroyAll<PioneerInvectorAmmoBridge>(root, destroyComponent);
            DestroyAll<PioneerInvectorBootstrap>(root, destroyComponent);
            DestroyAll<PioneerInvectorSurvivalBridge>(root, destroyComponent);
            DestroyAll<PioneerInvectorDeathRagdoll>(root, destroyComponent);
            DestroyAll<PioneerInvectorNullAimCanvas>(root, destroyComponent);
            DestroyAll<PioneerPlayerInputBinder>(root, destroyComponent);
            DestroyAll<PioneerShooterMeleeInput>(root, destroyComponent);
            DestroyAll<PioneerTerrainRescue>(root, destroyComponent);
            DestroyAll<PlayerInput>(root, destroyComponent);
            DestroyAll<RangedCombatHud>(root, destroyComponent);
            DestroyAll<MeleeCombatController>(root, destroyComponent);
            DestroyAll<RangedCombatController>(root, destroyComponent);
            DestroyAll<EquippedItemVisual>(root, destroyComponent);
            DestroyAll<EquippedVisualMarker>(root, destroyComponent);
            DestroyAll<EquipmentController>(root, destroyComponent);
            DestroyAll<InventorySystem>(root, destroyComponent);
            DestroyAll<WeaponAmmoState>(root, destroyComponent);
            DestroyAll<PlayerDeathHandler>(root, destroyComponent);
            DestroyAll<CombatFocusController>(root, destroyComponent);
            DestroyAll<PlayerController>(root, destroyComponent);
            DestroyAll<ResourceGatherer>(root, destroyComponent);
            DestroyAll<SurvivalStats>(root, destroyComponent);
            DestroyAll<EnemyAnimationController>(root, destroyComponent);
        }

        private static void StripInvectorPlayerStack(GameObject root, Action<Component> destroyComponent)
        {
            DestroyAll<vShooterMeleeInput>(root, destroyComponent);
            DestroyAll<vThirdPersonInput>(root, destroyComponent);
            DestroyAll<vMeleeCombatInput>(root, destroyComponent);
            DestroyAll<vLockOnShooter>(root, destroyComponent);
            DestroyAll<vAmmoManager>(root, destroyComponent);
            DestroyAll<vHeadTrack>(root, destroyComponent);
            // vRagdoll kept for enemy death presentation.
            DestroyAll<vFootStep>(root, destroyComponent);
            DestroyAll<vGenericAction>(root, destroyComponent);
            DestroyAll<vLadderAction>(root, destroyComponent);
            DestroyAll<vCollectShooterMeleeControl>(root, destroyComponent);
            DestroyAll<vCollectMeleeControl>(root, destroyComponent);
            DestroyAll<vHUDController>(root, destroyComponent);
            DestroyAll<vDamageReceiver>(root, destroyComponent);
            DestroyAll<vItemManager>(root, destroyComponent);
            DestroyAll<vInventory>(root, destroyComponent);
            DestroyAll<vEquipArea>(root, destroyComponent);
            DestroyAll<vEquipAreaControl>(root, destroyComponent);
            DestroyAll<vWeaponHolderManager>(root, destroyComponent);
            DestroyAll<vItemCollection>(root, destroyComponent);
            DestroyAll<vItemCollectionDisplay>(root, destroyComponent);
            DestroyAll<vItemCollectionDisplay_v2>(root, destroyComponent);
            DestroyAll<vOpenCloseInventoryTrigger>(root, destroyComponent);
            DestroyAll<vMasterWindow>(root, destroyComponent);
            DestroyAll<vAmmoStandalone>(root, destroyComponent);
            DestroyAll<vControlAreaByInput>(root, destroyComponent);
            DestroyAll<vDrawHideMeleeWeapons>(root, destroyComponent);
            DestroyAll<vDrawHideShooterWeapons>(root, destroyComponent);
            DestroyAll<vInput>(root, destroyComponent);
            DestroyAll<vMousePositionHandler>(root, destroyComponent);
            DestroyAll<global::Invector.vCamera.vThirdPersonCamera>(root, destroyComponent);
            DestroyAll<global::Invector.vCamera.vChangeCameraAngleTrigger>(root, destroyComponent);
        }

        private static void DestroyPlayerOnlyChildRoots(GameObject root, Action<GameObject> destroyRoot)
        {
            for (int i = 0; i < PlayerOnlyChildRootNames.Length; i++)
            {
                Transform child = root.transform.Find(PlayerOnlyChildRootNames[i]);
                if (child != null)
                    destroyRoot(child.gameObject);
            }
        }

        private static void DestroyAll<T>(GameObject root, Action<Component> destroyComponent) where T : Component
        {
            T[] components = root.GetComponentsInChildren<T>(true);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null)
                    destroyComponent(components[i]);
            }
        }
    }
}
