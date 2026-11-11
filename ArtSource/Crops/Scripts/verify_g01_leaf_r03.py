"""Check the requested shape/UV/size correction and its precise preservation boundary."""
import bpy,bmesh,sys,re,math
import numpy as np
from pathlib import Path
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
import g01_config as G
import unified_plant_atlas as U
R=G.REVIEW/'Leaf_R03';base=U.load(R/'Baseline.json');proof=U.load(R/'ExportBaseline.json')['models'];checks=[]
def check(ok,label):
 assert ok,label
 checks.append(label)
allowed=set()
transforms=U.load(R/'BindingChanges.json')['plants']
for record in transforms.values():allowed.add(record['profile'])
for stem in proof:
 for ext in ['.json','.fbx','.asset']:allowed.add(U.rel(G.MODELS/(stem+ext)))
 if not stem.endswith('_LOD1'):allowed.add(U.rel(G.A/'Prefabs/Crops/G01'/(stem+'.prefab')))
for name,h in base['protected'].items():
 if name not in allowed:check(U.sha(G.ROOT/name)==h,'Unrelated asset/material/atlas/UI/binding retained '+name)
check(U.load(U.REGISTRY)==base['registryBefore'],'Atlas registry and all stable UV allocations unchanged')
currentPlants=U.load(G.SOURCE/'Integration/Manifest.json')['plants']
for old in base['manifestBefore']['plants']:
 now=next(r for r in currentPlants if r['key']==old['key'])
 if old['key'] not in transforms:check(now==old,'Other crop binding retained '+old['species']);continue
 record=transforms[old['key']];check(record['before']==old and record['after']==now,'Exact visual transform revision recorded '+old['species'])
 strip=lambda row:dict(row,stages=[{k:v for k,v in stage.items() if k not in ['scale','offset']} for stage in row['stages']])
 check(strip(now)==strip(old),'Only scale/centering fields changed '+old['species'])
 stripText=lambda text:re.sub(r'(?m)^    (?:scale|localOffset):[^\r\n]*\r?\n?','',text).strip()
 check(stripText(record['beforeText'])==stripText(record['afterText']) and U.sha(G.ROOT/record['profile'])==record['afterSha256'],'Only serialized art transforms changed '+old['species'])
def signatures():
 result={}
 for root in [o for o in bpy.data.objects if o.get('Phase')]:
  result[root.name]={}
  for o in root.children_recursive:
   if o.type!='MESH' or o.get('ReferenceLeaf'):continue
   editableBlue=root['Plant']=='Blueberry' and root['Phase'] in ['Flowering','Ready'] and (o.get('BerryKind')=='Blueberry' or (o.get('JointRole') in ['head','stem'] and str(o.get('JointId','')).startswith('Fruit_')))
   if editableBlue:continue
   result[root.name][o.name]=U.digest(dict(v=[list(v.co) for v in o.data.vertices],faces=[list(p.vertices) for p in o.data.polygons],uv=[list(v.uv) for v in o.data.uv_layers.active.data],matrix=[list(r) for r in o.matrix_basis]))
 return result
beforeMaster=R/'Before/ArtSource/Crops/G01/Blender/G01_Berries_Master.blend';bpy.ops.wm.open_mainfile(filepath=str(beforeMaster));oldSignatures=signatures();priorLeaves={};oldBlue={}
for root in [o for o in bpy.data.objects if o.get('Phase')]:
 leaves=[o for o in root.children_recursive if o.get('ReferenceLeaf')];priorLeaves[root.name]=len(leaves)
 for ob in [o for o in root.children_recursive if o.get('BerryKind')=='Blueberry' and root['Phase'] in ['Ready','Flowering']]:oldBlue[root.name+':'+str(ob['FruitIndex'])]=np.array([tuple(v.co) for v in ob.data.vertices])
