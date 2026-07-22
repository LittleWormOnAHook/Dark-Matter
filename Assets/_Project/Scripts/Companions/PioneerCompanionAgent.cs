using Project.Companions.Abilities;
using Project.Companions.Invector;
using Project.Companions;
using Project.Companions.Abilities;
using Project.Pioneers;
using UnityEngine;

namespace Project.Companions
{
    /// <summary>
    /// Runtime expedition companion hosting follow, combat, sense, and task state.
    /// </summary>
    public class PioneerCompanionAgent : MonoBehaviour
    {
        private CompanionFollowController followController;
        private CompanionAnimationDriver animationDriver;
        private CompanionCombatController combatController;
        private CompanionSenseController senseController;
        private CompanionEquipmentVisual equipmentVisual;
        private CompanionTaskQueue taskQueue;

        private string pioneerRecordId;
        private string displayName;
        private SkilledPioneerClass pioneerClass;

        public string PioneerRecordId => pioneerRecordId;
        public string DisplayName => displayName;
        public SkilledPioneerClass PioneerClass => pioneerClass;

        /// <summary>
        /// The live roster record this agent was spawned from — its data-asset buffs and spec stats
        /// (radiationResistance/expeditionEfficiency/combatSynergy) are read from here at runtime by
        /// CompanionExposureResponder and CompanionGroupBuffService so a companion's authored data
        /// file actually affects hazard response and squad buffs, not just the roster UI.
        /// </summary>
        public SkilledPioneerRecord BoundRecord { get; private set; }
        public CompanionTaskQueue TaskQueue => taskQueue;
        public PioneerFollowMode FollowMode => followController != null
            ? followController.FollowMode
            : PioneerFollowMode.FollowPlayer;

        private void Awake()
        {
            followController = GetComponent<CompanionFollowController>();
            if (followController == null)
                followController = gameObject.AddComponent<CompanionFollowController>();

            animationDriver = GetComponent<CompanionAnimationDriver>();
            if (animationDriver == null)
                animationDriver = gameObject.AddComponent<CompanionAnimationDriver>();

            combatController = GetComponent<CompanionCombatController>();
            if (combatController == null)
                combatController = gameObject.AddComponent<CompanionCombatController>();

            if (GetComponent<CompanionThreatSensor>() == null)
                gameObject.AddComponent<CompanionThreatSensor>();

            if (GetComponent<CompanionHealth>() == null)
                gameObject.AddComponent<CompanionHealth>();

            if (GetComponent<CompanionInjuryHandler>() == null)
                gameObject.AddComponent<CompanionInjuryHandler>();

            senseController = GetComponent<CompanionSenseController>();
            if (senseController == null)
                senseController = gameObject.AddComponent<CompanionSenseController>();

            if (GetComponent<CompanionInvectorBootstrap>() == null)
            {
                equipmentVisual = GetComponent<CompanionEquipmentVisual>();
                if (equipmentVisual == null)
                    equipmentVisual = gameObject.AddComponent<CompanionEquipmentVisual>();
            }

            taskQueue = new CompanionTaskQueue();

            if (CompanionInvectorBootstrap.HasInvectorStack(this) && animationDriver != null)
                animationDriver.enabled = false;
        }

        public void BindRecord(SkilledPioneerRecord record, Transform owner, int formationSlot)
        {
            if (record == null)
                return;

            CompanionModelSanitizer.StripPlayerComponents(gameObject);
            if (!CompanionInvectorBootstrap.HasInvectorStack(this))
            {
                Debug.LogError(
                    $"[{name}] PioneerCompanionAgent requires CompanionInvectorBootstrap. " +
                    "Use PioneerCompanion_Invector prefab.");
                return;
            }

            EnsureCompanionInvectorSetup(record);

            pioneerRecordId = record.id;
            displayName = record.displayName;
            pioneerClass = record.pioneerClass;
            BoundRecord = record;
            gameObject.name = $"Companion_{displayName}";

            PioneerLoadoutDefaults.EnsureDefaults(record);
            PioneerBehaviorProfile profile = PioneerBehaviorDefaults.ResolveForRecord(record);
            profile.followMode = record.ResolvedFollowMode;
            ApplyBuffMoveSpeed(profile, record);

            followController.Initialize(owner, taskQueue, formationSlot, record.id);
            followController.ApplyBehaviorProfile(profile, record.pioneerClass);
            followController.SetBehaviorMode(CompanionFollowBehaviorMode.Follow);
            animationDriver.ApplyBehaviorProfile(profile);
            combatController.Initialize(record.id);
            combatController.ApplyBehaviorProfile(profile, record.pioneerClass);
            senseController.Initialize(pioneerClass);
            taskQueue.SetFollow();

            ApplyLoadout(record);

            PioneerCompanionVisualProfile visualProfile = GetComponent<PioneerCompanionVisualProfile>();
            if (visualProfile == null)
                visualProfile = gameObject.AddComponent<PioneerCompanionVisualProfile>();
            visualProfile.Apply(record);

            CompanionHealth health = GetComponent<CompanionHealth>();
            health?.Initialize(record.id);

            CompanionInvectorIncomingDamageBridge incomingDamage =
                GetComponent<CompanionInvectorIncomingDamageBridge>();
            if (incomingDamage != null && health != null)
                incomingDamage.BindHealth(health);

            CompanionInjuryHandler injuryHandler = GetComponent<CompanionInjuryHandler>();
            injuryHandler?.Bind(record.id);
        }

