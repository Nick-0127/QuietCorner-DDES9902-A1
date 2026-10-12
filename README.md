# Quiet Corner - DDES9902 A1

Publication verified 12 October 2026: the public WebGL scene loaded and mouse selection changed LIGHT to cool. See `Evidence/Publication_Verification.txt` and saved public-page screenshots. Physical headset and human usability tests remain unperformed.

By Nick Li. A greybox prototype for a brief, self-directed pause between study sessions.
Built on the previous Quiet Corner / Chapter 6 project, using Unity 6000.4.0a2.

## Play

The published WebGL preview is hosted with GitHub Pages:
https://Nick-0127.github.io/QuietCorner-DDES9902-A1/

The browser version is keyboard/mouse XR simulation, not WebXR headset support.
Wait for the scene to load, then click in the scene. Use a recent desktop browser.

- WASD: move slowly.
- Hold right mouse button and move mouse: look around.
- Point with the mouse and left-click a labelled control or a floor destination.
- Q / E: snap turn by 30 degrees.
- Esc: immediately stop sound and the optional guide, and release the cursor.

LIGHT is beside the left lantern; SOUND is beside the right garden; PAUSE is in
front of the orb. The sound is off initially. The 48-second visual guide is
optional; there is no need to match the orb. STOP ALL ends guide and sound.
RESET returns warm / silent / stopped settings and keeps your location/view.

## Open the project

Open this repository root in Unity 6000.4.0a2. Packages are restored from
`Packages/manifest.json` and `packages-lock.json`; the Library cache is excluded.

- `Assets/Scenes/QuietCorner_Chapter6_Simulation.unity`: no headset required.
- `Assets/Scenes/QuietCorner_Chapter6_OpenXR.unity`: configured OpenXR scene;
  physical headset use, stereo rendering and performance remain untested.
- `Assets/Scenes/QuietCorner.unity`: retained earlier desktop scene, for provenance.

Select the simulation scene for normal preview. The old desktop scene is not
the A1 experience. Player input in the XR scenes comes through Input System
simulated/real tracked devices and pose drivers.

## EZPZ integration

The MIT-licensed EZPZ Interaction Toolkit by Matt Cabanag is used through its
original `InteractableGeneral`, `ScalePulse` and `GenUtils` runtime scripts.
`GenUtils` has two unused editor/IDE imports removed for player compatibility.
See the included upstream attribution and MIT licence in
`Assets/EZPZ Interaction Toolkit/`.

Selection follows: controller trigger -> Chapter6XRRay -> EZPZ primary event ->
EZPZQuietCornerBridge -> QuietCornerController. The actual primary event also
invokes the toolkit's scale pulse. This is a subset integration with the
existing custom rig, not the EZPZ XR player prefab or a claim that tutorial
exercises were completed.

## Verification and limitations

`Evidence/Verification.txt`: 42 passing simulated-device integration assertions.
`Evidence/WebGL_Browser_Test.txt`: browser loading and mouse-selection evidence.
Screenshots show actual running scene frames. Windows and WebGL build summaries
are included. These are technical checks, not human usability or efficacy tests.

The visual guide, lighting and original procedural mono audio are independent
choices. There are no scores, health claims or personal telemetry in the custom
prototype code. The host can log ordinary requests. Text scaling, alternative
input, non-visual navigation, physical room-boundary integration and teleport
comfort fades remain unresolved. Hardware motion comfort is untested.

## Honest work history

The first commit imports the existing Chapter 6 source and explicitly states
that prior work was recorded on the current date. Later commits record actual
A1 changes. No timestamps are backdated, and this does not reconstruct a
semester-long commit record. AI assistance supported implementation, technical
checks, report drafting and demonstration editing. No classmate feedback or
headset trial is fabricated.

## Reproduce builds and checks

`A1Setup.PrepareAndBuildWindows` updates the two A1 XR scenes and builds a
Windows simulation. `A1Setup.BuildWebGL` builds the browser preview. Run these
as Unity batchmode execute methods; outputs are written beside the project.

Launch the Windows player with `-chapter6-verify -chapter6-evidence <folder>`
to run the integration assertions. Optional demonstration frame capture uses
`-a1-demo -a1-demo-dir <folder>`. These recording modes are disabled in normal play.

The static WebGL build in `docs/` is served by Pages. Build assets are compiled
engine outputs, not imported high-resolution design assets.

## References

- EZPZ: https://github.com/AVataRR626/EZPZ_Interaction_Toolkit
- W3C XR Accessibility User Requirements: https://www.w3.org/TR/xaur/
- Slater (2009), place illusion: https://doi.org/10.1098/rstb.2009.0138
- Headspace product precedent: https://www.headspace.com/meditation

The separately supplied assessment report explains design reasoning and ethics.
The 2:30 demonstration contains actual simulated-device runtime frames;
explanatory captions and synchronized original procedural audio were edited
after capture. It does not depict a physical headset session.
