"""G06-G09 gameplay additions; the accepted art and earlier twelve crops stay intact."""
from pathlib import Path
import json,re,hashlib,csv
ROOT=Path(__file__).resolve().parents[3]
A=ROOT/'Assets/Nature  Paradaise'
OUT=ROOT/'ArtSource/Crops/Integration'
ART=ROOT/'ArtSource/UIIcons/Seeds'
DEST=A/'Textures/UI/Icons'
NEW=[('G06','Radish','radish','Lobak'),('G06','Onion','onion','Bawang Bombay'),
     ('G07','Spinach','spinach','Bayam'),('G07','WaterSpinach','water_spinach','Kangkung'),
     ('G08','Rice','rice','Padi'),('G08','Wheat','wheat','Gandum'),
     ('G09','Rose','rose','Mawar'),('G09','Hibiscus','hibiscus','Kembang Sepatu'),('G09','Jasmine','jasmine','Melati')]
FLOWERS={'rose','hibiscus','jasmine'}
SOIL_BURIAL={('Radish','RootForming'):.15625,('Radish','Ready'):.25,('Onion','BulbForming'):.1458,('Onion','Ready'):.27}
def load(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def rel(p):return str(p.relative_to(ROOT)).replace('\\','/')
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def guid(p):return re.search(r'^guid: (\w+)',Path(str(p)+'.meta').read_text(),re.M)[1]
def reference(g,file=11400000,kind=2):return '{fileID: '+str(file)+', guid: '+g+', type: '+str(kind)+'}'
def setval(t,k,v):
 pattern=r'(?m)^  '+re.escape(k)+r':[^\r\n]*';line='  '+k+': '+str(v)
 return re.sub(pattern,lambda _:line,t) if re.search(pattern,t) else t.rstrip()+'\n'+line+'\n'
def csvread(p):
 with p.open(encoding='utf-8-sig',newline='') as f:
  reader=csv.DictReader(f);return reader.fieldnames,list(reader)
