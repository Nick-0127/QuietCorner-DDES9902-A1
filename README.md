# Quiet Corner

DDES9902 Assessment 2 prototype by Nick Li, z5745451. Current source revision: 25 November 2026.

Quiet Corner is a small interactive room for a self-directed break between study sessions. Visitors can change the lighting and music, try an optional guided break, sit at a sofa or table, and use a water/tea dispenser. It is an exploratory design prototype and makes no therapeutic or productivity claims.

## Play the current version

[Quiet Corner on Unity Play](https://play.unity.com/en/games/cb6480bf-c6c9-478d-bd55-268582bd716d/quiet-corner)

Use a desktop browser with a keyboard and mouse. This is a browser simulation of a 3D environment; it does not provide WebXR headset support. The current music revision was uploaded to this existing game on 25 November 2026.

The [GitHub Pages root](https://Nick-0127.github.io/QuietCorner-DDES9902-A1/) retains the historical October A1 build. Use Unity Play for current testing. [Three separately playable feedback-round archives](https://nick-0127.github.io/QuietCorner-DDES9902-A1/versions/) are also available under `docs/versions/`; they preserve retained published builds without changing their original contents.

## Controls

| Action | Control |
| --- | --- |
| Start looking | Click inside the game, then move the mouse |
| Select a labelled control | Aim the centre dot at it and left-click |
| Move | WASD or the arrow keys |
| Move faster | Hold Shift while moving |
| Turn in steps | Q turns left; E turns right by 30 degrees when no nearby chair takes priority |
| Sit at a nearby table chair | Press E or aim at the chair and click |
| Sofa and nearby seating | F toggles nearby seating; sitting on the sofa starts the guide |
| Stand | E or F while seated |
| Stop music and guidance | Esc or STOP ALL; Esc also releases the pointer |

In the tested Unity Play embedded session, pointer-lock requests were rejected. When capture is unavailable, press **R** to pause turning, move the pointer back to the centre, then press **R** to resume. Alternatively, hold the right mouse button while recentering and release it to continue looking. Repeated sweeps allow a complete turn; Q/E are another turning option. The view does not rotate automatically when the pointer is stationary at an edge. Where pointer capture is permitted, relative mouse movement can continue turning normally.

Near a table chair, E prioritises sitting rather than turning. Movement is disabled while seated. Standing restores the saved standing position and view.

## Activities

- **LIGHT** switches between warm and cool environments.
- **MUSIC** enables or disables the matching music. Music starts off. Changing the light crossfades between the two tracks.
- **START GUIDED BREAK** starts a 48-second sequence with spoken breathing cues, matching captions and an animated orb. The same control stops it. Sitting on the sofa also starts the guide; standing ends it.
- **Table chairs** provide optional seating without starting the guide.
- **Water and tea dispenser** offers WATER or TEA, shows preparation, and presents a cup to collect. Collect the current drink before selecting another.
- **RESET** restores warm lighting with music and guide stopped while preserving location and view.

The 25 November audio revision makes the existing light choice easier to hear: warm light uses low, rounded chords and a slow pluck; cool light uses higher open chords and clear bell notes. The generated tracks have matched average levels. Normal playback is approximately 7.9 dB higher in digital RMS level than the previous build. Music lowers during spoken guidance and returns gently afterward. This is a signal measurement, not a claim about sound pressure or perceived loudness on every device.

## Open and build

Open this repository root in **Unity 6000.4.0a2**. Dependencies are declared in `Packages/manifest.json` and `Packages/packages-lock.json`. Unity restores its Library cache; the cache is not committed.

- `Assets/Scenes/QuietCorner_Chapter6_Simulation.unity` is the current desktop/browser simulation.
- `Assets/Scenes/QuietCorner_Chapter6_OpenXR.unity` is the configured OpenXR scene. Physical-headset use and performance remain untested.
- `Assets/Scenes/QuietCorner.unity` is an earlier retained scene, not the current experience.

Current batchmode build entry points are `A1Setup.BuildMusicContrastWebGL` and `A1Setup.BuildMusicContrastWindows`. For example:

```text
Unity.exe -batchmode -quit -projectPath <repository> -executeMethod A1Setup.BuildMusicContrastWebGL -logFile <webgl-build-log>
Unity.exe -batchmode -quit -projectPath <repository> -executeMethod A1Setup.BuildMusicContrastWindows -logFile <windows-build-log>
```

Outputs are written to a sibling directory, `MusicContrast_2026-11-25/WebGL` or `MusicContrast_2026-11-25/Windows`. For Unity Play, ZIP the WebGL folder contents so that `index.html` is at the archive root. Older build methods remain for local iteration history.

The normal browser preview uses direct camera input. The native XR scene retains tracked/simulated device input and pose drivers. Verification modes are opt-in and disabled in ordinary play.

## EZPZ integration

The project uses the MIT-licensed [EZPZ Interaction Toolkit](https://github.com/AVataRR626/EZPZ_Interaction_Toolkit) by Matt Cabanag. Included original runtime scripts are `InteractableGeneral`, `ScalePulse` and `GenUtils`; two unused editor/IDE imports were removed from `GenUtils` for player compatibility. Attribution and licence are in `Assets/EZPZ Interaction Toolkit/`.

Selections use the toolkit's primary interaction event, which invokes `EZPZQuietCornerBridge` and the scene behaviour. Browser selections and tracked-controller selections share this event route. This is a subset integration with a custom rig, not the EZPZ XR player prefab.

## Verification and limitations

[Current technical evidence](Evidence/A2_Current/README.md) records the following:

- 25 November Windows and WebGL builds succeeded with zero build errors.
- The Windows player passed 24 runtime smoke checks, including music level, bounded crossfade, voice ducking and recovery, immediate stop, subsequent-frame silence, captions and reset.
- Actual clips exported by Unity had matched 0.11 RMS levels, peaks below 0.40 full scale and smooth loop boundaries.
- 24 November input-adapter regression records contain 14 full-turn/recentering checks and 14 stationary-edge checks. These are automated adapter checks, not 28 participant trials.
- The newly published Unity Play scene loaded. Online loading verification does not replace the Windows audio tests or independent listener feedback.

To repeat the current runtime smoke checks, launch the Windows build with:

```text
QuietCorner.exe -qc-verify -qc-output <evidence-folder>
```

To export the actual generated clips from Unity, use `QuietCornerMusicVerification.Export` with `-qc-audio-output <output-folder>`. The resulting WAV files and CSV support signal inspection. No new build is claimed to have been tested by classmates merely because the technical checks pass.

Physical-headset comfort, non-visual navigation, text scaling and alternative input remain unvalidated. The guide is optional, music starts silent, and STOP ALL/Esc stop music and guidance. The custom code does not intentionally submit personal telemetry; hosting and survey services have their own data practices.

## History and evidence

[CHANGELOG.md](CHANGELOG.md) summarises the recorded local iterations. Git commits use their actual creation dates; this update does not reconstruct or backdate a semester-long commit history. The older `Evidence/` files and `docs/` deployment refer to the historical A1 version unless explicitly labelled otherwise.

The independent [Test 1](https://nick-0127.github.io/QuietCorner-DDES9902-A1/versions/test1/), [Test 2](https://nick-0127.github.io/QuietCorner-DDES9902-A1/versions/test2/) and [Test 3](https://nick-0127.github.io/QuietCorner-DDES9902-A1/versions/test3/) URLs were restored from existing ZIPs on 25 November 2026. Their original recorded publication dates were 3, 10 and 24 November. The forms did not log build hashes, so these are the retained published builds preceding the rounds, not a verified respondent-to-build mapping. See the [archive provenance](docs/versions/README.md) and [file hashes](docs/versions/archive_manifest.json).

AI assistance supported implementation, verification and report drafting. The procedural music is generated by the project code; English guide clips were generated locally with Microsoft Zira. No peer recordings or fabricated participant results were used. Questionnaire responses and class discussion screenshots are retained in the assessment report/evidence package, separately from this public source repository.

## References

- Cabanag, M. [EZPZ Interaction Toolkit](https://github.com/AVataRR626/EZPZ_Interaction_Toolkit), MIT licence.
- W3C. [XR Accessibility User Requirements](https://www.w3.org/TR/xaur/).
- Slater, M. (2009). [Place illusion and plausibility can lead to realistic behaviour in immersive virtual environments](https://doi.org/10.1098/rstb.2009.0138).
