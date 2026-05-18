Blueprint Rotation Code Findings
================================

KEYBINDINGS (DefaultKeybindings.cs)
-----------------------------------
The blueprint rotation is controlled through the "building-placement" keybindings layer:

1. rotate-cw (Rotate Clockwise): 
   - Keyboard: R key
   - Controller: DPad Right
   - new Keybinding("rotate-cw", new KeySet(KeyCode.R), new KeySet(KeyCode.None, KeyCode.None, KeyCode.None, ControllerBinding.DPadRight))

2. rotate-ccw (Rotate Counter-Clockwise):
   - Keyboard: R + Left Shift
   - new Keybinding("rotate-ccw", new KeySet(KeyCode.R, KeyCode.LeftShift))

3. mirror (Mirror/Flip):
   - Keyboard: F key
   - new Keybinding("mirror", new KeySet(KeyCode.F))

4. mirror-inverse (Inverse Mirror):
   - Keyboard: F + Left Shift
   - new Keybinding("mirror-inverse", new KeySet(KeyCode.F, KeyCode.LeftShift))

BUILDING PLACEMENT LAYER
------------------------
These keybindings are part of the "building-placement" KeybindingsLayer which also includes:
- confirm-placement (Mouse0 / Action2 on controller)
- cancel-placement (Mouse1 / Action1 on controller)
- place-checkpoint (C key / DPad Up)
- blueprint-allow-replacement (Left Shift)
- save-blueprint (Ctrl+S / Cmd+S on Mac)

ROTATION PROPERTY
-----------------
Based on the searches and code patterns found:
- BuildingModel likely has a "Rotation" or "Yaw" property
- The InputHandler.cs has been updated to use reflection to detect and log either property
- Transform objects are used in rendering (entity.Transform seen in CornerCutterSimulationRenderer)

GAME INPUT PIPELINE
-------------------
From previous investigation (noted in notes.txt):
- GameInputManager processes keybindings into InputDownstreamContext
- KeySet.GetKey(KeyCode) uses legacy UnityEngine.Input internally
- PlayerInteractionOrchestrator manages the overall interaction state

RELATED CLASSES TO INVESTIGATE FURTHER
---------------------------------------
If you need to find the actual rotation handling code, look for:
1. BuildingPlacementState or similar state classes
2. PlayerInteractionOrchestrator implementation
3. Classes that handle "building-placement" layer keybinding events
4. Blueprint placement orchestrator/manager classes

The actual rotation logic would be in the code that responds to the "rotate-cw" 
and "rotate-ccw" keybinding events within the building placement state.
