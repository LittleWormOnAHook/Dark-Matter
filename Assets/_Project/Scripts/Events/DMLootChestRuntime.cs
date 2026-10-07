using System.Collections.Generic;
using UnityEngine;

namespace Project.Events
{
    /// <summary>
    /// One ticker for every loot chest timer, hold and dissolve (loot plan 6.3 / 9.5 / 11), instead of an Update per chest.
    /// Chests join while a countdown, hold or dissolve is running and leave when idle. Ticks scaled game time, so a full
    /// pause (loot window, storage) freezes everything; chests also gate on their own phase (never tick while open).
    /// Keeps ticking chests whose tile is disabled, as long as the process lives.
    /// Also the save registry (loot plan 6.3 / 9): state keyed by chestId, unknown ids kept, New Game reset.
    /// </summary>
    public static class DMLootChestRuntime
    {
        private static readonly List<DMItemCollection> ticking = new List<DMItemCollection>(16);
        private static readonly List<DMItemCollection> known = new List<DMItemCollection>(32);
        private static readonly Dictionary<string, LootChestSave> saved = new Dictionary<string, LootChestSave>(32);
        private static int generation;
        private static Ticker ticker;

        /// <summary>Bumps on every load / New Game; a chest applies registry state once per generation.</summary>
        public static int Generation => generation;

        public static int TickingCount => ticking.Count;

        public static void Track(DMItemCollection chest)
        {
            if (chest == null || ticking.Contains(chest))
                return;

            ticking.Add(chest);
            EnsureTicker();
        }

        public static void Untrack(DMItemCollection chest)
        {
            ticking.Remove(chest);
        }

        /// <summary>Every chest that has woken up (active or hidden as Gone) joins so load / New Game can reach it.</summary>
        public static void Register(DMItemCollection chest)
        {
            if (chest != null && !known.Contains(chest))
                known.Add(chest);
        }

        public static void Unregister(DMItemCollection chest)
        {
            known.Remove(chest);
            ticking.Remove(chest);
        }

        /// <summary>Saved state for a chest id from the last load (null when none).</summary>
        public static LootChestSave GetSaved(string chestId)
        {
            if (string.IsNullOrWhiteSpace(chestId))
                return null;
            return saved.TryGetValue(chestId, out LootChestSave state) ? state : null;
        }

        /// <summary>v24 save: live chests overwrite their entry; unknown ids (tiles not loaded) are kept (9.1).</summary>
        public static LootChestSave[] BuildSave()
        {
            for (int i = known.Count - 1; i >= 0; i--)
            {
                DMItemCollection chest = known[i];
                if (chest == null)
                {
                    known.RemoveAt(i);
                    continue;
                }

                LootChestSave state = chest.CaptureSave();
                if (state != null)
                    saved[state.chestId] = state;
            }

            LootChestSave[] result = new LootChestSave[saved.Count];
            saved.Values.CopyTo(result, 0);
            return result;
        }

        /// <summary>Load: registry wins over authored loot; chests without an entry start fresh (9.2).</summary>
        public static void ApplySave(LootChestSave[] saves)
        {
            saved.Clear();
            if (saves != null)
            {
                for (int i = 0; i < saves.Length; i++)
                {
                    LootChestSave state = saves[i];
                    if (state != null && !string.IsNullOrWhiteSpace(state.chestId))
                        saved[state.chestId] = state;
                }
            }

            RestoreKnownChests();
        }

        /// <summary>New Game / pre-v24 save: forget all chest state and restore every chest to its authored loot (9.3).</summary>
        public static void ResetAll()
        {
            saved.Clear();
            RestoreKnownChests();
        }

        private static void RestoreKnownChests()
        {
            generation++;
            ticking.Clear();
            for (int i = known.Count - 1; i >= 0; i--)
            {
                DMItemCollection chest = known[i];
                if (chest == null)
                {
                    known.RemoveAt(i);
                    continue;
                }

                chest.ApplyRegistryState(GetSaved(chest.ChestId), generation);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ticking.Clear();
            known.Clear();
            saved.Clear();
            generation = 0;
            ticker = null;
        }

        private static void EnsureTicker()
        {
            if (ticker != null || !Application.isPlaying)
                return;

            GameObject host = new GameObject("DM_LootChestRuntime");
            host.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
            Object.DontDestroyOnLoad(host);
            ticker = host.AddComponent<Ticker>();
        }

        private static void TickAll(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            for (int i = ticking.Count - 1; i >= 0; i--)
            {
                if (i >= ticking.Count)
                    continue;

                DMItemCollection chest = ticking[i];
                if (chest == null)
                {
                    ticking.RemoveAt(i);
                    continue;
                }

                if (!chest.TickRuntime(deltaTime))
                    ticking.Remove(chest);
            }
        }

        [AddComponentMenu("")]
        private sealed class Ticker : MonoBehaviour
        {
            private void Update()
            {
                TickAll(Time.deltaTime);
            }

            private void OnDestroy()
            {
                if (ticker == this)
                    ticker = null;
            }
        }
    }
}
