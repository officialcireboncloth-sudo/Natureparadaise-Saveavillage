"""Registered vector colour and relief for the two specific G07 leaf types."""
import random,math
from g07_config import PALETTE
def svg(name,height=False):
 base,hi,lo=PALETTE[name] if not height else ('#808080','#ababab','#727272')
 p=['<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256">']
 def line(d,c,w=1,opacity=1):p.append(f'<path d="{d}" stroke="{c}" stroke-width="{w}" opacity="{opacity}" fill="none" stroke-linecap="round"/>')
 def ellipse(x,y,rx,ry,c,opacity):p.append(f'<ellipse cx="{x:.3f}" cy="{y:.3f}" rx="{rx:.3f}" ry="{ry:.3f}" fill="{c}" opacity="{opacity}"/>')
 p.append(f'<defs><radialGradient id="g" cx="48%" cy="48%" r="72%"><stop stop-color="{hi}"/><stop offset=".48" stop-color="{base}"/><stop offset="1" stop-color="{lo}"/></radialGradient><radialGradient id="cell"><stop stop-color="{hi}"/><stop offset="1" stop-color="{base}" stop-opacity="0"/></radialGradient></defs>')
 p.append(f'<rect width="256" height="256" fill="{base if height else "url(#g)"}"/>')
 if name in ['LeafOrchid','LeafWaterSpinach']:
  spinach=name=='LeafOrchid';vein=('#a2bc67' if spinach else '#b2c975') if not height else '#b5b5b5'
  # Leaf UVs run from the petiole at y=236 to the rounded/pointed tip at y=20.
  for side in [-1,1]:
   for j in range(7 if spinach else 6):
    y=53+j*(24 if spinach else 29);ex=128+side*(76 if spinach else 83)
    ellipse(128+side*39,y+1,37,17,'url(#cell)',.43 if height else .13)
    d=f'M128 {y+28} Q{128+side*37} {y+9} {ex} {y-12}'
    line(d,lo,4,.30);line(d,vein,1.4 if spinach else 1.2,.86)
    for k in [1,2,3]:
     x=128+side*(12+k*15);yy=y+20-k*5
     line(f'M{x} {yy} q{side*12} -8 {side*17} -24',vein,.55,.52)
     line(f'M{x} {yy} q{side*12} 6 {side*23} 1',vein,.50,.30)
  line('M128 240 C125 174 130 89 128 18',lo,6,.35)
  line('M128 240 C125 174 130 89 128 18',vein,2.6 if spinach else 2,.92)
  rng=random.Random(name)
  for j in range(180):
   x,y=rng.uniform(20,236),rng.uniform(20,236)
   ellipse(x,y,rng.uniform(1.5,4.5),rng.uniform(2,7),hi if j%2 else lo,.025 if not height else .015)
 elif name=='StemPale':
  for j in range(22):
   x=j*12;line(f'M{x} 0 Q{x+2} 128 {x} 256',hi if j%3 else lo,.8,.23)
 elif name=='HollowStem':
  for j in range(14):
   x=7+j*18;line(f'M{x} 0 L{x} 256',hi,.7,.14)
 elif name=='CutStem':
  for r in [28,65,100]:p.append(f'<circle cx="128" cy="128" r="{r}" fill="none" stroke="{hi if r!=100 else lo}" stroke-width="2" opacity=".45"/>')
  rng=random.Random(707)
  for _ in range(100):
   a=rng.random()*math.tau;r=95*math.sqrt(rng.random());ellipse(128+r*math.cos(a),128+r*math.sin(a),.7,.7,hi,.25)
 p.append('</svg>');return ''.join(p)
