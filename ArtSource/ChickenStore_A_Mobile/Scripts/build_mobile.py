"""Generate a mobile variant from the approved exterior builder; keep the original asset intact."""
import ast
from pathlib import Path
import bpy
ART=Path(__file__).resolve().parents[1];ROOT=ART.parents[1]
LIVE=globals().get('LIVE_BUILD',False)
if LIVE:bpy.ops.wm.save_as_mainfile(filepath=str(ART/'References/BeforeMobile_LiveSnapshot.blend'),copy=True)
original=(ROOT/'ArtSource/ChickenStore_A/Scripts/build_chicken_store.py').read_text(encoding='utf-8')
def replace_function(src,name,new):
 node=next(n for n in ast.parse(src).body if isinstance(n,ast.FunctionDef) and n.name==name)
 lines=src.splitlines();return '\n'.join(lines[:node.lineno-1]+[new]+lines[node.end_lineno:])
roof='''def make_roofs(tile,base,planes):
 # Preserve the analytical roof valleys; replace individual shingle solids with surfaces and thin seams.
 for i,p in enumerate(planes):
  xmin,xmax,ymin,ymax=p['rect'];h=p['height']
  def visible(poly):
   parts=[poly]
   for j,q in enumerate(planes):
    if i==j:continue
    a,b,c,d=q['rect'];oh=q['height']
    conditions=[lambda x,y,a=a:x-a,lambda x,y,b=b:b-x,lambda x,y,c=c:y-c,lambda x,y,d=d:d-y,lambda x,y,oh=oh,h=h:oh(x,y)-h(x,y)-1e-6]
    parts=[piece for part in parts for piece in subtract(part,conditions)]
   return parts
  for poly in visible([(xmin,ymin),(xmax,ymin),(xmax,ymax),(xmin,ymax)]):roof_prism(base,poly,h,'roof',.08)
  along_x=p['ridge']=='X';lo,hi=(xmin,xmax) if along_x else (ymin,ymax);rlo,rhi=(ymin,ymax) if along_x else (xmin,xmax)
  rows=max(1,math.ceil((rhi-rlo)/.48));pitch=(rhi-rlo)/rows;tile_w=.80
  def seam(a,b,c,d):
   poly=[(a,c),(b,c),(b,d),(a,d)] if along_x else [(c,a),(d,a),(d,b),(c,b)]
   for piece in visible(poly):tile.face([(x,y,h(x,y)+.001) for x,y in piece],'metal',(0,0,1))
  for row in range(rows):
   aa=rlo+row*pitch;bb=aa+pitch
   if row:seam(lo+.015,hi-.015,aa-.004,aa+.004)
   start=lo+tile_w-(row%2)*tile_w/2
   while start<hi-.02:
    seam(start-.004,start+.004,aa+.015,bb-.015);start+=tile_w
'''
foundation='''def foundation(b,w,d,h):
 b.box((0,0,h/2),(w+.18,d+.18,h),'foundation')
 # Flat mortar seams on front/back; no bevelled per-stone geometry.
 for yy in [-d/2-.091,d/2+.091]:
  for i in range(1,math.ceil(w/.8)):
   x=-w/2+i*w/math.ceil(w/.8)
   b.face([(x-.007,yy,.012),(x+.007,yy,.012),(x+.007,yy,h-.012),(x-.007,yy,h-.012)],'wood_shadow',(0,-1 if yy<0 else 1,0))
'''
stats='''def stats(obs):
 points=[world(o)@v.co for o in obs for v in o.data.vertices]
 engine=0
 for o in obs:
  unique=set();uv=o.data.uv_layers.active
  for p in o.data.polygons:
   for li in p.loop_indices:unique.add((o.data.loops[li].vertex_index,*[round(c,6) for c in p.normal],*[round(c,6) for c in uv.data[li].uv]))
  engine+=len(unique)
 return {'triangles':sum(len(p.vertices)-2 for o in obs for p in o.data.polygons),'meshVertices':sum(len(o.data.vertices) for o in obs),'estimatedVerticesAfterNormalAndUVSplits':engine,'bounds':[[min(v[i] for v in points) for i in range(3)],[max(v[i] for v in points) for i in range(3)]]}
'''
code=replace_function(original,'make_roofs',roof)
code=replace_function(code,'foundation',foundation)
code=replace_function(code,'stats',stats)
code=code.replace("source,.012 if key in {'Foundation','DoorLeaf','Counter','Frames'} else 0)","source,0)")
code=code.replace('curve.extrude=.002;curve.resolution_u=2','curve.extrude=0;curve.resolution_u=1')
code=code.replace('math.ceil((b-a)/.24)','math.ceil((b-a)/.48)')
code=code.replace('ChickenStore_A','ChickenStore_Mobile_A').replace('ChickenCoop_Companion_A','ChickenCoop_Mobile_A').replace('ChickenCoop_WeatherVane_Optional','ChickenCoop_Mobile_WeatherVane_Optional')
code=code.replace('Coop_Optional_WeatherVane','Coop_Mobile_Optional_WeatherVane')
code=code.replace('01 Chicken Store - Editable','01 Chicken Store MOBILE - Editable').replace('02 Coop Companion - Editable','02 Coop MOBILE - Editable').replace('00 Chicken Store + Coop Overview','00 Chicken Store MOBILE Overview')
# Distinct source collections/mesh IDs coexist with original assets in a live Blender file.
code=code.replace("'01 EDITABLE Store_Main'","'01 EDITABLE MOBILE Store_Main'").replace("'01 EDITABLE Coop_Companion'","'01 EDITABLE MOBILE Coop_Companion'")
# Fix butt joints on the small side-vent frames as well.
code=code.replace("(.14,.07,.42)","(.14,.07,.30)")
code=code.replace("source,.012 if", "source,0 if")
(ART/'Scripts/generated_mobile_builder.py').write_text(code,encoding='utf-8')
exec(compile(code,str(ART/'Scripts/generated_mobile_builder.py'),'exec'),{'__file__':str(ART/'Scripts/generated_mobile_builder.py'),'__name__':'__main__','LIVE_BUILD':LIVE})
