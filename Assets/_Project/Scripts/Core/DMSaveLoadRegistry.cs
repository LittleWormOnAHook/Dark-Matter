using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Project.Core
{
    [Serializable]
    public class DMSaveLoadRecord
    {
        public string id;
        public string kind;
        public int slotIndex = -1;
        public int playerLevel;
        public int playerXp;
        public int skillPoints;
        public string source;
        public string note;
        public bool pinned;
        public long utcTicks;
        public string summary;
    }

    [Serializable]
    public class DMSaveLoadRegistryData
    {
        public DMSaveLoadRecord[] records = System.Array.Empty<DMSaveLoadRecord>();
    }

    public static class DMSaveLoadRegistry
    {
        public const int MaxRecords = 80;
        private const string FileName = "save_load_registry.json";

        private static DMSaveLoadRegistryData cached;

        public static event Action Changed;

        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public static IReadOnlyList<DMSaveLoadRecord> Records
        {
            get
            {
                DMSaveLoadRecord[] records = EnsureLoaded().records;
                return records ?? System.Array.Empty<DMSaveLoadRecord>();
            }
        }

        public static void Record(
            string kind,
            int slotIndex,
            string source,
            string summary = null,
            int playerLevel = 0,
            int playerXp = 0,
            int skillPoints = 0)
        {
            if (string.IsNullOrEmpty(kind))
                return;

            List<DMSaveLoadRecord> list = ToList(EnsureLoaded());
            DMSaveLoadRecord record = new DMSaveLoadRecord
            {
                id = Guid.NewGuid().ToString("N"),
                kind = kind,
                slotIndex = slotIndex,
                playerLevel = playerLevel,
                playerXp = playerXp,
                skillPoints = skillPoints,
                source = source ?? string.Empty,
                note = string.Empty,
                pinned = false,
                utcTicks = DateTime.UtcNow.Ticks,
                summary = summary ?? string.Empty
            };

            list.Insert(0, record);
            TrimUnpinned(list);
            Save(FromList(list));
        }

        public static void RecordLive(string kind, int slotIndex, string source, string summary = null)
        {
            Project.Progression.PlayerProgressionManager progression =
                Project.Progression.PlayerProgressionManager.EnsureExists();
            Record(
                kind,
                slotIndex,
                source,
                summary,
                progression != null ? progression.Level : 0,
                progression != null ? progression.CurrentXp : 0,
                progression != null ? progression.UnspentSkillPoints : 0);
        }

        public static bool TrySetNote(string id, string note)
        {
            DMSaveLoadRecord record = Find(id);
            if (record == null)
                return false;

            record.note = note ?? string.Empty;
            Save(EnsureLoaded());
            return true;
        }

        public static bool TrySetPinned(string id, bool pinned)
        {
            DMSaveLoadRecord record = Find(id);
            if (record == null)
                return false;

            record.pinned = pinned;
            Save(EnsureLoaded());
            return true;
        }

        public static bool TryRemove(string id)
        {
            List<DMSaveLoadRecord> list = ToList(EnsureLoaded());
            int removed = list.RemoveAll(r => r != null && r.id == id);
            if (removed <= 0)
                return false;

            Save(FromList(list));
            return true;
        }

        public static void ClearUnpinned()
        {
            List<DMSaveLoadRecord> list = ToList(EnsureLoaded());
            list.RemoveAll(r => r == null || !r.pinned);
            Save(FromList(list));
        }

        public static string FormatLine(DMSaveLoadRecord record)
        {
            if (record == null)
                return string.Empty;

            DateTime when = new DateTime(record.utcTicks, DateTimeKind.Utc).ToLocalTime();
            string slot = record.slotIndex >= 0 ? $" slot {record.slotIndex + 1}" : string.Empty;
            string note = string.IsNullOrEmpty(record.note) ? string.Empty : $" — {record.note}";
            string pin = record.pinned ? "[PIN] " : string.Empty;
            return $"{pin}{when:g}  {record.kind}{slot}  Lv {record.playerLevel} XP {record.playerXp} SP {record.skillPoints}  ({record.source}){note}";
        }

        private static DMSaveLoadRecord Find(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            List<DMSaveLoadRecord> records = ToList(EnsureLoaded());
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i] != null && records[i].id == id)
                    return records[i];
            }

            return null;
        }

        private static List<DMSaveLoadRecord> ToList(DMSaveLoadRegistryData data)
        {
            List<DMSaveLoadRecord> list = new List<DMSaveLoadRecord>();
            if (data?.records == null)
                return list;

            for (int i = 0; i < data.records.Length; i++)
            {
                if (data.records[i] != null)
                    list.Add(data.records[i]);
            }

            return list;
        }

        private static DMSaveLoadRegistryData FromList(List<DMSaveLoadRecord> list)
        {
            return new DMSaveLoadRegistryData
            {
                records = list != null ? list.ToArray() : System.Array.Empty<DMSaveLoadRecord>()
            };
        }

        private static void TrimUnpinned(List<DMSaveLoadRecord> list)
        {
            if (list.Count <= MaxRecords)
                return;

            for (int i = list.Count - 1; i >= 0 && list.Count > MaxRecords; i--)
            {
                if (list[i] == null || !list[i].pinned)
                    list.RemoveAt(i);
            }
        }

        private static DMSaveLoadRegistryData EnsureLoaded()
        {
            if (cached != null)
                return cached;

            cached = new DMSaveLoadRegistryData();
            string path = FilePath;
            if (!File.Exists(path))
                return cached;

            try
            {
                DMSaveLoadRegistryData loaded = JsonUtility.FromJson<DMSaveLoadRegistryData>(File.ReadAllText(path));
                if (loaded != null && loaded.records != null)
                    cached = loaded;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("DMSaveLoadRegistry: failed to read. " + exception.Message);
            }

            if (cached.records == null)
                cached.records = System.Array.Empty<DMSaveLoadRecord>();

            return cached;
        }

        private static void Save(DMSaveLoadRegistryData data)
        {
            cached = data;
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(data, prettyPrint: true));
            }
            catch (Exception exception)
            {
                Debug.LogWarning("DMSaveLoadRegistry: failed to write. " + exception.Message);
            }

            Changed?.Invoke();
        }
    }
}
