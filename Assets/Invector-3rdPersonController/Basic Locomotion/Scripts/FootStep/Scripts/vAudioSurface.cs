using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Invector
{
    public class vAudioSurface : ScriptableObject
    {
        public AudioSource audioSource;
        public AudioMixerGroup audioMixerGroup;                 // The AudioSource that will play the clips.   
        public List<string> TextureOrMaterialNames;             // The tag on the surfaces that play these sounds.
        public List<AudioClip> audioClips;                      // The different clips that can be played on this surface.    
        public GameObject particleObject;

        private vFisherYatesRandom randomSource = new vFisherYatesRandom();       // For randomly reordering clips.   

        public bool useStepMark;
        [vHideInInspector("useStepMark")]
        public GameObject stepMark;
        [vHideInInspector("useStepMark")]
        public LayerMask stepLayer;
        [vHideInInspector("useStepMark")]
        public float timeToDestroy = 5f;

        public vAudioSurface()
        {
            audioClips = new List<AudioClip>();
            TextureOrMaterialNames = new List<string>();
        }
        /// <summary>
        /// Spawn surface effect
        /// </summary>
        /// <param name="footStepObject">step object surface info</param>
        /// <param name="playSound">Spawn sound effect</param>
        /// <param name="spawnParticle">Spawn particle effect</param>
        /// <param name="spawnStepMark">Spawn step Mark effect</param>

        public virtual void SpawnSurfaceEffect(FootStepObject footStepObject)
        {

            // initialize variable if not already started
            if (randomSource == null)
            {
                randomSource = new vFisherYatesRandom();
            }
            var resolved = Project.Player.DMFootstepManager.Resolve(footStepObject);

            ///Create audio Effect
            if (footStepObject.spawnSoundEffect)
            {
                PlaySound(footStepObject);
            }
            LayerMask walkableMask = ResolveWalkableStepMask();
            ///Create particle Effect
            if (footStepObject.spawnParticleEffect && footStepObject.ground && walkableMask.ContainsLayer(footStepObject.ground.gameObject.layer)
                && (resolved.dustPrefab != null || particleObject != null))
            {
                SpawnParticle(footStepObject, resolved);
            }
            ///Create Step Mark Effect
            if (footStepObject.spawnStepMarkEffect && useStepMark)
            {
                StepMark(footStepObject, walkableMask, resolved);
            }
        }


        /// <summary>
        /// Spawn Sound effect
        /// </summary>
        /// <param name="footStepObject">Step object surface info</param>      
        protected virtual void PlaySound(FootStepObject footStepObject)
        {
            if (footStepObject != null && Project.Audio.GameAudioManager.Instance != null)
            {
                string tag = Project.Player.DMFootstepManager.ResolveTag(footStepObject);
                Project.Audio.GameAudioManager.Instance.PlayFootstep(
                    footStepObject.sender.position,
                    tag,
                    false,
                    footStepObject.terrainLayerIndex);
                return;
            }

            // if there are no clips to play return.
            if (audioClips == null || audioClips.Count == 0)
            {
                return;
            }

            AudioSource source = null;
            if (audioSource != null)
            {
                source = Instantiate(audioSource, footStepObject.sender.position, Quaternion.identity);
                source.transform.SetParent(vObjectContainer.root, true);
            }
            if (audioSource)
            {
                if (audioMixerGroup != null)
                {
                    source.outputAudioMixerGroup = audioMixerGroup;
                }
            }
            int index = randomSource.Next(audioClips.Count);
            source.PlayOneShot(audioClips[index], footStepObject.volume);
        }
        /// <summary>
        /// Spawn Particle effect
        /// </summary>
        /// <param name="footStepObject">Step object surface info</param>
        protected virtual void SpawnParticle(FootStepObject footStepObject, Project.Player.DMFootstepResolved resolved)
        {
            GameObject prefab = resolved.dustPrefab != null ? resolved.dustPrefab : particleObject;
            if (prefab == null)
                return;

            Project.Effects.DustTrackLifetime.Spawn(
                prefab,
                footStepObject.sender.position,
                footStepObject.sender.rotation,
                resolved.dustMaterial,
                vObjectContainer.root);
        }
        /// <summary>
        /// Spawn Step Mark effect
        /// </summary>
        /// <param name="footStepObject">Step object surface info</param>
        protected virtual void StepMark(FootStepObject footStep, LayerMask walkableMask, Project.Player.DMFootstepResolved resolved)
        {
            var profile = Project.Player.DMFootstepProfile.Live;
            GameObject markPrefab = resolved.stepMarkPrefab != null ? resolved.stepMarkPrefab : stepMark;
            float life = profile != null ? profile.stepMarkLifetime : timeToDestroy;

            RaycastHit hit;
            if (Physics.Raycast(footStep.sender.transform.position + new Vector3(0, 0.25f, 0), Vector3.down, out hit, 1f, walkableMask))
            {
                if (markPrefab)
                {
                    var angle = Quaternion.FromToRotation(footStep.sender.up, hit.normal);
                    var step = Instantiate(markPrefab, hit.point, angle * footStep.sender.rotation);
                    step.transform.SetParent(vObjectContainer.root, true);
                    Destroy(step, life);
                }
            }
        }

        /// <summary>
        /// Playable Gaia tile is Climbable (23). Surface assets often only list Default,
        /// which drops both dust and step marks on the live terrain.
        /// </summary>
        protected virtual int ResolveWalkableStepMask()
        {
            var profile = Project.Player.DMFootstepProfile.Live;
            if (profile != null)
                return profile.ResolveWalkableMask(stepLayer);

            int mask = stepLayer.value;
            if (mask == 0)
                mask = 1;

            int climbable = LayerMask.NameToLayer("Climbable");
            if (climbable >= 0)
                mask |= 1 << climbable;

            return mask;
        }
    }
}