using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Pet
{
    public class PetManager : MonoBehaviour
    {
        public static PetManager Instance { get; private set; }

        private readonly List<PetController> _pets = new List<PetController>();

        public IReadOnlyList<PetController> Pets => _pets;
        public event Action OnPetsChanged;

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
            foreach (PetController pet in found)
                Register(pet);
        }

        public void Register(PetController pet)
        {
            if (pet == null || _pets.Contains(pet))
                return;

            _pets.Add(pet);
            OnPetsChanged?.Invoke();
        }

        public void Unregister(PetController pet)
        {
            if (pet == null || !_pets.Remove(pet))
                return;

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
