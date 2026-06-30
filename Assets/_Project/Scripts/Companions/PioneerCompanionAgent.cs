using Project.Pioneers;
using UnityEngine;

namespace Project.Companions
{
    /// <summary>
    /// Runtime expedition companion hosting follow, animation, combat, sense, and task state.
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

            equipmentVisual = GetComponent<CompanionEquipmentVisual>();
            if (equipmentVisual == null)
                equipmentVisual = gameObject.AddComponent<CompanionEquipmentVisual>();

            taskQueue = new CompanionTaskQueue();
        }

        public void BindRecord(SkilledPioneerRecord record, Transform owner, int formationSlot)
        {
            if (record == null)
                return;

            CompanionModelSanitizer.StripPlayerComponents(gameObject);
            EnsurePioneerAnimator();

            pioneerRecordId = record.id;
            displayName = record.displayName;
            pioneerClass = record.pioneerClass;
            gameObject.name = $"Companion_{displayName}";

            PioneerLoadoutDefaults.EnsureDefaults(record);
            PioneerBehaviorProfile profile = PioneerBehaviorDefaults.ResolveForRecord(record);
            profile.followMode = record.ResolvedFollowMode;

            followController.Initialize(owner, taskQueue, formationSlot, record.id);
            followController.ApplyBehaviorProfile(profile, record.pioneerClass);
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

            CompanionInjuryHandler injuryHandler = GetComponent<CompanionInjuryHandler>();
            injuryHandler?.Bind(record.id);
        }

        public void RefreshLoadout(SkilledPioneerRecord record)
        {
            if (record == null || record.id != pioneerRecordId)
                return;

            ApplyLoadout(record);
        }

        private void ApplyLoadout(SkilledPioneerRecord record)
        {
            bool drawn = CompanionCombatCoordinator.Instance != null && CompanionCombatCoordinator.Instance.IsCombatEngaged;
            equipmentVisual.ApplyWeapon(record.weaponItemId, drawn);
            combatController.RefreshLoadoutWeapon(record.weaponItemId);
        }

        private void EnsurePioneerAnimator()
        {
            Animator animator = GetComponentInChildren<Animator>(true);
            if (animator == null)
                return;

            RuntimeAnimatorController pioneerController = PioneerCompanionDefaults.LoadPioneerAnimatorController();
            if (pioneerController == null)
                return;

            if (animator.runtimeAnimatorController != pioneerController)
            {
                animator.runtimeAnimatorController = pioneerController;
                animator.applyRootMotion = false;
            }
        }

        public void SetCommand(CompanionCommand command)
        {
            taskQueue?.SetCommand(command);
        }

        public void SetFollowMode(PioneerFollowMode mode)
        {
            followController?.SetFollowMode(mode);
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
