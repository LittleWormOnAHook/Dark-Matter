#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Project.EditorTools.Audit
{
    [Serializable]
    public sealed class DMAuditCheck
    {
        public string id;        // e.g. "melee.damage"
        public string group;     // e.g. "Weapons"
        public string label;
        public string status;    // PASS FAIL WARN SKIP INFO
        public string value;     // measured value(s)
        public string cause;     // probable cause / note
    }

    [Serializable]
    public sealed class DMAuditClip
    {
        public string category;
        public string clip;
        public float length;
        public bool observed;
    }

    [Serializable]
    public sealed class DMAuditEnemyResult
    {
        public string name;
        public string prefabPath;
        public string definition;
        public string melee;
        public string ranged;
        public string note;
        public float seconds;
        public List<DMAuditCheck> checks = new List<DMAuditCheck>();
        public List<DMAuditClip> clips = new List<DMAuditClip>();
        public List<string> consoleLines = new List<string>();
        public int consoleErrors;
        public int consoleWarnings;
        public bool cached;       // reused from the unit store (unchanged since cachedFrom)
        public string cachedFrom;

        public void Add(string group, string id, string label, string status, string value, string cause = "")
        {
            checks.Add(new DMAuditCheck { group = group, id = id, label = label, status = status, value = value, cause = cause });
        }

        public DMAuditCheck Get(string id)
        {
            foreach (DMAuditCheck c in checks)
                if (c.id == id)
                    return c;
            return null;
        }

        public string Status(string id)
        {
            DMAuditCheck c = Get(id);
            return c != null ? c.status : "-";
        }

        public int Count(string status)
        {
            int n = 0;
            foreach (DMAuditCheck c in checks)
                if (c.status == status)
                    n++;
            return n;
        }
    }

    [Serializable]
    public sealed class DMAuditTimingRow
    {
        public string unit; public string phase; public float seconds; public int frames;
        public float avgMs; public float maxMs; public float physicsMs; public float renderMs; public float scriptsMs;
    }

    [Serializable]
    public sealed class DMAuditRun
    {
        public string mode = "full";
        public float wallSeconds;
        public string timingNote;
        public List<DMAuditTimingRow> timings = new List<DMAuditTimingRow>();
        public List<string> findingsFixes = new List<string>();
        public string tool = "enemy-runtime";
        public string startedLocal;
        public string finishedLocal;
        public string scene;
        public string unityVersion;
        public string state;      // running / complete / aborted
        public string abortReason;
        public List<string> environment = new List<string>();
        public List<DMAuditEnemyResult> enemies = new List<DMAuditEnemyResult>();
        public List<DMAuditEnemyResult> weapons = new List<DMAuditEnemyResult>();
        public List<DMAuditEnemyResult> scenarios = new List<DMAuditEnemyResult>();
        public List<string> baselineChanges = new List<string>();
        public string baselineNote;

        public IEnumerable<KeyValuePair<string, DMAuditEnemyResult>> Scoped()
        {
            foreach (DMAuditEnemyResult e in enemies) yield return new KeyValuePair<string, DMAuditEnemyResult>("enemy", e);
            foreach (DMAuditEnemyResult e in weapons) yield return new KeyValuePair<string, DMAuditEnemyResult>("weapon", e);
            foreach (DMAuditEnemyResult e in scenarios) yield return new KeyValuePair<string, DMAuditEnemyResult>("scenario", e);
        }

        public int CountAll(string status)
        {
            int n = 0;
            foreach (var kv in Scoped()) n += kv.Value.Count(status);
            return n;
        }
    }

    [Serializable]
    public sealed class DMAuditBaselineEntry { public string key; public string status; }

    [Serializable]
    public sealed class DMAuditBaseline
    {
        public string created;
        public List<DMAuditBaselineEntry> entries = new List<DMAuditBaselineEntry>();
    }

    /// <summary>Markdown + JSON writer for the enemy runtime audit.</summary>
    public static class DMEnemyAuditReport
    {
        public const string Folder = "Assets/_Project/Documentation/Audits";
        public const string LatestMd = Folder + "/DM_Enemy_Runtime_Audit_Latest.md";
        public const string LatestJson = Folder + "/DM_Enemy_Runtime_Audit_Latest.json";

        private static readonly string[] ColumnIds =
        {
            "cfg", "vis", "unaware", "notice", "melee.damage", "ranged.damage", "player.dmg", "hitmarks", "death", "console"
        };
        private static readonly string[] ColumnNames =
        {
            "Config", "Weapons shown", "No circling", "Notice/chase", "Melee dmg", "Ranged dmg", "Player->enemy", "Hit marks", "Death/loot", "Console"
        };

        public const string BaselineJson = Folder + "/DM_Enemy_Runtime_Audit_Baseline.json";

        /// <summary>One-line pass/fail summary of the newest run (for the hub), or null.</summary>
        public static string SummaryLine(out int fails)
        {
            fails = -1;
            try
            {
                if (!File.Exists(LatestJson)) return null;
                DMAuditRun r = JsonUtility.FromJson<DMAuditRun>(File.ReadAllText(LatestJson));
                if (r == null) return null;
                fails = r.CountAll("FAIL");
                return $"{r.CountAll("PASS")} PASS / {fails} FAIL / {r.CountAll("WARN")} warn, {r.baselineChanges.Count} change(s) vs baseline ({r.state}, {r.finishedLocal})";
            }
            catch { return null; }
        }

        private static string KeyOf(string scope, DMAuditEnemyResult e, DMAuditCheck c) { return scope + "|" + e.name + "|" + c.id; }

        private static void CompareBaseline(DMAuditRun run, bool update)
        {
            run.baselineChanges.Clear();
            var cur = new Dictionary<string, string>();
            var scopes = new HashSet<string>();
            foreach (var kv in run.Scoped())
            {
                scopes.Add(kv.Key + "|" + kv.Value.name + "|");
                foreach (DMAuditCheck c in kv.Value.checks) cur[KeyOf(kv.Key, kv.Value, c)] = c.status;
            }

            DMAuditBaseline old = null;
            try { if (File.Exists(BaselineJson)) old = JsonUtility.FromJson<DMAuditBaseline>(File.ReadAllText(BaselineJson)); } catch { old = null; }
            if (old == null || old.entries.Count == 0)
            {
                run.baselineNote = "no baseline yet: this run was saved as the baseline";
                update = true;
            }
            else
            {
                var oldMap = new Dictionary<string, string>();
                foreach (var en in old.entries) { if (en.key.IndexOf("Lisa_Hybrid", StringComparison.OrdinalIgnoreCase) >= 0) continue; oldMap[en.key] = en.status; }
                foreach (var kv in cur)
                {
                    if (!oldMap.TryGetValue(kv.Key, out string was)) run.baselineChanges.Add("NEW " + kv.Key + " = " + kv.Value);
                    else if (was != kv.Value) run.baselineChanges.Add("CHANGED " + kv.Key + ": " + was + " -> " + kv.Value);
                }

                foreach (var kv in oldMap)
                    if (!cur.ContainsKey(kv.Key) && scopes.Any(sc => kv.Key.StartsWith(sc, StringComparison.Ordinal)))
                        run.baselineChanges.Add("MISSING " + kv.Key + " (was " + kv.Value + ")");
                run.baselineNote = $"compared with baseline from {old.created}: {run.baselineChanges.Count} change(s)" + (update ? "; baseline updated" : "");
            }

            if (update)
            {
                var nb = new DMAuditBaseline { created = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) };
                foreach (var kv in cur) nb.entries.Add(new DMAuditBaselineEntry { key = kv.Key, status = kv.Value });
                File.WriteAllText(BaselineJson, JsonUtility.ToJson(nb, true), new UTF8Encoding(false));
            }
        }

        public static string Write(DMAuditRun run, bool updateBaseline = false)
        {
            Directory.CreateDirectory(Folder);
            try { CompareBaseline(run, updateBaseline); } catch (Exception e) { run.baselineNote = "baseline compare failed: " + e.Message; }
            string md = BuildMarkdown(run);
            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);
            bool complete = run.state == "complete";
            string stamped = Folder + "/DM_Enemy_Runtime_Audit_" + stamp + (complete ? "" : "-aborted") + ".md";
            File.WriteAllText(stamped, md, new UTF8Encoding(false));
            if (!complete)
            {
                // an aborted/partial run never replaces Latest: Latest always points at the last COMPLETE report
                File.WriteAllText(Folder + "/DM_Enemy_Runtime_Audit_" + stamp + "-aborted.json", JsonUtility.ToJson(run, true), new UTF8Encoding(false));
                return stamped;
            }

            File.WriteAllText(LatestMd, md, new UTF8Encoding(false));
            File.WriteAllText(LatestJson, JsonUtility.ToJson(run, true), new UTF8Encoding(false));
            return LatestMd;
        }

        private static string Cell(DMAuditEnemyResult e, string id)
        {
            string s = e.Status(id);
            switch (s)
            {
                case "PASS": return "PASS";
                case "FAIL": return "**FAIL**";
                case "WARN": return "warn";
                case "SKIP": return "n/a";
                case "INFO": return "info";
                default: return "-";
            }
        }

        public static string BuildMarkdown(DMAuditRun run)
        {
            var sb = new StringBuilder(32 * 1024);
            sb.AppendLine("# Dark Matter Genesis - Enemy Runtime Audit");
            sb.AppendLine();
            sb.AppendLine($"- Run: {run.startedLocal} -> {run.finishedLocal} ({run.state}{(string.IsNullOrEmpty(run.abortReason) ? "" : ": " + run.abortReason)})");
            sb.AppendLine($"- Unity {run.unityVersion}, scene `{run.scene}` (never saved by the audit)");
            foreach (string env in run.environment)
                sb.AppendLine("- " + env);
            sb.AppendLine($"- Mode: **{run.mode}**, wall time {(int)(run.wallSeconds / 60f)}m{(int)(run.wallSeconds % 60f):00}s");
            sb.AppendLine();

            int fails = 0, warns = 0;
            foreach (DMAuditEnemyResult e in run.enemies)
            {
                fails += e.Count("FAIL");
                warns += e.Count("WARN");
            }

            sb.AppendLine($"**{run.enemies.Count} enemies audited - {fails} FAIL, {warns} warn.** Statuses: PASS / **FAIL** / warn (suspicious or inconclusive) / n/a.");
            sb.AppendLine($"**Overall (enemies + {run.weapons.Count} weapon cases + {run.scenarios.Count} scenarios): {run.CountAll("PASS")} PASS, {run.CountAll("FAIL")} FAIL, {run.CountAll("WARN")} warn.**");
            sb.AppendLine();
            if (!string.IsNullOrEmpty(run.baselineNote))
            {
                sb.AppendLine("## Changes since baseline");
                sb.AppendLine();
                sb.AppendLine(run.baselineNote + ".");
                sb.AppendLine();
                foreach (string ch in run.baselineChanges.Take(80)) sb.AppendLine("- " + ch);
                if (run.baselineChanges.Count > 80) sb.AppendLine($"- ... {run.baselineChanges.Count - 80} more");
                sb.AppendLine();
            }

            AppendTiming(sb, run);
            AppendRanked(sb, run);

            sb.AppendLine("## Summary table");
            sb.AppendLine();
            sb.Append("| Enemy | Melee | Ranged |");
            foreach (string n in ColumnNames)
                sb.Append(" ").Append(n).Append(" |");
            sb.AppendLine();
            sb.Append("|---|---|---|");
            foreach (string _ in ColumnNames)
                sb.Append("---|");
            sb.AppendLine();
            foreach (DMAuditEnemyResult e in run.enemies)
            {
                sb.Append($"| {e.name} | {e.melee} | {e.ranged} |");
                foreach (string id in ColumnIds)
                    sb.Append(" ").Append(Cell(e, id)).Append(" |");
                sb.AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine("## Weapons that deal NO damage (headline)");
            sb.AppendLine();
            bool any = false;
            foreach (DMAuditEnemyResult e in run.enemies)
            {
                foreach (string id in new[] { "melee.damage", "ranged.damage" })
                {
                    DMAuditCheck c = e.Get(id);
                    if (c != null && c.status == "FAIL")
                    {
                        any = true;
                        sb.AppendLine($"- **{e.name}** - {(id == "melee.damage" ? "melee `" + e.melee + "`" : "ranged `" + e.ranged + "`")}: {c.value}. **Probable cause:** {c.cause}");
                    }
                }
            }

            foreach (DMAuditEnemyResult w in run.weapons)
            {
                DMAuditCheck c = w.Get("w.damage");
                if (c != null && c.status == "FAIL")
                {
                    any = true;
                    sb.AppendLine($"- **Weapon matrix: {w.name}** ({w.note}): {Esc(c.value.Length > 300 ? c.value.Substring(0, 300) + "..." : c.value)}. **Probable cause:** {c.cause}");
                }
            }

            if (!any)
                sb.AppendLine("- none detected in this run.");
            sb.AppendLine();

            if (run.weapons.Count > 0)
            {
                sb.AppendLine("## Weapon matrix (every weapon item on the template, spawn override)");
                sb.AppendLine();
                int wf = 0;
                foreach (DMAuditEnemyResult w in run.weapons) wf += w.Count("FAIL");
                sb.AppendLine($"{run.weapons.Count} cases, {wf} FAIL. Each weapon is equipped on `HumanoidEnemy_Invector` through ConfigureSpawnLoadout (the spawner path) and made to attack the player.");
                sb.AppendLine();
                sb.AppendLine("| Weapon | Kind | Equips | Damages player | One drawn | Measured | Probable cause |");
                sb.AppendLine("|---|---|---|---|---|---|---|");
                foreach (DMAuditEnemyResult w in run.weapons)
                {
                    DMAuditCheck dmg = w.Get("w.damage");
                    string meas = dmg != null ? dmg.value : string.Join(" ; ", w.checks.Where(c => c.id.StartsWith("ovr")).Select(c => c.label + ": " + c.status));
                    string why = string.Join(" | ", w.checks.Where(c => c.status == "FAIL" && !string.IsNullOrEmpty(c.cause)).Select(c => c.cause).Distinct());
                    sb.AppendLine($"| {w.name} | {w.note} | {Cell(w, "w.equip")} | {Cell(w, "w.damage")} | {Cell(w, "vis")} | {Esc(meas.Length > 260 ? meas.Substring(0, 260) + "..." : meas)} | {Esc(why)} |");
                }

                sb.AppendLine();
                foreach (DMAuditEnemyResult w in run.weapons)
                {
                    sb.AppendLine($"### Weapon: {w.name}");
                    sb.AppendLine();
                    sb.AppendLine("| Group | Check | Status | Measured | Probable cause / note |");
                    sb.AppendLine("|---|---|---|---|---|");
                    foreach (DMAuditCheck c in w.checks)
                        sb.AppendLine($"| {c.group} | {c.label} | {c.status} | {Esc(c.value)} | {Esc(c.cause)} |");
                    if (w.consoleLines.Count > 0)
                    {
                        sb.AppendLine();
                        foreach (string l in w.consoleLines.Take(8))
                            sb.AppendLine("- `" + Esc(l) + "`");
                    }

                    sb.AppendLine();
                }
            }

            if (run.scenarios.Count > 0)
            {
                sb.AppendLine("## Scenarios (block/parry, movement, multi-enemy, player weapons, performance)");
                sb.AppendLine();
                sb.AppendLine("| Scenario | PASS | FAIL | warn | time |");
                sb.AppendLine("|---|---|---|---|---|");
                foreach (DMAuditEnemyResult sc in run.scenarios)
                    sb.AppendLine($"| {sc.name} | {sc.Count("PASS")} | {sc.Count("FAIL")} | {sc.Count("WARN")} | {sc.seconds:F0}s |");
                sb.AppendLine();
                foreach (DMAuditEnemyResult sc in run.scenarios)
                {
                    sb.AppendLine($"### Scenario: {sc.name}");
                    sb.AppendLine();
                    sb.AppendLine("| Group | Check | Status | Measured | Probable cause / note |");
                    sb.AppendLine("|---|---|---|---|---|");
                    foreach (DMAuditCheck c in sc.checks)
                        sb.AppendLine($"| {c.group} | {c.label} | {c.status} | {Esc(c.value)} | {Esc(c.cause)} |");
                    if (sc.consoleLines.Count > 0)
                    {
                        sb.AppendLine();
                        foreach (string l in sc.consoleLines.Take(8)) sb.AppendLine("- `" + Esc(l) + "`");
                    }

                    sb.AppendLine();
                }
            }

            sb.AppendLine("## Per-enemy detail");
            foreach (DMAuditEnemyResult e in run.enemies)
            {
                sb.AppendLine();
                sb.AppendLine($"### {e.name}");
                sb.AppendLine();
                sb.AppendLine($"Prefab `{e.prefabPath}` - definition `{e.definition}` - melee `{e.melee}` / ranged `{e.ranged}` - audited in {e.seconds:F0}s{(string.IsNullOrEmpty(e.note) ? "" : " - " + e.note)}");
                sb.AppendLine();
                sb.AppendLine("| Group | Check | Status | Measured | Probable cause / note |");
                sb.AppendLine("|---|---|---|---|---|");
                foreach (DMAuditCheck c in e.checks)
                    sb.AppendLine($"| {c.group} | {c.label} | {c.status} | {Esc(c.value)} | {Esc(c.cause)} |");

                if (e.clips.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("Animation clip timings (controller clips; * = seen playing during the run):");
                    sb.AppendLine();
                    sb.AppendLine("| Category | Clip | Length (s) |");
                    sb.AppendLine("|---|---|---|");
                    foreach (DMAuditClip cl in e.clips.OrderBy(x => x.category).ThenBy(x => x.clip))
                        sb.AppendLine($"| {cl.category} | {cl.clip}{(cl.observed ? " *" : "")} | {cl.length:F2} |");
                }

                if (e.consoleLines.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine($"Console during run: {e.consoleErrors} error(s), {e.consoleWarnings} warning(s):");
                    foreach (string l in e.consoleLines.Take(12))
                        sb.AppendLine("- `" + Esc(l) + "`");
                }
            }

            sb.AppendLine();
            sb.AppendLine("## Manual checklist (things only a human can judge)");
            sb.AppendLine();
            sb.AppendLine("- [ ] Weapon pose: the drawn weapon sits in the hand correctly (grip, no floating/offset blade) for every enemy.");
            sb.AppendLine("- [ ] Attack animations look right and the visual swing lines up with when damage lands (no hits from a long way off, no whiffs while visibly connecting).");
            sb.AppendLine("- [ ] Ranged enemies: muzzle position/flash and tracer origin match the weapon; aim looks natural.");
            sb.AppendLine("- [ ] Idle/patrol/walk/run animation quality; no foot sliding or crossed-arm idle.");
            sb.AppendLine("- [ ] Hit-mark art (decals, glow) reads correctly on the body type; hit reactions/stagger feel right.");
            sb.AppendLine("- [ ] Death: ragdoll/dissolve looks right, loot bag pickup works, no weapon left floating.");
            sb.AppendLine("- [ ] Audio: swing, impact, shot, hurt, death sounds present and not doubled.");
            sb.AppendLine("- [ ] Difficulty feel: time-to-notice, chase speed and attack cadence are fair against the measured numbers above.");
            sb.AppendLine("- [ ] Check the Game view during a run for any menu/loading overlay still covering the screen.");
            sb.AppendLine();
            sb.AppendLine("_Generated by Tools > Dark Matter Genesis > Audit > Run Enemy Runtime Audit (editor-only)._");
            return sb.ToString();
        }


        private static void AppendTiming(StringBuilder sb, DMAuditRun run)
        {
            sb.AppendLine("## Timing");
            sb.AppendLine();
            sb.AppendLine($"Wall time {run.wallSeconds:0}s ({run.wallSeconds / 60f:0.0} min), mode {run.mode}. {run.timingNote}");
            sb.AppendLine();
            sb.AppendLine("| Unit | Scope | Seconds | Source |");
            sb.AppendLine("|---|---|---|---|");
            foreach (var kv in run.Scoped())
                sb.AppendLine($"| {kv.Value.name} | {kv.Key} | {kv.Value.seconds:0.0} | {(kv.Value.cached ? "cached (unchanged since " + kv.Value.cachedFrom + ")" : "ran")} |");
            sb.AppendLine();
            if (run.timings.Count > 0)
            {
                sb.AppendLine("Top phases (frame split per phase: avg/max frame ms, physics / render / scripts ms per frame):");
                sb.AppendLine();
                sb.AppendLine("| Unit | Phase | Seconds | Frames | Avg ms | Max ms | Physics | Render | Scripts |");
                sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
                foreach (var t in run.timings.OrderByDescending(x => x.seconds).Take(30))
                    sb.AppendLine($"| {t.unit} | {t.phase} | {t.seconds:0.0} | {t.frames} | {t.avgMs:0.0} | {t.maxMs:0.0} | {t.physicsMs:0.00} | {t.renderMs:0.00} | {t.scriptsMs:0.00} |");
                sb.AppendLine();
                float total = run.timings.Sum(x => x.seconds);
                int fr = run.timings.Sum(x => x.frames);
                if (fr > 0)
                    sb.AppendLine($"Whole run (ran units only): {total:0}s over {fr} frames, avg frame {run.timings.Sum(x => x.avgMs * x.frames) / fr:0.0} ms, physics {run.timings.Sum(x => x.physicsMs * x.frames) / fr:0.00} ms, render {run.timings.Sum(x => x.renderMs * x.frames) / fr:0.00} ms, scripts {run.timings.Sum(x => x.scriptsMs * x.frames) / fr:0.00} ms per frame.");
                sb.AppendLine();
            }
        }

        private static int Impact(string scope, DMAuditCheck c)
        {
            string id = c.id ?? "";
            int s = 20;
            if (id == "melee.damage" || id == "ranged.damage" || id == "w.damage" || id.StartsWith("pw.")) s = 100;
            else if (id == "player.dmg" || id == "death") s = 90;
            else if (id == "notice" || id == "vis") s = 85;
            else if (id == "unaware" || id == "console") s = 80;
            else if (id == "w.equip" || id.StartsWith("silent.")) s = 75;
            else if (id.StartsWith("blk.") || id == "eblk") s = 70;
            else if (id == "multi") s = 55;
            else if (id == "hitmarks") s = 50;
            else if (id.StartsWith("mv.")) s = 45;
            else if (id.StartsWith("parry") || id == "eparry") s = 40;
            else if (id.StartsWith("perf.")) s = 30;
            else if (id == "cfg") s = 60;
            if (c.status == "WARN") s -= 25;
            return s;
        }

        private static string FixHint(DMAuditCheck c)
        {
            string id = c.id ?? "";
            string cause = c.cause ?? "";
            if (cause.Contains("0 weapon hit events")) return "Check the weapon's hit box: collider layer/tag against the player's hit tags, animation events that open the hitbox (ActiveDamage/OnEnableAttack), and that the weapon's vMeleeWeapon damage and the item damage are > 0.";
            if (id == "melee.damage" || id == "w.damage") return "Open the weapon prefab: hit box collider (isTrigger, layer), vMeleeWeapon.damage, hitDamageTags; confirm the attack animation sends the damage-window events; then re-run the matrix.";
            if (id == "ranged.damage" || id.StartsWith("pw.")) return "Check ammo/magazine (reload), shooter manager weapon binding, projectile damage mapping and the receiver on the target; make sure the ItemData damage is > 0.";
            if (id == "console") return "Fix the exception at the listed stack frame (null-guard or initialise the field); it repeats on every spawn.";
            if (id.StartsWith("silent.")) return "Repair the prefab (re-add the script / fix the material / assign controller+avatar) via the Character Creator Fix or by hand.";
            if (id == "unaware") return "Make patrol/wander anchors world-space and gate engagement/orbit logic behind a valid target.";
            if (id == "notice") return "Review senses (vision range/FOV/LOS) and aggro rules; check the EnemySenses reference on the prefab.";
            if (id == "vis") return "Let EnemyInvectorLoadoutBridge be the only weapon authority; hide every Drawn_/Holstered_ slot except the resolved ones.";
            if (id == "death") return "Check EnemyLootable/EnemyLootBag wiring and destroyDelay in the definition.";
            if (id == "hitmarks") return "Add DMEnemyHitMarks + the layer-26 hitbox rig; verify the body-type hit-mark profile.";
            if (id.StartsWith("blk.") || id == "eblk") return "Verify the block input reaches the controller (isBlocking), meleeBlockDefenseHalfAngle, TryAbsorbBlockedMelee, and the enemy defense rate.";
            if (id.StartsWith("mv.")) return "Tune AI turn speed / personal space / lose-target time in the definition or brain profile.";
            if (id == "multi") return "Add enemy separation and make the attack token rotate between enemies.";
            if (id.StartsWith("perf.")) return "Reduce spawn-time work/allocations (pool or cache lookups done in Awake/Start/Update).";
            return "Inspect the measured value and cause on the detail row.";
        }

        private static void AppendRanked(StringBuilder sb, DMAuditRun run)
        {
            var groups = new Dictionary<string, List<string>>();
            var rows = new Dictionary<string, DMAuditCheck>();
            var order = new Dictionary<string, int>();
            foreach (var kv in run.Scoped())
                foreach (DMAuditCheck c in kv.Value.checks)
                {
                    if (c.status != "FAIL" && c.status != "WARN") continue;
                    string key = c.id + "|" + c.status + "|" + (c.cause ?? "");
                    if (!groups.ContainsKey(key)) { groups[key] = new List<string>(); rows[key] = c; order[key] = Impact(kv.Key, c); }
                    groups[key].Add(kv.Value.name);
                }

            sb.AppendLine("## Findings ranked by player impact");
            sb.AppendLine();
            if (groups.Count == 0) { sb.AppendLine("- no failures or warnings in this run."); sb.AppendLine(); return; }
            sb.AppendLine("Ranked by what a player would notice (zero damage, dead/undead enemies, wrong weapons and AI first; perf and polish last). Identical causes are merged. Includes silent failures that raise no error.");
            sb.AppendLine();
            sb.AppendLine("| # | Impact | Status | Finding | Affects | Probable cause | Proposed fix |");
            sb.AppendLine("|---|---|---|---|---|---|---|");
            int n = 0;
            foreach (var key in order.OrderByDescending(x => x.Value).Select(x => x.Key).Take(60))
            {
                n++;
                DMAuditCheck c = rows[key];
                var who = groups[key].Distinct().ToList();
                string affects = who.Count > 4 ? string.Join(", ", who.Take(4)) + $" +{who.Count - 4} more" : string.Join(", ", who);
                string cause = (c.cause ?? "");
                if (cause.Length > 300) cause = cause.Substring(0, 300) + "...";
                sb.AppendLine($"| {n} | {order[key]} | {c.status} | {Esc(c.label)} | {Esc(affects)} | {Esc(cause)} | {Esc(FixHint(c))} |");
            }

            sb.AppendLine();
        }

        private static string Esc(string s)
        {
            return string.IsNullOrEmpty(s) ? "" : s.Replace("|", "/").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
#endif