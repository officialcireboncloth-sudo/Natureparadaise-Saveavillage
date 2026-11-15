from pathlib import Path

work = Path(__file__).resolve().parent
project = work.parents[1]
template_path = project / 'Documentation/EggPlacement-2026-10-20/prepare_fixture.py'
template = template_path.read_text()
template = template.replace("dest = root / 'Documentation/EggPlacement-2026-10-20/NativeProject'",
                            "dest = root / 'tmp/TreePlanting/NativeProject'")
start = template.index("seeds += [game / 'Script/Editor/EggPlacementValidation.cs']")
end = template.index("seeds += list((root / 'Assets/TextMesh Pro/Shaders')", start)
template = template[:start] + """seeds += [game / 'Resources/Items/Farming/Seeds/Avocado Seedling.asset']
seeds += [game / 'Prefabs/Player/PlayerVisual.prefab']
seeds += [game / 'Player/Animations/Player Animation Set.asset']
""" + template[end:]
template = template[:template.index("(dest / 'Assets/EggPlacementPromptDriver.cs')")].replace(
    'productName: EggPlacementValidation', 'productName: TreeInputValidation')
exec(compile(template, str(template_path), 'exec'), {'__file__': str(template_path)})
