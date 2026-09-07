# Spatial Blueprint MR

An initial Unity prototype for placing an architectural floor plan into a room at life size. It imports **ASCII DXF** drawings, identifies wall layers, renders them as walk-through 3D walls, and lets the user calibrate and position the plan.

## What this first version does

- Parses DXF `LINE`, `LWPOLYLINE`, and legacy `POLYLINE` entities.
- Reads common DXF drawing units (`$INSUNITS`) and converts the drawing to Unity metres.
- Prefers layers whose name contains `WALL`; if none are found, it renders every supported line entity.
- Extrudes the imported wall centre lines into wall meshes with colliders.
- Supports manual placement, rotation, height, locking, calibration, and local persistence.
- Runs in the Unity Editor first, then can be connected to OpenXR hand/controller interaction for a headset build.

Native `.dwg` is a proprietary binary format, so export a drawing as an **ASCII DXF** from AutoCAD (or convert it with a licensed DWG conversion tool) before importing it here. The sample plan is in `Assets/StreamingAssets/SampleStudio.dxf`.

## Open it

1. In Unity Hub, add this folder as a project using **Unity 6.6 (6000.6.0f1) or a newer Unity 6.6 patch**.
2. On first open, Unity resolves the Unity 6.6-compatible **Input System 1.20.0**, **XR Interaction Toolkit 3.6.0**, **XR Plug-in Management 4.7.0**, and **OpenXR 1.18.0** packages. Enable OpenXR for your headset in Project Settings > XR Plug-in Management.
3. Set Project Settings > Player > Other Settings > **Active Input Handling** to **Input System Package (New)**. This is required because OpenXR does not support Unity's legacy Input Manager.
4. Import the XR Interaction Toolkit's **Starter Assets** sample, then create an **XR Origin (Action-based)** through `GameObject > XR`. This provides current controller bindings and an in-headset camera rig.
5. Select `Tools > Spatial Blueprint MR > Create Unity 6 Starter Scene`, save the scene, and press Play. A sample studio plan is automatically generated.
4. In the `DxfBlueprintImporter` component, assign another `.dxf` file as **Default Plan** or use `Tools > Spatial Blueprint MR > Copy DXF into Project…`.

The sample uses feet (one DXF unit equals one foot), so its wall geometry is created in metres automatically. For a project with no declared DXF units, set **Fallback Units** in the inspector before importing. The desktop preview controls use Unity's current Input System, so they remain compatible with OpenXR.

## Calibration and placement

The imported root is already 1:1 when its DXF units are correct. If a drawing's scale is wrong, call `Calibrate(referenceLengthInDrawingUnits, measuredLengthInMetres)` on `PlanPlacementController`; the script applies a uniform correction factor. It also exposes `Nudge`, `Rotate`, `Raise`, `LockPlacement`, and `UnlockPlacement`, so XR buttons or hand gestures can call those methods directly.

In Editor/desktop simulation, use the arrow keys to move the plan, `Q`/`E` to rotate it, `Page Up`/`Page Down` to raise/lower it, brackets to adjust scale, `L` to lock/unlock it, and `P` to save its placement. These controls are only a development fallback; a production headset build should call the same public methods from XR grab and UI actions.

## Recommended next milestones

1. Add an `XRGrabInteractable` to the generated `BlueprintRoot` and map two-hand grab to scale/rotate the root.
2. Add a room-anchor adapter for the target headset so saved plans restore relative to a persistent spatial anchor rather than only the current XR origin.
3. Render doors, windows, furniture, electrical, and plumbing as separate selectable layers.
4. Match detected room planes against imported wall segments and use a least-squares fit to recommend alignment.
5. Add a native file picker for the final build platform and import a DXF into the app's persistent-data `Blueprints` directory.

## Architecture

```text
DXF file
  -> DxfFloorPlanParser (entities, layers, declared units)
  -> DxfBlueprintImporter (wall-layer filtering, metres conversion)
  -> BlueprintModel (wall meshes + plan overlay)
  -> PlanPlacementController (calibration, transform, persistence)
```
