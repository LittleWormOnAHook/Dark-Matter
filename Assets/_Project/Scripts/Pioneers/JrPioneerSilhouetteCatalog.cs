using System;
using UnityEngine;

namespace Project.Pioneers
{
    /// <summary>
    /// Twelve unnamed Jr. silhouette ID badges — identical thin gold ring, unique black silhouettes.
    /// Four expedition core classes × 3 variants. UI titles like "Jr. Tactician_01" (UI-only).
    /// </summary>
    public static class JrPioneerSilhouetteCatalog
    {
        public const int Count = 12;

        public readonly struct Entry
        {
            public readonly string id;
            public readonly SkilledPioneerClass pioneerClass;
            public readonly string uiTitle;
            public readonly string resourceName;

            public Entry(string id, SkilledPioneerClass pioneerClass, string uiTitle)
            {
                this.id = id;
                this.pioneerClass = pioneerClass;
                this.uiTitle = uiTitle;
                // File name matches id (e.g. Jr_Tactician_01.png under Resources/Portraits).
                resourceName = id;
            }
        }

        private static readonly Entry[] entries =
        {
            new Entry("Jr_Engineer_01", SkilledPioneerClass.ArchitectEngineer, "Jr. Engineer_01"),
            new Entry("Jr_Engineer_02", SkilledPioneerClass.ArchitectEngineer, "Jr. Engineer_02"),
            new Entry("Jr_Engineer_03", SkilledPioneerClass.ArchitectEngineer, "Jr. Engineer_03"),
            new Entry("Jr_Science_Specialist_01", SkilledPioneerClass.ScienceSpecialist, "Jr. Science Specialist_01"),
            new Entry("Jr_Science_Specialist_02", SkilledPioneerClass.ScienceSpecialist, "Jr. Science Specialist_02"),
            new Entry("Jr_Science_Specialist_03", SkilledPioneerClass.ScienceSpecialist, "Jr. Science Specialist_03"),
            new Entry("Jr_Tactician_01", SkilledPioneerClass.CombatTactician, "Jr. Tactician_01"),
            new Entry("Jr_Tactician_02", SkilledPioneerClass.CombatTactician, "Jr. Tactician_02"),
            new Entry("Jr_Tactician_03", SkilledPioneerClass.CombatTactician, "Jr. Tactician_03"),
            new Entry("Jr_Scout_01", SkilledPioneerClass.InfiltratorScout, "Jr. Scout_01"),
            new Entry("Jr_Scout_02", SkilledPioneerClass.InfiltratorScout, "Jr. Scout_02"),
            new Entry("Jr_Scout_03", SkilledPioneerClass.InfiltratorScout, "Jr. Scout_03"),
        };

        public static Entry[] Entries => entries;

        public static bool TryGet(string silhouetteId, out Entry entry)
        {
            entry = default;
            if (string.IsNullOrWhiteSpace(silhouetteId))
                return false;

            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].id == silhouetteId || entries[i].resourceName == silhouetteId)
                {
                    entry = entries[i];
                    return true;
                }
            }

            return false;
        }

        public static string GetUiTitle(string silhouetteId)
        {
            return TryGet(silhouetteId, out Entry entry) ? entry.uiTitle : string.Empty;
        }

        /// <summary>Map any skilled class onto the four Jr. silhouette families.</summary>
        public static SkilledPioneerClass ResolveJrFamily(SkilledPioneerClass pioneerClass)
        {
            return pioneerClass switch
            {
                SkilledPioneerClass.ArchitectEngineer => SkilledPioneerClass.ArchitectEngineer,
                SkilledPioneerClass.SalvageEngineer => SkilledPioneerClass.ArchitectEngineer,
                SkilledPioneerClass.LogisticsOfficer => SkilledPioneerClass.ArchitectEngineer,
                SkilledPioneerClass.ScienceSpecialist => SkilledPioneerClass.ScienceSpecialist,
                SkilledPioneerClass.MedTech => SkilledPioneerClass.ScienceSpecialist,
                SkilledPioneerClass.IoHybrid => SkilledPioneerClass.ScienceSpecialist,
                SkilledPioneerClass.CombatTactician => SkilledPioneerClass.CombatTactician,
                SkilledPioneerClass.InfiltratorScout => SkilledPioneerClass.InfiltratorScout,
                SkilledPioneerClass.CommunicationsOfficer => SkilledPioneerClass.InfiltratorScout,
                _ => SkilledPioneerClass.CombatTactician
            };
        }

        public static string PickForClass(SkilledPioneerClass pioneerClass)
        {
            SkilledPioneerClass family = ResolveJrFamily(pioneerClass);
            int first = -1;
            int count = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].pioneerClass != family)
                    continue;
                if (first < 0)
                    first = i;
                count++;
            }

            if (count <= 0)
                return entries[0].id;

            int offset = UnityEngine.Random.Range(0, count);
            return entries[first + offset].id;
        }

        public static string PickStableForClass(SkilledPioneerClass pioneerClass, string pioneerId)
        {
            SkilledPioneerClass family = ResolveJrFamily(pioneerClass);
            int first = -1;
            int count = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].pioneerClass != family)
                    continue;
                if (first < 0)
                    first = i;
                count++;
            }

            if (count <= 0)
                return entries[0].id;

            int hash = string.IsNullOrEmpty(pioneerId) ? 0 : pioneerId.GetHashCode();
            int offset = Math.Abs(hash) % count;
            return entries[first + offset].id;
        }

        public static string ResourcePath(string silhouetteId)
        {
            if (TryGet(silhouetteId, out Entry entry))
                return PioneerPortraitResolver.ResourcesFolder + "/" + entry.resourceName;
            return PioneerPortraitResolver.ResourcesFolder + "/" + silhouetteId;
        }
    }
}
