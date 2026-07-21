using System;
using System.Collections.Generic;
using Project.Core;
using UnityEngine;

namespace Project.Pet
{
    public class PetManager : MonoBehaviour
    {
        public const int MaxOwnedPets = 10;

        public static PetManager Instance { get; private set; }

        private readonly List<PetController> _pets = new List<PetController>();
        private readonly HashSet<string> ownedPetIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> tamingProgressByInstanceId =
            new Dictionary<string, float>(StringComparer.Ordinal);
        private PetController toolbarPet;

        public IReadOnlyList<PetController> Pets => _pets;
        public PetController ToolbarPet => toolbarPet;
        public event Action OnPetsChanged;
        public event Action<PetController, bool> OnPetAdopted;

        public static PetManager EnsureExists(MonoBehaviour host = null)
        {
            if (Instance != null)
                return Instance;

            if (host != null)
            {
                PetManager existing = host.GetComponent<PetManager>();
                if (existing != null)
                {
                    Instance = existing;
                    return existing;
                }

                Instance = host.gameObject.AddComponent<PetManager>();
                return Instance;
            }

            GameObject go = new GameObject("PetManager");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<PetManager>();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            PetController[] found = FindObjectsByType<PetController>();
            for (int i = 0; i < found.Length; i++)
                Register(found[i], notify: false);

            ApplyToolbarVisibility();
            OnPetsChanged?.Invoke();
        }

        public IReadOnlyList<PetController> GetOwnedPets()
        {
            List<PetController> owned = new List<PetController>(_pets.Count);
            for (int i = 0; i < _pets.Count; i++)
            {
                PetController pet = _pets[i];
                if (pet != null && IsOwned(pet))
                    owned.Add(pet);
            }

            return owned;
        }

        public void Register(PetController pet, bool notify = true)
        {
            if (pet == null || _pets.Contains(pet))
                return;

            _pets.Add(pet);
            if (notify)
                OnPetsChanged?.Invoke();
        }

        public void Unregister(PetController pet)
        {
            if (pet == null || !_pets.Remove(pet))
                return;

            if (toolbarPet == pet)
                toolbarPet = null;

            OnPetsChanged?.Invoke();
        }

        public bool IsOwned(PetController pet)
        {
            return pet != null && ownedPetIds.Contains(pet.PetId);
        }

