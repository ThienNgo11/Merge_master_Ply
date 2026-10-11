"""Check exported PCM data, not just whether Luna created an audio file."""
import json
from pathlib import Path
import wave

root = Path(__file__).resolve().parents[1]
sounds = root / 'LunaTemp/stage1/assets/sounds'
targets = {
    'Assets/Resources/Audio/Music/FL/FL_Bg.ogg',
    'Assets/Resources/Audio/SFX/FL/FL_Win.ogg',
}
results = []
for metadata in sounds.glob('*.json'):
    asset = json.loads(metadata.read_text(encoding='utf-8'))
    if asset.get('path') not in targets:
        continue
    wav = metadata.with_suffix('.wav')
    if not wav.exists():
        results.append({'asset': asset['path'], 'status': 'MISSING_WAV'})
        continue
    with wave.open(str(wav), 'rb') as audio:
        pcm = audio.readframes(audio.getnframes())
        # Unity's PCM export uses signed 16-bit samples; all zero bytes mean silence.
        if audio.getsampwidth() != 2:
            raise ValueError('Unexpected PCM format: ' + str(wav))
        nonzero = sum(value != 0 for value in pcm)
        results.append({
            'asset': asset['path'],
            'status': 'NON_SILENT' if nonzero else 'SILENT',
            'seconds': round(audio.getnframes() / audio.getframerate(), 3),
            'pcm_bytes': len(pcm),
            'nonzero_bytes': nonzero,
        })
for missing in targets - {item['asset'] for item in results}:
    results.append({'asset': missing, 'status': 'NOT_EXPORTED'})
print(json.dumps(results, indent=2))
raise SystemExit(any(item['status'] != 'NON_SILENT' for item in results))
