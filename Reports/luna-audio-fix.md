# Luna audio investigation — 2026-10-12

## Confirmed findings

- User confirmed combat/purchase/merge sounds work; background music and victory sound do not.
- Both `FL_Bg` and `FL_Win` exist in the existing Luna resource mapping and exported sound files. Neither was removed by asset cleanup.
- Existing `FL_Bg` WAV contains **17,422,892 PCM bytes, all zero** (197.538 seconds). The editor log reports: `Can't get data from streamed samples for audio clip "FL_Bg"` and recommends DecompressOnLoad.
- Existing `FL_Win` WAV has nonzero PCM samples. Its victory call is present in the exported UIDirector coroutine. The exact runtime reason this UI-source sound was inaudible was not isolated.

## Changes

- Set `FL_Bg` to Decompress On Load, including its Android override, and enable Preload Audio Data. Enable preload for `FL_Win` as well.
- Assign both clips directly to AudioDirector in GamePlay so the two playable cues have explicit serialized dependencies.
- Retry pending music initialization and restart once on the first gameplay touch. Configure the loop before starting playback.
- Play the victory clip through the existing combat emitter, which the user confirms is audible, instead of the UI source/resource lookup path. Retain the previous lookup as a fallback for scenes without the new assignment.
- No changes to the shared GameTool directory. Level-3 completion/store flow preserved.

## Validation and remaining check

MSBuild succeeded with 0 errors and one pre-existing unused-field warning for `GameplayCore.fuseIndicatorsLive`. `git diff --check` passed.

The open Unity instance remained on `Estimating assets size`; the cancel action timed out. A new Luna export and audible browser playback could not be verified in this session. Chrome also prevented pasting runtime queries into DevTools; that protection was left intact.

Once Unity is responsive, reimport/refresh the modified audio clips and scene, rebuild Luna, then reload the preview. Run `Tools/check_luna_audio.py` after export: both clips must report `NON_SILENT`. Check that music loops after the first touch, victory audio plays at the end of each won level, and level 3 still opens the store after its outro.

The remaining old export is expected to fail the PCM check until rebuilt; do not interpret successful C# compilation as a successful audio export.
