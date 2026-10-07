"""Reopen saved XCF files in native GIMP and validate editable layers."""
import os, json
from gi.repository import Gimp, Gio
root = os.path.abspath('ArtSource/StylizedMeadow')
report = []
for name, count in [('GrassGround',2), ('LeafPalette',9)]:
    image = Gimp.file_load(Gimp.RunMode.NONINTERACTIVE, Gio.File.new_for_path(root+'/GIMP/'+name+'.xcf'))
    assert image.get_width() == 1024 and image.get_height() == 1024
    layers = image.get_layers()
    assert len(layers) == count, (name,len(layers))
    report.append({'file':name+'.xcf','width':image.get_width(),'height':image.get_height(),
                   'layers':[layer.get_name() for layer in layers]})
    image.delete()
open(root+'/Previews/GimpSourceAudit.json','w').write(json.dumps(report,indent=2))
print('GIMP_SOURCE_VERIFIED')
