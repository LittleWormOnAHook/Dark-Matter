using System.Collections.Generic;
using Project.Pioneers;
using UnityEngine;

namespace Project.Companions
{
    public enum CompanionAttackMode
    {
        Sequential,
        Paired,
        Staggered
    }

    /// <summary>
    /// Seed-driven attack scheduling: turn order, paired bursts, and interval scaling by active trio size.
    /// </summary>
    public class CompanionCombatCoordinator : MonoBehaviour
    {
        public static CompanionCombatCoordinator Instance { get; private set; }

        [SerializeField] private float baseAttackInterval = 1.1f;
        [SerializeField] private float pairedOverlapWindow = 0.35f;
        [SerializeField] private int attackModeSeed = 92831;

        private readonly List<CompanionCombatController> registered = new List<CompanionCombatController>(4);
        private readonly HashSet<CompanionCombatController> activeAttackers = new HashSet<CompanionCombatController>();
        private CompanionAttackMode attackMode = CompanionAttackMode.Sequential;
        private int turnIndex;
        private float pairedWindowEnds;
        private int activeEngagements;
        private bool combatEngaged;

        public float BaseAttackInterval => baseAttackInterval;
        public bool IsCombatEngaged => combatEngaged;
        public CompanionAttackMode AttackMode => attackMode;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            attackMode = (CompanionAttackMode)(Mathf.Abs(attackModeSeed) % 3);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static CompanionCombatCoordinator EnsureExists(MonoBehaviour host)
        {
            if (Instance != null)
                return Instance;

            if (host != null)
                return host.gameObject.AddComponent<CompanionCombatCoordinator>();

            GameObject go = new GameObject("CompanionCombatCoordinator");
            Instance = go.AddComponent<CompanionCombatCoordinator>();
            return Instance;
        }

        public void Register(CompanionCombatController controller)
        {
            if (controller == null || registered.Contains(controller))
                return;

            registered.Add(controller);
            RecomputeAttackMode();
        }

        public void Unregister(CompanionCombatController controller)
        {
            if (controller == null)
                return;

            registered.Remove(controller);
            activeAttackers.Remove(controller);
            turnIndex = registered.Count == 0 ? 0 : turnIndex % registered.Count;
            RecomputeAttackMode();
        }

        public void NotifyEngagementChanged(bool engaged)
        {
            activeEngagements += engaged ? 1 : -1;
            activeEngagements = Mathf.Max(0, activeEngagements);

            bool next = activeEngagements > 0;
            if (combatEngaged == next)
                return;

            combatEngaged = next;
            if (!combatEngaged)
            {
                activeAttackers.Clear();
                pairedWindowEnds = 0f;
            }
        }

        public float GetScaledAttackInterval(CompanionCombatController requester)
        {
            int count = Mathf.Max(1, registered.Count);
            float personal = requester != null ? requester.GetPersonalIntervalMultiplier() : 1f;
            float interval = baseAttackInterval * count * personal;
            if (requester != null && requester.PioneerClass == SkilledPioneerClass.CombatTactician)
                interval *= 0.5f;

            return interval;
        }

        public float RollAttackChance(CompanionCombatController requester)
        {
            if (requester == null)
                return 0.5f;

            if (requester.PioneerClass == SkilledPioneerClass.CombatTactician)
                return 1f;

            int count = Mathf.Max(1, registered.Count);
            float soloBias = requester.GetPersonalAttackBias();
            float countScale = 1f / count;
            return Mathf.Clamp01(soloBias * countScale + 0.12f);
        }

        public bool TryBeginAttack(CompanionCombatController requester, bool forceAggressive = false)
        {
            if (requester == null || registered.Count == 0)
                return false;

            if (forceAggressive && requester.PioneerClass == SkilledPioneerClass.CombatTactician)
            {
                if (activeAttackers.Contains(requester))
                    return false;

                activeAttackers.Add(requester);
                return true;
            }

            int maxSimultaneous = attackMode switch
            {
                CompanionAttackMode.Paired => 2,
                CompanionAttackMode.Staggered => Time.time < pairedWindowEnds && activeAttackers.Count < 2 ? 2 : 1,
                _ => 1
            };

            if (activeAttackers.Count >= maxSimultaneous)
                return false;

            if (attackMode == CompanionAttackMode.Paired)
            {
                if (activeAttackers.Count == 0)
                {
                    activeAttackers.Add(requester);
                    pairedWindowEnds = Time.time + pairedOverlapWindow;
                    turnIndex = (turnIndex + 1) % registered.Count;
                    return true;
                }

                if (Time.time <= pairedWindowEnds && activeAttackers.Count < 2)
                {
                    activeAttackers.Add(requester);
                    return true;
                }

                return false;
            }

            if (attackMode == CompanionAttackMode.Staggered)
            {
                if (activeAttackers.Count == 0)
                {
                    activeAttackers.Add(requester);
                    if (ShouldStaggerDoubleBurst())
                        pairedWindowEnds = Time.time + pairedOverlapWindow * 0.85f;
                    turnIndex = (turnIndex + 1) % registered.Count;
                    return true;
                }

                if (Time.time <= pairedWindowEnds && activeAttackers.Count < 2 && ShouldStaggerDoubleBurst())
                {
                    activeAttackers.Add(requester);
                    return true;
                }

                return false;
            }

            CompanionCombatController current = registered[turnIndex % registered.Count];
            if (current != requester)
                return false;

            activeAttackers.Add(requester);
            turnIndex = (turnIndex + 1) % registered.Count;
            return true;
        }

        public void EndAttack(CompanionCombatController requester)
        {
            if (requester == null)
                return;

            activeAttackers.Remove(requester);
        }

        public bool IsAttacking(CompanionCombatController requester)
        {
            return requester != null && activeAttackers.Contains(requester);
        }

        private bool ShouldStaggerDoubleBurst()
        {
            return (attackModeSeed & 3) != 0;
        }

        private void RecomputeAttackMode()
        {
            int seed = attackModeSeed;
            for (int i = 0; i < registered.Count; i++)
            {
                if (registered[i] == null)
                    continue;

                seed ^= registered[i].PioneerSeed.GetHashCode();
            }

            attackMode = (CompanionAttackMode)(Mathf.Abs(seed) % 3);
        }
    }
}
