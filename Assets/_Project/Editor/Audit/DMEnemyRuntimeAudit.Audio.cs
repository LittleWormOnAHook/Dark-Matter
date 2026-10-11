#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Invector.vMelee;
using Project.Data;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Project.EditorTools.Audit
{
    public static partial class DMEnemyRuntimeAudit
    {
        // =============================================================== audio probe
        // Watches every AudioSource in the scene each frame and records new one-shot starts (PlayOneShot / PlayClipAtPoint / pooled SFX
        // sources / Instantiated hit-sound sources). Used to decide whether a swing swish or a hit sound actually played for an event.
        private sealed class AudioStart { public float t; public string source; public string clip; public float distPlayer; public float distEnemy; }

        private static class AudioProbe
        {
            public static bool On;
            public static readonly List<AudioStart> Starts = new List<AudioStart>();
            private static readonly HashSet<int> Playing = new HashSet<int>();
            private static readonly Dictionary<int, float> LastTime = new Dictionary<int, float>();

            private static float _next;

            public static void Begin()
            {
                Starts.Clear(); Playing.Clear(); LastTime.Clear(); On = true; _next = 0f;
                foreach (AudioSource s in Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude))
                    if (s != null && s.isPlaying) Playing.Add(s.GetHashCode());
            }

            public static void End() { On = false; }

            public static void Tick(Live L)
            {
                if (!On) return;
                if (Time.unscaledTime < _next) return; // 20 Hz is plenty for swish / hit windows of 0.05s+
                _next = Time.unscaledTime + 0.05f;
                foreach (AudioSource s in Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude))
                {
                    if (s == null) continue;
                    int id = s.GetHashCode();
                    if (!s.isPlaying) { Playing.Remove(id); LastTime.Remove(id); continue; }
                    // a pooled source that restarted between two ticks is still playing but its time went backwards: count it as a new start
                    bool restarted = LastTime.TryGetValue(id, out float lt) && s.time + 0.001f < lt;
                    LastTime[id] = s.time;
                    if (!Playing.Add(id) && !restarted) continue;
                    if (s.loop) continue;
                    if (s.clip != null && (s.clip.name.IndexOf("wind", StringComparison.OrdinalIgnoreCase) >= 0 || s.clip.name.IndexOf("ambient", StringComparison.OrdinalIgnoreCase) >= 0 || s.clip.name.IndexOf("howl", StringComparison.OrdinalIgnoreCase) >= 0)) continue; // ambient beds are not combat sounds
                    string clip = s.clip != null ? s.clip.name : "(one-shot)";
                    if (clip.IndexOf("foot", StringComparison.OrdinalIgnoreCase) >= 0 || clip.IndexOf("step", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                    Starts.Add(new AudioStart
                    {
                        t = Time.time, source = s.gameObject.name, clip = clip,
                        distPlayer = _player != null ? Vector3.Distance(s.transform.position, _player.transform.position) : -1f,
                        distEnemy = L != null && L.go != null ? Vector3.Distance(s.transform.position, L.go.transform.position) : -1f
                    });
                }
            }

            public static List<AudioStart> Window(float from, float to) { return Starts.Where(a => a.t >= from && a.t <= to).ToList(); }
            public static string Describe(List<AudioStart> l) { return l.Count == 0 ? "none" : string.Join(", ", l.Take(4).Select(a => a.source + ":" + a.clip)); }
        }

        // ------------------------------------------------------------ static code facts (cached)
        private static Dictionary<string, List<string>> _callerCache;

        /// <summary>Files under Assets/_Project/Scripts that contain `needle`, except `exceptFile`.</summary>
        private static List<string> CodeCallers(string needle, string exceptFile)
        {
            if (_callerCache == null) _callerCache = new Dictionary<string, List<string>>();
            string key = needle + "|" + exceptFile;
            if (_callerCache.TryGetValue(key, out var hit)) return hit;
            var res = new List<string>();
            try
            {
                foreach (string root in new[] { Path.Combine(Application.dataPath, "_Project", "Scripts"), Path.Combine(Application.dataPath, "Invector-3rdPersonController", "Melee Combat") })
                {
                    if (!Directory.Exists(root)) continue;
                    foreach (string f in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
                    {
                        if (exceptFile != null && Path.GetFileName(f) == exceptFile) continue;
                        if (File.ReadAllText(f).Contains(needle)) res.Add(Path.GetFileName(f));
                    }
                }
            }
            catch { /* ignore */ }
            _callerCache[key] = res;
            return res;
        }

        private static object AudioProfileObj()
        {
            Type gam = FindType("GameAudioManager");
            if (gam == null) return null;
            object inst = gam.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public)?.GetValue(null);
            if (inst == null) return null;
            return Mem(inst, "Profile") ?? Mem(inst, "profile");
        }

        private static int ClipCount(object profile, string field)
        {
            if (profile == null) return -1;
            object v = Mem(profile, field);
            return v is Array a ? a.Length : (v == null ? 0 : -1);
        }

        /// <summary>One-time static audio facts: profile clip lists and who calls the swing / hit sound methods.</summary>
        private static void AudioStaticFacts(DMAuditEnemyResult R)
        {
            object prof = AudioProfileObj();
            int sw = ClipCount(prof, "weaponSwingClips"), hit = ClipCount(prof, "weaponHitClips"), crit = ClipCount(prof, "weaponCriticalHitClips");
            R.Add("Audio", "audio.profile", "GameAudioProfile combat clips assigned", prof == null ? "WARN" : (sw > 0 && hit > 0 ? "PASS" : "FAIL"),
                prof == null ? "GameAudioManager.Instance / profile not found at runtime" : $"weaponSwingClips {sw}, weaponHitClips {hit}, weaponCriticalHitClips {crit}",
                prof == null ? "audio manager not in the scene or created lazily" : (sw > 0 && hit > 0 ? "" : "GameAudioProfile asset has empty clip arrays: assign swish/hit clips (swing: " + sw + ", hit: " + hit + ")"));

            List<string> swingCallers = CodeCallers("PlayWeaponSwing(", "GameAudioManager.cs");
            R.Add("Audio", "audio.swing.callers", "Swing swish (GameAudioManager.PlayWeaponSwing) is called by gameplay code", swingCallers.Count > 0 ? "PASS" : "FAIL",
                $"callers outside GameAudioManager: {(swingCallers.Count == 0 ? "NONE" : string.Join(", ", swingCallers))}",
                swingCallers.Count > 0 ? "" : "PlayWeaponSwing / PlayPunchSwing are never called, so NO weapon (player or enemy) ever plays a swish: add a call where a melee attack window opens (vMeleeManager.SetActiveAttack active=true or the animation event OnEnableAttack)");

            List<string> hitCallers = CodeCallers("CombatHitAudio.PlayWeaponHit", "CombatHitAudio.cs");
            bool playerPath = hitCallers.Any(f => f.IndexOf("Player", StringComparison.OrdinalIgnoreCase) >= 0 || f.IndexOf("DamageBridge", StringComparison.OrdinalIgnoreCase) >= 0 || f.IndexOf("Survival", StringComparison.OrdinalIgnoreCase) >= 0 || f.IndexOf("vMeleeManager", StringComparison.OrdinalIgnoreCase) >= 0); // vMeleeManager.OnDamageHit is the enemy-weapon -> player hit path (confirmed at runtime by the Regression scenario row)
            R.Add("Audio", "audio.hit.callers", "Melee hit sound has a caller on the damage path", hitCallers.Count > 0 ? (playerPath ? "PASS" : "WARN") : "FAIL",
                $"CombatHitAudio.PlayWeaponHit callers: {(hitCallers.Count == 0 ? "NONE" : string.Join(", ", hitCallers))}",
                hitCallers.Count == 0 ? "no code plays a melee hit sound" : (playerPath ? "" : "the only callers are the enemy-body receiver (PioneerInvectorDamageReceiver), MeleeCombatController and CombatHitResolver: damage TO THE PLAYER (PioneerInvectorDamageBridge / the player's vDamageReceiver) has no hit-sound call, so enemy weapons hitting the player are silent"));
        }

        /// <summary>Static per-weapon audio: does the drawn weapon prefab carry hit sounds (vHitEffects)?</summary>
        private static string WeaponHitSoundInfo(Live L, out bool hasHitSounds)
        {
            hasHitSounds = false;
            if (L == null || L.go == null) return "no enemy";
            var w = L.mm != null ? L.mm.rightWeapon ?? L.mm.leftWeapon : null;
            if (w == null) return "no drawn melee weapon";
            var fx = w.GetComponent<vHitEffects>();
            if (fx == null) return $"weapon '{w.name}' has no vHitEffects component";
            hasHitSounds = fx.audioSource != null && fx.hitSounds != null && fx.hitSounds.Length > 0;
            return $"vHitEffects on '{w.name}': audioSource prefab {(fx.audioSource != null ? "set" : "NULL")}, hitSounds {(fx.hitSounds != null ? fx.hitSounds.Length : 0)}, recoilSounds {(fx.recoilSounds != null ? fx.recoilSounds.Length : 0)}, defSounds {(fx.defSounds != null ? fx.defSounds.Length : 0)}";
        }

        /// <summary>Evaluates a finished melee phase (swing + hit on the player) and adds the audio rows.</summary>
        private static void AddEnemyMeleeAudioRows(DMAuditEnemyResult R, string weaponName, Live L)
        {
            if (L == null) return;
            float attackAbs = L.firstAttackT >= 0f ? L.t0 + L.firstAttackT : -1f;
            float hitAbs = L.firstDamageT >= 0f ? L.t0 + L.firstDamageT : -1f;
            string info = WeaponHitSoundInfo(L, out bool hasHitSounds);
            if (attackAbs >= 0f)
            {
                // The swish plays when the attack window (hit box) opens, which for fast weapons is the same frame as the impact: allow it up to
                // 0.1s after the hit, but then require a second start so the swish is not confused with the hit sound.
                float end = hitAbs >= 0f ? hitAbs + 0.1f : attackAbs + 0.9f;
                var swingAll = AudioProbe.Window(attackAbs - 0.1f, Mathf.Max(attackAbs + 0.15f, end));
                var swing = hitAbs >= 0f ? swingAll.Where(a => a.t < hitAbs - 0.02f).ToList() : swingAll;
                if (hitAbs >= 0f && swing.Count == 0 && swingAll.Count >= 2) swing = swingAll.Take(1).ToList();
                R.Add("Audio", "audio.swing." + weaponName, $"'{weaponName}' swing plays a swish", swing.Count > 0 ? "PASS" : "FAIL",
                    $"new audio between attack start and impact: {AudioProbe.Describe(swing)}",
                    swing.Count > 0 ? "" : "no AudioSource started during the swing: gameplay never calls GameAudioManager.PlayWeaponSwing (no caller), the weapon has no swing clip, or the attack animation has no sound event");
            }

            if (hitAbs >= 0f)
            {
                var hit = AudioProbe.Window(hitAbs - 0.05f, hitAbs + 0.5f);
                R.Add("Audio", "audio.hitplayer." + weaponName, $"'{weaponName}' hitting the player plays a hit sound", hit.Count > 0 ? "PASS" : "FAIL",
                    $"new audio at impact: {AudioProbe.Describe(hit)}; {info}",
                    hit.Count > 0 ? "" : (hasHitSounds ? "weapon has hit sounds but nothing played on the player: vHitEffects.PlayHitEffects is only wired to vMeleeWeapon.onDamageHit and the audioSource prefab/Listener distance may mute it" : "weapon vHitEffects has no hit sounds or audioSource prefab (or no vHitEffects), and the player's damage path never calls CombatHitAudio.PlayWeaponHit"));
            }
        }
    }
}
#endif
