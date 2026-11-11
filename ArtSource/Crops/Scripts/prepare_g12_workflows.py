"""Register Orchid with the common export workflow; keep all previous groups intact."""
from g12_config import *
scripts=SOURCE/'Scripts'
t=(scripts/'build_g11_blender.py').read_text().replace('g11_config','g12_config').replace('g11_geometry','g12_geometry').replace('G11','G12').replace('Tulip','Orchid')
t=t.replace("revision='R03 spatial radial leaves and rounded elliptical Orchid'","revision='R01 reference Phalaenopsis, closed fleshy spatial leaves and articulated flowers'")
t=t.replace("n.inputs['Strength'].default_value=.22","n.inputs['Strength'].default_value=.30")
(scripts/'build_g12_blender.py').write_text(t)
print('G12_BUILD_WORKFLOW_READY',flush=True)
