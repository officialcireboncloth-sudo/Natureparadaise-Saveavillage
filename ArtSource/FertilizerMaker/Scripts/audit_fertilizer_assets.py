"""Verify the exported mesh buffers, atlas UVs, surface-mask alpha and prefab wiring."""
from pathlib import Path
import json, re, math, struct, zlib, hashlib

ROOT=next(p for p in Path(__file__).resolve().parents if (p/'Assets/Nature  Paradaise').is_dir())
A=ROOT/'Assets/Nature  Paradaise'
MODELS=A/'mesh/Props/FertilizerMaker'
ATLAS=A/'Textures/Shared/Cozy'
REVIEW=ROOT/'ArtSource/FertilizerMaker/Review'

def png_rgba(path):
    data=path.read_bytes();assert data[:8]==b'\x89PNG\r\n\x1a\n'
    at=8;compressed=bytearray();width=height=0
    while at<len(data):
        length=struct.unpack('>I',data[at:at+4])[0];kind=data[at+4:at+8];payload=data[at+8:at+8+length];at+=12+length
        if kind==b'IHDR':
            width,height,depth,color,_,_,interlace=struct.unpack('>IIBBBBB',payload)
            assert depth==8 and color==6 and interlace==0
        if kind==b'IDAT':compressed.extend(payload)
    raw=zlib.decompress(compressed);stride=width*4;offset=0;previous=bytearray(stride);rows=[]
    for _ in range(height):
        mode=raw[offset];offset+=1;row=bytearray(raw[offset:offset+stride]);offset+=stride
        for i in range(stride):
            left=row[i-4] if i>=4 else 0;up=previous[i];upper_left=previous[i-4] if i>=4 else 0
            if mode==1:value=left
            elif mode==2:value=up
            elif mode==3:value=(left+up)//2
            elif mode==4:
                p=left+up-upper_left;dist=[abs(p-left),abs(p-up),abs(p-upper_left)]
                value=[left,up,upper_left][dist.index(min(dist))]
            else:assert mode==0;value=0
            row[i]=(row[i]+value)&255
        rows.append(row);previous=row
    return width,height,rows

report=dict(status='PASS',lods=[],checks=[])
rects={}
for page in ['01','05']:
    filename='NP_CozyAtlasSlots'+('' if page=='01' else '_05')+'.json'
    rects[page]=[slot['uvSafe'] for slot in json.loads((ATLAS/filename).read_text())['slots']]
for lod in range(3):
    name='FertilizerMaker_Cozy_LOD'+str(lod);spec=json.loads((MODELS/(name+'.json')).read_text())
    asset=(MODELS/(name+'.asset')).read_text()
    vertices=spec['vertices'];count=len(vertices)
    raw=bytes.fromhex(re.search(r'^    _typelessdata: (\w+)',asset,re.M)[1])
    assert len(raw)==count*48
    native_indices=list(struct.unpack('<'+'I'*(len(bytes.fromhex(re.search(r'^  m_IndexBuffer: (\w+)',asset,re.M)[1]))//4),
                                     bytes.fromhex(re.search(r'^  m_IndexBuffer: (\w+)',asset,re.M)[1])))
    assert native_indices==[i for sub in spec['submeshes'] for i in sub]
    for index,(p,n,uv) in enumerate(zip(vertices,spec['normals'],spec['uv'])):
        values=struct.unpack_from('<12f',raw,index*48)
        assert all(math.isfinite(v) for v in values)
        assert max(abs(a-b) for a,b in zip(values[:3],p))<.00001
        assert max(abs(a-b) for a,b in zip(values[3:6],n))<.00001
        assert max(abs(a-b) for a,b in zip(values[10:12],uv))<.00001
        assert .99<sum(v*v for v in values[6:9])<1.01
    for sub,page in zip(spec['submeshes'],['01','05']):
        for i in set(sub):
            u,v=spec['uv'][i]
            assert any(r[0]-.000001<=u<=r[2]+.000001 and r[1]-.000001<=v<=r[3]+.000001 for r in rects[page]),(name,page,u,v)
    assert spec['triangles']==len(native_indices)//3
    assert (MODELS/(name+'.fbx')).read_bytes().startswith(b'Kaydara FBX Binary')
    report['lods'].append(dict(name=name,triangles=spec['triangles'],vertices=count,atlasUVs='PASS',serializedBuffers='PASS'))
assert report['lods'][0]['triangles']>report['lods'][1]['triangles']>report['lods'][2]['triangles']
w,h,rows=png_rgba(ATLAS/'NP_CozySurfaces_05_SurfaceMask.png')
assert (w,h)==(512,256)
left=list(rows[128][64*4:64*4+4]);right=list(rows[128][400*4:400*4+4])
assert 75<=left[3]<=78 and 29<=right[3]<=32,(left,right)
assert 29<=left[0]<=33 and right[0]==0,(left,right)
report['surfaceMask']=dict(enamelRGBA=left,gaugeRGBA=right)
before=json.loads((REVIEW/'ExistingAtlasHashes.json').read_text(encoding='utf-8-sig'))
for filename,digest in before.items():assert hashlib.sha256((ATLAS/filename).read_bytes()).hexdigest().upper()==digest
report['checks'].append('All twelve existing atlas 01-04 PNG hashes unchanged')
prefab=(A/'Resources/World/FertilizerMaker_Dummy.prefab').read_text()
assert len(re.findall(r'^--- !u!23 ',prefab,re.M))==3
assert len(re.findall(r'^--- !u!65 ',prefab,re.M))==1
assert len(re.findall(r'^--- !u!205 ',prefab,re.M))==1
assert 'processorId: farm-fertilizer-processor' in prefab
for lod in range(3):
    guid=re.search(r'^guid: (\w+)',(MODELS/('FertilizerMaker_Cozy_LOD'+str(lod)+'.asset.meta')).read_text(),re.M)[1]
    assert '{fileID: 4300000, guid: '+guid+', type: 2}' in prefab
for page in ['01','05']:
    guid=re.search(r'^guid: (\w+)',(A/('Material/Shared/NP_CozySurfaces_'+page+'.mat.meta')).read_text(),re.M)[1]
    assert prefab.count('{fileID: 2100000, guid: '+guid+', type: 2}')==3
report['checks'].extend(['Three LOD mesh bindings, two shared materials per LOD, one collider',
                         'Machine save ID and prefab identity retained','Blender FBX headers valid'])
manifest=json.loads((ROOT/'ArtSource/FertilizerMaker/Blender/FertilizerMaker_Manifest.json').read_text())
assert manifest['parts']==154
report['nativeUnityPlayMode']='Not run; editor importer is provided and queued for the next editor compilation.'
(REVIEW/'ArtAudit.json').write_text(json.dumps(report,indent=2))
print('FERTILIZER_ART_AUDIT_PASS '+json.dumps(report),flush=True)
