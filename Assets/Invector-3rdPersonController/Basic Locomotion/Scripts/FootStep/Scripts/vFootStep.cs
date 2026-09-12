using System.Collections.Generic;
using UnityEngine;

namespace Invector
{
    public class vFootStep : vFootStepBase
    {
        public AnimationType animationType = AnimationType.Humanoid;
        public bool debugTextureName;

        [SerializeField, Range(0, 1f)] protected float _volume = 1f;
        [vHelpBox("Enable or disable spawn particle when foot step is triggered")]
        [SerializeField] protected bool _spawnParticle = true;
        [vHelpBox("Enable or disable spawn step mark when foot step is triggered")]
        [SerializeField] protected bool _spawnStepMark = true;
        [vHelpBox("The step effect is spawned from on trigger enter event of the Foot Step Triggers. If you need to play step sound only by external events you need to disable this variable.<b>\n*Disable this to play step sound using animation events</b>")]
        [SerializeField] protected bool _useTriggerEnter = true;

        public float Volume { get { return _volume; } set { _volume = value; } }
        public bool SpawnParticle { get { return _spawnParticle; } set { _spawnParticle = value; } }
        public bool SpawnStepMark { get { return _spawnStepMark; } set { _spawnStepMark = value; } }
        public bool UseTriggerEnter { get { return _useTriggerEnter; } set { _useTriggerEnter = value; } }

        protected int surfaceIndex = 0;
        protected Terrain terrain;
        protected TerrainCollider terrainCollider;
        protected TerrainData terrainData;
        protected Vector3 terrainPos;

        public vFootStepTrigger leftFootTrigger;
        public vFootStepTrigger rightFootTrigger;
        public Transform currentStep;
        public List<vFootStepTrigger> footStepTriggers;

        protected FootStepObject currentFootStep;
        private float lastLeftStepUnscaled = -10f;
        private float lastRightStepUnscaled = -10f;

        protected virtual void Start()
        {
            InitFootStep();
        }

