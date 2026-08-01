using System.Collections.Generic;
using Project.Companions;
using Project.Pet;
using Project.Survival;
using UnityEngine;

namespace Project.Creatures
{
    /// <summary>
    /// Resolves combat targets for Malbers brains: player, owned pets, expedition companions,
    /// and other DMI creatures — excluding allies that share this creature's id (e.g. Sulfur Hound packs).
    /// </summary>
    public static class DMICreatureTargetResolver
    {
        private static readonly List<Transform> CandidateBuffer = new List<Transform>(32);

        public static bool TryResolveThreat(
            DMICreatureBridge self,
            float senseRange,
            out Transform threat,
            out float distanceSqr)
        {
            threat = null;
            distanceSqr = float.MaxValue;

            if (self == null)
                return false;

            Vector3 origin = self.transform.position;
            float rangeSqr = senseRange > 0f ? senseRange * senseRange : float.MaxValue;
            string allyId = self.Definition != null ? self.Definition.creatureId : null;

            CandidateBuffer.Clear();
            CollectPlayer(CandidateBuffer);
            CollectPets(CandidateBuffer);
            CollectCompanions(CandidateBuffer);
            CollectOtherCreatures(CandidateBuffer, self, allyId);

            Transform best = null;
            float bestDist = float.MaxValue;

            for (int i = 0; i < CandidateBuffer.Count; i++)
            {
                Transform candidate = CandidateBuffer[i];
                if (candidate == null)
                    continue;

                float dist = (candidate.position - origin).sqrMagnitude;
                if (dist > rangeSqr || dist >= bestDist)
                    continue;

                best = candidate;
                bestDist = dist;
            }

            if (best == null)
                return false;

            threat = best;
            distanceSqr = bestDist;
            return true;
        }

        public static bool IsAllyCreature(DMICreatureBridge self, Transform other)
        {
            if (self == null || other == null)
                return false;

            DMICreatureBridge otherBridge = other.GetComponentInParent<DMICreatureBridge>();
            if (otherBridge == null || otherBridge == self)
                return otherBridge == self;

            string selfId = self.Definition != null ? self.Definition.creatureId : null;
            string otherId = otherBridge.Definition != null ? otherBridge.Definition.creatureId : null;
            if (string.IsNullOrEmpty(selfId) || string.IsNullOrEmpty(otherId))
                return false;

            return selfId == otherId;
        }

        public static bool IsValidSpitOrMeleeTarget(DMICreatureBridge self, Transform other)
        {
            if (self == null || other == null)
                return false;

            if (other.IsChildOf(self.transform) || self.transform.IsChildOf(other))
                return false;

            if (IsAllyCreature(self, other))
                return false;

            if (other.GetComponentInParent<SurvivalStats>() != null)
                return true;

            if (other.GetComponentInParent<CompanionHealth>() != null)
                return true;

            if (other.GetComponentInParent<PetController>() != null)
                return true;

            DMICreatureBridge otherCreature = other.GetComponentInParent<DMICreatureBridge>();
            return otherCreature != null && otherCreature != self;
        }

        private static void CollectPlayer(List<Transform> buffer)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                buffer.Add(player.transform);
        }

        private static void CollectPets(List<Transform> buffer)
        {
            PetController[] pets = Object.FindObjectsByType<PetController>(FindObjectsInactive.Exclude);
            for (int i = 0; i < pets.Length; i++)
            {
                PetController pet = pets[i];
                if (pet == null || !pet.IsOwned || !pet.CompanionActive)
                    continue;

                buffer.Add(pet.transform);
            }
        }

        private static void CollectCompanions(List<Transform> buffer)
        {
            CompanionHealth[] companions = Object.FindObjectsByType<CompanionHealth>(FindObjectsInactive.Exclude);
            for (int i = 0; i < companions.Length; i++)
            {
                CompanionHealth companion = companions[i];
                if (companion == null || companion.IsDead)
                    continue;

                buffer.Add(companion.transform);
            }
        }

        private static void CollectOtherCreatures(List<Transform> buffer, DMICreatureBridge self, string allyId)
        {
            DMICreatureBridge[] creatures = Object.FindObjectsByType<DMICreatureBridge>(FindObjectsInactive.Exclude);
            for (int i = 0; i < creatures.Length; i++)
            {
                DMICreatureBridge creature = creatures[i];
                if (creature == null || creature == self)
                    continue;

                if (creature.Health != null && creature.Health.IsDead)
                    continue;

                if (!string.IsNullOrEmpty(allyId) &&
                    creature.Definition != null &&
                    creature.Definition.creatureId == allyId)
                    continue;

                buffer.Add(creature.transform);
            }
        }
    }
}
