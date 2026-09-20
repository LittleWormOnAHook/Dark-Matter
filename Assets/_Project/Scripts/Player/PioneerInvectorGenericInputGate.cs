using System;
using System.Collections.Generic;
using System.Reflection;
using Invector.vCharacterController;
using UnityEngine;

namespace Project.Player.Invector
{
    /// <summary>
    /// Mutes Invector <see cref="GenericInput"/> fields while the Gamepad scheme is active
    /// so legacy joystick/axis polling cannot fight Input System + Pioneer binders.
    /// </summary>
    public static class PioneerInvectorGenericInputGate
    {
        private static readonly Dictionary<int, bool> SavedUseInput = new Dictionary<int, bool>(64);
        private static readonly BindingFlags FieldFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        public static void ApplyGamepadMute(vThirdPersonInput input, bool mute)
        {
            if (input == null)
                return;

            for (Type type = input.GetType(); type != null && type != typeof(MonoBehaviour); type = type.BaseType)
            {
                FieldInfo[] fields = type.GetFields(FieldFlags);
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo field = fields[i];
                    if (field.FieldType != typeof(GenericInput))
                        continue;

                    GenericInput generic = field.GetValue(input) as GenericInput;
                    if (generic == null)
                        continue;

                    int key = MakeKey(input.GetInstanceID(), field.Name);
                    if (mute)
                    {
                        if (!SavedUseInput.ContainsKey(key))
                            SavedUseInput[key] = generic.useInput;
                        generic.useInput = false;
                    }
                    else if (SavedUseInput.TryGetValue(key, out bool saved))
                    {
                        generic.useInput = saved;
                    }
                }
            }
        }

        private static int MakeKey(int ownerInstanceId, string fieldName)
        {
            unchecked
            {
                return (ownerInstanceId * 397) ^ fieldName.GetHashCode();
            }
        }
    }
}
