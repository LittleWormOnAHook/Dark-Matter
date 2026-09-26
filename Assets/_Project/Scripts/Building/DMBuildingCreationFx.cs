using System.Collections.Generic;
using UnityEngine;

namespace Project.Building
{
    /// <summary>
    /// Plays DMBuildingCreationFxProfile on a freshly built piece, then restores its exact seat, size and finish and removes itself.
    /// Pieces restored from a save never play it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMBuildingCreationFx : MonoBehaviour
    {
        DMBuildingCreationFxProfile profile;
        Vector3 restPosition;
        Vector3 restScale;
        float elapsed;
        float totalSeconds;
        bool vfxSpawned;
        bool swapOn;
        bool swapDone;
        Bounds pieceBounds;
        readonly List<Renderer> swappedRenderers = new List<Renderer>();
        readonly List<Material[]> originalMaterials = new List<Material[]>();

        public static void Play(DMBuildingGhost piece)
        {
            DMBuildingCreationFxProfile fx = DMBuildingCreationFxProfile.Live;
            if (piece == null || fx == null || !fx.enabled)
                return;

            bool motion = fx.duration > 0f && (fx.scalePunch > 0f || fx.bounceHeightMeters > 0f);
            bool vfx = fx.vfxPrefab != null;
            bool swap = fx.swapMaterial && fx.swapMaterialAsset != null && fx.swapDuration > 0f;
            if (!motion && !vfx && !swap)
                return;

            DMBuildingCreationFx player = piece.GetComponent<DMBuildingCreationFx>();
            if (player != null)
                player.Finish();
            player = piece.gameObject.AddComponent<DMBuildingCreationFx>();
            player.Begin(fx, motion, vfx, swap);
        }

        /// <summary>Where the piece really sits, ignoring any bounce in progress (used by the save).</summary>
        public static Vector3 RestPosition(DMBuildingGhost piece)
        {
            if (piece == null)
                return Vector3.zero;
            DMBuildingCreationFx player = piece.GetComponent<DMBuildingCreationFx>();
            return player != null && player.enabled ? player.restPosition : piece.transform.position;
        }

        void Begin(DMBuildingCreationFxProfile fx, bool motion, bool vfx, bool swap)
        {
            profile = fx;
            restPosition = transform.position;
            restScale = transform.localScale;
            pieceBounds = MeasureBounds();
            totalSeconds = motion ? fx.duration : 0f;
            if (vfx)
                totalSeconds = Mathf.Max(totalSeconds, fx.vfxDelay);
            else
                vfxSpawned = true;
            if (swap)
                totalSeconds = Mathf.Max(totalSeconds, fx.swapStart + fx.swapDuration);
            else
                swapDone = true;
            Tick(0f);
        }

        void Update()
        {
            Tick(Time.deltaTime);
        }

        void Tick(float delta)
        {
            if (profile == null)
            {
                Finish();
                return;
            }

            elapsed += delta;

            if (profile.duration > 0f && elapsed < profile.duration)
            {
                float t = elapsed / profile.duration;
                float grow = 1f + profile.scalePunch * profile.scaleCurve.Evaluate(t);
                transform.localScale = restScale * grow;
                transform.position = restPosition + Vector3.up * (profile.bounceHeightMeters * profile.bounceCurve.Evaluate(t));
            }
            else
            {
                transform.localScale = restScale;
                transform.position = restPosition;
            }

            if (!vfxSpawned && elapsed >= profile.vfxDelay)
                SpawnVfx();

            if (!swapDone)
            {
                if (!swapOn && elapsed >= profile.swapStart)
                    SwapOn();
                if (swapOn && elapsed >= profile.swapStart + profile.swapDuration)
                    SwapOff();
            }

            if (elapsed >= totalSeconds)
                Finish();
        }

        void SpawnVfx()
        {
            vfxSpawned = true;
            if (profile.vfxPrefab == null)
                return;

            Vector3 point = pieceBounds.center;
            if (profile.vfxAnchor == DMBuildingFxAnchor.Base)
                point.y = pieceBounds.min.y;
            else if (profile.vfxAnchor == DMBuildingFxAnchor.Top)
                point.y = pieceBounds.max.y;

            GameObject spawned = Instantiate(profile.vfxPrefab, point, transform.rotation);
            if (profile.vfxScaleWithPiece)
            {
                float footprint = Mathf.Max(pieceBounds.size.x, pieceBounds.size.z);
                float scale = Mathf.Clamp(footprint / 4f, 0.25f, 3f);
                spawned.transform.localScale = profile.vfxPrefab.transform.localScale * scale;
            }

            Destroy(spawned, Mathf.Max(0.1f, profile.vfxLifetime));
        }

        void SwapOn()
        {
            swapOn = true;
            Material swap = profile.swapMaterialAsset;
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || r is ParticleSystemRenderer)
                    continue;
                Material[] original = r.sharedMaterials;
                var replaced = new Material[original.Length];
                for (int m = 0; m < replaced.Length; m++)
                    replaced[m] = swap;
                swappedRenderers.Add(r);
                originalMaterials.Add(original);
                r.sharedMaterials = replaced;
            }
        }

        void SwapOff()
        {
            swapOn = false;
            swapDone = true;
            for (int i = 0; i < swappedRenderers.Count; i++)
            {
                if (swappedRenderers[i] != null)
                    swappedRenderers[i].sharedMaterials = originalMaterials[i];
            }

            swappedRenderers.Clear();
            originalMaterials.Clear();
        }

        void Finish()
        {
            if (swapOn)
                SwapOff();
            transform.localScale = restScale;
            transform.position = restPosition;
            enabled = false;
            Destroy(this);
        }

        void OnDestroy()
        {
            if (swapOn)
                SwapOff();
        }

        Bounds MeasureBounds()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            bool any = false;
            var bounds = new Bounds(transform.position, Vector3.zero);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || renderers[i] is ParticleSystemRenderer)
                    continue;
                if (!any)
                {
                    bounds = renderers[i].bounds;
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            return bounds;
        }
    }
}
