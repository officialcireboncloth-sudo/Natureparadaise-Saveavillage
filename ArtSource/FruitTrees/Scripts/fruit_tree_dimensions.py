import re

from fruit_tree_atlas import ASSETS


PLAYER_HEIGHT = float(re.search(
    r'\bconst float Height\s*=\s*([\d.]+)f',
    (ASSETS / 'Script/Player/PlayerDimensions.cs').read_text(),
)[1])
AVOCADO_REFERENCE_HEIGHT = 1.5 * PLAYER_HEIGHT
ORCHARD_HEIGHT = AVOCADO_REFERENCE_HEIGHT * 1.25
