"""Record completion without regenerating or changing approved placement coordinates."""
import hashlib
import json
from pathlib import Path

review = Path(__file__).resolve().parent.parent
root = review.parents[2]
manifest = review / 'proposed-layouts.json'
data = json.loads(manifest.read_text(encoding='utf-8-sig'))
placement_before = json.dumps(data['levels'], sort_keys=True)
scene = root / data['scenePath']
native = (review / 'applied-native-checks.txt').read_text(encoding='utf-8-sig')
play = (review / 'applied-play-checks.txt').read_text(encoding='utf-8-sig')
assert 'PASS 86 approved' in native and 'FAIL' not in native
assert 'PASS return to world finishes' in play and 'FAIL' not in play
data['status'] = 'APPLIED_VALIDATED'
data['application'] = {
    'date': '2026-10-10',
    'authorization': 'User approved layout A: nah oke acc',
    'unityVersion': '6000.0.81f1',
    'appliedSceneSha256': hashlib.sha256(scene.read_bytes()).hexdigest(),
    'nativeFurnitureChecks': 86,
    'playModePassedChecks': sum(line.startswith('PASS ') for line in play.splitlines()),
    'nativeReport': 'applied-native-checks.txt',
    'gameplayReport': 'applied-play-checks.txt',
    'screenshots': [f'level-{level}-applied.png' for level in (3, 4, 5)],
}
assert json.dumps(data['levels'], sort_keys=True) == placement_before
manifest.write_text(json.dumps(data, indent=2) + '\n', encoding='utf-8')
print(json.dumps(data['application'], indent=2))
