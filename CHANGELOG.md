# Quiet Corner change history

Dates below describe retained local implementation and verification records. The A2 source was synchronised to GitHub on 25 November 2026 in a current-date commit; these entries do not imply separate historical commits or participant tests on each date.

## 25 November 2026

- Increased normal music playback by approximately 7.9 dB RMS relative to the previous release.
- Differentiated warm low-register chords and slow plucks from cool higher-register open chords and bell notes.
- Matched generated track RMS, retained smooth light/music crossfades, and added gentle music ducking during spoken cues.
- Cleared the music envelope when stopping to preserve immediate silence.
- Windows runtime verification passed 24 checks. Windows and WebGL builds succeeded with zero build errors. Actual Unity-generated audio was exported and measured.
- Replaced the existing Unity Play game build. The public scene loaded; independent participant evaluation of this final audio change remains outstanding.

## 24 November 2026

- Diagnosed rejected pointer-lock requests in the tested embedded Unity Play session.
- Added explicit R-toggle and held-RMB recentering without turning, plus Q/E step-turn fallback. Repeated sweeps allow an accumulated full turn when pointer capture is unavailable.
- Preserved chair priority on E and disabled stationary-edge rotation.
- Passed 14 full-turn/recenter adapter checks and 14 no-edge adapter checks.
- Moved the sofa label away from the central control cluster and shortened first-use instructions.

## 23 November 2026

- Removed automatic turning at stationary screen edges while retaining mouse-driven looking.
- A later rear-view report showed that denied-capture behaviour still required explicit recentering; this was addressed on 24 November.

## 21 and 22 November 2026

- Added Shift movement at 1.8 times walking speed, three tables with six chairs, and a water/tea vending machine.
- Added drink preparation, collection and recovery states through the existing EZPZ interaction event route.
- Added E to sit at a nearby chair and stand again, with chair interaction taking precedence over right turning.
- Retained local cafe and seating runtime verification records; these technical checks are distinct from peer feedback.

## 20 November 2026

- Added warm and cool procedural music tied to lighting, a seated sofa activity, spoken guide cues with captions, and arrow-key movement.
- Renamed START PAUSE to START GUIDED BREAK and used a 48-second optional guide.
- Added matching browser/desktop mouse-look instructions and kept stop/reset actions available.

## 9 and 10 November 2026

- Responded to reports of blocked text, too much first-use information and mouse-looking failure.
- Reduced and separated instructions, introduced START HERE, and applied depth-tested world text.
- Separated browser camera input from simulated XR pose drivers and iterated through drag and click-once mouse-follow implementations.
- Added cursor hiding while looking. A temporary automatic-edge-turn fallback was later removed on 23 November; it is not current behaviour.

## Historical A1 baseline

- Retained a Chapter 6/OpenXR source baseline, integrated the original EZPZ event scripts, and recorded simulated-device technical checks.
- The GitHub history through 12 October 2026 records the A1 implementation and initial Pages deployment. Earlier work was imported and documented on that actual date rather than backdated.
- `docs/` remains the historical A1 GitHub Pages build. Current A2 testing is hosted on the [existing Unity Play game](https://play.unity.com/en/games/cb6480bf-c6c9-478d-bd55-268582bd716d/quiet-corner).
