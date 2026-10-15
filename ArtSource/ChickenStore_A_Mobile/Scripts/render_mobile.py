from pathlib import Path
ART=Path(__file__).resolve().parents[1];ROOT=ART.parents[1]
code=(ROOT/'ArtSource/ChickenStore_A/Scripts/render_chicken_store.py').read_text()
code=code.replace('ChickenStore_A.blend','ChickenStore_Mobile_A.blend').replace('01 Chicken Store - Editable','01 Chicken Store MOBILE - Editable').replace('02 Coop Companion - Editable','02 Coop MOBILE - Editable').replace('00 Chicken Store + Coop Overview','00 Chicken Store MOBILE Overview')
exec(compile(code,__file__,'exec'),{'__file__':__file__})
