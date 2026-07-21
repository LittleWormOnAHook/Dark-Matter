using System;
using System.Collections.Generic;
using Project.Pioneers;
using UnityEngine;

namespace Project.Companions
{
    /// <summary>
    /// Aggregates the active expedition trio's data-asset buffs (CompanionBuffModifier, authored per
    /// companion in their NamedPioneerDefinition .asset under Assets/_Project/Data/Companions) plus
    /// their base spec stats into one shared "group buff" snapshot.
    ///
    /// This is what makes a companion's data file actually do something in the game world: hazard
    /// mitigation and combat synergy apply to the whole active squad (player + every companion),
    /// not just the companion whose asset the buff came from — recomputed any time
    /// CompanionRosterBridge's active trio changes (spawn, despawn, or roster refresh).
    /// </summary>
    public static class CompanionGroupBuffService
    {
        public struct GroupBuffSnapshot
        {
            /// <summary>0-1 fraction shaved off incoming radiation/thermal/sulfur/volcano pressure for
            /// the whole squad, sourced from the trio's average radiationResistance spec plus the
            /// strongest active debuffResistance buff.</summary>
            public float HazardMitigation01;

            /// <summary>Additive multiplier bonus applied to companion combat damage, sourced from the
            /// trio's combined combatSynergyBonus buffs.</summary>
            public float CombatSynergyBonus;

            /// <summary>Additive multiplier bonus for gathering/crafting-style tasks, sourced from the
            /// trio's combined expeditionEfficiencyBonus buffs. Exposed for task/crafting systems to
            /// read as they come online.</summary>
            public float ExpeditionEfficiencyBonus;

            public static GroupBuffSnapshot Empty => new GroupBuffSnapshot();
        }

        private const float MaxHazardMitigation01 = 0.6f;
        private const float MaxCombatSynergyBonus = 0.5f;
        private const float MaxExpeditionEfficiencyBonus = 0.5f;

        public static GroupBuffSnapshot Current { get; private set; } = GroupBuffSnapshot.Empty;

        /// <summary>Raised whenever the aggregate snapshot is recomputed — HUD/UI can subscribe
        /// instead of polling.</summary>
        public static event Action Changed;

        public static void Recompute(IReadOnlyList<PioneerCompanionAgent> activeCompanions)
        {
            float radiationResistanceSum = 0f;
            int recordCount = 0;
            float maxDebuffResistance = 0f;
            float combatSynergySum = 0f;
            float expeditionEfficiencySum = 0f;

            if (activeCompanions != null)
            {
                for (int i = 0; i < activeCompanions.Count; i++)
                {
                    PioneerCompanionAgent agent = activeCompanions[i];
                    SkilledPioneerRecord record = agent != null ? agent.BoundRecord : null;
                    if (record == null)
                        continue;

                    recordCount++;
                    radiationResistanceSum += Mathf.Clamp01(record.radiationResistance);

                    if (record.buffs == null)
                        continue;

                    for (int b = 0; b < record.buffs.Length; b++)
                    {
                        CompanionBuffModifier buff = record.buffs[b];
                        if (buff == null)
                            continue;

                        maxDebuffResistance = Mathf.Max(maxDebuffResistance, buff.debuffResistance);
                        combatSynergySum += buff.combatSynergyBonus;
                        expeditionEfficiencySum += buff.expeditionEfficiencyBonus;
                    }
                }
            }

            float radiationResistanceAvg = recordCount > 0 ? radiationResistanceSum / recordCount : 0f;

            Current = new GroupBuffSnapshot
            {
                HazardMitigation01 = Mathf.Clamp(radiationResistanceAvg * 0.25f + maxDebuffResistance, 0f, MaxHazardMitigation01),
                CombatSynergyBonus = Mathf.Clamp(combatSynergySum, 0f, MaxCombatSynergyBonus),
                ExpeditionEfficiencyBonus = Mathf.Clamp(expeditionEfficiencySum, 0f, MaxExpeditionEfficiencyBonus)
            };

            Changed?.Invoke();
        }

        public static void Clear()
        {
            Current = GroupBuffSnapshot.Empty;
            Changed?.Invoke();
        }
    }
}
