using System;
using System.Collections.Generic;
using Project.World.Clock;
using UnityEngine;

namespace Project.Vendor
{
    public static class DMVendorRuntime
    {
        private static readonly Dictionary<string, DMVendorRuntimeState> States =
            new Dictionary<string, DMVendorRuntimeState>(StringComparer.Ordinal);

        public static event Action StatesChanged;

        public static void NotifyChanged()
        {
            StatesChanged?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            States.Clear();
            StatesChanged = null;
            DMIoClock.OnDayRolled -= HandleDayRolled;
        }

        public static void EnsureHooked()
        {
            DMIoClock.OnDayRolled -= HandleDayRolled;
            DMIoClock.OnDayRolled += HandleDayRolled;
        }

        public static void ResetAll()
        {
            States.Clear();
            StatesChanged?.Invoke();
        }

        public static DMVendorRuntimeState GetOrCreate(DMVendorProfile profile)
        {
            EnsureHooked();
            if (profile == null || string.IsNullOrEmpty(profile.vendorId))
                return null;

            if (!States.TryGetValue(profile.vendorId, out DMVendorRuntimeState state) || state == null)
            {
                state = new DMVendorRuntimeState { VendorId = profile.vendorId };
                States[profile.vendorId] = state;
            }

            RestockIfNeeded(profile, state);
            return state;
        }

        public static void RestockIfNeeded(DMVendorProfile profile, DMVendorRuntimeState state)
        {
            if (profile == null || state == null)
                return;

            int day = DMIoClock.Day;
            int listingCount = profile.catalog != null && profile.catalog.listings != null
                ? profile.catalog.listings.Length
                : 0;

            if (state.LastRestockDay == day)
            {
                while (state.Stock.Count < listingCount)
                    state.Stock.Add(0);
                return;
            }

            state.LastRestockDay = day;
            int purseMin = Mathf.Min(profile.purseMin, profile.purseMax);
            int purseMax = Mathf.Max(profile.purseMin, profile.purseMax);
            state.CurrentAc = UnityEngine.Random.Range(purseMin, purseMax + 1);

            DMVendorListing[] listings = profile.catalog != null ? profile.catalog.listings : null;
            for (int i = 0; i < listingCount; i++)
            {
                DMVendorListing listing = listings[i];
                if (listing == null || listing.armorPlaceholder || listing.item == null || !listing.item.ResolveCanBuy())
                {
                    state.SetStock(i, 0);
                    continue;
                }

                int min = Mathf.Max(0, listing.stockMin);
                int max = Mathf.Max(min, listing.stockMax);
                state.SetStock(i, UnityEngine.Random.Range(min, max + 1));
            }

            StatesChanged?.Invoke();
        }

        public static void RestockAllOpenProfiles(IList<DMVendorProfile> profiles)
        {
            if (profiles == null)
                return;

            for (int i = 0; i < profiles.Count; i++)
            {
                DMVendorProfile profile = profiles[i];
                if (profile == null)
                    continue;
                DMVendorRuntimeState state = GetOrCreate(profile);
                if (state != null)
                    state.LastRestockDay = -1;
                RestockIfNeeded(profile, state);
            }
        }

        public static VendorRuntimeSave[] BuildSave()
        {
            if (States.Count == 0)
                return Array.Empty<VendorRuntimeSave>();

            var saves = new List<VendorRuntimeSave>(States.Count);
            foreach (KeyValuePair<string, DMVendorRuntimeState> pair in States)
            {
                DMVendorRuntimeState state = pair.Value;
                if (state == null)
                    continue;

                saves.Add(new VendorRuntimeSave
                {
                    vendorId = state.VendorId,
                    lastRestockDay = state.LastRestockDay,
                    currentAc = state.CurrentAc,
                    stock = state.Stock.ToArray(),
                    held = state.Held.Count > 0 ? state.Held.ToArray() : Array.Empty<VendorHeldStack>()
                });
            }

            return saves.ToArray();
        }

        public static void ApplySave(VendorRuntimeSave[] saves)
        {
            States.Clear();
            if (saves == null)
            {
                StatesChanged?.Invoke();
                return;
            }

            for (int i = 0; i < saves.Length; i++)
            {
                VendorRuntimeSave save = saves[i];
                if (save == null || string.IsNullOrEmpty(save.vendorId))
                    continue;

                var state = new DMVendorRuntimeState
                {
                    VendorId = save.vendorId,
                    LastRestockDay = save.lastRestockDay,
                    CurrentAc = Mathf.Max(0, save.currentAc)
                };
                if (save.stock != null)
                {
                    for (int s = 0; s < save.stock.Length; s++)
                        state.Stock.Add(Mathf.Max(0, save.stock[s]));
                }

                if (save.held != null)
                {
                    for (int h = 0; h < save.held.Length; h++)
                    {
                        VendorHeldStack held = save.held[h];
                        if (held == null || held.amount <= 0 || string.IsNullOrEmpty(held.itemId))
                            continue;
                        state.Held.Add(new VendorHeldStack
                        {
                            itemId = held.itemId,
                            amount = held.amount
                        });
                    }
                }

                state.ResolveHeldItems();
                States[save.vendorId] = state;
            }

            StatesChanged?.Invoke();
        }

        private static void HandleDayRolled()
        {
            IReadOnlyList<DMVendorNpc> npcs = DMVendorNpc.Active;
            for (int i = 0; i < npcs.Count; i++)
            {
                DMVendorNpc npc = npcs[i];
                if (npc == null || npc.Profile == null || string.IsNullOrEmpty(npc.Profile.vendorId))
                    continue;

                if (!States.TryGetValue(npc.Profile.vendorId, out DMVendorRuntimeState state) || state == null)
                {
                    state = new DMVendorRuntimeState { VendorId = npc.Profile.vendorId };
                    States[npc.Profile.vendorId] = state;
                }

                state.LastRestockDay = -1;
                RestockIfNeeded(npc.Profile, state);
            }
        }
    }
}
