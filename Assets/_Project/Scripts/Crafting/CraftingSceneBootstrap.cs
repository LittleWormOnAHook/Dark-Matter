using System.Collections;
using System.Collections.Generic;
using Project.Core;
using UnityEngine;

namespace Project.Crafting
{
    /// <summary>
    /// Wires Cooking, Workbench, and recipe pickups when they exist in the active scene.
    /// Avoids requiring the editor menu before play-testing.
    /// </summary>
    public static class CraftingSceneBootstrap
    {
        private static bool wiredCurrentScene;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoWireAfterSceneLoad()
        {
            if (!SceneHasCraftingObjects())
                return;

            ScheduleWireForActiveScene();
        }

        public static void EnsureWired()
        {
            if (wiredCurrentScene)
                return;

            WireStation("Cooking", CraftingStationType.Cooking);
            WireStation("Workbench", CraftingStationType.Workbench);
            WireRecipePickups("Recipe Book");
            EnsurePlayerCraftingManager();
            wiredCurrentScene = true;
        }

        private static void ScheduleWireForActiveScene()
        {
            wiredCurrentScene = false;

            CraftingSceneBootstrapRunner existing = Object.FindAnyObjectByType<CraftingSceneBootstrapRunner>();
            if (existing != null)
                return;

            GameObject runner = new GameObject(nameof(CraftingSceneBootstrapRunner));
            runner.AddComponent<CraftingSceneBootstrapRunner>();
        }

        private static bool SceneHasCraftingObjects()
        {
            return FindSceneObject("Cooking") != null
                || FindSceneObject("Workbench") != null
                || FindSceneObject("Recipe Book") != null;
        }

        private static GameObject FindSceneObject(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                return null;

            GameObject activeMatch = GameObject.Find(objectName);
            if (activeMatch != null)
                return activeMatch;

            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);

            for (int i = 0; i < transforms.Length; i++)
            {
                Transform transform = transforms[i];
                if (transform != null && transform.name == objectName)
                    return transform.gameObject;
            }

            return null;
        }

        private static void WireStation(string objectName, CraftingStationType stationType)
        {
            GameObject target = FindSceneObject(objectName);
            if (target == null)
                return;

            CraftingStation station = target.GetComponent<CraftingStation>();
            if (station == null)
                station = target.AddComponent<CraftingStation>();

            if (station == null)
            {
                Debug.LogWarning($"CraftingSceneBootstrap: Could not add CraftingStation to '{objectName}'.");
                return;
            }

            station.Configure(stationType);
        }

        private static void WireRecipePickups(string anchorName)
        {
            GameObject anchor = FindSceneObject(anchorName);
            if (anchor == null)
                return;

            Transform host = anchor.transform.Find("RecipePickups");
            if (host == null)
            {
                GameObject hostObject = new GameObject("RecipePickups");
                hostObject.transform.SetParent(anchor.transform, false);
                host = hostObject.transform;
            }

            IReadOnlyList<RecipeDefinition> recipes = RecipeRegistry.GetAllRecipes();
            if (recipes.Count == 0)
                return;

            Vector3 basePosition = anchor.transform.position + anchor.transform.forward * 1.2f + Vector3.up * 0.35f;
            Vector3 right = anchor.transform.right;

            for (int i = 0; i < recipes.Count; i++)
            {
                RecipeDefinition recipe = recipes[i];
                if (recipe == null)
                    continue;

                string childName = $"RecipePickup_{recipe.ResolvedId}";
                Transform existing = host.Find(childName);
                GameObject pickupObject = existing != null ? existing.gameObject : new GameObject(childName);

                if (existing == null)
                {
                    pickupObject.transform.SetParent(host, false);
                    pickupObject.transform.position = basePosition + right * (i - 1.5f) * 0.65f;
                }

                EnsureTriggerCollider(pickupObject, new Vector3(0.5f, 0.5f, 0.5f));

                RecipePickup pickup = pickupObject.GetComponent<RecipePickup>();
                if (pickup == null)
                    pickup = pickupObject.AddComponent<RecipePickup>();

                if (pickup == null)
                {
                    Debug.LogWarning($"CraftingSceneBootstrap: Could not add RecipePickup for '{recipe.ResolvedId}'.");
                    continue;
                }

                pickup.Configure(recipe.ResolvedId);
                pickupObject.SetActive(true);
            }
        }

        private static void EnsureTriggerCollider(GameObject target, Vector3 boxSize)
        {
            if (target == null)
                return;

            Collider[] colliders = target.GetComponents<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null)
                    continue;

                collider.isTrigger = true;
                if (collider is BoxCollider existingBox)
                    existingBox.size = boxSize;

                return;
            }

            BoxCollider box = target.AddComponent<BoxCollider>();
            box.size = boxSize;
            box.isTrigger = true;
        }

        private static void EnsurePlayerCraftingManager()
        {
            GameObject player = PlayerLocator.FindPlayerObject();
            if (player == null)
                return;

            if (player.GetComponent<CraftingManager>() == null)
                player.AddComponent<CraftingManager>();
        }

        private sealed class CraftingSceneBootstrapRunner : MonoBehaviour
        {
            private void Start()
            {
                StartCoroutine(WireAfterSceneReady());
            }

            private IEnumerator WireAfterSceneReady()
            {
                yield return null;
                EnsureWired();
                Destroy(gameObject);
            }
        }
    }
}
