using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace FORGE3D
{
    public class F3DTurretUI : MonoBehaviour
    {
        public List<F3DFXController> _fxControllers = new List<F3DFXController>();

        public Text WeaponTypeText;

        Keyboard keyboard = Keyboard.current;
        
        // GUI captions
        private string[] _fxTypeName =
        {
            "Vulcan", "Sologun", "Sniper", "Shotgun", "Seeker", "Railgun", "Plasmagun", "Plasma beam",
            "Heavy plasma beam", "Lightning gun", "Flamethrower", "Pulse laser"
        };

        // Use this for initialization
        private void Awake()
        {
            _fxControllers.AddRange(FindObjectsByType<F3DFXController>(FindObjectsInactive.Exclude));
        }

        private void Start()
        {
            if (_fxControllers.Count > 0)
                SetWeaponTypeText();
        }

        void SetWeaponTypeText()
        {
            WeaponTypeText.text = _fxTypeName[(int) _fxControllers[0].DefaultFXType];
        }

        public void OnButtonNext()
        {
            ToggleWeaponType(true);
        }

        public void OnButtonPrevious()
        {
            ToggleWeaponType(false);
        }

        private void ToggleWeaponType(bool next)
        {
            foreach (var _fx in _fxControllers)
            {
                if (next) _fx.NextWeapon();
                else _fx.PrevWeapon();
                _fx.Stop();
            }

            SetWeaponTypeText();
        }

        private void Update()
        {
            if (Project.Combat.Turrets.DMTurretRuntime.SuppressDemoInput)
                return;
            if (Keyboard.current != null)
            {
                // Switch weapon types using keyboard keys
                if (Keyboard.current.eKey.wasPressedThisFrame)
                    OnButtonNext();
                else if (Keyboard.current.qKey.wasPressedThisFrame)
                    OnButtonPrevious();
            }
        }
    }
}