from pathlib import Path
p = Path(r"Assets/_Project/Scripts/Prototypes/Weapons/DMWeaponMorphRuntime.cs")
t = p.read_text(encoding="utf-8")
old = '''            var all = Object.FindObjectsByType<DMWeaponMorphController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                DMWeaponMorphController c = all[i];
                if (c == null || c.BoundItem != drawn)
                    continue;
                if (!c.isActiveAndEnabled && !c.gameObject.activeInHierarchy)
                    continue;
                controller = c;
                Active = c;
                return true;
            }

            return false;'''
new = '''            var all = Object.FindObjectsByType<DMWeaponMorphController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            DMWeaponMorphController fallback = null;
            for (int i = 0; i < all.Length; i++)
            {
                DMWeaponMorphController c = all[i];
                if (c == null || c.BoundItem != drawn)
                    continue;
                // Prefer the mounted held visual (PioneerVisual_*) over world pickups sharing the same ItemData.
                string n = c.gameObject.name;
                bool mounted = n.StartsWith("PioneerVisual_", System.StringComparison.Ordinal)
                    || c.transform.root == equipment.transform.root;
                if (mounted)
                {
                    controller = c;
                    Active = c;
                    return true;
                }
                if (fallback == null)
                    fallback = c;
            }
            if (fallback != null)
            {
                controller = fallback;
                Active = fallback;
                return true;
            }
            return false;'''
if old not in t:
    raise SystemExit('needle missing for TryGetDrawnController')
p.write_text(t.replace(old, new, 1), encoding="utf-8", newline="\n")
print("runtime filter OK")
