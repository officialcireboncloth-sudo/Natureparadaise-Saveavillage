"""Independent saved-file and FBX roundtrip audit; never saves over the source."""
import bpy,json,math,hashlib
from pathlib import Path
from mathutils import Vector
ART=Path(__file__).resolve().parents[1]
SPEC=json.loads((ART/'References/Barn_A_Approved.json').read_text(encoding='utf-8'))
ROOT=ART.parents[1]
atlas=ROOT/SPEC['materials']['atlasPath']
assert hashlib.sha256(atlas.read_bytes()).hexdigest()==SPEC['materials']['sha256']
bpy.ops.wm.open_mainfile(filepath=str(ART/'Blender/Barn_A_Master.blend'))
scene=bpy.data.scenes['Barn A - Editable Master Lv3']
bpy.context.window.scene=scene
source=bpy.data.collections['01 EDITABLE - master dimensions Lv3']
objects=list(source.all_objects)
parts={p['name']:p for p in SPEC['parts'] if p['type']!='surface' and p['name'] not in ('Foundation','WallBody')}
assert len([o for o in objects if o.get('ApprovedPart')])==len(parts)
def extrema(obj,matrix=None):
    transform=obj.matrix_world if matrix is None else matrix
    pts=[transform@v.co for v in obj.data.vertices]
    return [min(v[i] for v in pts) for i in range(3)],[max(v[i] for v in pts) for i in range(3)]
placement=[]
for obj in objects:
    if obj.name in parts:
        part=parts[obj.name]
        if part['type']=='box':
            center=part['centerXYZ'];size=part['sizeXYZ'];order=(0,2,1)
            lo=[center[i]-size[i]/2 for i in order];hi=[center[i]+size[i]/2 for i in order]
            actual_lo,actual_hi=extrema(obj)
            assert all(abs(actual_lo[i]-lo[i])<1e-5 and abs(actual_hi[i]-hi[i])<1e-5 for i in range(3)),obj.name
        placement.append(obj.name)
    if obj.name.startswith('WallBoards_'):
        direction={'Front':(0,-1,0),'Rear':(0,1,0),'Right':(1,0,0),'Left':(-1,0,0)}[obj.name.split('_')[-1]]
        assert all(p.normal.dot(Vector(direction))>.99 for p in obj.data.polygons),obj.name
    if obj.name.startswith('Roof'):
        # Upper shingles must face up, not be invisible with backface culling.
        for p in obj.data.polygons:
            verts=[obj.data.vertices[i].co for i in p.vertices]
            if abs(p.normal.z)>.7 and sum(v.z for v in verts)/len(verts)>4:
                # Underside below the plane is intentionally down-facing.
                on_top=sum(v.z+.6*abs(v.x)-6.6 for v in verts)/len(verts)>-.01
                if on_top:assert p.normal.z>.7,(obj.name,p.index,p.normal[:])
    assert tuple(obj.scale)==(1,1,1)

levels=bpy.data.collections['Level instances - uniform size only']
bpy.context.window.scene=bpy.data.scenes['Barn A - Levels 1 to 5 - shared mesh']
bpy.context.view_layer.update()
roots=[o for o in levels.objects if o.type=='EMPTY' and o.name.startswith('Barn_A_Lv')]
instances=[o for o in levels.objects if o.type=='MESH']
assert len(roots)==5 and len(instances)==5
assert len({o.data.as_pointer() for o in instances})==1
for lv in SPEC['levels']:
    root=next(o for o in roots if o.name=='Barn_A_Lv'+str(lv['level']))
    assert all(abs(s-lv['uniformScale'])<1e-7 for s in root.scale)
    model=next(o for o in instances if o.parent==root)
    assert tuple(model.scale)==(1,1,1) and tuple(model.location)==(0,0,0)
    # Saved secondary-scene raw matrix_world can be lazy until its render
    # depsgraph runs. Verify the actual stored hierarchy transforms directly.
    assert root.parent is None
    hierarchy=root.matrix_basis@model.matrix_parent_inverse@model.matrix_basis
    lo,hi=extrema(model,hierarchy)
    assert all(abs(hi[i]-lo[i]-lv['visualEnvelopeXYZ'][[0,2,1][i]])<1e-5 for i in range(3)),(lv['level'],lo,hi,lv['visualEnvelopeXYZ'])

assert len([i for i in bpy.data.images if i.packed_file and i.get('UnityAtlasGUID')])==1
source_report={'approvedPartPlacements':len(placement),'boxDimensions':'PASS','outwardWallAndRoofNormals':'PASS',
               'levelInstances':5,'uniformScales':'PASS','sharedLevelMesh':'PASS','originalAtlasUnchanged':'PASS',
               'packedBlenderPreviewImage':True}

fbx_checks=[]
for lod in (0,1,2):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    filename=ART/'Exports'/f'Barn_A_Master_LOD{lod}.fbx'
    assert filename.stat().st_size>1000
    bpy.ops.import_scene.fbx(filepath=str(filename),use_anim=False)
    models=[o for o in bpy.context.scene.objects if o.type=='MESH']
    assert len(models)==1
    obj=models[0];lo,hi=extrema(obj)
    assert all(abs(hi[i]-lo[i]-[8.9,10.9,6.8][i])<1e-4 for i in range(3)),(lo,hi)
    assert all(abs(v)<1e-5 for v in obj.location)
    assert len(obj.data.materials)==2
    assert {m.name for m in obj.data.materials}=={'TFP_Atlas_1A','TFP_Atlas_Lights_1A'}
    assert len(obj.data.uv_layers)>=1
    count=sum(len(p.vertices)-2 for p in obj.data.polygons)
    assert count<=[4500,1800,450][lod],count
    assert all(p.area>1e-10 for p in obj.data.polygons),(lod,'degenerate face')
    assert not any(o.type in ('LIGHT','CAMERA','FONT') for o in bpy.context.scene.objects)
    fbx_checks.append({'lod':lod,'triangles':count,'dimensionsBlenderXYZ':[hi[i]-lo[i] for i in range(3)],
                       'groundCentreOrigin':'PASS','materialNames':'PASS','UVLayer':'PASS','noDegenerateFaces':'PASS',
                       'previewObjectsExcluded':'PASS','sha256':hashlib.sha256(filename.read_bytes()).hexdigest()})
report={'status':'PASS','scope':'Blender source and FBX audit; Unity integration is audited separately in UnityImportAudit.txt',
        'sourceBlend':source_report,'fbxRoundtrip':fbx_checks}
(ART/'Previews/BlenderSourceAudit.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print('BARN_VERIFICATION_PASS '+json.dumps(report))
