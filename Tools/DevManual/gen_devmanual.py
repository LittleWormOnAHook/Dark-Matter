# Dev Manual generator: python Tools/DevManual/gen_devmanual.py  (regenerates tables from code; notes live in notes.md blocks)
import os,re,io,datetime
R=os.path.abspath(os.path.join(os.path.dirname(__file__),"..",".."))
OUT=os.path.join(R,"Assets","_Project","Documentation","DevManual")
NOTES=os.path.join(os.path.dirname(__file__),"notes")
def rd(p):
    with open(p,"rb") as f: return f.read().decode("utf-8-sig","replace")
def rel(p): return os.path.relpath(p,R).replace("\\","/")
def esc(s): return (s or "").replace("|","\\|").replace("\n"," ").strip()
# ---------- index of scripts ----------
guid2cs={}; cls2cs={}; allcs=[]
for dp,dn,fn in os.walk(os.path.join(R,"Assets")):
    for f in fn:
        if f.endswith(".cs"):
            p=os.path.join(dp,f); allcs.append(p); cls2cs.setdefault(f[:-3],p)
        elif f.endswith(".cs.meta"):
            try:
                m=re.search(r"guid:\s*(\w+)",rd(os.path.join(dp,f)))
                if m: guid2cs[m.group(1)]=os.path.join(dp,f[:-5])
            except: pass
def asset_script(ap):
    p=os.path.join(R,ap)
    if not os.path.exists(p): return None
    with open(p,"rb") as f: head=f.read(4000).decode("utf-8","replace")
    m=re.search(r"m_Script:\s*\{fileID:\s*\d+,\s*guid:\s*(\w+)",head)
    return guid2cs.get(m.group(1)) if m else None
FIELD=re.compile(r"^(?P<attrs>(?:\s*\[[^\]]*(?:\([^)]*\))?[^\]]*\])*)\s*(?P<acc>public|private|protected|internal)?\s*(?P<type>[\w<>\[\],\.]+(?:\s*<[^>]*>)?)\s+(?P<name>\w+)\s*(?:=\s*(?P<def>[^;]+))?;\s*(?://.*)?$")
def fields(cs):
    t=rd(cs); rows=[]; grp=""; pend=""
    for raw in t.split("\n"):
        s=raw.strip()
        if not s or s.startswith("//"): continue
        if re.match(r"^(\[.*\])+$",s): pend+=s; continue
        m=FIELD.match(s)
        if not m:
            pend=""; continue
        attrs=pend+(m.group("attrs") or ""); pend=""
        if re.search(r"\b(static|const|readonly|return|new|var|using|class)\b",s.split("=")[0]): continue
        acc=m.group("acc") or ""
        if "SerializeField" not in attrs and acc!="public": continue
        if "HideInInspector" in attrs or "NonSerialized" in attrs: continue
        h=re.search(r'Header\("([^"]*)"\)',attrs)
        if h: grp=h.group(1)
        rg=re.search(r"Range\(([^,]+),([^)]+)\)",attrs); mn=re.search(r"\bMin\(([^)]+)\)",attrs)
        tt=re.search(r'Tooltip\("((?:[^"\\]|\\.)*)"',attrs)
        rows.append((grp,m.group("name"),m.group("type"),esc(m.group("def") or ""),(f"{rg.group(1).strip()}–{rg.group(2).strip()}" if rg else (f"≥{mn.group(1)}" if mn else "")),esc(tt.group(1) if tt else "")))
    return rows
def presets(cs):
    return sorted(set(re.findall(r"public\s+(?:static\s+)?void\s+(Apply\w*Preset\w*|Apply\w+)\s*\(",rd(cs))))
def ftable(cs,limit=400):
    rows=fields(cs)
    if not rows: return "_No serialized fields parsed._\n"
    o=io.StringIO(); o.write("| Group | Field | Type | Default | Range | Notes |\n|---|---|---|---|---|---|\n")
    for g,n,ty,d,r,tt in rows[:limit]: o.write(f"| {esc(g)} | `{n}` | {esc(ty)} | {d} | {r} | {tt} |\n")
    pr=presets(cs)
    if pr: o.write("\n**Preset / apply methods:** "+", ".join(f"`{p}()`" for p in pr)+"\n")
    return o.getvalue()
# ---------- registry ----------
reg=rd(os.path.join(R,"Assets/_Project/Editor/GenesisStudio/DMStudioRegistry.cs"))
cats=[]
for cm in re.finditer(r"new DMStudioCategory\(\s*\"([^\"]*)\",\s*\"([^\"]*)\",\s*\"[^\"]*\",\s*\"([^\"]*)\"",reg):
    cats.append({"id":cm.group(1),"label":cm.group(2),"desc":cm.group(3),"pos":cm.start(),"subs":[]})
