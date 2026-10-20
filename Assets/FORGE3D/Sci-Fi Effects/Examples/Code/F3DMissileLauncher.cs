using System;
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace FORGE3D
{
    public class F3DMissileLauncher : MonoBehaviour
    {
        public Transform missilePrefab;
        public Transform target;
        public Transform[] socket;
        public Transform explosionPrefab;

        private F3DMissile.MissileType missileType;

        public Text missileTypeLabel;
        
        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;

        // Use this for initialization
        private void Start()
        {
            missileType = F3DMissile.MissileType.Unguided;
            missileTypeLabel.text = "Unguided";
        }

        // Spawns explosion
        public void SpawnExplosion(Vector3 position)
        {
            F3DPoolManager.Pools["GeneratedPool"]
                .Spawn(explosionPrefab, position, Quaternion.identity, null);
        }


        // Processes input for launching missile
        private void ProcessInput()
        {
            if(Mouse.current == null || Keyboard.current == null) return;
            
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                var randomSocketId = Random.Range(0, socket.Length);
                var tMissile = F3DPoolManager.Pools["GeneratedPool"].Spawn(missilePrefab,
                    socket[randomSocketId].position, socket[randomSocketId].rotation, null);

                if (tMissile != null)
                {
                    var missile = tMissile.GetComponent<F3DMissile>();

                    missile.launcher = this;
                    missile.missileType = missileType;

                    if (target != null)
                        missile.target = target;
                }
            }

            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                missileType = F3DMissile.MissileType.Unguided;
                missileTypeLabel.text = "Unguided";
            }
            else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                missileType = F3DMissile.MissileType.Guided;
                missileTypeLabel.text = "Guided";
            }
            else if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                missileType = F3DMissile.MissileType.Predictive;
                missileTypeLabel.text = "Predictive";
            }
        }

        // Update is called once per frame
        private void Update()
        {
            ProcessInput();
        }
    }
}