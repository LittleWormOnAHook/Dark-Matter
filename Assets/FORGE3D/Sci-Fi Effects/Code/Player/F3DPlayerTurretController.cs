using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

namespace FORGE3D
{
    public class F3DPlayerTurretController : MonoBehaviour
    {
        RaycastHit hitInfo; // Raycast structure
        public F3DTurret turret;
        bool isFiring; // Is turret currently in firing state
        public F3DFXController fxController;

        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;

        void Update()
        {
            if (Project.Combat.Turrets.DMTurretRuntime.SuppressDemoInput)
                return;
            CheckForTurn();
            CheckForFire();
        }

        void CheckForFire()
        {
            if(Mouse.current == null) return;
            // Fire turret
            if (!isFiring && Mouse.current.leftButton.wasPressedThisFrame)
            {
                isFiring = true;
                fxController.Fire();
            }

            // Stop firing
            if (isFiring && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                isFiring = false;
                fxController.Stop();
            }
        }

        void CheckForTurn()
        {
            if(Mouse.current == null) return;
            // Construct a ray pointing from screen mouse position into world space
            Ray cameraRay = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

            // Raycast
            if (Physics.Raycast(cameraRay, out hitInfo, 500f))
            {
                turret.SetNewTarget(hitInfo.point);
            }
        }
    }
}