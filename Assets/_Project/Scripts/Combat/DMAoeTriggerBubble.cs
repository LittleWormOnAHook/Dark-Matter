using System.Collections;
using System.Collections.Generic;
using Project.Data;
using Project.UI;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Project.Combat
{
    /// <summary>
    /// Visible splash trigger bubble spawned at impact. The first damage pulse is applied by
    /// <see cref="CombatHitResolver"/>; this object marks the radius and, when ordinance skills
    /// are allocated, lingers as Persistent Hazard and/or fires a Cluster Munitions second pulse.
    /// </summary>
    public sealed class DMAoeTriggerBubble : MonoBehaviour
    {
        private const int RingSegments = 40;
        private const float PulseVisualSeconds = 0.45f;
        private const float ClusterDelaySeconds = 0.32f;
        private const float ClusterRadiusScale = 0.62f;
        private const float CloudTickSeconds = 0.45f;
        private const float CloudTickDamageScale = 0.18f;

        private static Material s_ringMaterial;
        private static Material s_sphereMaterial;
        private static readonly Color RingColor = DarkMatterGenesisUiPalette.WithAlpha(
            DarkMatterGenesisUiPalette.RichFuchsia, 0.85f);
        private static readonly Color SphereColor = DarkMatterGenesisUiPalette.WithAlpha(
            DarkMatterGenesisUiPalette.DeepMagenta, 0.14f);

        private ItemData ammoItem;
        private GameObject owner;
        private Collider excludeCollider;
        private float radius;
        private float centerDamage;
        private float dotDurationScale;
        private bool forceResidue;
        private bool cluster;
        private bool cloud;
        private float clusterDamageScale;
        private float cloudDuration;
        private float lifeSeconds;
        private float nextCloudTickTime;
        private bool clusterFired;
        private LineRenderer ring;
        private MeshRenderer sphereRenderer;
        private Material sphereInstance;
        private readonly HashSet<int> cloudTickedThisInterval = new HashSet<int>();

        public static void Spawn(
            Vector3 center,
            float radius,
            ItemData ammoItem,
            GameObject owner,
            Collider excludeCollider,
            float centerDamage,
            float dotDurationScale,
            bool forceResidue,
            bool cluster,
            bool cloud,
            float clusterDamageScale,
            float cloudDuration)
        {
            if (ammoItem == null || radius <= 0.05f)
                return;

            GameObject go = new GameObject("DMAoeTriggerBubble");
            go.transform.position = center;
            DMAoeTriggerBubble bubble = go.AddComponent<DMAoeTriggerBubble>();
            bubble.Configure(
                ammoItem,
                owner,
                excludeCollider,
                radius,
                centerDamage,
                dotDurationScale,
                forceResidue,
                cluster,
                cloud,
                clusterDamageScale,
                cloudDuration);
        }

        private void Configure(
            ItemData ammo,
            GameObject source,
            Collider exclude,
            float splashRadius,
            float damage,
            float durationScale,
            bool residue,
            bool useCluster,
            bool useCloud,
            float clusterScale,
            float lingerSeconds)
        {
            ammoItem = ammo;
            owner = source;
            excludeCollider = exclude;
            radius = splashRadius;
            centerDamage = damage;
            dotDurationScale = durationScale;
            forceResidue = residue;
            cluster = useCluster;
            cloud = useCloud;
            clusterDamageScale = Mathf.Max(0.05f, clusterScale);
            cloudDuration = Mathf.Max(PulseVisualSeconds, lingerSeconds);
            lifeSeconds = cloud ? cloudDuration : PulseVisualSeconds;
            nextCloudTickTime = Time.time + CloudTickSeconds;

            BuildVisuals();
            if (cloud)
                BuildTrigger();

            StartCoroutine(RunLifetime());
        }

        private IEnumerator RunLifetime()
        {
            float elapsed = 0f;
            while (elapsed < lifeSeconds)
            {
                elapsed += Time.deltaTime;
                float remain = 1f - Mathf.Clamp01(elapsed / lifeSeconds);
                SetVisualAlpha(cloud ? Mathf.Lerp(0.35f, 1f, remain) : remain);

                if (cluster && !clusterFired && elapsed >= ClusterDelaySeconds)
                {
                    clusterFired = true;
                    CombatHitResolver.ApplySplashPulse(
                        ammoItem,
                        transform.position,
                        centerDamage * clusterDamageScale,
                        radius * ClusterRadiusScale,
                        owner,
                        excludeCollider,
                        dotDurationScale,
                        forceResidue);
                }

                if (cloud && Time.time >= nextCloudTickTime)
                {
                    nextCloudTickTime = Time.time + CloudTickSeconds;
                    cloudTickedThisInterval.Clear();
                    CombatHitResolver.ApplySplashPulse(
                        ammoItem,
                        transform.position,
                        centerDamage * CloudTickDamageScale,
                        radius,
                        owner,
                        excludeCollider,
                        dotDurationScale,
                        forceResidue);
                }

                yield return null;
            }

            Destroy(gameObject);
        }

        private void OnTriggerStay(Collider other)
        {
            if (!cloud || other == null || other == excludeCollider)
                return;
            if (CombatHitResolver.IsOwnerCollider(owner, other))
                return;

            int id = other.GetEntityId().GetHashCode();
            if (!cloudTickedThisInterval.Add(id))
                return;

            CombatHitResolver.ApplyStatusEffect(
                ammoItem,
                other,
                owner,
                dotDurationScale,
                forceResidue,
                centerDamage * 0.08f);
        }

        private void BuildVisuals()
        {
            ring = gameObject.AddComponent<LineRenderer>();
            ring.loop = true;
            ring.useWorldSpace = true;
            ring.positionCount = RingSegments;
            ring.widthMultiplier = Mathf.Clamp(radius * 0.035f, 0.04f, 0.12f);
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false;
            ring.numCapVertices = 2;
            ring.sharedMaterial = ResolveRingMaterial();
            ring.startColor = RingColor;
            ring.endColor = RingColor;

            Vector3 center = transform.position + Vector3.up * 0.06f;
            for (int i = 0; i < RingSegments; i++)
            {
                float angle = (i / (float)RingSegments) * Mathf.PI * 2f;
                ring.SetPosition(i, center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "BubbleMesh";
            sphere.transform.SetParent(transform, false);
            sphere.transform.localScale = Vector3.one * radius * 2f;
            Collider meshCollider = sphere.GetComponent<Collider>();
            if (meshCollider != null)
                Destroy(meshCollider);

            sphereRenderer = sphere.GetComponent<MeshRenderer>();
            if (sphereRenderer != null)
            {
                Material source = ResolveSphereMaterial();
                sphereInstance = source != null ? new Material(source) : null;
                if (sphereInstance != null)
                {
                    sphereInstance.name = "DM_AoeTriggerSphereInstance";
                    sphereInstance.hideFlags = HideFlags.HideAndDontSave;
                    sphereRenderer.sharedMaterial = sphereInstance;
                }

                sphereRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                sphereRenderer.receiveShadows = false;
            }
        }

        private void OnDestroy()
        {
            if (sphereInstance != null)
                Destroy(sphereInstance);
        }

        private void BuildTrigger()
        {
            SphereCollider trigger = gameObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = radius;

            Rigidbody body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.detectCollisions = true;
        }

        private void SetVisualAlpha(float alpha)
        {
            Color ringColor = DarkMatterGenesisUiPalette.WithAlpha(RingColor, RingColor.a * alpha);
            if (ring != null)
            {
                ring.startColor = ringColor;
                ring.endColor = ringColor;
            }

            if (sphereInstance != null)
            {
                Color sphereColor = DarkMatterGenesisUiPalette.WithAlpha(SphereColor, SphereColor.a * alpha);
                ApplyColor(sphereInstance, sphereColor);
            }
        }

        private static Material ResolveRingMaterial()
        {
            if (s_ringMaterial != null)
                return s_ringMaterial;

            s_ringMaterial = CreateHdrpUnlit("DM_AoeTriggerRing (Runtime)", RingColor);
            return s_ringMaterial;
        }

        private static Material ResolveSphereMaterial()
        {
            if (s_sphereMaterial != null)
                return s_sphereMaterial;

            s_sphereMaterial = CreateHdrpUnlit("DM_AoeTriggerSphere (Runtime)", SphereColor);
            return s_sphereMaterial;
        }

        private static Material CreateHdrpUnlit(string materialName, Color color)
        {
            Shader shader = Shader.Find("HDRP/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Color");
            if (shader == null)
                return null;

            Material material = new Material(shader)
            {
                name = materialName,
                hideFlags = HideFlags.HideAndDontSave,
                color = color,
                renderQueue = 3000
            };

            ApplyColor(material, color);
            if (shader.name.StartsWith("HDRP/", System.StringComparison.Ordinal))
            {
                HDMaterial.SetSurfaceType(material, true);
                if (material.HasProperty("_BlendMode"))
                    material.SetFloat("_BlendMode", 0f);
            }

            return material;
        }

        private static void ApplyColor(Material material, Color color)
        {
            if (material == null)
                return;

            material.color = color;
            if (material.HasProperty("_UnlitColor"))
                material.SetColor("_UnlitColor", color);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
        }
    }
}