for i,c in enumerate(cats):
    end=cats[i+1]["pos"] if i+1<len(cats) else len(reg)
    block=reg[c["pos"]:end]
    parts=re.split(r"new DMStudioSubtab\(",block)[1:]
    for p in parts:
        p=p.split("new DMStudioSubtab(")[0]
        lits=re.findall(r'(?<![:\w])\s*"((?:[^"\\]|\\.)*)"',p)
        named=dict(re.findall(r'(\w+):\s*"((?:[^"\\]|\\.)*)"',p))
        mode=re.search(r"DMStudioPanelMode\.(\w+)",p)
        pos=[l for l in re.findall(r'(?<!: )"((?:[^"\\]|\\.)*)"',p)]
        sub={"id":pos[0] if pos else "","label":pos[1] if len(pos)>1 else "","desc":pos[2] if len(pos)>2 else "","mode":mode.group(1) if mode else ""}
        sub["asset"]=named.get("assetPath") or (pos[3] if len(pos)>3 and pos[3].startswith("Assets/") else "")
        sub["folder"]=named.get("searchFolder",""); sub["type"]=named.get("typeFilter",""); sub["menu"]=named.get("externalMenuPath","")
        c["subs"].append(sub)
# ---------- menus ----------
menus=[]
for p in allcs:
    if "FORGE3D" in p: continue
    try: t=rd(p)
    except: continue
    for m in re.finditer(r'MenuItem\(\s*"([^"]+)"(?:\s*,\s*(true|false))?',t):
        if m.group(2)=="true": continue
        path=m.group(1)
        if path.startswith(("Tools/Dark Matter Genesis/","GameObject/Dark Matter","GameObject/Genesis","Assets/Dark Matter")):
            menus.append((path,rel(p)))
menus=sorted(set(menus))
def note(key):
    p=os.path.join(NOTES,key+".md")
    return rd(p).strip()+"\n" if os.path.exists(p) else ""
FULL={"world","combat","environment"}
ORDER=["world","combat","environment","survival","player","companions","crafting","controls","ui","building","audio","rendering"]
cmap={c["id"]:c for c in cats}
files={}; az=[]; assets=[]
def anchor(s): return re.sub(r"[^a-z0-9 -]","",s.lower()).strip().replace(" ","-")
num=2
for cid in ORDER:
    if cid not in cmap: continue
    c=cmap[cid]; fn=f"{num:02d}-{c['label'].replace(' ','-').replace('&','and')}.md"; num+=1
    o=io.StringIO()
    st="Full (generated from code + reviewed notes)" if cid in FULL else "Stub (generated tables; notes pending)"
    o.write(f"# {c['label']} — Genesis Studio\n\n**Status:** {st}  \n**Open:** Tools → Dark Matter Genesis → Genesis Studio → **{c['label']}**  \n{c['desc']}\n\n")
    o.write(note(cid)); seen={}
    o.write("## Tabs\n\n"+"".join(f"- [{s['label']}](#{anchor(s['label'])})\n" for s in c["subs"])+"\n")
    for s in c["subs"]:
        o.write(f"## {s['label']}\n\n**What it does:** {esc(s['desc'])}\n\n**Where:**\n- Studio: `Genesis Studio / {c['label']} / {s['label']}` (panel mode `{s['mode']}`)\n")
        cs=None
        if s["asset"]:
            o.write(f"- Asset: `{s['asset']}`\n"); assets.append((s["asset"],f"{c['label']} / {s['label']}",fn)); cs=asset_script(s["asset"])
        if s["folder"]:
            o.write(f"- Asset folder: `{s['folder']}` (`{s['type']}`)\n"); assets.append((s["folder"]+"/",f"{c['label']} / {s['label']}",fn))
            if s["type"].startswith("t:"): cs=cls2cs.get(s["type"][2:])
        if s["menu"]: o.write(f"- Menu: `{s['menu']}`\n")
        if cs: o.write(f"- Script: `{rel(cs)}`\n")
        o.write("\n"+note(cid+"__"+s["id"]))
        if cs and cs in seen: o.write(f"**Key settings:** same script as [{seen[cs]}](#{anchor(seen[cs])}).\n\n")
        elif cs: seen[cs]=s["label"]; o.write("**Key settings** (from code):\n\n"+ftable(cs)+"\n")
        az.append((s["label"],fn,anchor(s["label"])))
    files[fn]=o.getvalue()
