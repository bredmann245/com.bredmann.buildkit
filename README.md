# BuildKit

Turn any single mesh or prefab into a snap kit for the Unity editor. Pick one object, analyze it, then place, line, fill, stack and bake copies to build out buildings or anything else fast.

Works with any render pipeline. No runtime cost: the package only adds a small `BuildModule` component that stores bounds.

## Install

Package Manager > Add package from git URL:

```
https://github.com/bredmann245/com.bredmann.buildkit.git
```

Or add it to `Packages/manifest.json`:

```json
"com.bredmann.buildkit": "https://github.com/bredmann245/com.bredmann.buildkit.git"
```

## Quick start

1. Open **Tools > BuildKit > Window**.
2. Drag a prefab or scene object into **Source** and press **Analyze**. BuildKit measures the renderers and stores the size on a `BuildModule` component.
3. Press **Place**, **Line** or **Fill** to activate a scene tool, then click or drag on the ground plane.
4. Select a floor of modules and press **Stack** to duplicate it upward.
5. Select everything and press **Bake** to merge it into one mesh with a collider.

## Tools

| Tool | What it does |
| --- | --- |
| Place | Click to drop one module snapped to the grid. Hold the mouse and drag to paint. |
| Line | Drag to repeat the module along the longest axis of the drag. Walls auto rotate to match the line. |
| Fill | Drag a rectangle to fill it with modules. Good for floors and roofs. |
| Stack | Duplicates the selected objects upward by the module height, N times. |
| Bake | Merges selected objects into one mesh asset, grouped by material, with an optional mesh collider. |

## Hotkeys (while a BuildKit tool is active)

| Key | Action |
| --- | --- |
| R | Rotate 90 degrees (Shift+R rotates back) |
| M | Mirror on X |
| Page Up / Page Down | Move up or down one level |
| 1 to 9 | Switch to a variant from the window's variant list |
| Esc | Return to the Move tool |

## Scene feedback

While a tool is active the scene view shows where you are:

- **Green faces**: the ghost shares a face with a module already placed. This is the "it snapped" signal.
- **Red ghost and red outline**: the ghost overlaps an existing module. With **Skip occupied cells** on, those cells are not placed.
- **Grid overlay**: a faint grid around the cursor at the current level, with brighter lines on the world X and Z axes.
- **Level marker**: on levels above or below zero, a dotted line drops to the ground so you can see the height. The label shows the cell coordinates, level and world Y.
- **Module count**: Line and Fill show how many modules the drag will create.

Each cue can be turned off under **Feedback** in the window.

## How snapping works

The grid cell defaults to the analyzed size of the source module, so modules tile edge to edge from the world origin. Turn off **Use module size as grid** to type a custom cell size. Level height is the cell's Y size.

Placement uses the bottom center of the module's bounds, so pivots do not need to be clean.

## Roadmap

- Corner and end cap auto swap on lines
- Door and window variant slots that cut the wall they replace
- Save and load kits as ScriptableObjects
- Probuilder export hook

## License

MIT
