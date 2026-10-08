using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Invector.vMelee
{
    using Project.Combat;
    using vEventSystems;
    [vClassHeader("Melee Object", openClose = false)]
    public partial class vMeleeAttackObject : vMonoBehaviour
    {
        [vReadOnly(false)] public string attackObjectName;
        public vDamage damage;
        public Transform overrideDamageSender;
        public List<vHitBox> hitBoxes;
        public int damageModifier;
        [HideInInspector]
        public bool canApplyDamage;
        /// <summary>
        /// Event called when attack was successful
        /// </summary>
        [vHelpBox("Event called when attack was successful")]
        public OnHitEnter onDamageHit;
        /// <summary>
        /// Event called when the attack causes recoil
        /// </summary>
        [vHelpBox("Event called when the attack causes recoil")]
        public OnHitEnter onRecoilHit;
        /// <summary>
        /// Event called when causes damage 
        /// </summary>
        [vHelpBox("Event called when causes damage ")]
        public OnReceiveDamage onPassDamage;
        [vHelpBox("Events called when  Damage applier (HitBoxes) is enabled or disabled ")]
        public UnityEvent onEnableDamage;
        public UnityEvent onDisableDamage;
        private Dictionary<vHitBox, List<GameObject>> targetColliders;
        [HideInInspector]
        public vMeleeManager meleeManager;

        protected virtual void Start()
        {
            // init list of targetColliders
            targetColliders = new Dictionary<vHitBox, List<GameObject>>();

            if (hitBoxes.Count > 0)
            {
                // initialize hitBox properties
                foreach (vHitBox hitBox in hitBoxes)
                {
                    hitBox.attackObject = this;
                    targetColliders.Add(hitBox, new List<GameObject>());
                }
            }
            else
            {
                this.enabled = false;
            }
        }

        /// <summary>
        /// Set Active all hitBoxes of the MeleeAttackObject
        /// </summary>
        /// <param name="value"> active value</param>  
        public virtual void SetActiveDamage(bool value)
        {
            try
            {
                SetActiveDamageInternal(value);
            }
            catch (System.ArgumentOutOfRangeException)
            {
                canApplyDamage = value;
            }
        }

        private void SetActiveDamageInternal(bool value)
        {
            canApplyDamage = value;
            if (hitBoxes == null)
                return;

            if (targetColliders == null)
            {
                targetColliders = new Dictionary<vHitBox, List<GameObject>>();
                for (int i = 0; i < hitBoxes.Count; i++)
                {
                    vHitBox hitBox = hitBoxes[i];
                    if (hitBox != null && !targetColliders.ContainsKey(hitBox))
                        targetColliders.Add(hitBox, new List<GameObject>());
                }
            }

            for (int i = hitBoxes.Count - 1; i >= 0; i--)
            {
                var hitCollider = hitBoxes[i];
                if (hitCollider == null)
                {
                    hitBoxes.RemoveAt(i);
                    continue;
                }

                Collider trigger = hitCollider.trigger;
                if (trigger == null)
                    continue;

                trigger.enabled = value;
                if (value == false && targetColliders != null && targetColliders.ContainsKey(hitCollider))
                {
                    targetColliders[hitCollider].Clear();
                }
            }
            if (value)
                InvokeDamageEvent(onEnableDamage);
            else
                InvokeDamageEvent(onDisableDamage);
        }

        /// <summary>
        /// Enemy armature strips leave serialized UnityEvent listeners pointing at destroyed objects;
        /// Invoke() then throws ArgumentOutOfRangeException inside Unity's persistent callback list.
        /// </summary>
        private static void InvokeDamageEvent(UnityEvent evt)
        {
            if (evt == null)
                return;

            try
            {
                evt.Invoke();
            }
            catch (System.ArgumentOutOfRangeException)
            {
                // Ignore broken persistent targets — combat hitboxes do not rely on these events.
            }
        }

        /// <summary>
        /// Hitboxes Call Back
        /// </summary>
        /// <param name="hitBox">vHitBox object</param>
        /// <param name="other">target Collider</param>
        public virtual void OnHit(vHitBox hitBox, Collider other)
        {
            if (hitBox == null || other == null)
                return;

            if (targetColliders == null)
                targetColliders = new Dictionary<vHitBox, List<GameObject>>();
            if (!targetColliders.ContainsKey(hitBox))
                targetColliders.Add(hitBox, new List<GameObject>());

            if (meleeManager == null)
                meleeManager = GetComponentInParent<vMeleeManager>();

            // Pioneer patch: ignore triggers, self/child colliders, and non-combat overlaps.
            if (!canApplyDamage
                || other == null
                || PioneerMeleeOutgoingHitFilter.ShouldIgnoreTriggerForMelee(other)
                || targetColliders[hitBox].Contains(other.gameObject)
                || meleeManager == null
                || PioneerMeleeOutgoingHitFilter.IsExcludedAttackerCollider(meleeManager, other))
            {
                return;
            }

            HitProperties _hitProperties = meleeManager.hitProperties;

            bool inDamage = false;
            bool inRecoil = false;

            if ((hitBox.triggerType & vHitBoxType.Damage) != 0)
            {
                if (_hitProperties.hitDamageTags == null || _hitProperties.hitDamageTags.Count == 0)
                    inDamage = true;
                else if (MatchesDamageTag(other, _hitProperties.hitDamageTags))
                    inDamage = true;

                if (inDamage && PioneerMeleeOutgoingHitFilter.IsAttackerPlayerSide(meleeManager))
                    inDamage = PioneerMeleeOutgoingHitFilter.IsValidOutgoingDamageTarget(other, _hitProperties);
            }

            if (!inDamage
                && (hitBox.triggerType & vHitBoxType.Recoil) != 0
                && (_hitProperties.hitRecoilLayer == (_hitProperties.hitRecoilLayer | (1 << other.gameObject.layer))))
            {
                inRecoil = !PioneerMeleeOutgoingHitFilter.ShouldSuppressWorldRecoil(meleeManager, other);
            }

            if (inDamage || inRecoil)
                {
                    // add target collider in the list to control the frequency of hit
                    targetColliders[hitBox].Add(other.gameObject);
                    vHitInfo hitInfo = new vHitInfo(this, hitBox, other, hitBox.transform.position);

                    if (inDamage == true)
                    {
                        // If there is a meleeManager then call onDamageHit to control damage values
                        // and it will call the ApplyDamage after filter the damage
                        // if meleeManager is null the damage will be directly applied
                        // Finally the OnDamageHit event is called
                        if (meleeManager)
                        {
                            meleeManager.OnDamageHit(ref hitInfo);
                        }
                        else
                        {
                            damage.sender = overrideDamageSender ? overrideDamageSender : transform;
                            // ApplyDamage(hitBox, other, damage);
                        }

                        if (!hitInfo.targetIsBlocking)
                        {
                            onDamageHit.Invoke(hitInfo);
                        }
                    }

                    // recoil just work with OnRecoilHit event and meleeManger
                    if (inRecoil == true)
                    {
                        if (meleeManager)
                        {
                            meleeManager.OnRecoilHit(hitInfo);
                        }

                        onRecoilHit.Invoke(hitInfo);
                    }
                }
        }

        /// <summary>
        /// Apply damage to target collider (TakeDamage, damage))
        /// </summary>
        /// <param name="hitBox">vHitBox object</param>
        /// <param name="other">collider target</param>
        /// <param name="damage"> damage</param>
        public virtual bool ApplyDamage(vHitBox hitBox, Collider other, vDamage damage)
        {
            if (hitBox == null || other == null || damage == null)
                return false;

            vDamage _damage = new vDamage(damage);
            _damage.receiver = other.transform;
            _damage.damageValue = Mathf.RoundToInt(((damage.damageValue + damageModifier) * (hitBox.damagePercentage * 0.01f)));
            _damage.hitPosition = hitBox.transform.position;
            vIMeleeFighter attacker = meleeManager != null ? meleeManager.fighter : null;
            ResolveDamageReceiver(other).ApplyDamage(_damage, attacker);
            if (_damage.hitReaction && _damage.damageValue > 0 && onPassDamage != null)
            {
                onPassDamage.Invoke(_damage);
            }

            return _damage.hitReaction;
        }

        /// <summary>
        /// Hit colliders on a dummy or enemy are often a child mesh. Invector only
        /// queries receivers on the hit GameObject, so walk up to the root receiver.
        /// </summary>
        private static GameObject ResolveDamageReceiver(Collider other)
        {
            GameObject hitObject = other.gameObject;
            if (hitObject.GetComponent<vIAttackReceiver>() != null || hitObject.GetComponent<vIDamageReceiver>() != null)
                return hitObject;

            vIDamageReceiver parentReceiver = other.GetComponentInParent<vIDamageReceiver>();
            return parentReceiver != null ? parentReceiver.gameObject : hitObject;
        }

        private static bool MatchesDamageTag(Collider other, List<string> tags)
        {
            if (other == null || tags == null || tags.Count == 0)
                return false;

            Transform node = other.transform;
            while (node != null)
            {
                if (tags.Contains(node.tag))
                    return true;
                node = node.parent;
            }

            return false;
        }
    }
}

namespace Invector.vMelee
{
    #region Secundary Class
    [System.Serializable]
    public class OnHitEnter : UnityEvent<vHitInfo> { }

    public partial class vHitInfo
    {
        public vMeleeAttackObject attackObject;
        public vHitBox hitBox;
        public Vector3 hitPoint;
        public Collider targetCollider;
        public bool targetIsBlocking;
        public vHitInfo(vMeleeAttackObject attackObject, vHitBox hitBox, Collider targetCollider, Vector3 hitPoint)
        {
            this.attackObject = attackObject;
            this.hitBox = hitBox;
            this.targetCollider = targetCollider;
            this.hitPoint = hitPoint;
        }
    }

    [System.Serializable]
    public class HitProperties
    {
        [Tooltip("Tag to receive Damage")]
        public List<string> hitDamageTags = new List<string>() { "Enemy" };
        [Tooltip("Trigger a HitRecoil animation if the character attacks a obstacle")]
        public bool useRecoil = true;
        public bool drawRecoilGizmos;
        [Range(0, 180f)]
        public float recoilRange = 90f;
        [Tooltip("layer to Recoil Damage")]
        public LayerMask hitRecoilLayer = 1 << 0;
    }
    #endregion
}