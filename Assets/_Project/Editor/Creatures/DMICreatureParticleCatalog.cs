using System.Collections.Generic;
using Project.EditorTools;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Creatures
{
    /// <summary>
    /// Lists ranged-attack particle prefabs under <see cref="ProjectAssetPaths.PrefabsParticles"/>.
    /// Creatures Manager uses this so each creature can pick its own spit / breath / ball VFX.
    /// </summary>
    public static class DMICreatureParticleCatalog
    {
        public const string PoisonSpitPrefabPath =
            "Assets/_Project/Prefabs/Particles/Poison Spit.prefab";

        private static GameObject[] cachedParticles = System.Array.Empty<GameObject>();
        private static string[] cachedLabels = System.Array.Empty<string>();
        private static double cacheTime = -1d;

        public static GameObject[] LoadParticlePrefabs(bool forceRefresh = false)
        {
            EnsureCache(forceRefresh);
            return cachedParticles;
        }

        public static string[] LoadParticleLabels(bool forceRefresh = false)
        {
            EnsureCache(forceRefresh);
            return cachedLabels;
        }

        public static GameObject LoadPoisonSpitPrefab()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(PoisonSpitPrefabPath);
        }

        public static GameObject FindByName(string particleName)
        {
            if (string.IsNullOrWhiteSpace(particleName))
                return null;

            GameObject[] particles = LoadParticlePrefabs();
            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i] != null && particles[i].name == particleName)
                    return particles[i];
            }

            return null;
        }

        public static int IndexOf(GameObject particle)
        {
            if (particle == null)
                return -1;

            GameObject[] particles = LoadParticlePrefabs();
            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i] == particle)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Popup that assigns a particle from Prefabs/Particles onto <paramref name="current"/>.
        /// Returns the selected particle (may be unchanged).
        /// </summary>
        public static GameObject DrawParticlePopup(string label, GameObject current)
        {
            EnsureCache(false);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);

            string currentName = current != null ? current.name : "(None)";
            if (GUILayout.Button(currentName, EditorStyles.popup))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("(None)"), current == null, () => ParticlePickSession.Pending = null);

                for (int i = 0; i < cachedParticles.Length; i++)
                {
                    GameObject particle = cachedParticles[i];
                    if (particle == null)
                        continue;

                    GameObject captured = particle;
                    menu.AddItem(
                        new GUIContent(particle.name),
                        particle == current,
                        () => ParticlePickSession.Pending = captured);
                }

                menu.ShowAsContext();
            }

            if (GUILayout.Button("Refresh", GUILayout.Width(64f)))
                EnsureCache(true);

            EditorGUILayout.EndHorizontal();

            if (ParticlePickSession.PendingConsumed(out GameObject picked))
                return picked;

            return current;
        }

        private static void EnsureCache(bool forceRefresh)
        {
            // Refresh at most once per editor second unless forced.
            if (!forceRefresh && cachedParticles.Length > 0 && EditorApplication.timeSinceStartup - cacheTime < 1d)
                return;

            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsParticles);
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { ProjectAssetPaths.PrefabsParticles });
            List<GameObject> particles = new List<GameObject>(guids.Length);
            List<string> labels = new List<string>(guids.Length);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;

                particles.Add(prefab);
                labels.Add(prefab.name);
            }

            particles.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            labels.Clear();
            for (int i = 0; i < particles.Count; i++)
                labels.Add(particles[i].name);

            cachedParticles = particles.ToArray();
            cachedLabels = labels.ToArray();
            cacheTime = EditorApplication.timeSinceStartup;
        }

        /// <summary>
        /// Holds a one-shot GenericMenu selection between GUI layout passes.
        /// </summary>
        private static class ParticlePickSession
        {
            private static GameObject pending;
            private static bool hasPending;

            public static GameObject Pending
            {
                set
                {
                    pending = value;
                    hasPending = true;
                    if (EditorWindow.focusedWindow != null)
                        EditorWindow.focusedWindow.Repaint();
                }
            }

            public static bool PendingConsumed(out GameObject value)
            {
                if (!hasPending)
                {
                    value = null;
                    return false;
                }

                value = pending;
                pending = null;
                hasPending = false;
                return true;
            }
        }
    }
}
