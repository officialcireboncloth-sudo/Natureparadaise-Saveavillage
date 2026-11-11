"""Prove reference-derived teeth survive in the actual near meshes and Blueberry detail is restored."""
import sys,json
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))
# Reuse the strict file/atlas/binding boundary checks, allowing only this revision's one surface.
source=Path(__file__).with_name('verify_g01_bush_r04.py');prefix=source.read_text().split("bpy.ops.wm.open_mainfile(filepath=str(G.MASTER));",1)[0];prefix=prefix.replace('Bush_R04','Leaf_R05').replace("changed={'NP_Plants_Berries_Source_01:BlueberryLeaf','NP_Plants_Berries_Source_01:BlackberryLeaf','NP_Plants_Raspberry_Source_01:RaspberryLeaf'}","changed={'NP_Plants_Berries_Source_01:BlueberryLeaf'}");exec(compile(prefix,str(source),'exec'),globals())
slot=next(s for s in base['registryBefore']['slots'] if s['key']=='NP_Plants_Berries_Source_01:BlueberryLeaf');x,y,w,h=slot['pixelRectTopLeft']
for suffix in ['BaseColor','Normal']:
 current=arr(G.RUNTIME_TEXTURES/(U.NAME+'_'+suffix+'.png'));original=arr(G.REVIEW/'Bush_R04/Before/Assets/Nature  Paradaise/Textures/Shared/Plants'/(U.NAME+'_'+suffix+'.png'));check(np.array_equal(current[y:y+h,x:x+w],original[y:y+h,x:x+w]),'Blueberry '+suffix+' restored pixel-exactly from the detailed pre-R04 surface');del current,original
def nonleaf_signature():
 result={}
 for root in [o for o in bpy.data.objects if o.get('Phase')]:
  result[root.name]={}
  for ob in root.children_recursive:
   if ob.type!='MESH' or (ob.get('ReferenceLeaf') in ['Blackberry','Raspberry'] and root['Phase'] not in ['Seed','Harvest']):continue
   result[root.name][ob.name]=U.digest(dict(v=[list(v.co) for v in ob.data.vertices],faces=[list(p.vertices) for p in ob.data.polygons],uv=[list(v.uv) for v in ob.data.uv_layers.active.data],materials=[m.name for m in ob.data.materials],matrix=[list(r) for r in ob.matrix_basis]))
 return result
bpy.ops.wm.open_mainfile(filepath=str(W/'Before/ArtSource/Crops/G01/Blender/G01_Berries_Master.blend'));before=nonleaf_signature();bpy.ops.wm.open_mainfile(filepath=str(G.MASTER));check(nonleaf_signature()==before,'All Blueberry geometry, all branches/flowers/fruit and seed/harvest source geometry retained')
refs=U.load(W/'ReferenceContours.json')['leaves'];closed=0;preservedVertices=0;profiles={}
for kind,ref in refs.items():check(U.sha(G.ROOT/ref['reference'])==ref['referenceSha256'] and len(ref['left'])>=18 and len(ref['right'])>=18,kind+' silhouette measured from the actual supplied image')
for root in [o for o in bpy.data.objects if o.get('Phase') and o['Plant'] in refs and o['Phase'] not in ['Seed','Harvest']]:
 kind,stage=root['Plant'],root['Phase'];leaves=[o for o in root.children_recursive if o.get('ReferenceLeaf')==kind];spec=U.load(G.MODELS/('NP_'+kind+'_'+stage+'.json'));native={tuple(round(float(v),5) for v in p) for p in spec['vertices']};profiles[root.name]=len(leaves)
 for ob in leaves:
  check(ob.get('LeafRevision')=='R05' and ob.get('ReferenceContourSha256')==refs[kind]['referenceSha256'],ob.name+' traced species-specific silhouette');check(abs(ob['LeafWidth']/ob['LeafLength']-refs[kind]['measuredAspect'])<1e-6,ob.name+' actual reference blade aspect');bm=bmesh.new();bm.from_mesh(ob.data);check(all(len(e.link_faces)==2 for e in bm.edges) and abs(bm.calc_volume(signed=True))>1e-12,ob.name+' closed curved leaf volume');bm.free();closed+=1
  for index in ob['ReferenceBoundaryIndices']:
   point=ob.data.vertices[int(index)].co;key=tuple(round(float(v),5) for v in [point.x,point.z,point.y]);check(key in native,ob.name+' measured tooth/tip vertex retained in published near geometry');preservedVertices+=1
for name,record in proof.items():
 spec=U.load(G.MODELS/(name+'.json'));check(spec['materials']==[U.NAME] and U.geometry_hash(spec)==record['geometrySha256'],name+' exact published geometry and shared material');check(spec['triangles']<=(2800 if name.endswith('_LOD1') else 26000),name+' mobile triangle budget')
check(closed==432,'All 432 planted Rubus leaf blades revised; Blueberry geometry retained');check(U.load(W/'GimpAudit.json')['status']=='PASS','Editable Blueberry restoration masters reopened')
U.write(W/'ArtAudit.json',dict(status='PASS',revision='Leaf_R05',checks=len(checks),changedLeafBlades=closed,exactNearBoundaryVertices=preservedVertices,profiles=profiles,referenceSpecies=list(refs),blueberryColorAndNormalRestoredExactly=True,blueberryGeometryRetained=True,nonLeafAndHarvestSourceGeometryRetained=True,changedAtlasSurfaces=1,otherAtlasSurfacesRetained=153,atlasSlots=154,updatedNearDistanceMeshes=16,updatedPrefabs=8,runtimeMaterials=1,masterSha256=U.sha(G.MASTER),sourceHashes={U.rel(p):U.sha(p) for p in (G.SOURCE/'Scripts').glob('*leaf*r05*.py')},unityPlayMode='NOT_RUN'))
print('LEAF_R05_ART_PASS',len(checks),'checks;',closed,'traced blades;',preservedVertices,'exact published boundary vertices',flush=True)
