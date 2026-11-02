"""Rebuild the current Cozy revision into this original master and FBX path."""
from pathlib import Path
import runpy
root=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir())
revision=runpy.run_path(str(root/'Assets/Nature  Paradaise/Source~/MarketPropsCozy/build_market_props_cozy.py'))
revision['shipping']()
