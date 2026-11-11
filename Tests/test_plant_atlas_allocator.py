"""Packing invariants and remaining berry capacity without modifying any live atlas."""
import copy,json,random,sys,unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];sys.path.insert(0,str(ROOT/'ArtSource/Crops/Scripts'))
import unified_plant_atlas as U

def fits(rect,occupied,limit):
 x,y,w,h=rect
 return min(x,y)>=0 and max(x+w,y+h)<=limit and all(x+w<=a or a+c<=x or y+h<=b or b+d<=y for a,b,c,d in occupied)

class AllocatorTests(unittest.TestCase):
 def test_real_berry_capacity_keeps_all_134_current_allocations(self):
  before=U.sha(U.REGISTRY);snapshot=ROOT/'ArtSource/Crops/G01/Review/Baseline.json';registry=U.load(snapshot)['registryBefore'] if snapshot.exists() else U.load(U.REGISTRY);old=copy.deepcopy(registry['slots']);occupied=[s['pixelRectTopLeft'] for s in old]
  for i,size in enumerate([256]*8+[128]*12):
   rect=U.allocate(registry,'future-berry-'+str(i),size);self.assertTrue(fits(rect,occupied,4096));self.assertTrue(all(n%32==0 for n in rect));occupied.append(rect);registry['slots'].append(dict(key='future-berry-'+str(i),pixelRectTopLeft=rect))
  self.assertEqual(registry['slots'][:len(old)],old);self.assertEqual(U.sha(U.REGISTRY),before)
 def test_fit_detection_agrees_with_exhaustive_small_grid(self):
  rng=random.Random(1301)
  for _ in range(40):
   occupied=[[x*32,y*32,32,32] for y in range(8) for x in range(8) if rng.random()<.32];registry=dict(size=256,slots=[dict(pixelRectTopLeft=r) for r in occupied])
   for size in [32,64,96,128]:
    possible=any(fits([x,y,size,size],occupied,256) for y in range(0,257-size,32) for x in range(0,257-size,32))
    if possible:self.assertTrue(fits(U.allocate(registry,'probe',size),occupied,256))
    else:
     with self.assertRaises(RuntimeError):U.allocate(registry,'probe',size)
 def test_full_atlas_fails_without_changing_existing_records(self):
  registry=dict(size=64,slots=[dict(key='fixed',pixelRectTopLeft=[0,0,64,64])]);old=copy.deepcopy(registry)
  with self.assertRaises(RuntimeError):U.allocate(registry,'overflow',32)
  self.assertEqual(registry,old)
 def test_results_are_deterministic(self):
  registry=U.load(U.REGISTRY);self.assertEqual(U.allocate(registry,'a',128),U.allocate(copy.deepcopy(registry),'b',128))
 def test_current_remaining_raspberry_capacity(self):
  registry=U.load(U.REGISTRY);old=copy.deepcopy(registry['slots']);occupied=[s['pixelRectTopLeft'] for s in old]
  for i,size in enumerate([256]*4+[128]*4):
   rect=U.allocate(registry,'remaining-'+str(i),size);self.assertTrue(fits(rect,occupied,registry['size']));occupied.append(rect);registry['slots'].append(dict(pixelRectTopLeft=rect))
  self.assertEqual(registry['slots'][:len(old)],old)

if __name__=='__main__':
 suite=unittest.defaultTestLoader.loadTestsFromTestCase(AllocatorTests);result=unittest.TextTestRunner(verbosity=2).run(suite)
 if not result.wasSuccessful():sys.exit(1)
 destination='G01' if (ROOT/'ArtSource/Crops/G01/Review/Baseline.json').exists() else 'G13'
 U.write(ROOT/f'ArtSource/Crops/{destination}/Review/AllocatorAudit.json',dict(status='PASS',tests=result.testsRun,exhaustiveLayouts=40,regressionBerrySurfaces={'256px':8,'128px':12},remainingRaspberrySurfaces={'256px':4,'128px':4},currentAllocationsRetained=len(U.load(U.REGISTRY)['slots']),registryUnchanged=True,sourceHashes={U.rel(p):U.sha(p) for p in [Path(__file__),ROOT/'ArtSource/Crops/Scripts/unified_plant_atlas.py']}))
