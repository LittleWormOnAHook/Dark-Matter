using System.Collections.Generic;
using Project.Creatures;
using Project.Interaction;
using Project.Pet;
using UnityEngine;

namespace Project.Core
{
    /// <summary>
    /// Live register of gameplay actors that would otherwise require FindObjectsByType in hot paths.
    /// Components register in OnEnable and unregister in OnDisable.
    /// </summary>
    public static class GameplayActorRegistry
    {
        private static readonly List<PetController> Pets = new List<PetController>(16);
        private static readonly List<IExpeditionCompanionActor> Companions = new List<IExpeditionCompanionActor>(8);
        private static readonly List<DMICreatureBridge> Creatures = new List<DMICreatureBridge>(64);
        private static readonly List<ScannableTarget> Scannables = new List<ScannableTarget>(32);
        private static readonly List<ItemPickup> ItemPickups = new List<ItemPickup>(128);

        public static IReadOnlyList<PetController> ActivePets => Pets;
        public static IReadOnlyList<IExpeditionCompanionActor> ActiveCompanions => Companions;
        public static IReadOnlyList<DMICreatureBridge> ActiveCreatures => Creatures;
        public static IReadOnlyList<ScannableTarget> ActiveScannables => Scannables;
        public static IReadOnlyList<ItemPickup> ActiveItemPickups => ItemPickups;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            Pets.Clear();
            Companions.Clear();
            Creatures.Clear();
            Scannables.Clear();
            ItemPickups.Clear();
        }

        public static void Register(PetController pet)
        {
            if (pet == null || Pets.Contains(pet))
                return;
            Pets.Add(pet);
        }

        public static void Unregister(PetController pet)
        {
            if (pet == null)
                return;
            Pets.Remove(pet);
        }

        public static void Register(IExpeditionCompanionActor companion)
        {
            if (companion == null || Companions.Contains(companion))
                return;
            Companions.Add(companion);
        }

        public static void Unregister(IExpeditionCompanionActor companion)
        {
            if (companion == null)
                return;
            Companions.Remove(companion);
        }

        public static void Register(DMICreatureBridge creature)
        {
            if (creature == null || Creatures.Contains(creature))
                return;
            Creatures.Add(creature);
        }

        public static void Unregister(DMICreatureBridge creature)
        {
            if (creature == null)
                return;
            Creatures.Remove(creature);
        }

        public static void Register(ScannableTarget scannable)
        {
            if (scannable == null || Scannables.Contains(scannable))
                return;
            Scannables.Add(scannable);
        }

        public static void Unregister(ScannableTarget scannable)
        {
            if (scannable == null)
                return;
            Scannables.Remove(scannable);
        }

        public static void Register(ItemPickup pickup)
        {
            if (pickup == null || ItemPickups.Contains(pickup))
                return;
            ItemPickups.Add(pickup);
        }

        public static void Unregister(ItemPickup pickup)
        {
            if (pickup == null)
                return;
            ItemPickups.Remove(pickup);
        }
    }
}
