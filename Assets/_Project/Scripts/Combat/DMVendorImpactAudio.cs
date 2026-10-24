using System.Collections.Generic;
using FORGE3D;
using Project.Audio;
using Project.Data;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// 2026-10-24n: vendor (FORGE3D) impact "explosion" audio for player ammo, played on EVERY confirmed hit.
    /// Before: the sound came only from the vendor F3DProjectile on the pooled tracer/projectile. Our PoolManager never
    /// calls F3DProjectile.OnSpawned, so its isHit/isFXSpawned flags stayed true after the first life -> explosion
    /// audio only on each pooled instance's first shot. Now the vendor driver is disabled on our pooled instances and
    /// the hit clip is played here from F3DAudioController clips, with per-ammo min interval + max voices.
    /// </summary>
    public static class DMVendorImpactAudio
    {
        private static readonly Dictionary<ItemData, int> FxCache = new Dictionary<ItemData, int>(); // -1 = none
        private static readonly Dictionary<ItemData, float> LastPlay = new Dictionary<ItemData, float>();
        private static readonly List<float> VoiceEnds = new List<float>(32);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { FxCache.Clear(); LastPlay.Clear(); VoiceEnds.Clear(); }

        /// <summary>Disable FORGE3D self-driving projectile logic on a player-pooled instance (we own movement + hits).</summary>
        public static void SuppressVendorDriver(GameObject instance)
        {
            if (instance == null) return;
            foreach (var p in instance.GetComponentsInChildren<F3DProjectile>(true))
                if (p != null) p.enabled = false;
        }

        public static void Play(ItemData ammo, ItemData weapon, Vector3 pos)
        {
            if (ammo == null) return;
            DMAmmoFxProfile prof = ammo as DMAmmoFxProfile ?? DMCombatFx.ResolveProfile(ammo, weapon);
            if (prof != null && prof.vendorImpactAudio == DMAmmoFxProfile.VendorImpactAudioMode.Off) return;
            int fx = ResolveFx(ammo, prof);
            if (fx < 0) return;
            F3DAudioController ac = F3DAudioController.instance;
            if (ac == null) return;
            AudioClip[] clips = ClipsFor(ac, (F3DFXType)fx);
            if (clips == null || clips.Length == 0) return;
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip == null) return;

            float now = Time.time;
            float minInterval = prof != null ? prof.impactAudioMinInterval : 0.05f;
            int maxVoices = prof != null ? prof.impactAudioMaxVoices : 4;
            if (LastPlay.TryGetValue(ammo, out float last) && now - last < minInterval) return;
            for (int i = VoiceEnds.Count - 1; i >= 0; i--) if (VoiceEnds[i] <= now) VoiceEnds.RemoveAt(i);
            if (VoiceEnds.Count >= Mathf.Max(1, maxVoices)) return;

            LastPlay[ammo] = now;
            VoiceEnds.Add(now + clip.length);
            float vol = (prof != null ? prof.impactAudioVolume : 1f) * Random.Range(0.8f, 1f);
            GameAudioManager.PlayWorldSfx(clip, pos, vol);
        }

        private static int ResolveFx(ItemData ammo, DMAmmoFxProfile prof)
        {
            if (prof != null && prof.vendorImpactAudio != DMAmmoFxProfile.VendorImpactAudioMode.Auto)
                return (int)prof.vendorImpactAudio - 2; // explicit F3DFXType
            if (FxCache.TryGetValue(ammo, out int c)) return c;
            int fx = -1;
            GameObject[] cands = { ammo.tracerPrefab, ammo.projectilePrefab };
            foreach (var go in cands)
            {
                if (go == null) continue;
                var p = go.GetComponentInChildren<F3DProjectile>(true);
                if (p != null) { fx = (int)p.fxType; break; }
            }
            FxCache[ammo] = fx;
            return fx;
        }

        private static AudioClip[] ClipsFor(F3DAudioController ac, F3DFXType t)
        {
            switch (t)
            {
                case F3DFXType.Vulcan: return ac.vulcanHit;
                case F3DFXType.SoloGun: return ac.soloGunHit;
                case F3DFXType.Sniper: return ac.sniperHit;
                case F3DFXType.ShotGun: return ac.shotGunHit;
                case F3DFXType.Seeker: return ac.seekerHit;
                case F3DFXType.RailGun: return ac.railgunHit;
                case F3DFXType.PlasmaGun: return ac.plasmagunHit;
                case F3DFXType.LaserImpulse: return ac.laserImpulseHit;
                default: return null;
            }
        }
    }
}