        public void RefreshLoadout(SkilledPioneerRecord record)
        {
            if (record == null || record.id != pioneerRecordId)
                return;

            BoundRecord = record;
            ApplyLoadout(record);
        }

        /// <summary>
        /// Data-asset buffs (CompanionBuffModifier.moveSpeedBonus) are a per-companion personal
        /// perk — folded directly into this agent's own follow speed rather than the shared group
        /// buff aggregate (which only covers hazard mitigation + combat synergy).
        /// </summary>
        private static void ApplyBuffMoveSpeed(PioneerBehaviorProfile profile, SkilledPioneerRecord record)
        {
            if (profile == null || record?.buffs == null)
                return;

            float bonus = 0f;
            for (int i = 0; i < record.buffs.Length; i++)
            {
                if (record.buffs[i] != null)
                    bonus += record.buffs[i].moveSpeedBonus;
            }

            if (bonus == 0f)
                return;

            profile.walkSpeed += bonus;
            profile.runSpeed += bonus;
            profile.catchUpSpeed += bonus;
        }

        private void ApplyLoadout(SkilledPioneerRecord record)
        {
            CompanionInvectorLoadoutBridge invectorLoadout = GetComponent<CompanionInvectorLoadoutBridge>();
            if (invectorLoadout != null)
            {
                invectorLoadout.ApplyLoadout(record, false);
                combatController.RefreshLoadoutWeapon(record.weaponItemId);
                return;
            }

            equipmentVisual?.ApplyWeapon(record.weaponItemId, false);
            combatController.RefreshLoadoutWeapon(record.weaponItemId);
        }

        private void EnsureCompanionInvectorSetup(SkilledPioneerRecord record)
        {
            if (animationDriver != null)
                animationDriver.enabled = false;

            CompanionInvectorBootstrap bootstrap = GetComponent<CompanionInvectorBootstrap>();
            bootstrap?.EnsureInvectorPhysicsReady();

            CompanionAbilityController abilityController = GetComponent<CompanionAbilityController>();
            if (abilityController == null)
                abilityController = gameObject.AddComponent<CompanionAbilityController>();
            abilityController.Bind(record);

            if (record.pioneerClass == SkilledPioneerClass.MedTech)
            {
                MedTechCompanionAbilityController medTechController = GetComponent<MedTechCompanionAbilityController>();
                if (medTechController == null)
                    medTechController = gameObject.AddComponent<MedTechCompanionAbilityController>();
                medTechController.Bind(this, record);
            }
        }

        public void SetCommand(CompanionCommand command)
        {
            taskQueue?.SetCommand(command);
        }

        public void SetFollowMode(PioneerFollowMode mode)
        {
            followController?.SetFollowMode(mode);
            followController?.SetBehaviorMode(CompanionFollowBehaviorMode.Follow);
            taskQueue?.SetFollow();
        }

        public void SetBehaviorMode(CompanionFollowBehaviorMode mode)
        {
            followController?.SetBehaviorMode(mode);
            if (mode == CompanionFollowBehaviorMode.Follow)
                taskQueue?.SetFollow();
        }

        public void SetHold(Vector3 worldPosition, float facingYaw)
        {
            taskQueue?.SetHold(worldPosition, facingYaw);
        }

        public void ReleaseHold()
        {
            taskQueue?.SetFollow();
        }
    }
}
