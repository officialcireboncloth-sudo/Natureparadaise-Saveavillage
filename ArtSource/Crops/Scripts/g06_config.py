"""G06 shares the Trellis material; its edited tiles were unused by prior groups."""
from plant_library import ROOT, A, SOURCE, TEXTURES

ATLAS = 'NP_Plants_Trellis_01'
GROUP = SOURCE / 'G06'
REVIEW = GROUP / 'Review'
MASTER = GROUP / 'Blender/G06_RootVegetables_Master.blend'
GENERATED = SOURCE / 'Textures/Generated/G06'
MODELS = A / 'mesh/Crops/G06'
CHANGED = ['LeafLobed', 'LeafStrap', 'RadishGradient', 'OnionSkin', 'RadishCut', 'OnionCut', 'RootCream']
SMOOTH = dict(LeafLobed=.29, LeafStrap=.31, RadishGradient=.52, OnionSkin=.27, RadishCut=.25, OnionCut=.30, RootCream=.20)
PALETTE = {
    'LeafLobed': ('#69a921', '#a1cb49', '#387b15'),
    'LeafStrap': ('#559719', '#a9c943', '#2a690d'),
    'RadishGradient': ('#e71932', '#fff0da', '#b90923'),
    'OnionSkin': ('#e99c3c', '#ffcf7a', '#bd641f'),
    'RadishCut': ('#f7eee2', '#fffaf1', '#e6cebb'),
    'OnionCut': ('#f3ead3', '#fff9e9', '#d9bc80'),
    'RootCream': ('#f1d9ac', '#fff1d0', '#ceb58a'),
}
PHASES = {
    'Radish': ['Seed', 'Sprout', 'Young', 'RootForming', 'Ready', 'Harvest'],
    'Onion': ['Seed', 'Sprout', 'Young', 'BulbForming', 'Ready', 'Harvest'],
}
