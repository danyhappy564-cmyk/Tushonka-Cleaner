"""허용된 이름 CSV만으로 규칙과 맵별 예상 후보 집계를 재생성합니다.
CSV 평균 크기는 실물별 크기나 부모 컴포넌트를 증명하지 않습니다.
실제 숨김은 C#의 추가 안전 검사를 통과해야 합니다.
"""
import csv
import fnmatch
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
# 이 패턴은 이번 CSV를 직접 읽은 결과에서 선정했습니다. 부분 문자열 glass/food/debris는 쓰지 않습니다.
FAMILIES = [
    ("Litter", 2.5, ["garbage_bag*", "TrashBag_*", "Package_garbage_set_*", "sack_construction_garbage_*", "trash_pile_1x1_*", "trash_pile_05x1*", "goshan_garbage*"]),
    ("Rubble", 3.0, ["plaster_debris_*", "debris_concrete_*", "debris_rubble_pile_*"]),
    ("Casings", 3.0, ["rifle_shells_grp", "shotshell_grp", "rifleshell_grp"]),
    ("Food", 1.25, ["vegetables_scattered_*", "vegetables_tomato*", "vegetables_cucumber_*", "Pizza*_LOD"]),
    ("Puddles", 6.0, ["Puddle_atlas_*_LOD", "puddle", "puddle_oil"]),
    ("Glass", 3.5, ["Glass_Shards_*_LOD", "glass_debris_LOD", "glass_crush01_LOD", "shards_glass"]),
    ("Decals", 6.0, ["decal_dirt", "decal_drip", "decal_paper", "Sand_decal_*_LOD", "Dust_decal_*_LOD", "Ground_Decal_*_LOD", "Reserve_decal_footstep*", "goshan_decal*", "Plaster_debris_decal*", "plaster_debris_decal*", "City_Food_trash_pile_decal_LOD"]),
]
CATEGORIES = [x[0] for x in FAMILIES]
DENY = ["*door*", "*window*", "item_*", "*loot*", "*interactive*", "*weapon*", "*quest*", "*switch*", "*extract*", "*corpse*", "*player*", "*pool*", "*fence*", "*lamp*", "*barrel*", "*rack*", "*basket*", "*decal*"]

def glob(pattern, name):
    return fnmatch.fnmatchcase(name.lower(), pattern.lower())

def main():
    rows = []
    sources = []
    for file in sorted((ROOT / "scene-names").glob("names-*.csv")):
        sources.append({"file": file.relative_to(ROOT).as_posix(), "sha256": hashlib.sha256(file.read_bytes()).hexdigest()})
        for row in csv.DictReader(file.open(encoding="utf-8-sig", newline="")):
            row["map"] = file.stem[6:]
            rows.append(row)
    # 하나의 정규화 이름이 어느 맵에서든 콜라이더와 관측되면 해당 이름 전체를 보수적으로 제외합니다.
    colliders = {r["name"].lower() for r in rows if float(r["with_collider"]) > 0}
    rules, selected = [], set()
    for category, limit, patterns in FAMILIES:
        # 대소문자만 다른 이름에도 전체 순서를 정합니다. set 순서에 따라 JSON이 바뀌지 않습니다.
        candidates = sorted({r["name"] for r in rows if r["renderer"] == "MeshRenderer" and 0 < float(r["avg_size_m"]) <= limit and any(glob(p, r["name"]) for p in patterns)}, key=lambda s: (s.lower(), s))
        for name in candidates:
            if name.lower() in selected or name.lower() in colliders:
                continue
            deny = DENY if category != "Decals" else [p for p in DENY if p != "*decal*"]
            if any(glob(p, name) for p in deny):
                continue
            # 관측된 이름만 허용합니다. 넓은 접두사 패턴 자체는 런타임 데이터로 내보내지 않습니다.
            rules.append({"category": category, "pattern": name, "renderer": "MeshRenderer", "maxSize": limit})
            selected.add(name.lower())
    doc = {"version": 1, "rules": rules}
    (ROOT / "data").mkdir(exist_ok=True)
    (ROOT / "data/scene-rules.json").write_text(json.dumps(doc, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
    byname = {r["pattern"].lower(): r for r in rules}
    counts = {m: {c: 0 for c in CATEGORIES} for m in sorted({r["map"] for r in rows})}
    audit = []
    for row in rows:
        rule = byname.get(row["name"].lower())
        if rule and row["renderer"] == rule["renderer"] and 0 < float(row["avg_size_m"]) <= rule["maxSize"]:
            counts[row["map"]][rule["category"]] += int(row["count"])
            audit.append({"map":row["map"], "name":row["name"], "category":rule["category"], "count":int(row["count"]), "avg_size_m":float(row["avg_size_m"]), "static":float(row["static"])})
    (ROOT / "data/rule-audit.json").write_text(json.dumps({"sources":sources,"counts":counts,"matches":audit},ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    header = "| 맵 | 쓰레기 | 잔해 | 탄피 | 장식 음식 | 물웅덩이 | 유리 조각 | 데칼 |"
    table = [header, "|---|---:|---:|---:|---:|---:|---:|---:|"]
    table += ["| " + m + " | " + " | ".join(str(counts[m][c]) for c in CATEGORIES) + " |" for m in counts]
    (ROOT / "data/counts.md").write_text("\n".join(table)+"\n",encoding="utf-8")
    print("rules:",len(rules),"maps:",len(counts),"CSV candidate count:",sum(sum(v.values()) for v in counts.values()))
    print("\n".join(table))

if __name__ == "__main__":
    main()
