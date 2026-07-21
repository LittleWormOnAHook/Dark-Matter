using UnityEngine;

namespace Project.Pet
{
    public static class PetCatalog
    {
        private const string DefinitionsResourcePath = "Pets/Definitions";

        public static PetDefinition Resolve(string petId)
        {
            if (string.IsNullOrWhiteSpace(petId))
                return null;

            PetDefinition[] definitions = Resources.LoadAll<PetDefinition>(DefinitionsResourcePath);
            for (int i = 0; i < definitions.Length; i++)
            {
                PetDefinition definition = definitions[i];
                if (definition != null && definition.petId == petId)
                    return definition;
            }

            return null;
        }

        public static PetDefinition[] GetAll()
        {
            return Resources.LoadAll<PetDefinition>(DefinitionsResourcePath);
        }
    }
}
