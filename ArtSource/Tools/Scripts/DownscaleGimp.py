"""Run with GIMP 3's python-fu-eval batch interpreter. Originals are preserved outside Unity Assets."""
import hashlib
import json
import shutil
from pathlib import Path
from gi.repository import Gimp, Gio

ROOT = Path(__file__).resolve().parents[3]
ASSETS = ROOT / 'Assets/Nature  Paradaise/mesh/Prop/Tools'
SOURCE = ROOT / 'ArtSource/Tools'
TOOLS = {
    '25479981-6580-4005-ac02-8df05faa1ef6': 'FishingRod',
    '376509ef-580b-495e-b990-44409a3c0804': 'Hammer',
    '657ff6f0-60e4-47b0-88d3-3f545de2900f': 'Pickaxe',
    '727a563a-82d5-4ece-a15d-4d6fa4b8546a': 'Shears',
    'a033f0a5-1955-42ff-9250-9e8a8b169e7a': 'Milker',
    'a5d09808-f4df-407c-b303-ae4b51be2f3b': 'Hoe',
    'b61a32d9-b69e-406d-a143-63c06bfa56fc': 'Brush',
    'dbe8eb66-c571-4da6-87f3-107d41ede891': 'WateringCan',
    'e172e440-73b4-4cf0-8646-a3a9b30ae1f4': 'Sickle',
    'f10af669-bddb-4af7-a466-bc386297f5fa': 'Axe',
}

def preserve(path, destination):
    destination.parent.mkdir(parents=True, exist_ok=True)
    if not destination.exists():
        shutil.copy2(path, destination)
    else:
        # Never overwrite a previous full-resolution source with an optimized copy.
        if hashlib.sha256(path.read_bytes()).digest() != hashlib.sha256(destination.read_bytes()).digest():
            raise RuntimeError(f'Original backup already differs: {destination}')
    meta = Path(str(path) + '.meta')
    if meta.exists() and not Path(str(destination) + '.meta').exists():
        shutil.copy2(meta, Path(str(destination) + '.meta'))

manifest = {'processor': 'GIMP 3.0.8 / NOHALO', 'resolution': [256, 256], 'tools': [], 'textures': []}
Gimp.context_push()
try:
    Gimp.context_set_interpolation(Gimp.InterpolationType.NOHALO)
    for identifier, name in TOOLS.items():
        models = list(ASSETS.glob(f'tripo_convert_{identifier}*.fbx'))
        if len(models) != 1:
            raise RuntimeError(f'Expected one FBX for {name}: {models}')
        model = models[0]
        folder = ASSETS / f'tripo_convert_{identifier}.fbm'
        if not folder.is_dir():
            raise RuntimeError(f'Missing texture folder: {folder}')
        source_folder = SOURCE / 'Originals' / name
        preserve(model, source_folder / model.name)
        manifest['tools'].append({'name': name, 'modelBefore': model.relative_to(ROOT).as_posix(),
            'modelAfter': (ASSETS / f'Tool_{name}.fbx').relative_to(ROOT).as_posix(),
            'folderBefore': folder.relative_to(ROOT).as_posix(),
            'folderAfter': (ASSETS / f'Tool_{name}.fbm').relative_to(ROOT).as_posix()})
        for texture in sorted(folder.iterdir()):
            if texture.suffix.lower() not in ['.png', '.jpg', '.jpeg']:
                continue
            original = source_folder / 'Textures' / texture.name
            preserve(texture, original)
            before_bytes = texture.stat().st_size
            image = Gimp.file_load(Gimp.RunMode.NONINTERACTIVE, Gio.File.new_for_path(str(texture)))
            before_size = [image.get_width(), image.get_height()]
            if not image.scale(256, 256):
                raise RuntimeError(f'Scale failed: {texture}')
            proc = Gimp.get_pdb().lookup_procedure('file-png-export' if texture.suffix.lower() == '.png' else 'file-jpeg-export')
            config = proc.create_config()
            config.set_property('run-mode', Gimp.RunMode.NONINTERACTIVE)
            config.set_property('image', image)
            config.set_property('file', Gio.File.new_for_path(str(texture)))
            if texture.suffix.lower() != '.png':
                config.set_property('quality', .98)
            config.set_property('include-exif', False)
            config.set_property('include-xmp', False)
            config.set_property('include-iptc', False)
            result = proc.run(config)
            if result.index(0) != Gimp.PDBStatusType.SUCCESS:
                raise RuntimeError(f'Export failed: {texture} / {result.index(0)}')
            image.delete()
            verification = Gimp.file_load(Gimp.RunMode.NONINTERACTIVE, Gio.File.new_for_path(str(texture)))
            if [verification.get_width(), verification.get_height()] != [256, 256]:
                raise RuntimeError(f'Export dimensions incorrect: {texture}')
            verification.delete()
            manifest['textures'].append({'tool': name, 'filename': texture.name, 'sizeBefore': before_size,
                'sizeAfter': [256, 256], 'bytesBefore': before_bytes, 'bytesAfter': texture.stat().st_size,
                'backup': original.relative_to(ROOT).as_posix(),
                'pathAfter': (ASSETS / f'Tool_{name}.fbm' / texture.name).relative_to(ROOT).as_posix()})
            print(f'[TOOLS GIMP] {name}/{texture.name}: {before_size} -> 256x256', flush=True)
finally:
    Gimp.context_pop()
(SOURCE / 'TextureManifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
print(f'[TOOLS GIMP] COMPLETE: {len(manifest["textures"])} textures', flush=True)
