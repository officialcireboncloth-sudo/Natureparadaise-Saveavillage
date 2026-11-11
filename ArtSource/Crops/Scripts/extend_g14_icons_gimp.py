"""Use the existing layered seed packet, label and mutation treatment for both flowers."""
from pathlib import Path
import sys

from gi.repository import Gimp, Gio

sys.path.insert(0, str(Path(__file__).resolve().parent))
import g01_integration_config as config


def apply_packet_print(image, key):
    path = config.ART / f'Generated/G14_{key}_PacketPrint.png'
    if not path.is_file():
        return
    art = Gimp.file_load(Gimp.RunMode.NONINTERACTIVE, Gio.File.new_for_path(str(path)))
    name = f'Illustration - {key}'
    old = next(layer for layer in image.get_layers() if layer.get_name() == name)
    layer = Gimp.Layer.new_from_drawable(art.get_layers()[0], image)
    layer.set_name(name)
    image.insert_layer(layer, None, image.get_layers().index(old))
    image.remove_layer(old)
    layer.set_offsets(0, 0)
    art.delete()


config.NEW = [('G14', 'Daisy', 'daisy', 'Daisy'), ('G14', 'Moonflower', 'moonflower', 'Moonflower')]
if '--daisy-only' in sys.argv:
    config.NEW = config.NEW[:1]
script = config.ART / 'extend_g01_seed_packets_gimp.py'
source = script.read_text()
source = source.replace('Generated/G01_Illustrations.png', 'Generated/G14_Illustrations.png')
source = source.replace('Generated/G01_AtlasCells.json', 'Generated/G14_AtlasCells.json')
source = source.replace('G01_IconAudit.json', 'G14_IconAudit.json')
source = source.replace('GIMP_G01_SEED_PASS 30 packets, two new editable source pairs', 'GIMP_G14_SEED_PASS')
packet_call = "packet=ns['packet'](key,label.upper(),sprite);seedmaster="
assert packet_call in source
source = source.replace(packet_call, "packet=ns['packet'](key,label.upper(),sprite);apply_packet_print(packet.image,key);seedmaster=")
exec(compile(source, str(script), 'exec'),
     {'__file__': str(script), '__name__': '__main__', 'apply_packet_print': apply_packet_print})
