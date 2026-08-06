using Invector.vCharacterController;
using Invector.vMelee;
using Invector.vShooter;
using Project.Data;
using Project.Player.Invector;
using System;
using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// Equips preloaded Drawn/Holstered weapon slots on humanoid enemies from EnemyDefinition ItemData.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyInvectorLoadoutBridge : MonoBehaviour
    {
        [SerializeField] private ItemData meleeWeaponItem;
        [SerializeField] private ItemData rangedWeaponItem;
        [SerializeField] private bool preferRangedAtRange = true;
        [SerializeField] private bool startWithMeleeWeapon = true;

        private vShooterManager _shooterManager;
        private vMeleeManager _meleeManager;
        private vThirdPersonController _controller;
        private ItemData _activeItem;
        private GameObject _activeDrawnInstance;
        private GameObject _lastDroppedWeapon;

        public ItemData ActiveItem => _activeItem;
        public GameObject ActiveDrawnInstance => _activeDrawnInstance;
        public GameObject LastDroppedWeapon => _lastDroppedWeapon;
        public bool PrefersRangedAtRange => preferRangedAtRange;

        private void Awake()
        {
            _shooterManager = GetComponent<vShooterManager>();
            _meleeManager = GetComponent<vMeleeManager>();
            _controller = GetComponent<vThirdPersonController>();
        }

        private void Start()
        {
            EquipStartingWeapon();
        }

        public void ConfigureFromDefinition(EnemyDefinition definition)
        {
            if (definition == null)
                return;

            meleeWeaponItem = definition.meleeWeaponItem;
            rangedWeaponItem = definition.rangedWeaponItem;
            preferRangedAtRange = definition.preferRangedWeapon;
            EquipStartingWeapon();
        }

        public void ConfigureSpawnLoadout(
            ItemData melee,
            ItemData ranged,
            bool equipMeleeOnStart,
            bool preferRangedWhenFar)
        {
            if (melee != null)
                meleeWeaponItem = melee;
            if (ranged != null)
                rangedWeaponItem = ranged;

            startWithMeleeWeapon = equipMeleeOnStart;
            preferRangedAtRange = preferRangedWhenFar;
            EquipStartingWeapon();
        }

        public void EquipStartingWeapon()
        {
            ItemData weapon = ResolveStartingWeapon();
            EquipWeapon(weapon);
        }

        public void RefreshFromSerializedWeapons()
        {
            EquipWeapon(ResolveStartingWeapon());
        }

        public void EquipSpecificWeapon(ItemData weapon)
        {
            EquipWeapon(weapon);
        }

        /// <summary>
        /// Detaches the drawn weapon so it falls with physics instead of lifting during corpse disintegration.
        /// </summary>
        public void DropHeldWeaponOnDeath()
        {
            GameObject weaponRoot = ResolveDrawnWeaponRoot();
            if (weaponRoot == null)
                return;

            _activeDrawnInstance = null;
            _activeItem = null;

            ClearInvectorWeapons();
            weaponRoot.SetActive(true);
            weaponRoot.transform.SetParent(null, true);
            weaponRoot.transform.position += Vector3.up * 0.05f;

            DisableInvectorWeaponBehaviours(weaponRoot);
            EnableWeaponPhysics(weaponRoot);
            DeactivateHolsteredWeaponVisuals();
            _lastDroppedWeapon = weaponRoot;
        }

        public void ClearDroppedWeaponReference()
        {
            _lastDroppedWeapon = null;
        }

        public void FreezeDroppedWeaponForDissolve()
        {
            if (_lastDroppedWeapon == null)
                return;

            Rigidbody body = _lastDroppedWeapon.GetComponent<Rigidbody>();
            if (body != null)
            {
                // Unity 6 rejects velocity writes on kinematic bodies.
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }

                body.isKinematic = true;
                body.useGravity = false;
            }

            Collider[] colliders = _lastDroppedWeapon.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider != null)
                    collider.enabled = false;
            }
        }

        public void DestroyDroppedWeapon()
        {
            if (_lastDroppedWeapon == null)
                return;

            Destroy(_lastDroppedWeapon);
            _lastDroppedWeapon = null;
        }

        private GameObject ResolveDrawnWeaponRoot()
        {
            if (_activeDrawnInstance != null)
                return _activeDrawnInstance;

            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == null || child == transform)
                    continue;

                if (!child.name.StartsWith("Drawn_", StringComparison.Ordinal))
                    continue;

                if (!child.gameObject.activeInHierarchy)
                    continue;

                return child.gameObject;
            }

            return null;
        }

        private void DeactivateHolsteredWeaponVisuals()
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == null || child == transform)
                    continue;

                if (child.name.StartsWith("Holstered_", StringComparison.Ordinal))
                    child.gameObject.SetActive(false);
            }
        }

        private static void DisableInvectorWeaponBehaviours(GameObject weaponRoot)
        {
            Behaviour[] behaviours = weaponRoot.GetComponentsInChildren<Behaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                Behaviour behaviour = behaviours[i];
                if (behaviour == null)
                    continue;

                string typeName = behaviour.GetType().Name;
                if (typeName.Contains("MeleeWeapon") || typeName.Contains("ShooterWeapon"))
                    behaviour.enabled = false;
            }
        }

        private static void EnableWeaponPhysics(GameObject weaponRoot)
        {
            SetPhysicsLayerRecursively(weaponRoot, 0);

            Rigidbody body = weaponRoot.GetComponent<Rigidbody>();
            if (body == null)
                body = weaponRoot.AddComponent<Rigidbody>();

            body.isKinematic = false;
            body.useGravity = true;
            body.mass = Mathf.Max(body.mass, 1f);
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            bool hasSolidCollider = false;
            Collider[] colliders = weaponRoot.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null)
                    continue;

                if (collider.isTrigger)
                {
                    collider.enabled = false;
                    continue;
                }

                collider.enabled = true;
                hasSolidCollider = true;
            }

            if (!hasSolidCollider)
            {
                BoxCollider box = weaponRoot.AddComponent<BoxCollider>();
                Renderer renderer = weaponRoot.GetComponentInChildren<Renderer>();
                if (renderer != null)
                {
                    box.center = weaponRoot.transform.InverseTransformPoint(renderer.bounds.center);
                    box.size = renderer.bounds.size;
                }
                else
                {
                    box.size = Vector3.one * 0.2f;
                }
            }

            Physics.SyncTransforms();
        }

        private static void SetPhysicsLayerRecursively(GameObject root, int layer)
        {
            if (root == null)
                return;

            root.layer = layer;
            Transform transform = root.transform;
            for (int i = 0; i < transform.childCount; i++)
                SetPhysicsLayerRecursively(transform.GetChild(i).gameObject, layer);
        }

        public ItemData ResolveStartingWeapon()
        {
            if (startWithMeleeWeapon)
                return meleeWeaponItem != null ? meleeWeaponItem : rangedWeaponItem;

            return rangedWeaponItem != null ? rangedWeaponItem : meleeWeaponItem;
        }

        public ItemData ResolvePreferredWeapon()
        {
            return ResolveStartingWeapon();
        }

        public ItemData ResolveWeaponForTargetDistance(float distance, float meleeRange)
        {
            if (preferRangedAtRange && rangedWeaponItem != null && distance > meleeRange * 1.15f)
                return rangedWeaponItem;

            if (meleeWeaponItem != null)
                return meleeWeaponItem;

            return rangedWeaponItem;
        }

        private void EquipWeapon(ItemData weapon)
        {
            _activeItem = weapon;
            ClearInvectorWeapons();
            DeactivateAllWeaponVisuals();
            _activeDrawnInstance = null;

            if (weapon == null)
            {
                ResetUnarmedAnimatorState();
                return;
            }

            GameObject drawn = PioneerInvectorWeaponBridge.FindPreloadedDrawnSlot(transform, weapon);
            if (drawn == null)
            {
                ResetUnarmedAnimatorState();
                return;
            }

            GameObject holstered = PioneerInvectorWeaponBridge.FindPreloadedHolsteredSlot(transform, weapon);
            if (holstered != null)
                holstered.SetActive(false);

            if (weapon.itemType == ItemType.MeleeWeapon)
                PioneerInvectorWeaponBridge.PrepareDrawnMeleeSlot(drawn, weapon);
            else if (weapon.IsRangedWeapon)
                PioneerInvectorWeaponBridge.PrepareDrawnRangedSlot(drawn, weapon);

            drawn.SetActive(true);
            RestoreDrawnWeaponVisuals(drawn);
            PioneerInvectorWeaponBridge.ApplyItemStatsToInstance(weapon, drawn);
            _activeDrawnInstance = drawn;

            if (weapon.itemType == ItemType.MeleeWeapon)
                EquipMelee(drawn);
            else if (weapon.IsRangedWeapon)
                EquipRanged(drawn);

            SyncAnimatorWeaponState(weapon);
        }

        private void DeactivateAllWeaponVisuals()
        {
            vShooterWeapon[] ranged = GetComponentsInChildren<vShooterWeapon>(true);
            for (int i = 0; i < ranged.Length; i++)
            {
                if (ranged[i] != null)
                    ranged[i].gameObject.SetActive(false);
            }

            vMeleeWeapon[] melee = GetComponentsInChildren<vMeleeWeapon>(true);
            for (int i = 0; i < melee.Length; i++)
            {
                if (melee[i] != null)
                    melee[i].gameObject.SetActive(false);
            }
        }

        private void EquipMelee(GameObject drawn)
        {
            if (_meleeManager == null || drawn == null)
                return;

            vMeleeWeapon weapon = drawn.GetComponentInChildren<vMeleeWeapon>(true);
            if (weapon != null)
                _meleeManager.SetRightWeapon(weapon.gameObject);
        }

        /// <summary>
        /// After VBOT stock-body hide (or a blanket renderer re-enable), Drawn slots must show the
        /// ItemData PioneerVisual mesh and keep GreatSword/handgun vendor leftovers component-disabled.
        /// </summary>
        private static void RestoreDrawnWeaponVisuals(GameObject drawn)
        {
            if (drawn == null)
                return;

            Transform pioneerVisual = null;
            Transform[] children = drawn.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == null || child == drawn.transform)
                    continue;

                if (!child.name.StartsWith("PioneerVisual_", StringComparison.Ordinal))
                    continue;

                pioneerVisual = child;
                child.gameObject.SetActive(true);
                break;
            }

            Renderer[] renderers = drawn.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                bool underPioneer = pioneerVisual != null &&
                                    (renderer.transform == pioneerVisual ||
                                     renderer.transform.IsChildOf(pioneerVisual));

                if (pioneerVisual != null)
                {
                    renderer.enabled = underPioneer;
                    if (underPioneer && !renderer.gameObject.activeSelf)
                        renderer.gameObject.SetActive(true);
                }
                else
                {
                    renderer.enabled = true;
                    if (!renderer.gameObject.activeSelf)
                        renderer.gameObject.SetActive(true);
                }
            }
        }

        private void EquipRanged(GameObject drawn)
        {
            if (_shooterManager == null || drawn == null)
                return;

            vShooterWeapon weapon = drawn.GetComponentInChildren<vShooterWeapon>(true);
            if (weapon != null)
            {
                PrepareUnifiedProjectileWeapon(weapon);
                _shooterManager.SetRightWeapon(weapon.gameObject);
            }
        }

        /// <summary>
        /// Same treatment as PioneerInvectorWeaponBridge/CompanionInvectorLoadoutBridge: fully hand
        /// shot-effect ownership to our own ammo-driven pipeline. Invector's own ShotEffect() spawns
        /// its own bullet+trail (projectile), plays its own bundled gunshot (fireClip), fires its own
        /// muzzle-flash particles (emittShurykenParticle), and flashes its own light (lightOnShot) —
        /// all independent of and in addition to CombatProjectileSpawner/CombatHitResolver. Clearing
        /// all of them plus forcing isInfinityAmmo/dontUseReload the moment the weapon is equipped
        /// means every visual/audio/damage effect comes from our ammoItem-driven system only.
        /// </summary>
        private static void PrepareUnifiedProjectileWeapon(vShooterWeapon weapon)
        {
            if (weapon == null)
                return;

            weapon.projectile = null;
            weapon.fireClip = null;
            weapon.emittShurykenParticle = null;
            weapon.lightOnShot = null;
            weapon.isInfinityAmmo = true;
            weapon.dontUseReload = true;
            weapon.ammo = weapon.clipSize > 0 ? weapon.clipSize : 999;
        }

        private void ClearInvectorWeapons()
        {
            _shooterManager?.SetRightWeapon((GameObject)null);
            _meleeManager?.SetRightWeapon((GameObject)null);
        }

        private void SyncAnimatorWeaponState(ItemData weapon)
        {
            if (_controller == null || _controller.animator == null || weapon == null)
                return;

            if (weapon.itemType == ItemType.MeleeWeapon && _meleeManager != null)
            {
                _controller.animator.SetFloat("MoveSet_ID", _meleeManager.GetMoveSetID());
                _controller.animator.SetFloat("UpperBody_ID", 0f);
                return;
            }

            if (weapon.IsRangedWeapon)
            {
                _controller.animator.SetFloat("MoveSet_ID", 0f);
                _controller.animator.SetFloat("UpperBody_ID", 1f);
                // isStrafing is managed per-frame by EnemyInvectorCombatBridge.UpdateRangedAimStance
                // based on whether the enemy is actively in a ranged engagement.
            }
        }

        private void ResetUnarmedAnimatorState()
        {
            if (_controller == null || _controller.animator == null)
                return;

            _controller.animator.SetFloat("MoveSet_ID", 0f);
            _controller.animator.SetFloat("UpperBody_ID", 0f);
        }
    }
}
