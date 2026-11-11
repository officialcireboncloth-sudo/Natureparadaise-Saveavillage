"""Adapt registered texture and export infrastructure for a single tulip."""
from g11_config import *
scripts=SOURCE/'Scripts'
t=(scripts/'paint_g10_gimp.py').read_text().replace('g10_config','g11_config').replace('RadialFlowers_Source_Sheet.png','Tulip_Source_Sheet.png')
t=t.replace('SURFACES[:8]','SURFACES[:4]').replace('rh//4','rh//2').replace('if i<8:','if i<4:').replace('2048,2048','2048,1024').replace('G10_GIMP_PASS: 16 regions','G11_GIMP_PASS: 8 regions')
t+="\nif (GENERATED/'Tulip_R02_Surfaces.png').is_file():\n p=SOURCE/'Scripts/paint_g11_r02_gimp.py';exec(compile(p.read_text(),str(p),'exec'),{'__file__':str(p)})\n"
(scripts/'paint_g11_gimp.py').write_text(t)
t=(scripts/'build_g10_blender.py').read_text().replace('g10_config','g11_config').replace('g10_geometry','g11_geometry').replace('G10','G11').replace('(1.5-row)*1.9','0').replace('phaseAppearances=24,uniqueNewExports=24','phaseAppearances=6,uniqueNewExports=6')
t=t.replace("revision='R01 species-specific radial flowers'","revision='R02 rounded elliptical Tulip and curved leaves' if (GENERATED/'Tulip_R02_Surfaces.png').is_file() else 'R01 reference Tulip'")
(scripts/'build_g11_blender.py').write_text(t)
t=(scripts/'prepare_g10_unity_page.py').read_text().replace('g10_config','g11_config').replace('G10','G11');(scripts/'prepare_g11_unity_page.py').write_text(t)
print('G11_WORKFLOWS_READY',flush=True)
