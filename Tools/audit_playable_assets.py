"""Conservative GUID/resource audit; writes a reviewable manifest, never deletes files."""
import collections
import hashlib
import json
import pathlib
import re
import subprocess

ROOT = pathlib.Path(__file__).resolve().parents[1]
files = {p.relative_to(ROOT).as_posix(): p for p in (ROOT / 'Assets').rglob('*') if p.is_file() and p.suffix != '.meta'}
guid_path = {}
for name, path in files.items():
    meta = pathlib.Path(str(path) + '.meta')
    if meta.exists():
        match = re.search(r'^guid: ([0-9a-f]{32})', meta.read_text(errors='ignore'), re.M)
        if match:
            guid_path[match[1]] = name
refs = {}
texts = {}
for name, path in files.items():
    data = path.read_bytes()
    text = data.decode('utf-8', errors='ignore') if b'\0' not in data[:4096] else ''
    meta = pathlib.Path(str(path) + '.meta')
    text += '\n' + (meta.read_text(errors='ignore') if meta.exists() else '')
    texts[name] = text
    refs[name] = {guid_path[g] for g in re.findall(r'guid: ([0-9a-f]{32})', text) if g in guid_path and guid_path[g] != name}

# Retain scripts, editor tooling, packages, every existing scene and all non-art files.
# Resource audio is eligible only if neither serialized references nor resource paths use it.
art_roots = ('Assets/ROBOT/', 'Assets/RefVFX/', 'Assets/Layer Lab/', 'Assets/_ProjectTemplate/')
art_ext = {'.fbx', '.obj', '.png', '.jpg', '.jpeg', '.tga', '.psd', '.mat', '.anim', '.controller', '.overridecontroller', '.prefab', '.asset', '.mp3', '.wav', '.ogg', '.mp4', '.blend'}
eligible = {n for n in files if n.startswith(art_roots) and pathlib.Path(n).suffix.lower() in art_ext and '/Editor/' not in n and '/Resources/' not in n}
eligible.update(n for n in files if n.startswith('Assets/_ProjectTemplate/Scenes/') and n.endswith(('.bak', '.pre_cleanup_bak')))
eligible.update(n for n in files if n.startswith('Assets/Resources/Audio/') and pathlib.Path(n).suffix.lower() in {'.mp3', '.wav', '.ogg'})
status = subprocess.check_output(['git', 'status', '--porcelain', '-z'], cwd=ROOT).decode().split('\0')
dirty = [line[3:].replace('\\', '/') for line in status if line]
eligible = {n for n in eligible if not any(n == d or n.startswith(d.rstrip('/') + '/') or n + '.meta' == d for d in dirty)}
roots = set(files) - eligible
source_and_tables = '\n'.join(t for n, t in texts.items() if pathlib.Path(n).suffix in {'.cs', '.asset', '.prefab', '.unity', '.json'})
for n in eligible:
    if n in source_and_tables:
        roots.add(n)
    if '/Resources/' in n:
        resource = n.split('/Resources/', 1)[1].rsplit('.', 1)[0]
        if resource in source_and_tables:
            roots.add(n)
for p in (ROOT / 'ProjectSettings').glob('*'):
    if p.is_file():
        roots.update(guid_path[g] for g in re.findall(r'guid: ([0-9a-f]{32})', p.read_text(errors='ignore')) if g in guid_path)
reachable = set()
todo = list(roots)
while todo:
    n = todo.pop()
    if n in reachable:
        continue
    reachable.add(n)
    todo.extend(refs[n] - reachable)
candidates = sorted(eligible - reachable)
stats_path = ROOT / 'LunaTemp/stage3/stats.json'
exported = collections.Counter()
if stats_path.exists():
    stats = json.loads(stats_path.read_text())
    for group in stats.get('types', []):
        for item in group.get('assets', []):
            exported[item.get('path', '')] += item.get('size', 0)
report = {
    'method': 'GUID closure from all retained assets/scenes/scripts/resources/settings, plus dynamic resource path strings; modified/untracked files protected',
    'files': [{'path': n, 'bytes': files[n].stat().st_size,
               'sha256': hashlib.sha256(files[n].read_bytes()).hexdigest(),
               'guid': next((g for g, p in guid_path.items() if p == n), None),
               'previous_export_bytes': exported[n] if stats_path.exists() else None} for n in candidates],
    'source_bytes': sum(files[n].stat().st_size for n in candidates),
    'previous_export_bytes': sum(exported[n] for n in candidates) if stats_path.exists() else None,
}
out = ROOT / 'Reports'
out.mkdir(exist_ok=True)
(out / 'playable-cleanup-candidates.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps({k: v for k, v in report.items() if k != 'files'}, indent=2))
print('Candidates:', len(candidates))
groups = collections.Counter()
for n in candidates:
    groups['/'.join(n.split('/')[:3])] += files[n].stat().st_size
print('Source bytes by folder:', groups)
print('Largest previously exported candidates:', sorted([(exported[n], n) for n in candidates if exported[n]], reverse=True)[:30])
print('Largest exported assets:', exported.most_common(20))
