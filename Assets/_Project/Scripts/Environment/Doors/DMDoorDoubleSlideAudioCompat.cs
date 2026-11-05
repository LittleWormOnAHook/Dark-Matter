using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Project.Environment.Doors
{
    /// <summary>
    /// 2026-11-05: Creepy Cat / kit DoorDoubleSlide calls GetComponent&lt;AudioSource&gt;().Play() with no null check.
    /// Ensures an AudioSource exists before trigger callbacks and disables duplicate DoorDoubleSlide when
    /// <see cref="DMSlidingDoorController"/> already drives the same door hierarchy.
    /// Vendor scripts are never edited.
    /// </summary>
    [DefaultExecutionOrder(-450)]
    public sealed class DMDoorDoubleSlideAudioCompat : MonoBehaviour
    {
        private static DMDoorDoubleSlideAudioCompat instance;
        private static Type doorDoubleSlideType;
        private readonly HashSet<MonoBehaviour> repaired = new HashSet<MonoBehaviour>();
        private float nextRescan;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            doorDoubleSlideType = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (instance != null)
                return;

            var go = new GameObject("DMDoorDoubleSlideAudioCompat (auto)");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<DMDoorDoubleSlideAudioCompat>();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRescan)
                return;

            nextRescan = Time.unscaledTime + 2f;
            ScanAndRepair();
        }

        private void ScanAndRepair()
        {
            Type type = ResolveDoorDoubleSlideType();
            if (type == null)
                return;

            UnityEngine.Object[] doors = UnityEngine.Object.FindObjectsByType(
                type,
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < doors.Length; i++)
            {
                if (doors[i] is not MonoBehaviour behaviour || behaviour == null)
                    continue;

                if (ShouldDisableForDmController(behaviour))
                {
                    behaviour.enabled = false;
                    continue;
                }

                if (!behaviour.enabled || repaired.Contains(behaviour))
                    continue;

                if (EnsureAudioSource(behaviour))
                    repaired.Add(behaviour);
            }
        }

        private static bool ShouldDisableForDmController(MonoBehaviour doorDoubleSlide)
        {
            DMSlidingDoorController controller =
                doorDoubleSlide.GetComponentInParent<DMSlidingDoorController>(true);
            return controller != null && controller.isActiveAndEnabled;
        }

        private static bool EnsureAudioSource(MonoBehaviour doorBehaviour)
        {
            GameObject host = doorBehaviour.gameObject;
            AudioSource audio = host.GetComponent<AudioSource>();
            if (audio == null)
                audio = host.AddComponent<AudioSource>();

            audio.playOnAwake = false;
            if (audio.spatialBlend < 0.5f)
                audio.spatialBlend = 1f;

            TryCopyClipFields(doorBehaviour, audio);
            return true;
        }

        private static void TryCopyClipFields(MonoBehaviour doorBehaviour, AudioSource audio)
        {
            if (audio.clip != null)
                return;

            Type t = doorBehaviour.GetType();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo[] fields = t.GetFields(flags);
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].FieldType != typeof(AudioClip))
                    continue;

                string name = fields[i].Name;
                if (name.IndexOf("open", StringComparison.OrdinalIgnoreCase) < 0
                    && name.IndexOf("close", StringComparison.OrdinalIgnoreCase) < 0
                    && name.IndexOf("clip", StringComparison.OrdinalIgnoreCase) < 0
                    && name.IndexOf("sound", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                if (fields[i].GetValue(doorBehaviour) is AudioClip clip && clip != null)
                {
                    audio.clip = clip;
                    return;
                }
            }
        }

        private static Type ResolveDoorDoubleSlideType()
        {
            if (doorDoubleSlideType != null)
                return doorDoubleSlideType;

            string[] names =
            {
                "DoorDoubleSlide",
                "creepycat.scifikitvol3.DoorDoubleSlide",
                "Creepycat.ScifiKitVol3.DoorDoubleSlide"
            };

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int n = 0; n < names.Length; n++)
            {
                for (int a = 0; a < assemblies.Length; a++)
                {
                    Type t = assemblies[a].GetType(names[n], false);
                    if (t != null && typeof(MonoBehaviour).IsAssignableFrom(t))
                    {
                        doorDoubleSlideType = t;
                        return doorDoubleSlideType;
                    }
                }
            }

            for (int a = 0; a < assemblies.Length; a++)
            {
                Type[] types;
                try
                {
                    types = assemblies[a].GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }

                if (types == null)
                    continue;

                for (int i = 0; i < types.Length; i++)
                {
                    Type t = types[i];
                    if (t != null && t.Name == "DoorDoubleSlide" && typeof(MonoBehaviour).IsAssignableFrom(t))
                    {
                        doorDoubleSlideType = t;
                        return doorDoubleSlideType;
                    }
                }
            }

            return null;
        }
    }
}
