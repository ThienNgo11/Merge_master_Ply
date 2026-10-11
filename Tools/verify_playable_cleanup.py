"""Verify the applied cleanup against its saved manifest and remaining assets."""
import json
import pathlib
import re

root = pathlib.Path(__file__).resolve().parents[1]
manifest = json.loads((root / 'Reports/playable-cleanup-manifest.json').read_text())
deleted = {f['guid']: f['path'] for f in manifest['files'] if f['guid']}
errors = []
for entry in manifest['files']:
    for suffix in ('', '.meta'):
        if (root / (entry['path'] + suffix)).exists():
            errors.append('Still exists: ' + entry['path'] + suffix)
    if entry['path'].startswith('Assets/GameTool/'):
        errors.append('Protected GameTool asset removed: ' + entry['path'])
for folder in ('Assets', 'ProjectSettings'):
    for path in (root / folder).rglob('*'):
        if not path.is_file():
            continue
        data = path.read_bytes()
        for guid in set(re.findall(rb'guid: ([0-9a-f]{32})', data)):
            guid = guid.decode()
            if guid in deleted:
                errors.append(f'{path.relative_to(root)} references removed {deleted[guid]}')
scene = (root / 'Assets/_ProjectTemplate/Scenes/GamePlay.unity').read_text()
stages = scene.split('  stageAdvancement:\n', 1)[1].split('  exclusiveGuideStages:', 1)[0]
if len(re.findall(r'guid:', stages)) != 3:
    errors.append('Expected exactly three stage layout references')
result = {
    'removed_assets': len(manifest['files']),
    'removed_source_bytes_excluding_meta': manifest['source_bytes'],
    'remaining_references_to_removed_guids': errors,
    'status': 'PASS' if not errors else 'FAIL',
    'limitations': 'Static reference validation only; Unity batch blocked by another open instance. Playworks export and browser gameplay not verified.',
}
(root / 'Reports/playable-cleanup-verification.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
print(json.dumps(result, indent=2))
raise SystemExit(bool(errors))
