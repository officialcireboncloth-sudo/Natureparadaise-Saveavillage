"""Rebuild the current Cozy revision into this original master and FBX path."""
from pathlib import Path
import runpy
root=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir())
runpy.run_path(str(root/'Assets/Nature  Paradaise/Source~/MarketCart/build_market_cart.py'),run_name='__main__')
