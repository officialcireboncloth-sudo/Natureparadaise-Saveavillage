"""Run the unchanged transfer helper with persistent NTFS alternate-stream attributes."""
import os,re,sys,runpy
from pathlib import Path
def attribute_path(path,name):
 if isinstance(name,bytes):name=name.decode('utf-8')
 if not re.fullmatch(r'user\.[A-Za-z0-9_.-]+',name):raise ValueError('Unsupported attribute name')
 return str(path)+':'+name
def setxattr(path,name,value,*args,**kwargs):
 with open(attribute_path(path,name),'wb') as f:f.write(value)
def getxattr(path,name,*args,**kwargs):
 with open(attribute_path(path,name),'rb') as f:return f.read()
os.setxattr=setxattr;os.getxattr=getxattr
helper=Path(__file__).with_name('library_file_transfer.py');sys.argv=[str(helper)]+sys.argv[1:];runpy.run_path(str(helper),run_name='__main__')