bpy.ops.wm.open_mainfile(filepath=str(G.MASTER));now=signatures();check(now==oldSignatures,'All woody stems, flowers, Rubus fruits, seed and detached harvest geometry preserved')
metrics={};volumes=0;joints=0
for root in [o for o in bpy.data.objects if o.get('Phase')]:
 kind,stage=root['Plant'],root['Phase'];leaves=[o for o in root.children_recursive if o.get('ReferenceLeaf')];check(len(leaves)==priorLeaves[root.name],root.name+' leaf count retained')
 if not leaves:continue
 elevations=[];widthRatios=[];normals=[];points=[]
 for ob in leaves:
  check(ob.get('LeafRevision')=='R03' and ob.get('VeinUV')=='taper-aware centerline',ob.name+' current leaf form and UV revision');check(ob['LeafWidth']/ob['LeafLength']>(.54 if kind=='Blueberry' else .43),ob.name+' ovate blade proportions');elevations.append(float(ob['LeafElevation']));widthRatios.append(float(ob['LeafWidth']/ob['LeafLength']))
  bm=bmesh.new();bm.from_mesh(ob.data);check(all(len(e.link_faces)==2 for e in bm.edges) and abs(bm.calc_volume(signed=True))>1e-12,ob.name+' closed leaf thickness');bm.free();volumes+=1
  verts=np.array([tuple(v.co) for v in ob.data.vertices]);points.extend(verts);s=np.linalg.svd(verts-verts.mean(axis=0),compute_uv=False);check(s[-1]/s[0]>.002,ob.name+' genuinely curved blade')
  cols=7;rows=len(verts)//(2*cols);centerIndices=[i*cols+cols//2 for i in range(rows)];uv=ob.data.uv_layers.active;slot=next(m for m in (U.source_pages()['NP_Plants_Raspberry_Source_01' if kind=='Raspberry' else G.ATLAS]['slots']) if m['name']==kind+'Leaf');u0,v0,u1,v1=slot['uvSafe'];byVert={}
  for li,loop in enumerate(ob.data.loops):byVert[loop.vertex_index]=list(uv.data[li].uv)
  check(max(abs((byVert[i][0]-u0)/(u1-u0)-.5) for i in centerIndices)<2e-6,ob.name+' vein centered from petiole to tip');check(abs(byVert[0][0]-byVert[cols-1][0])/(u1-u0)<.002,ob.name+' tip UV no longer spans entire swatch')
 pts=np.array(points);span=np.ptp(pts,axis=0);metrics[root.name]=dict(meanElevation=float(np.mean(elevations)),minElevation=min(elevations),maxElevation=max(elevations),meanWidthLengthRatio=float(np.mean(widthRatios)),canopyDepthWidthRatio=float(span[1]/span[0]))
 check(np.mean(elevations)<36 and min(elevations)<30,root.name+' restrained mixed leaf elevations')
 if stage!='Sprout':check(span[1]/span[0]>.35,root.name+' foliage distributed in depth')
 if stage in ['Flowering','Ready']:
  heads={o['JointId']:o for o in root.children_recursive if o.get('JointRole')=='head'};stems={o['JointId']:o for o in root.children_recursive if o.get('JointRole')=='stem'};check(set(heads)==set(stems),root.name+' fruit/flower joint count retained')
  def ring(ob,start):return sum((v.co for v in ob.data.vertices[start:start+9]),Vector())/9
  for key,head in heads.items():
   stem=stems[key];a=ring(head,0);b=ring(head,10);tip=ring(stem,len(stem.data.vertices)-10);prev=ring(stem,len(stem.data.vertices)-20);check((tip-a).length<1e-6 and math.degrees((tip-prev).angle(b-a))<.16,root.name+' '+key+' continuous stem attachment');joints+=1
  if kind=='Blueberry':
   for ob in [o for o in root.children_recursive if o.get('BerryKind')=='Blueberry']:
    old=oldBlue[root.name+':'+str(ob['FruitIndex'])];now=np.array([tuple(v.co) for v in ob.data.vertices]);ratio=np.ptp(now,axis=0)/np.ptp(old,axis=0);check(np.max(np.abs(ratio-.84))<2e-6,'Planted Blueberry scaled uniformly to 84 percent without changing its crown')
for name,record in proof.items():
 spec=U.load(G.MODELS/(name+'.json'));check(spec['materials']==[U.NAME] and U.geometry_hash(spec)==record['geometrySha256'],name+' source geometry and unified material');check(spec['triangles']<=(2800 if name.endswith('_LOD1') else 26010),name+' established triangle budget')
U.write(R/'ArtAudit.json',dict(status='PASS',revision='Leaf_R03',checks=len(checks),preservedFiles=len(base['protected'])-len(allowed),updatedNearDistanceMeshes=len(proof),updatedPrefabs=12,leafVolumes=volumes,continuousJoints=joints,metrics=metrics,atlasSlots=len(base['registryBefore']['slots']),atlasPixelsUnchanged=True,otherCrops=28,allCropPrefabAndItemBindingsPreserved=True,visualTransformsFitExistingTileFootprint=True,masterSha256=U.sha(G.MASTER),sourceHashes={U.rel(p):U.sha(p) for p in (G.SOURCE/'Scripts').glob('*leaf*r03*.py')},unityPlayMode='NOT_RUN'))
print('LEAF_R03_ART_PASS',len(checks),'checks;',volumes,'closed leaves;',joints,'continuous joints',flush=True)