# ---------- Rocks ----------
o=io.StringIO()
o.write("# PCG Rocks & Rock Blend\n\n**Status:** Full (generated from code + reviewed notes)  \n**Open:** Tools → Dark Matter Genesis → PCG Rocks → *Rocks and Cliffs Studio* / *Rock Palette*\n\n")
o.write(note("rocks"))
o.write("## Menus\n\n"+"".join(f"- `{m}` — `{f}`\n" for m,f in menus if "/PCG Rocks/" in m or "Genesis PCG" in m)+"\n")
for sh in ["GenesisRockBlend.shader","GenesisRockBlendForward.shader"]:
    p=os.path.join(R,"Assets/Genesis PCG Rock Creation/Shaders",sh); t=rd(p)
    name=re.search(r'Shader\s+"([^"]+)"',t).group(1)
    o.write(f"## Shader `{name}`\n\n- File: `{rel(p)}`\n- Inspector: `GenesisRockBlendGUI`\n\n| Property | Label | Type / Range | Default |\n|---|---|---|---|\n")
    for m in re.finditer(r'^\s*(?:\[[^\]]*\]\s*)*(_\w+)\s*\(\s*"([^"]*)"\s*,\s*([^)]*\)?)\s*\)\s*=\s*([^\n]+)$',t,re.M):
        if m.group(1).startswith("_GRB") or m.group(1) in("_AlphaCutoff",):
            o.write(f"| `{m.group(1)}` | {esc(m.group(2))} | {esc(m.group(3))} | {esc(m.group(4))} |\n")
    o.write("\n")
for cls in ["PcgRockStudioWindow","DmRockBakeScheduler","PcgTerrainBindWatcher","GenesisRockBlendSceneFade"]:
    p=cls2cs.get(cls)
    if p: o.write(f"### `{cls}`\n\n- `{rel(p)}`\n\n"+(ftable(p) if fields(p) else "")+"\n")
files[f"{num:02d}-PCG-Rocks.md"]=o.getvalue(); az.append(("PCG Rocks / Rock Blend",f"{num:02d}-PCG-Rocks.md","")); num+=1
# ---------- Menus page ----------
o=io.StringIO(); o.write("# Menu Index\n\nAll project menu items (generated). Root: **Tools/Dark Matter Genesis/** (normalised 2026-10-25a).\n\n| Menu path | Script |\n|---|---|\n")
for m,f in menus: o.write(f"| `{m}` | `{f}` |\n")
files[f"{num:02d}-Menu-Index.md"]=o.getvalue(); num+=1
# ---------- other studios / rules ----------
files["01-Getting-Started.md"]=note("getting_started") or "# Getting Started\n"
files[f"{num:02d}-Other-Studios.md"]=note("other_studios") or "# Other Studios\n"; num+=1
files[f"{num:02d}-Rules-and-Gotchas.md"]=note("rules") or "# Rules\n"; num+=1
# ---------- index ----------
o=io.StringIO(); today=datetime.date.today().isoformat()
o.write(f"# Dark Matter: Genesis — Dev Manual\n\n_Generated {today} by `Tools/DevManual/gen_devmanual.py`. Source of truth is this folder; the GitHub wiki mirrors it._\n\n## Sections\n\n")
for fn in sorted(files): o.write(f"- [{files[fn].splitlines()[0].lstrip('# ')}]({fn})\n")
o.write("\n## By Studio\n\n| Studio | Group | Tab | Page |\n|---|---|---|---|\n")
for cid in ORDER:
    if cid in cmap:
        c=cmap[cid]
        fn=[k for k in files if files[k].startswith(f"# {c['label']} —")][0]
        for s in c["subs"]: o.write(f"| Genesis Studio | {c['label']} | {s['label']} | [{fn}]({fn}#{anchor(s['label'])}) |\n")
o.write("| Rocks and Cliffs Studio / Rock Palette | PCG Rocks | — | [PCG Rocks](%s) |\n" % [k for k in files if "PCG-Rocks" in k][0])
o.write("| Building / Combat / UI Studio, Audit Hub, Prefab Creator | — | — | [Other Studios](%s) |\n" % [k for k in files if "Other-Studios" in k][0])
o.write("\n## A–Z\n\n"+"".join(f"- [{l}]({f}{'#'+a if a else ''})\n" for l,f,a in sorted(az,key=lambda x:x[0].lower()))+"\n")
o.write("## Menu paths\n\nSee [Menu Index](%s) (%d items).\n\n" % ([k for k in files if "Menu-Index" in k][0],len(menus)))
o.write("## Asset paths\n\n| Asset | Used by | Page |\n|---|---|---|\n"+"".join(f"| `{a}` | {u} | [{f}]({f}) |\n" for a,u,f in sorted(set(assets)))+"\n")
o.write("## Rules\n\nSee [Rules and Gotchas](%s).\n" % [k for k in files if "Rules" in k][0])
files["00-Index.md"]=o.getvalue()
os.makedirs(OUT,exist_ok=True)
for fn,t in files.items():
    with open(os.path.join(OUT,fn),"w",encoding="utf-8",newline="\n") as f: f.write(t)
print(len(files),"pages", len(menus),"menus", sum(len(c["subs"]) for c in cats),"tabs")
for fn in sorted(files): print(fn,len(files[fn]))
