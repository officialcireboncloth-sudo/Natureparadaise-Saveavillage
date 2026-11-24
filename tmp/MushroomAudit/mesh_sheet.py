import importlib
import struct
import sys
import types
import zlib
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[2]
WORK = Path(__file__).resolve().parent
module = types.ModuleType('audit_fbx')
module.__path__ = ['C:/Program Files (x86)/Steam/steamapps/common/Blender/4.5/scripts/addons_core/io_scene_fbx']
sys.modules['audit_fbx'] = module
parser = importlib.import_module('audit_fbx.parse_fbx')


def child(node, name):
    return next(e for e in node.elems if e.id == name)


def png(path, image):
    h,w,_ = image.shape
    def chunk(tag, data):
        return struct.pack('>I',len(data))+tag+data+struct.pack('>I',zlib.crc32(tag+data))
    path.write_bytes(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',w,h,8,2,0,0,0))+
                     chunk(b'IDAT',zlib.compress(b''.join(b'\0'+row.tobytes() for row in image)))+chunk(b'IEND',b''))


def psd(path):
    with path.open('rb') as f:
        signature,version,_,channels,h,w,depth,mode=struct.unpack('>4sH6sHIIHH',f.read(26))
        assert signature == b'8BPS' and version == 1 and depth == 8 and mode == 3
        for _ in range(3):
            f.seek(struct.unpack('>I',f.read(4))[0],1)
        compression=struct.unpack('>H',f.read(2))[0]
        if compression == 0:
            data=f.read(channels*w*h)
        else:
            assert compression == 1,compression
            sizes=struct.unpack(f'>{channels*h}H',f.read(channels*h*2))
            rows=[]
            for size in sizes:
                source=f.read(size)
                row=bytearray()
                at=0
                while at<len(source):
                    count=source[at]
                    at+=1
                    if count<128:
                        row.extend(source[at:at+count+1]); at+=count+1
                    elif count>128:
                        row.extend(source[at:at+1]*(257-count)); at+=1
                rows.append(row)
            data=b''.join(rows)
        return np.frombuffer(data,np.uint8).reshape(channels,h,w)[:3].transpose(1,2,0).copy()


toon = ROOT/'Assets/Nature  Paradaise/Pack/Toon Series/Toon Farm Pack'
texture=psd(toon/'Textures/TFP_Atlas_1A.psd')
png(WORK/'ToonAtlas.png',texture)
files=sorted(toon.glob('Models/TFP_Mushroom_*.fbx'))
files+=sorted(ROOT.glob('Assets/**/Sources/Meshes/Mushrooms/SM_Mushroom_*.fbx'))
sheet=np.full((4*300,5*280,3),245,dtype=np.uint8)
labels=[]
for index,path in enumerate(files):
    tree,_=parser.parse(str(path))
    objects=child(tree,b'Objects')
    choices=[e for e in objects.elems if e.id==b'Geometry']
    geometry=next((e for e in choices if b'LOD0' in e.props[1]), choices[0])
    points=np.array(child(geometry,b'Vertices').props[0]).reshape(-1,3)
    span=points.max(0)-points.min(0)
    # The pack exports in Y-up coordinates.
    if path.name.startswith('SM_'):
        settings=child(tree,b'GlobalSettings')
        props=child(settings,b'Properties70')
        up=next(e.props[4] for e in props.elems if e.props[0]==b'UpAxis')
        if up == 2:
            points=points[:,[0,2,1]]
    points-= (points.max(0)+points.min(0))/2
    points/=max(points.max(0)-points.min(0))
    view=np.array((1.4,1.0,3.0)); view/=np.linalg.norm(view)
    right=np.cross((0,1,0),view); right/=np.linalg.norm(right)
    up=np.cross(view,right)
    screen=np.column_stack((points@right,-points@up))*220+np.array((140,155))
    depth=points@view
    uv_layer=child(geometry,b'LayerElementUV')
    uv=np.array(child(uv_layer,b'UV').props[0]).reshape(-1,2)
    uv_index=np.array(child(uv_layer,b'UVIndex').props[0])
    faces=[]; current=[]
    for loop,value in enumerate(child(geometry,b'PolygonVertexIndex').props[0]):
        current.append((value if value>=0 else -value-1, loop))
        if value<0:
            for j in range(1,len(current)-1):
                faces.append([current[k] for k in (0,j,j+1)])
            current=[]
    buf=np.full((300,280),-np.inf)
    row,col=divmod(index,5)
    image=sheet[row*300:(row+1)*300,col*280:(col+1)*280]
    for face in faces:
        ids,loops=np.array(face).T
        a,b,c=screen[ids]
        matrix=np.column_stack((b-a,c-a))
        if abs(np.linalg.det(matrix))<1e-8:
            continue
        low=np.maximum((0,0),np.floor(screen[ids].min(0))).astype(int)
        high=np.minimum((279,299),np.ceil(screen[ids].max(0))).astype(int)
        xx,yy=np.meshgrid(np.arange(low[0],high[0]+1),np.arange(low[1],high[1]+1))
        bary=(np.stack((xx+.5,yy+.5),-1)-a)@np.linalg.inv(matrix).T
        valid=(bary>=0).all(-1)&(bary.sum(-1)<=1)
        weights=np.column_stack((1-bary[valid].sum(-1),bary[valid]))
        x,y=xx[valid],yy[valid]
        z=weights@depth[ids]
        valid=z>buf[y,x]
        x,y,weights=x[valid],y[valid],weights[valid]
        buf[y,x]=z[valid]
        normal=np.cross(points[ids[1]]-points[ids[0]],points[ids[2]]-points[ids[0]])
        normal/=max(np.linalg.norm(normal),1e-8)
        shade=.64+.36*abs(normal@np.array((-.3,.8,.52)))
        if path.name.startswith('TFP'):
            tex=weights@uv[uv_index[loops]]
            h,w,_=texture.shape
            colors=texture[np.clip(((1-tex[:,1])*h).astype(int),0,h-1),np.clip((tex[:,0]*w).astype(int),0,w-1)]
        else:
            colors=np.full((len(x),3),(177,157,125))
        image[y,x]=np.clip(colors*shade,0,255)
    labels.append((index,path.name,str(path.relative_to(ROOT))))
png(WORK/'MeshAudit.png',sheet)
(WORK/'labels.tsv').write_text('\n'.join('\t'.join(map(str,row)) for row in labels))
for label in labels:
    print(label[0],label[1])
