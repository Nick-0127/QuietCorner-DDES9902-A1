# Chapter 6 — Working in XR

This is an XR adaptation of the existing Quiet Corner scene. The original desktop scene is retained as `Assets/Scenes/QuietCorner.unity`.

## Open and demonstrate

Open this folder as a Unity project with Unity **6000.4.0a2**, the editor already installed on this computer. Select **Chapter 6 → Open Simulation (No Headset)**, then press Play. The starting scene is `QuietCorner_Chapter6_Simulation`.

| Control | Action |
| --- | --- |
| W / A / S / D | Move around the scene |
| Hold right mouse and move | Rotate the simulated headset |
| Move the mouse | Aim the simulated right controller ray |
| Left mouse button | Press the controller trigger |
| Q / E | Snap turn by 30 degrees |
| Escape | Release the pointer, stop sound and the visual guide |

Point at LIGHT, SOUND, PAUSE, STOP, RESET or a floor pad and click. A ray turns green when it is over an interactive object. Floor pads teleport the XR player.

## Chapter 6 implementation

- The flat-screen player is disabled in both Chapter 6 scenes.
- `Assets/Chapter6/Prefabs/Chapter6 XR Player.prefab` contains an XR Origin, head camera, two tracked controllers, ray interaction and locomotion.
- The head and both controllers use Input System Tracked Pose Drivers. The head camera receives position and rotation from XR input.
- Windows XR Plug-in Management has the OpenXR loader enabled, with the Oculus Touch controller profile.
- XR starts explicitly in the headset scene. Automatic native XR startup is disabled so the simulation can run without an installed headset runtime.
- The simulation feeds Unity XR Interaction Toolkit simulated HMD/controller devices. The same pose bindings and trigger pathway drive the camera and scene interactions.
- Existing lighting, ambient sound, visual guide, stop/reset controls and teleport destinations remain usable.

## Physical headset scene

The second scene, `QuietCorner_Chapter6_OpenXR`, has the simulator disabled and starts the OpenXR loader. With a supported headset and a configured native OpenXR runtime, use this scene. A real headset uses the left stick to move, the right stick to snap turn and either trigger to activate spatial controls.

No headset was available for this work. **Physical headset tracking, Quest Link, stereo rendering and on-device performance have not been tested.** Keyboard/mouse simulation demonstrates the Unity setup and interaction pathway; it does not establish that the hardware portion of Chapter 6 is complete.

On this computer, native OpenXR plugin probing can write `XR_ERROR_RUNTIME_UNAVAILABLE` to the startup log because no native runtime is installed. The simulation does not need an OpenXR session. The verification result concerns Unity errors during the interaction checks, not native headset availability.

## Project contents

- `Assets`, `Packages`, `ProjectSettings`: Unity source project.
- XR dependencies are version-pinned in `Packages/manifest.json` and resolve from the official Unity package registry. A first import on another computer may need an internet connection.
- `Assets/Editor/Chapter6Setup.cs`: scene setup, scene validation and Windows build command.
- `Assets/Scripts/Chapter6Verification.cs`: simulated-device integration checks, enabled only with the `-chapter6-verify` command-line flag. Test input travels through the tracked pose drivers and controller rays.

The XR player is a custom prefab adapted to this existing project. It is not the EZPZ Toolkit player prefab shown in the tutorial. The original project did not contain EZPZ assets.

Tutorial reference: https://docs.google.com/presentation/d/1BWvrp8mk4qeQP5SQJedoCvqZ3y3SVxcBHL_ZGkJzfZo/edit?slide=id.g3fa8feddcf4_0_1029
