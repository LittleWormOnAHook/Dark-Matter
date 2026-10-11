using Invector.vCharacterController;
using Invector.vMelee;
using Invector.vShooter;
using Project.Combat;
using Project.Data;
using Project.Player.Invector;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Project.AI.Invector
{
    public enum EnemyWeaponSlot { Melee, Ranged }

    /// <summary>Who asked for a weapon switch. Brain holds win over AI distance switching while the hold lasts.</summary>
    public enum EnemyWeaponRequester { Spawn, Ai, Brain, Spawner, Restore }

    /// <summary>Where a resolved weapon came from.</summary>
    public enum EnemyWeaponSource { None, Definition, Override }

    public enum EnemyPreferRangedOverride { UseDefinition, ForceOn, ForceOff }

    /// <summary>
    /// SINGLE AUTHORITY for enemy weapons. Resolved weapon = this bridge's field when set (per-instance OVERRIDE),
    /// otherwise the linked EnemyDefinition's weapon (DEFAULT). Only the resolved weapons are ever equipped and
    /// every other weapon object (template Drawn_/Holstered_ slots, baked holder meshes) is hidden.
    /// Weapon switching goes through <see cref="TrySwitchTo"/> / <see cref="TrySwitchToSlot"/> only.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyInvectorLoadoutBridge : MonoBehaviour
    {
        [Header("Per-instance weapon OVERRIDES (empty = use the EnemyDefinition's weapon)")]
        [Tooltip("Melee weapon OVERRIDE. Leave empty to use the EnemyDefinition's melee weapon.")]
        [SerializeField] private ItemData meleeWeaponItem;
        [Tooltip("Ranged weapon OVERRIDE. Leave empty to use the EnemyDefinition's ranged weapon.")]
        [SerializeField] private ItemData rangedWeaponItem;
        [Tooltip("UseDefinition = follow EnemyDefinition.preferRangedWeapon.")]
        [SerializeField] private EnemyPreferRangedOverride preferRangedOverride = EnemyPreferRangedOverride.UseDefinition;

        [Header("Fallbacks (only used when NO EnemyDefinition is linked)")]
        [SerializeField] private bool preferRangedAtRange = true;
        [SerializeField] private bool startWithMeleeWeapon = true;

        [Header("Holster")]
        [Tooltip("Show the OTHER resolved weapon (melee or ranged) on its holster while the first one is drawn. Every non-resolved weapon stays hidden.")]
        [SerializeField] private bool showHolsteredSecondary = true;

        [Header("Weapon switching")]
        [Tooltip("How long a Brain-requested weapon stays drawn before AI distance switching may change it.")]
        [SerializeField] private float brainHoldSeconds = 1.5f;

        private vShooterManager _shooterManager;
        private vMeleeManager _meleeManager;
        private vThirdPersonController _controller;
        private ItemData _activeItem;
        private GameObject _activeDrawnInstance;
        private GameObject _lastDroppedWeapon;
        /// <summary>The vMeleeWeapon this bridge last bound to vMeleeManager.rightWeapon (null = none/ranged/unarmed).</summary>
        private vMeleeWeapon _boundMelee;
        private EnemyDefinition _definition;
        private EnemyHealth _health;
        private int _startMeleeOverride = -1;
        private float _brainHoldUntil;
        private ItemData _brainHoldItem;
        private float _nextEnforceTime;
        private Transform[] _slotRoots;
        private Transform[] _strayRoots;
        private GameObject _holsterKeep;

        public EnemyWeaponRequester LastSwitchBy { get; private set; } = EnemyWeaponRequester.Spawn;
        public bool BrainHoldActive => _brainHoldItem != null && Time.time < _brainHoldUntil;

        public ItemData ActiveItem => _activeItem;
        public GameObject ActiveDrawnInstance => _activeDrawnInstance;
        public GameObject LastDroppedWeapon => _lastDroppedWeapon;

        // ---- Resolved weapons (override, else definition) -------------------------------------------------
        /// <summary>The linked EnemyDefinition (runtime-configured, else the Bootstrap's serialized link).</summary>
        public EnemyDefinition ResolveDefinition()
        {
            if (_definition != null)
                return _definition;

            EnemyInvectorBootstrap bootstrap = GetComponent<EnemyInvectorBootstrap>();
            return bootstrap != null ? bootstrap.Definition : null;
        }

        /// <summary>Serialized per-instance override (null = use the definition).</summary>
        public ItemData MeleeOverride => meleeWeaponItem;
        public ItemData RangedOverride => rangedWeaponItem;
        public EnemyPreferRangedOverride PreferRangedOverride => preferRangedOverride;

        /// <summary>Mining tools (DM_Mining_Tool, isMiningTool) are player utility items, never enemy weapons: they are skipped by the loadout
        /// (they have no drawn enemy visual and deal fallback damage). Applies to definition weapons and per-instance overrides.</summary>
        public static ItemData EnemyUsable(ItemData item) => item != null && item.isMiningTool ? null : item;

        public ItemData ResolvedMelee
        {
            get
            {
                if (EnemyUsable(meleeWeaponItem) != null)
                    return meleeWeaponItem;
                EnemyDefinition d = ResolveDefinition();
                return d != null ? EnemyUsable(d.meleeWeaponItem) : null;
            }
        }

        public ItemData ResolvedRanged
        {
            get
            {
                if (EnemyUsable(rangedWeaponItem) != null)
                    return rangedWeaponItem;
                EnemyDefinition d = ResolveDefinition();
                return d != null ? EnemyUsable(d.rangedWeaponItem) : null;
            }
        }

        public bool ResolvedPreferRanged
        {
            get
            {
                if (preferRangedOverride == EnemyPreferRangedOverride.ForceOn)
                    return true;
                if (preferRangedOverride == EnemyPreferRangedOverride.ForceOff)
                    return false;
                EnemyDefinition d = ResolveDefinition();
                return d != null ? d.preferRangedWeapon : preferRangedAtRange;
            }
        }

        public EnemyWeaponSource MeleeSource =>
            meleeWeaponItem != null ? EnemyWeaponSource.Override
            : (ResolveDefinition() != null && ResolveDefinition().meleeWeaponItem != null ? EnemyWeaponSource.Definition : EnemyWeaponSource.None);

        public EnemyWeaponSource RangedSource =>
            rangedWeaponItem != null ? EnemyWeaponSource.Override
            : (ResolveDefinition() != null && ResolveDefinition().rangedWeaponItem != null ? EnemyWeaponSource.Definition : EnemyWeaponSource.None);

        public bool PrefersRangedAtRange => ResolvedPreferRanged;
        public ItemData MeleeWeaponItem => ResolvedMelee;
        public ItemData RangedWeaponItem => ResolvedRanged;
        public EnemyWeaponSlot ActiveSlot =>
            _activeItem != null && _activeItem.IsRangedWeapon ? EnemyWeaponSlot.Ranged : EnemyWeaponSlot.Melee;

        public ItemData ItemForSlot(EnemyWeaponSlot slot) =>
            slot == EnemyWeaponSlot.Ranged ? ResolvedRanged : ResolvedMelee;

        private void Awake()
        {
            _shooterManager = GetComponent<vShooterManager>();
            _meleeManager = GetComponent<vMeleeManager>();
            _controller = GetComponent<vThirdPersonController>();
            _health = GetComponent<EnemyHealth>();
        }

        /// <summary>
        /// The definition SEEDS the loadout (default weapons / prefer-ranged). Nothing is copied into the serialized
        /// override fields, so a later definition edit still flows through unless an override is set.
        /// </summary>
        public void ConfigureFromDefinition(EnemyDefinition definition)
        {
            if (definition == null)
                return;

            _definition = definition;
            EquipStartingWeapon();
        }

        /// <summary>
        /// Spawner / EnemySpawnConfigurator path: per-spawn OVERRIDES (items equal to the definition's are not treated
        /// as overrides, so they keep following the definition).
        /// </summary>
        public void ConfigureSpawnLoadout(
            ItemData melee,
            ItemData ranged,
            bool equipMeleeOnStart,
            bool preferRangedWhenFar)
        {
            EnemyDefinition def = ResolveDefinition();
            melee = EnemyUsable(melee);     // mining tools are never enemy weapons
            ranged = EnemyUsable(ranged);
            // An item equal to the definition's is not an override: clear any earlier override so the slot follows the definition.
            if (melee != null)
                meleeWeaponItem = (def == null || melee != def.meleeWeaponItem) ? melee : null;
            if (ranged != null)
                rangedWeaponItem = (def == null || ranged != def.rangedWeaponItem) ? ranged : null;

            if (def != null && preferRangedWhenFar == def.preferRangedWeapon)
                preferRangedOverride = EnemyPreferRangedOverride.UseDefinition;
            else
                preferRangedOverride = preferRangedWhenFar
                    ? EnemyPreferRangedOverride.ForceOn
                    : EnemyPreferRangedOverride.ForceOff;

            _startMeleeOverride = equipMeleeOnStart ? 1 : 0;
            startWithMeleeWeapon = equipMeleeOnStart;
            LastSwitchBy = EnemyWeaponRequester.Spawner;
            _brainHoldItem = null;
            _brainHoldUntil = 0f;
            EquipWeapon(ResolveStartingWeapon());
        }

        public void EquipStartingWeapon()
        {
            _brainHoldItem = null;
            _brainHoldUntil = 0f;
            LastSwitchBy = EnemyWeaponRequester.Restore;
            EquipWeapon(ResolveStartingWeapon());
        }

        public void RefreshFromSerializedWeapons()
        {
            EquipStartingWeapon();
        }

        /// <summary>Legacy entry point (AI distance switching). Same as <c>TrySwitchTo(weapon, Ai)</c>.</summary>
        public void EquipSpecificWeapon(ItemData weapon)
        {
            TrySwitchTo(weapon, EnemyWeaponRequester.Ai);
        }

        /// <summary>
        /// THE one switch call. AI (distance) and Brain (decisions) both use it. A Brain request holds the weapon for
        /// <see cref="brainHoldSeconds"/>; AI requests for a different weapon are refused during the hold, so the two
        /// can never flip-flop. Only resolved weapons (override, else definition) can be drawn.
        /// </summary>
        public bool TrySwitchTo(ItemData weapon, EnemyWeaponRequester who, float holdSeconds = -1f)
        {
            if (weapon == null)
                return false;

            if (weapon != ResolvedMelee && weapon != ResolvedRanged)
                return false;

            if (who == EnemyWeaponRequester.Brain)
            {
                _brainHoldItem = weapon;
                _brainHoldUntil = Time.time + (holdSeconds >= 0f ? holdSeconds : brainHoldSeconds);
            }
            else if (who == EnemyWeaponRequester.Ai && BrainHoldActive && weapon != _brainHoldItem)
            {
                return false;
            }

            LastSwitchBy = who;
            if (weapon == _activeItem && _activeDrawnInstance != null)
                return true;

            EquipWeapon(weapon);
            return _activeItem == weapon;
        }

        public bool TrySwitchToSlot(EnemyWeaponSlot slot, EnemyWeaponRequester who, float holdSeconds = -1f)
        {
            return TrySwitchTo(ItemForSlot(slot), who, holdSeconds);
        }

        /// <summary>
        /// Detaches the drawn weapon so it falls with physics instead of lifting during corpse disintegration.
        /// </summary>
        public void DropHeldWeaponOnDeath()
        {
            // Idempotent: a second call (death presenter + ragdoll bridge) must not clear
            // _lastDroppedWeapon or spawn a second physics body.
            if (_lastDroppedWeapon != null)
            {
                DeactivateHolsteredWeaponVisuals();
                return;
            }

            GameObject weaponRoot = ResolveDrawnWeaponRoot();
            if (weaponRoot == null)
            {
                DeactivateHolsteredWeaponVisuals();
                return;
            }

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
            ItemData melee = ResolvedMelee;
            ItemData ranged = ResolvedRanged;
            bool startMelee;
            if (_startMeleeOverride >= 0)
                startMelee = _startMeleeOverride == 1;
            else if (ResolveDefinition() != null || preferRangedOverride != EnemyPreferRangedOverride.UseDefinition)
                startMelee = !(ResolvedPreferRanged && ranged != null);
            else
                startMelee = startWithMeleeWeapon;

            if (startMelee)
                return melee != null ? melee : ranged;

            return ranged != null ? ranged : melee;
        }

        public ItemData ResolvePreferredWeapon()
        {
            return ResolveStartingWeapon();
        }

        public ItemData ResolveWeaponForTargetDistance(float distance, float meleeRange)
        {
            ItemData melee = ResolvedMelee;
            ItemData ranged = ResolvedRanged;

            // One owner at a time: while the Brain holds a weapon, distance switching follows it.
            if (BrainHoldActive && (_brainHoldItem == melee || _brainHoldItem == ranged))
                return _brainHoldItem;

            if (ResolvedPreferRanged && ranged != null)
            {
                // Hysteresis: once on the gun, stay on it until clearly inside melee reach (no flip-flop at the edge).
                float threshold = _activeItem == ranged ? meleeRange * 0.9f : meleeRange * 1.15f;
                if (distance > threshold)
                    return ranged;
            }

            return melee != null ? melee : ranged;
        }

        // ---- Weapon visibility enforcement ---------------------------------------------------------------
        private void LateUpdate()
        {
            if (_lastDroppedWeapon != null || (_health != null && _health.IsDead))
                return;

            // Cheap reference check every frame: vMeleeManager must never point at a weapon this bridge did not equip.
            SyncMeleeBinding();

            if (Time.time < _nextEnforceTime)
                return;

            _nextEnforceTime = Time.time + 0.25f;
            EnforceWeaponVisibility();
        }

        /// <summary>
        /// vMeleeManager.Init (its Start, which runs AFTER our first equip) re-binds rightWeapon to the FIRST
        /// vMeleeWeapon under the right arm, i.e. the template's default weap2_sword slot, whatever the enemy's
        /// resolved weapon is. EnemyInvectorCombatShutdown.EnableMeleeIfSafe then SetActive(true)s that stale sword
        /// on every swing, so a second weapon appeared next to the real one (and hits used the wrong weapon).
        /// This re-asserts the resolved weapon (or none when ranged/unarmed) whenever the binding drifts.
        /// </summary>
        public void SyncMeleeBinding()
        {
            if (_meleeManager == null)
                return;

            vMeleeWeapon current = _meleeManager.rightWeapon;
            if (current == _boundMelee)
                return;

            if (_boundMelee != null && _activeItem != null && _activeItem.itemType == ItemType.MeleeWeapon &&
                _activeDrawnInstance != null)
            {
                EquipMelee(_activeDrawnInstance, _activeItem);
                return;
            }

            if (_boundMelee == null && current != null)
                _meleeManager.SetRightWeapon((GameObject)null);
        }

        /// <summary>
        /// Hides every weapon object that is not the currently drawn, resolved weapon: all other Drawn_ slots, every
        /// Holstered_ slot, and stray baked weapon meshes parented under holder transforms.
        /// </summary>
        public void EnforceWeaponVisibility()
        {
            if (_slotRoots == null)
                CacheWeaponObjects();

            for (int i = 0; i < _slotRoots.Length; i++)
            {
                Transform t = _slotRoots[i];
                if (t == null || !t.gameObject.activeSelf)
                    continue;

                if (_activeDrawnInstance != null && t.gameObject == _activeDrawnInstance)
                    continue;
                if (_holsterKeep != null && t.gameObject == _holsterKeep)
                    continue;

                t.gameObject.SetActive(false);
            }

            for (int i = 0; i < _strayRoots.Length; i++)
            {
                Transform t = _strayRoots[i];
                if (t != null && t.gameObject.activeSelf)
                    t.gameObject.SetActive(false);
            }

            EnforceActiveDrawnVisuals();
        }

        private void CacheWeaponObjects()
        {
            var slots = new System.Collections.Generic.List<Transform>();
            var stray = new System.Collections.Generic.List<Transform>();
            Transform[] all = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null || t == transform)
                    continue;

                if (t.name.StartsWith("Drawn_", StringComparison.Ordinal) ||
                    t.name.StartsWith("Holstered_", StringComparison.Ordinal))
                    slots.Add(t);
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || r is SkinnedMeshRenderer || IsUnderWeaponSlot(r.transform))
                    continue;

                Transform holderChild = FindHolderChild(r.transform);
                if (holderChild != null && !stray.Contains(holderChild))
                    stray.Add(holderChild);
            }

            _slotRoots = slots.ToArray();
            _strayRoots = stray.ToArray();
        }

        private static bool IsUnderWeaponSlot(Transform t)
        {
            while (t != null)
            {
                if (t.name.StartsWith("Drawn_", StringComparison.Ordinal) ||
                    t.name.StartsWith("Holstered_", StringComparison.Ordinal))
                    return true;
                t = t.parent;
            }

            return false;
        }

        private static Transform FindHolderChild(Transform t)
        {
            Transform c = t;
            while (c != null && c.parent != null)
            {
                string parentName = c.parent.name;
                if (parentName.EndsWith("Holder", StringComparison.Ordinal) ||
                    parentName.Equals("WeaponHolders", StringComparison.Ordinal))
                    return c;
                c = c.parent;
            }

            return null;
        }

        /// <summary>Inside the drawn slot only the PioneerVisual mesh may render (vendor GreatSword leftovers stay off).</summary>
        private void EnforceActiveDrawnVisuals()
        {
            if (_activeDrawnInstance == null)
                return;

            Transform pioneerVisual = null;
            Transform[] kids = _activeDrawnInstance.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < kids.Length; i++)
            {
                if (kids[i] != null && kids[i].name.StartsWith("PioneerVisual_", StringComparison.Ordinal))
                {
                    pioneerVisual = kids[i];
                    break;
                }
            }

            if (pioneerVisual == null)
                return;

            Renderer[] renderers = _activeDrawnInstance.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || !r.enabled)
                    continue;

                if (r.transform != pioneerVisual && !r.transform.IsChildOf(pioneerVisual))
                    r.enabled = false;
            }
        }

        private void EquipWeapon(ItemData weapon)
        {
            EquipWeaponCore(weapon);
            // Nothing but the resolved, drawn weapon may stay visible.
            EnforceWeaponVisibility();
        }

        private void EquipWeaponCore(ItemData weapon)
        {
            EnemyInvectorCombatShutdown.SanitizeStaleHitBoxes(gameObject);

            _activeItem = weapon;
            _holsterKeep = null;
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
            _activeDrawnInstance = drawn;

            if (weapon.itemType == ItemType.MeleeWeapon)
            {
                EquipMelee(drawn, weapon);
                PioneerInvectorWeaponBridge.ApplyItemStatsToInstance(weapon, drawn);
            }
            else if (weapon.IsRangedWeapon)
            {
                PioneerInvectorWeaponBridge.ApplyItemStatsToInstance(weapon, drawn);
                EquipRanged(drawn);
            }

            SyncAnimatorWeaponState(weapon);
            ShowSecondaryHolster(weapon);
        }

        /// <summary>Keeps the other resolved weapon visible on its holster (one drawn, the other holstered).</summary>
        private void ShowSecondaryHolster(ItemData drawnWeapon)
        {
            if (!showHolsteredSecondary || drawnWeapon == null)
                return;

            ItemData other = drawnWeapon == ResolvedMelee ? ResolvedRanged : ResolvedMelee;
            if (other == null || other == drawnWeapon)
                return;

            GameObject holster = PioneerInvectorWeaponBridge.FindPreloadedHolsteredSlot(transform, other);
            if (holster == null)
                return;

            holster.SetActive(true);
            _holsterKeep = holster;
        }

        private void DeactivateAllWeaponVisuals()
        {
            EnemyInvectorCombatShutdown.DisableMeleeBeforeAnimatorEvents(gameObject);

            vShooterWeapon[] ranged = GetComponentsInChildren<vShooterWeapon>(true);
            for (int i = 0; i < ranged.Length; i++)
            {
                if (ranged[i] != null)
                    ranged[i].gameObject.SetActive(false);
            }

            vMeleeWeapon[] melee = GetComponentsInChildren<vMeleeWeapon>(true);
            for (int i = 0; i < melee.Length; i++)
            {
                if (melee[i] == null)
                    continue;

                melee[i].enabled = false;

                // Hide unused Drawn_ roots only. Never deactivate a nested vMeleeWeapon —
                // GetAttackID / GetMoveSetID require activeInHierarchy, and drawn.SetActive(true)
                // will not re-enable a child that was turned off here.
                Transform drawnRoot = FindDrawnSlotRoot(melee[i].transform);
                if (drawnRoot != null)
                    drawnRoot.gameObject.SetActive(false);
            }
        }

        private void EquipMelee(GameObject drawn, ItemData item)
        {
            if (_meleeManager == null || drawn == null)
                return;

            vMeleeWeapon weapon = drawn.GetComponentInChildren<vMeleeWeapon>(true);
            if (weapon == null)
                weapon = drawn.AddComponent<vMeleeWeapon>();

            ActivateDrawnWeaponHierarchy(weapon.transform, drawn.transform);
            ApplyMeleeAnimatorIds(weapon, item);
            CollectAndEnableWeaponHitBoxes(weapon);
            weapon.enabled = true;
            EnemyInvectorCombatShutdown.SanitizeMeleeDamageEvents(weapon);
            _meleeManager.SetRightWeapon(weapon);
            _boundMelee = weapon;
        }

        private static Transform FindDrawnSlotRoot(Transform node)
        {
            Transform current = node;
            while (current != null)
            {
                if (current.name.StartsWith("Drawn_", StringComparison.Ordinal))
                    return current;
                current = current.parent;
            }

            return null;
        }

        private static void ActivateDrawnWeaponHierarchy(Transform weapon, Transform drawnRoot)
        {
            if (weapon == null)
                return;

            Transform current = weapon;
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                    current.gameObject.SetActive(true);

                if (drawnRoot != null && current == drawnRoot)
                    break;

                current = current.parent;
            }
        }

        private static void ApplyMeleeAnimatorIds(vMeleeWeapon weapon, ItemData item)
        {
            if (weapon == null)
                return;

            bool twoHanded = item != null && item.weaponGrip == WeaponGrip.TwoHanded;
            if (weapon.attackID <= 0)
                weapon.attackID = twoHanded ? 2 : 1;
            if (weapon.movesetID <= 0)
                weapon.movesetID = twoHanded ? 2 : 1;
        }

        private static void CollectAndEnableWeaponHitBoxes(vMeleeWeapon weapon)
        {
            if (weapon == null)
                return;

            EnsureMeleeDamageEvents(weapon);

            if (weapon.hitBoxes == null)
                weapon.hitBoxes = new System.Collections.Generic.List<vHitBox>();

            vHitBox[] boxes = weapon.GetComponentsInChildren<vHitBox>(true);
            for (int i = 0; i < boxes.Length; i++)
            {
                vHitBox box = boxes[i];
                if (box == null)
                    continue;

                box.enabled = true;
                box.attackObject = weapon;
                if (!weapon.hitBoxes.Contains(box))
                    weapon.hitBoxes.Add(box);
            }

            PioneerMeleeHitboxTuning.ApplyToWeapon(weapon, enemyWeapon: true);
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
            _boundMelee = null;
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

        private static void EnsureMeleeDamageEvents(vMeleeAttackObject attackObject)
        {
            EnemyInvectorCombatShutdown.SanitizeMeleeDamageEvents(attackObject);
        }
    }
}
