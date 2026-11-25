# Current A2 technical evidence

This folder contains technical results recorded before the 25 November 2026 source synchronisation. No questionnaire responses, discussion screenshots or participant identities are included.

| Record | Scope |
| --- | --- |
| Music_Runtime_24_Checks.txt | 24 Windows player smoke checks passed for the 25 November music revision |
| Windows_Build_2026-11-25.txt | Windows build succeeded, zero build errors, Unity 6000.4.0a2 |
| WebGL_Build_2026-11-25.txt | WebGL build succeeded, zero build errors, Unity 6000.4.0a2 |
| Music_Signal_Analysis.json | Measurements from actual old/new clips exported by Unity, including RMS, peaks and frequency distribution |
| FullTurn_Regression_2026-11-24.txt | 14 input-adapter checks for captured/unlocked input, recentering and accumulated full turns |
| NoEdge_Regression_2026-11-24.txt | 14 input-adapter checks including no rotation at a stationary edge |

The current published WebGL package is `QuietCorner_MusicContrast_UnityPlay_2026-11-25.zip`, SHA256 `E24154A2A29BC7AAE1F1243DDE2F60134CD2484C0BC67ED853E53D42C1204D03`. It replaced the existing [Unity Play game](https://play.unity.com/en/games/cb6480bf-c6c9-478d-bd55-268582bd716d/quiet-corner). The edit form showed the uploaded filename and success status, Update returned to the public game page, and Play loaded the scene. The online check covered publication and loading; music behaviour was checked in the Windows player and exported signals.

The clips measured approximately 0.11 RMS each, peaks below 0.40 full scale and approximately +7.9 dB normal output RMS compared with the preceding music implementation. These are digital signal measurements. Device volume, listening comfort and whether participants readily distinguish the tracks require a separate listening test.

The runtime smoke test's camera assertion assigns a rotation; it is not a mouse-input test. The separate input-adapter records address defined input states, but do not establish comfort or accessibility for human users. The denied pointer-lock observation is specific to the tested embedded session, not every browser or every Unity Play game. No physical-headset validation or post-audio-update participant test is claimed.

The original local records are retained in the sibling release directories `MusicContrast_2026-11-25` and `Camera360Fix_2026-11-24`. This repository includes results needed to interpret the current source while keeping generated builds and private assessment evidence separate.
