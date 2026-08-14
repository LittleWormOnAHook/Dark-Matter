using System;
using System.Collections.Generic;
using Project.Map;
using Project.Quests;
using Project.Survival.Exposure;
using UnityEngine;

namespace Project.PPT
{
    public sealed class PptRegistryIndex
    {
        private readonly Dictionary<string, PptEntry> byId = new Dictionary<string, PptEntry>(StringComparer.Ordinal);
        private readonly Dictionary<string, PptNpcProfile> npcProfiles = new Dictionary<string, PptNpcProfile>(StringComparer.Ordinal);
        private readonly Dictionary<string, PptKeywordSource> keywordSources = new Dictionary<string, PptKeywordSource>(StringComparer.Ordinal);

        public void LoadRegistry(PptRegistry registry)
        {
            byId.Clear();
            npcProfiles.Clear();
            keywordSources.Clear();

            if (registry == null)
                return;

            if (registry.Entries != null)
            {
                for (int i = 0; i < registry.Entries.Length; i++)
                    RegisterEntry(registry.Entries[i]);
            }

            if (registry.NpcProfiles != null)
            {
                for (int i = 0; i < registry.NpcProfiles.Length; i++)
                    RegisterNpcProfile(registry.NpcProfiles[i]);
            }

            if (registry.KeywordSources != null)
            {
                for (int i = 0; i < registry.KeywordSources.Length; i++)
                {
                    PptKeywordSource source = registry.KeywordSources[i];
                    if (source != null)
                        keywordSources[source.name] = source;
                }
            }
        }

        public void RegisterEntry(PptEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.PptId))
                return;

            byId[entry.PptId] = entry;
        }

        public void RegisterNpcProfile(PptNpcProfile profile)
        {
            if (profile == null || string.IsNullOrWhiteSpace(profile.NpcId))
                return;

            npcProfiles[profile.NpcId] = profile;
        }

        public bool TryGetEntry(string pptId, out PptEntry entry)
        {
            return byId.TryGetValue(pptId, out entry);
        }

        public bool TryGetNpcProfile(string npcId, out PptNpcProfile profile)
        {
            return npcProfiles.TryGetValue(npcId, out profile);
        }

        public IEnumerable<PptEntry> AllEntries => byId.Values;

        public IEnumerable<PptKeywordSource> KeywordSources => keywordSources.Values;

        public void IndexSceneObjects()
        {
            MapMarker[] markers = UnityEngine.Object.FindObjectsByType<MapMarker>(FindObjectsInactive.Include);
            for (int i = 0; i < markers.Length; i++)
            {
                MapMarker marker = markers[i];
                if (marker == null)
                    continue;

                string id = "place_" + SanitizeId(marker.DiscoveryId);
                if (byId.ContainsKey(id))
                    continue;

                RegisterRuntimePlace(id, marker.Label, marker.DiscoveryId, marker.WorldPosition);
            }

            QuestGiverNpc[] givers = UnityEngine.Object.FindObjectsByType<QuestGiverNpc>(FindObjectsInactive.Include);
            for (int i = 0; i < givers.Length; i++)
            {
                QuestGiverNpc giver = givers[i];
                if (giver == null)
                    continue;

                string id = "person_" + SanitizeId(giver.NpcId);
                if (byId.ContainsKey(id))
                    continue;

                RegisterRuntimePerson(id, giver.NpcId, giver.transform.position);
            }
        }

        private void RegisterRuntimePlace(string pptId, string label, string discoveryId, Vector3 position)
        {
            // Scene-derived entries are tracked in a lightweight runtime map via manager.
            PptManager.Instance?.RegisterRuntimeEntry(pptId, label, PptType.Place, discoveryId, position);
        }

        private void RegisterRuntimePerson(string pptId, string npcId, Vector3 position)
        {
            PptManager.Instance?.RegisterRuntimeEntry(pptId, npcId, PptType.Person, string.Empty, position, npcId);
        }

        private static string SanitizeId(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "unknown";

            return raw.Trim().Replace(' ', '_').ToLowerInvariant();
        }
    }

    public sealed class PptZoneCatalog
    {
        private readonly List<PptSurfaceRegionAnchor> regionAnchors = new List<PptSurfaceRegionAnchor>();
        private readonly List<ExposureZoneVolume> exposureZones = new List<ExposureZoneVolume>();

        public void Refresh()
        {
            regionAnchors.Clear();
            exposureZones.Clear();

            regionAnchors.AddRange(UnityEngine.Object.FindObjectsByType<PptSurfaceRegionAnchor>(FindObjectsInactive.Include));
            exposureZones.AddRange(UnityEngine.Object.FindObjectsByType<ExposureZoneVolume>(FindObjectsInactive.Include));
        }

        public bool TryResolveAreaForEntry(PptEntry entry, out string areaName, out Vector3 areaCenter)
        {
            areaName = string.Empty;
            areaCenter = Vector3.zero;

            if (entry == null)
                return false;

            if (entry.SurfaceRegion != Project.Survival.World.IoSurfaceRegionId.None)
            {
                for (int i = 0; i < regionAnchors.Count; i++)
                {
                    PptSurfaceRegionAnchor anchor = regionAnchors[i];
                    if (anchor == null || anchor.SurfaceRegion != entry.SurfaceRegion)
                        continue;

                    areaName = anchor.DisplayName;
                    areaCenter = anchor.Center;
                    return true;
                }
            }

            if (!string.IsNullOrWhiteSpace(entry.ExposureZoneTag))
            {
                for (int i = 0; i < exposureZones.Count; i++)
                {
                    ExposureZoneVolume zone = exposureZones[i];
                    if (zone == null || zone.Profile == null)
                        continue;

                    string tag = zone.Profile.displayName;
                    if (!string.Equals(tag, entry.ExposureZoneTag, StringComparison.OrdinalIgnoreCase))
                        continue;

                    areaName = zone.Profile.displayName;
                    areaCenter = zone.transform.position;
                    return true;
                }
            }

            for (int i = 0; i < exposureZones.Count; i++)
            {
                ExposureZoneVolume zone = exposureZones[i];
                if (zone == null || zone.Profile == null)
                    continue;

                if (!string.Equals(zone.Profile.displayName, entry.DisplayName, StringComparison.OrdinalIgnoreCase))
                    continue;

                areaName = zone.Profile.displayName;
                areaCenter = zone.transform.position;
                return true;
            }

            return false;
        }

        public bool TryFindNearestRegionAnchor(Vector3 fromPosition, out PptSurfaceRegionAnchor anchor)
        {
            anchor = null;
            float best = float.MaxValue;

            for (int i = 0; i < regionAnchors.Count; i++)
            {
                PptSurfaceRegionAnchor candidate = regionAnchors[i];
                if (candidate == null)
                    continue;

                float dist = Vector3.SqrMagnitude(candidate.Center - fromPosition);
                if (dist >= best)
                    continue;

                best = dist;
                anchor = candidate;
            }

            return anchor != null;
        }
    }
}