        public virtual void InitFootStep()
        {
            var colls = GetComponentsInChildren<Collider>();
            if (animationType == AnimationType.Humanoid)
            {
                if (leftFootTrigger == null && rightFootTrigger == null)
                {
                    Debug.Log("Missing FootStep Sphere Trigger, please unfold the FootStep Component to create the triggers.");
                    return;
                }
                else
                {
                    leftFootTrigger.trigger.isTrigger = true;
                    rightFootTrigger.trigger.isTrigger = true;
                    Physics.IgnoreCollision(leftFootTrigger.trigger, rightFootTrigger.trigger);
                    for (int i = 0; i < colls.Length; i++)
                    {
                        var coll = colls[i];
                        if (coll.enabled && coll.gameObject != leftFootTrigger.gameObject)
                        {
                            Physics.IgnoreCollision(leftFootTrigger.trigger, coll);
                        }

                        if (coll.enabled && coll.gameObject != rightFootTrigger.gameObject)
                        {
                            Physics.IgnoreCollision(rightFootTrigger.trigger, coll);
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < colls.Length; i++)
                {
                    var coll = colls[i];
                    for (int a = 0; a < footStepTriggers.Count; a++)
                    {
                        var trigger = footStepTriggers[a];
                        trigger.trigger.isTrigger = true;
                        if (coll.enabled && coll.gameObject != trigger.gameObject)
                        {
                            Physics.IgnoreCollision(trigger.trigger, coll);
                        }
                    }
                }
            }
        }

        protected virtual void UpdateTerrainInfo(Terrain newTerrain)
        {
            if (terrain == null || terrain != newTerrain)
            {
                terrain = newTerrain;
                if (terrain != null)
                {
                    terrainData = terrain.terrainData;
                    terrainPos = terrain.transform.position;
                    terrainCollider = terrain.GetComponent<TerrainCollider>();
                }
            }
        }

        protected virtual int GetMainTexture(FootStepObject footStepObj)
        {
            UpdateTerrainInfo(footStepObj.terrain);
            if (terrain == null)
                return -1;

            return Project.Player.DMFootstepManager.SampleTerrainLayerIndex(terrain, footStepObj.sender.position);
        }

        protected virtual void OnDestroy()
        {
            if (leftFootTrigger != null)
            {
                Destroy(leftFootTrigger.gameObject);
            }

            if (rightFootTrigger != null)
            {
                Destroy(rightFootTrigger.gameObject);
            }

            if (footStepTriggers != null && footStepTriggers.Count > 0)
            {
                foreach (var comp in footStepTriggers)
                {
                    Destroy(comp.gameObject);
                }
            }
        }

        /// <summary>
        /// Step on Terrain
        /// </summary>
        /// <param name="footStepObject"></param>
        public override void StepOnTerrain(FootStepObject footStepObject)
        {
            if (footStepObject == null || footStepObject.sender == null)
                return;

            if (currentStep != null && currentStep == footStepObject.sender && _useTriggerEnter)
            {
                return;
            }

            currentStep = footStepObject.sender;
            surfaceIndex = GetMainTexture(footStepObject);

            if (surfaceIndex != -1)
            {
#if UNITY_2018_3_OR_NEWER
                var name = "";
                if (terrainData != null &&
                    terrainData.terrainLayers != null &&
                    surfaceIndex >= 0 &&
                    surfaceIndex < terrainData.terrainLayers.Length)
                {
                    TerrainLayer layer = terrainData.terrainLayers[surfaceIndex];
                    if (layer != null && layer.diffuseTexture != null)
                        name = layer.diffuseTexture.name;
                }
#else
                var name = (terrainData != null && terrainData.splatPrototypes.Length > 0) ? (terrainData.splatPrototypes[surfaceIndex]).texture.name : "";
#endif
                footStepObject.name = name;
                footStepObject.terrainLayerIndex = surfaceIndex;
                currentFootStep = footStepObject;
                if (_useTriggerEnter)
                {
                    PlayFootStepEffect();
                    if (debugTextureName)
                    {
                        Debug.Log(terrain.name + " " + name);
                    }
                }
            }
        }

        /// <summary>
        /// Step on Mesh
        /// </summary>
        /// <param name="footStepObject"></param>
        public override void StepOnMesh(FootStepObject footStepObject)
        {
            if (currentStep != null && currentStep == footStepObject.sender && _useTriggerEnter)
            {
                return;
            }

            currentStep = footStepObject.sender;
            footStepObject.terrainLayerIndex = -1;
            currentFootStep = footStepObject;
            if (_useTriggerEnter)
            {
                PlayFootStepEffect();
                if (debugTextureName)
                {
                    Debug.Log(footStepObject.name);
                }
            }
        }

        /// <summary>
        /// Play foot Step effect
        /// </summary>
        public override void PlayFootStepEffect()
        {
            if (currentFootStep == null || currentFootStep.sender == null)
                return;

            if (!TryAcceptFootPlant(currentFootStep.sender))
                return;

            var resolved = Project.Player.DMFootstepManager.Resolve(currentFootStep);
            currentFootStep.volume = resolved.volume;
            currentFootStep.spawnParticleEffect = resolved.spawnParticle;
            currentFootStep.spawnStepMarkEffect = resolved.spawnStepMark;
            if (debugTextureName)
            {
                Debug.Log(
                    "Footstep tag=" + Project.Player.DMFootstepManager.ResolveTag(currentFootStep) +
                    " terrainLayer=" + currentFootStep.terrainLayerIndex +
                    " name=" + currentFootStep.name);
            }

            SpawnSurfaceEffect(currentFootStep);
        }

        /// <summary>
        /// Play foot step effect from animation event
        /// </summary>
        /// <param name="evt"></param>
        public override void PlayFootStep(AnimationEvent evt)
        {
            if (_useTriggerEnter || evt.animatorClipInfo.weight <= 0.5f)
                return;

            PlayFootStepEffect();
        }

        /// <summary>
        /// Play left foot step effect from animation event
        /// </summary>
        /// <param name="evt"></param>
        public override void PlayFootStepLeft(AnimationEvent evt)
        {
            if (_useTriggerEnter || evt.animatorClipInfo.weight <= 0.5f || leftFootTrigger == null || currentFootStep == null)
                return;

            currentFootStep.sender = leftFootTrigger.transform;
            PlayFootStepEffect();
        }

        /// <summary>
        /// Play right foot step effect from animation event
        /// </summary>
        /// <param name="evt"></param>
        public override void PlayFootStepRight(AnimationEvent evt)
        {
            if (_useTriggerEnter || evt.animatorClipInfo.weight <= 0.5f || rightFootTrigger == null || currentFootStep == null)
                return;

            currentFootStep.sender = rightFootTrigger.transform;
            PlayFootStepEffect();
        }

        private bool TryAcceptFootPlant(Transform sender)
        {
            float minInterval = 0.18f;
            var profile = Project.Player.DMFootstepProfile.Live;
            if (profile != null)
                minInterval = profile.minSecondsBetweenFootVfx;

            bool isLeft = leftFootTrigger != null && sender == leftFootTrigger.transform;
            float last = isLeft ? lastLeftStepUnscaled : lastRightStepUnscaled;
            float now = Time.unscaledTime;
            if (now - last < minInterval)
                return false;

            if (isLeft)
                lastLeftStepUnscaled = now;
            else
                lastRightStepUnscaled = now;

            return true;
        }

    }

    public enum AnimationType
    {
        Humanoid, Generic
    }
}