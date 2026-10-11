# Playable cleanup — 2026-10-12

## Changes

- Removed 276 tracked, unused asset files and their `.meta` files: 42,982,681 bytes (42.98 MB / 40.99 MiB), excluding metadata.
- Removed the fourth stage reference from `GamePlay.unity`. The scene now references exactly three stage layouts.
- Winning levels 1 and 2 advances normally. Losing levels 1–3 retries the current level. Winning level 3 calls `GameManager.Instance.InstallFullGame()` after the victory outro without loading level 4.
- Completion is guarded against duplicate progression. A tap after completion can retry the store call if the host requires a user gesture.
- Preserved concurrent changes to confetti, victory audio/outro timing, robot animation and Luna configuration. Did not edit `Assets/GameTool`.

## Cleanup scope and evidence

`playable-cleanup-manifest.json` lists every removed asset, original size, GUID and SHA-256. All were tracked by Git and unchanged when removed, so the original versions remain recoverable from Git.

The audit traverses GUID dependencies from retained scenes, scripts, resources, editor assets and project settings. It also protects resource paths and explicit asset path strings, plus modified/untracked files. It retains all dependencies of those roots and deletes only eligible art assets and the two old scene backups outside that closure.

The retained references include unit prefabs, merging tiers, materials, animation controllers, effects, audio tables and dynamically loaded resources. No model tier was removed solely because its name appears to belong to a later level.

## Validation

- `Tools/verify_playable_cleanup.py`: PASS. No remaining asset or project setting references a removed GUID; removed files and metadata are absent; three level references remain.
- MSBuild `Assembly-CSharp.csproj`: succeeded, 0 warnings, 0 errors. See `msbuild-validation.log`.
- `git diff --check`: passed.
- Unity batch validation could not run: Unity reported this project already open in another instance. `PlayableAssetValidation.Run` is supplied as a batch entry point for a later check.

## Build and runtime limits

The 42.98 MB figure is source asset size, **not** a measured reduction in compressed Playworks output. Unreferenced non-Resources assets may already have been stripped by the exporter. No fresh Playworks HTML/package was produced or measured in this session.

Check in the open Unity/Playworks session: fresh start at level 1; win 1 and 2; lose/retry 3; win 3; automatic store call after the victory outro; no level 4; store fallback tap. Confirm visuals, merging, sound and confetti in the browser export.

The scene's `AndroidLinkGame` and `IOSLinkGame` fields are empty. Verify the intended destination is configured in Playworks or supply the store URLs before testing the final redirect on a device.
