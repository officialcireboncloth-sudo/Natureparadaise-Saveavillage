from pathlib import Path
import bpy

repo = Path(__file__).resolve().parents[2]
for species in ('Apple', 'Avocado', 'Cherry', 'Guava', 'Mango'):
    source = repo / f'ArtSource/FruitTrees/Blender/NP_{species}.blend'
    bpy.ops.wm.open_mainfile(filepath=str(source))
    for name in ('FruitForming','HarvestReady','HarvestReady_LOD1'):
        crown = bpy.data.collections[f'NP_{species}_{name}']
        height = max((obj.matrix_world @ vert.co).z for obj in crown.objects for vert in obj.data.vertices)
        assert abs(height - 2.8125) < .001, (species, name, height)
        leaves, = [obj for obj in crown.objects if obj.data.has_custom_normals]
        mat, = leaves.data.materials
        nodes = mat.node_tree.nodes
        assert nodes.get('Principled BSDF').inputs['Alpha'].is_linked
        images = [node.image for node in nodes if node.type == 'TEX_IMAGE']
        assert len(images) == 3
        assert all(Path(bpy.path.abspath(i.filepath)).is_file() and tuple(i.size) == (4096, 4096) for i in images)
    print(f'{species.upper()}_BLENDER_CUTOUT_REOPEN_PASS', flush=True)
