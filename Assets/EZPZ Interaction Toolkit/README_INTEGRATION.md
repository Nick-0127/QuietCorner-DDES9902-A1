# EZPZ Interaction Toolkit integration

This project uses the MIT-licensed EZPZ Interaction Toolkit by Matt Cabanag
(https://github.com/AVataRR626/EZPZ_Interaction_Toolkit).

The copied `InteractableGeneral`, `ScalePulse`, and `GenUtils` scripts are the
small runtime subset used by the Quiet Corner controls. `EZPZQuietCornerBridge`
adds `InteractableGeneral` to each labelled control and routes its
`onPrimaryInteract` event into the existing Quiet Corner action. The XR ray
therefore exercises the same EZPZ event layer in the headset and in the
simulated-device scene.

The full upstream toolkit remains available in the working folder used during
development; only the runtime subset needed by this prototype is included in
the student project to keep the repository small.