        public bool TryAdoptPet(PetController pet, out string message, bool wasTamed = false)
        {
            message = string.Empty;
            if (pet == null)
            {
                message = "Invalid pet.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(pet.PetId))
            {
                message = "Pet is missing an id.";
                return false;
            }

            if (ownedPetIds.Contains(pet.PetId))
            {
                message = $"{pet.DisplayName} is already on your pet list.";
                return false;
            }

            if (ownedPetIds.Count >= MaxOwnedPets)
            {
                message = $"Pet journal is full ({MaxOwnedPets}/{MaxOwnedPets}).";
                return false;
            }

            ownedPetIds.Add(pet.PetId);
            pet.ApplyDefinition();
            pet.SetOwned(true);

            PetWorldAdoptable adoptable = pet.GetComponent<PetWorldAdoptable>();
            if (adoptable != null)
                adoptable.enabled = false;

            ApplyToolbarVisibility();
            OnPetsChanged?.Invoke();
            OnPetAdopted?.Invoke(pet, wasTamed);
            message = $"{pet.DisplayName} joined your pet list.";
            return true;
        }

        public bool TryAssignToolbarPet(PetController pet)
        {
            if (pet == null || !IsOwned(pet))
                return false;

            toolbarPet = pet;
            Transform player = ResolvePlayerTransform();
            if (player != null)
                pet.BindOwner(player);

            ApplyToolbarVisibility();
            pet.SummonToOwner();
            OnPetsChanged?.Invoke();
            return true;
        }

        public void ClearToolbarPet()
        {
            toolbarPet = null;
            ApplyToolbarVisibility();
            OnPetsChanged?.Invoke();
        }

        public void ApplyToolbarVisibility()
        {
            Transform player = ResolvePlayerTransform();

            for (int i = 0; i < _pets.Count; i++)
            {
                PetController pet = _pets[i];
                if (pet == null)
                    continue;

                if (!IsOwned(pet))
                {
                    pet.gameObject.SetActive(true);
                    pet.CompanionActive = true;
                    pet.FollowEnabled = false;
                    continue;
                }

                bool onToolbar = pet == toolbarPet;
                if (!onToolbar)
                {
                    pet.CompanionActive = false;
                    pet.FollowEnabled = false;
                    pet.gameObject.SetActive(false);
                    continue;
                }

                if (player != null)
                    pet.BindOwner(player);

                pet.gameObject.SetActive(true);
                pet.CompanionActive = true;
                pet.FollowEnabled = true;
            }
        }

        private static Transform ResolvePlayerTransform()
        {
            GameObject player = PlayerLocator.FindPlayerObject();
            return player != null ? player.transform : null;
        }

        public float GetTamingProgress(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
                return 0f;

            return tamingProgressByInstanceId.TryGetValue(instanceId, out float progress) ? progress : 0f;
        }

        public void SetTamingProgress(string instanceId, float progress)
        {
            if (string.IsNullOrEmpty(instanceId))
                return;

            tamingProgressByInstanceId[instanceId] = Mathf.Clamp01(progress);
        }

        public void ClearTamingProgress(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
                return;

            tamingProgressByInstanceId.Remove(instanceId);
        }

        public PetTamingProgressSaveEntry[] BuildTamingSave()
        {
            PetTamingProgressSaveEntry[] entries = new PetTamingProgressSaveEntry[tamingProgressByInstanceId.Count];
            int index = 0;
            foreach (KeyValuePair<string, float> pair in tamingProgressByInstanceId)
            {
                entries[index++] = new PetTamingProgressSaveEntry
                {
                    petInstanceId = pair.Key,
                    progress = pair.Value
                };
            }

            return entries;
        }

        public void ApplyTamingSave(PetTamingProgressSaveEntry[] savedEntries)
        {
            tamingProgressByInstanceId.Clear();
            if (savedEntries == null)
                return;

            for (int i = 0; i < savedEntries.Length; i++)
            {
                PetTamingProgressSaveEntry entry = savedEntries[i];
                if (entry == null || string.IsNullOrEmpty(entry.petInstanceId))
                    continue;

                tamingProgressByInstanceId[entry.petInstanceId] = Mathf.Clamp01(entry.progress);
            }
        }

        public string[] BuildOwnedPetSave()
        {
            string[] ids = new string[ownedPetIds.Count];
            ownedPetIds.CopyTo(ids);
            return ids;
        }

        public string BuildToolbarPetSave()
        {
            return toolbarPet != null ? toolbarPet.PetId : string.Empty;
        }

        public void ApplySave(string[] savedOwnedPetIds, string savedToolbarPetId)
        {
            ownedPetIds.Clear();
            toolbarPet = null;

            if (savedOwnedPetIds != null)
            {
                for (int i = 0; i < savedOwnedPetIds.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(savedOwnedPetIds[i]))
                        ownedPetIds.Add(savedOwnedPetIds[i]);
                }
            }

            for (int i = 0; i < _pets.Count; i++)
            {
                PetController pet = _pets[i];
                if (pet == null)
                    continue;

                bool owned = ownedPetIds.Contains(pet.PetId);
                pet.SetOwned(owned);
                if (owned)
                {
                    PetWorldAdoptable adoptable = pet.GetComponent<PetWorldAdoptable>();
                    if (adoptable != null)
                        adoptable.enabled = false;
                }
            }

            if (!string.IsNullOrWhiteSpace(savedToolbarPetId))
            {
                for (int i = 0; i < _pets.Count; i++)
                {
                    PetController pet = _pets[i];
                    if (pet != null && pet.PetId == savedToolbarPetId && IsOwned(pet))
                    {
                        toolbarPet = pet;
                        break;
                    }
                }
            }

            ApplyToolbarVisibility();
            OnPetsChanged?.Invoke();
        }

        public void NotifyPetChanged()
        {
            OnPetsChanged?.Invoke();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
