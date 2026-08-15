using System.Collections.Generic;
using Project.Map;
using Project.Quests;
using UnityEngine;

namespace Project.PPT
{
    public sealed class PptDirectionResolver
    {
        private readonly PptRegistryIndex registry;
        private readonly PptZoneCatalog zoneCatalog;

        public PptDirectionResolver(PptRegistryIndex registry, PptZoneCatalog zoneCatalog)
        {
            this.registry = registry;
            this.zoneCatalog = zoneCatalog;
        }

        public PptDirectionResult Resolve(PptNpcProfile profile, PptEntry target, Vector3 npcPosition)
        {
            PptDirectionPhraseSet phrases = profile != null ? profile.PhraseSet : null;
            if (phrases == null)
                phrases = ScriptableObject.CreateInstance<PptDirectionPhraseSet>();

            if (profile != null && Random.value < profile.UnknownChance)
                return PptDirectionResult.Unknown(phrases.PickUnknownBark());

            if (target == null || !PptDiscoveryGate.IsAvailableToPlayer(target))
                return PptDirectionResult.Unknown(phrases.PickUnknownBark());

            if (!TryResolveWorldPosition(target, out Vector3 targetPosition))
            {
                if (zoneCatalog.TryResolveAreaForEntry(target, out string areaName, out Vector3 areaCenter))
                {
                    string phrase = phrases.PickGeneralAreaPhrase(areaName);
                    float bearing = ComputeBearing(npcPosition, areaCenter);
                    return new PptDirectionResult(
                        PptDirectionKind.GeneralArea,
                        phrase,
                        areaCenter,
                        bearing,
                        string.Empty,
                        string.Empty,
                        false);
                }

                return PptDirectionResult.Unknown(phrases.PickUnknownBark());
            }

            bool questRelated = IsQuestRelated(target);
            bool wantsPrecise = profile == null || Random.value <= profile.PreciseDirectionChance;

            if (questRelated && profile != null && profile.PreferReferToOtherNpc && Random.value > profile.PreciseDirectionChance)
            {
                if (TryBuildReferNpcResult(profile, phrases, out PptDirectionResult referResult))
                    return referResult;

                zoneCatalog.TryResolveAreaForEntry(target, out string areaName, out Vector3 areaCenter);
                if (!string.IsNullOrEmpty(areaName))
                {
                    float bearing = ComputeBearing(npcPosition, areaCenter);
                    return new PptDirectionResult(
                        PptDirectionKind.GeneralArea,
                        phrases.PickGeneralAreaPhrase(areaName),
                        areaCenter,
                        bearing,
                        string.Empty,
                        string.Empty,
                        false);
                }
            }

            float targetBearing = ComputeBearing(npcPosition, targetPosition);
            string cardinal = phrases.PickCardinalPhrase(targetBearing);

            if (!wantsPrecise)
            {
                if (zoneCatalog.TryResolveAreaForEntry(target, out string areaName, out Vector3 areaCenter))
                {
                    return new PptDirectionResult(
                        PptDirectionKind.GeneralArea,
                        phrases.PickGeneralAreaPhrase(areaName),
                        areaCenter,
                        targetBearing,
                        string.Empty,
                        string.Empty,
                        false);
                }
            }

            string precisePhrase = cardinal;
            if (zoneCatalog.TryFindNearestRegionAnchor(npcPosition, out PptSurfaceRegionAnchor routeAnchor))
            {
                float routeBearing = ComputeBearing(routeAnchor.Center, targetPosition);
                precisePhrase = $"{cardinal}, past {routeAnchor.DisplayName}";
            }

            return new PptDirectionResult(
                PptDirectionKind.Precise,
                precisePhrase,
                targetPosition,
                targetBearing,
                string.Empty,
                string.Empty,
                true);
        }

        private bool TryBuildReferNpcResult(PptNpcProfile profile, PptDirectionPhraseSet phrases, out PptDirectionResult result)
        {
            result = PptDirectionResult.Unknown(phrases.PickUnknownBark());
            if (profile.ReferNpcIds == null || profile.ReferNpcIds.Length == 0)
                return false;

            string referId = profile.ReferNpcIds[Random.Range(0, profile.ReferNpcIds.Length)];
            if (string.IsNullOrWhiteSpace(referId))
                return false;

            string displayName = referId;
            Vector3 referPosition = Vector3.zero;
            QuestGiverNpc[] givers = Object.FindObjectsByType<QuestGiverNpc>(FindObjectsInactive.Include);
            for (int i = 0; i < givers.Length; i++)
            {
                QuestGiverNpc giver = givers[i];
                if (giver == null || !string.Equals(giver.NpcId, referId, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                displayName = giver.NpcId;
                referPosition = giver.transform.position;
                break;
            }

            result = new PptDirectionResult(
                PptDirectionKind.ReferNpc,
                phrases.PickReferNpcPhrase(displayName),
                referPosition,
                0f,
                referId,
                displayName,
                false);

            return true;
        }

        private static bool IsQuestRelated(PptEntry entry)
        {
            QuestManager quests = QuestManager.Instance;
            if (quests == null)
                return false;

            IReadOnlyList<QuestProgress> all = quests.GetAllProgress();
            for (int i = 0; i < all.Count; i++)
            {
                QuestProgress progress = all[i];
                if (progress == null || progress.status != QuestStatus.Active)
                    continue;

                QuestDefinition def = quests.GetDefinition(progress.questId);
                if (def?.objectives == null)
                    continue;

                for (int o = 0; o < def.objectives.Count; o++)
                {
                    QuestObjectiveDefinition objective = def.objectives[o];
                    if (objective == null)
                        continue;

                    if (string.Equals(objective.targetId, entry.QuestObjectiveTargetId, System.StringComparison.OrdinalIgnoreCase)
                        || string.Equals(objective.targetId, entry.NpcId, System.StringComparison.OrdinalIgnoreCase)
                        || string.Equals(objective.targetId, entry.QuestLocationId, System.StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        public static bool TryResolveWorldPosition(PptEntry entry, out Vector3 position)
        {
            position = Vector3.zero;
            if (entry == null)
                return false;

            if (entry.HasAuthoredWorldPosition)
            {
                position = entry.AuthoredWorldPosition;
                return true;
            }

            if (!string.IsNullOrWhiteSpace(entry.MapMarkerDiscoveryId))
            {
                MapMarker[] markers = Object.FindObjectsByType<MapMarker>(FindObjectsInactive.Include);
                for (int i = 0; i < markers.Length; i++)
                {
                    MapMarker marker = markers[i];
                    if (marker == null)
                        continue;

                    if (!string.Equals(marker.DiscoveryId, entry.MapMarkerDiscoveryId, System.StringComparison.Ordinal))
                        continue;

                    position = marker.WorldPosition;
                    return true;
                }
            }

            if (!string.IsNullOrWhiteSpace(entry.NpcId))
            {
                QuestGiverNpc[] givers = Object.FindObjectsByType<QuestGiverNpc>(FindObjectsInactive.Include);
                for (int i = 0; i < givers.Length; i++)
                {
                    QuestGiverNpc giver = givers[i];
                    if (giver == null)
                        continue;

                    if (!string.Equals(giver.NpcId, entry.NpcId, System.StringComparison.OrdinalIgnoreCase))
                        continue;

                    position = giver.transform.position;
                    return true;
                }
            }

            if (PptManager.Instance != null && PptManager.Instance.TryGetRuntimePosition(entry.PptId, out position))
                return true;

            return false;
        }

        public static float ComputeBearing(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.001f)
                return 0f;

            return Mathf.Repeat(Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg, 360f);
        }
    }
}
