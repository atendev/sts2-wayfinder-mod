import json,sys,glob,os
from collections import Counter
def summarize(path):
    d=json.load(open(path))
    out=[]
    for i,act in enumerate(d["map_point_history"]):
        c=Counter(); dmg={"monster":[], "elite":[], "boss":[]}; turns={"monster":[], "elite":[], "boss":[]}
        cards=relics=upg=gold=mhp=0; hp_end=None
        for pt in act:
            t=pt.get("map_point_type"); c[t]+=1
            ps=pt["player_stats"][0]
            rt=[r.get("room_type") for r in pt.get("rooms",[])]
            for r in pt.get("rooms",[]):
                if r.get("room_type") in dmg:
                    dmg[r["room_type"]].append(ps.get("damage_taken",0)); turns[r["room_type"]].append(r.get("turns_taken",0))
            cards+=len(ps.get("cards_gained",[]))
            relics+=sum(1 for r in ps.get("relic_choices",[]) if r.get("was_picked"))
            upg+=len(ps.get("upgraded_cards",[]))
            gold+=ps.get("gold_gained",0); mhp+=ps.get("max_hp_gained",0)
            hp_end=(ps.get("current_hp"),ps.get("max_hp"))
        f=lambda L: f"{len(L)}x avg{sum(L)/len(L):.1f}dmg/{sum(turns_[0])/max(1,len(turns_[0])):.1f}t" if L else "-"
        line=[f"A{i+1} pts={len(act)}"]
        for k in dmg:
            L=dmg[k]; T=turns[k]
            line.append(f"{k}:{len(L)} avgdmg={sum(L)/len(L):.1f} turns={sum(T)/len(T):.1f}" if L else f"{k}:0")
        line.append(f"cards+{cards} relics+{relics} upg+{upg} gold+{gold} maxhp+{mhp} hp_end={hp_end}")
        out.append("  ".join(line))
    p=d["players"][0]
    return f"{os.path.basename(path)} {p['character']} A{d['ascension']} win={d['win']} build={d['build_id']} deck={len(p['deck'])} relics={len(p['relics'])}\n  "+"\n  ".join(out)
for f in sys.argv[1:]:
    try: print(summarize(f))
    except Exception as e: print(f, "ERR", e)
