using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

namespace FORGE3D
{
    public class F3DFlameThrower : MonoBehaviour
    {
        Mouse mouse = Mouse.current;
        public Light pLight; // Attached point light
        public ParticleSystem heat; // Heat particles

        int lightState; // Point light state flag (fading in or out)
        bool despawn; // Despawn state flag

        ParticleSystem ps;
        private readonly List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>(8);

        void Start()
        {
            ps = GetComponent<ParticleSystem>();
        }

        // OnSpawned called by pool manager 
        void OnSpawned()
        {
            despawn = false;

            lightState = 1;
            pLight.intensity = 0f;
            Project.Combat.Turrets.DMTurretWeaponContext.NotifySpawned(this);
        }

        void OnEnable()
        {
            Project.Combat.Turrets.DMTurretWeaponContext.NotifySpawned(this);
        }

        void OnParticleCollision(GameObject other)
        {
            if (ps == null)
                return;

            int count = ps.GetCollisionEvents(other, collisionEvents);
            for (int i = 0; i < count; i++)
            {
                ParticleCollisionEvent evt = collisionEvents[i];
                Collider col = other.GetComponent<Collider>();
                Rigidbody rb = other.GetComponent<Rigidbody>();
                Vector3 dir = evt.velocity.sqrMagnitude > 0.01f ? evt.velocity.normalized : transform.forward;
                if (Project.Combat.Turrets.DMForge3DHitBridge.TryHandleImpact(
                        this,
                        col,
                        evt.intersection,
                        dir,
                        8f,
                        rb,
                        continuous: true))
                    continue;
            }
        }

        // OnDespawned called by pool manager 
        void OnDespawned()
        {
        }

        // Despawn game object
        void OnDespawn()
        {
            F3DPoolManager.Pools["GeneratedPool"].Despawn(transform);
        }

        void Update()
        {
            // Despawn on mouse
            if (mouse.leftButton.wasReleasedThisFrame)
            {
                if (!despawn)
                {
                    // Set despawn flag and add despawn timer allowing particles fading
                    despawn = true;
                    F3DTime.time.AddTimer(1f, 1, OnDespawn);

                    // Stop the particle systems
                    ps.Stop();
                    if (heat)
                        heat.Stop(); 

                    // Toggle light state
                    pLight.intensity = 0.6f;
                    lightState = -1;
                }
            }

            // Fade in point light
            if (lightState == 1)
            {
                pLight.intensity = Mathf.Lerp(pLight.intensity, 0.7f, Time.deltaTime*10f);

                if (pLight.intensity >= 0.5f)
                    lightState = 0;
            }
            // Fade out point light
            else if (lightState == -1)
            {
                pLight.intensity = Mathf.Lerp(pLight.intensity, -0.1f, Time.deltaTime*10f);

                if (pLight.intensity <= 0f)
                    lightState = 0;
            }
        }
    }
}