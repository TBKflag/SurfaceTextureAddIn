# SurfaceTextureAddIn

SolidWorks 2020 compatible C# add-in for distributing an extruded texture seed body over a selected planar or curved face.

## Current workflow

1. In SolidWorks, create and keep the texture seed body as a separate solid body.
2. Pre-select:
   - one seed body
   - one target face on the destination solid
3. Run either:
   - `Generate Convex Texture`
   - `Generate Concave Texture`
4. Configure spacing, height/depth, margin, rotation and curvature filtering.
5. The add-in samples the face UV domain, builds local placement frames, copies the seed body to each placement, then performs body add or body cut operations.

## Project layout

- `src/SurfaceTextureAddIn/AddIn`: COM registration and SolidWorks add-in entry point
- `src/SurfaceTextureAddIn/Commands`: command orchestration
- `src/SurfaceTextureAddIn/Services`: seed analysis, face sampling, placement generation and boolean execution
- `src/SurfaceTextureAddIn/UI`: parameter page wrapper and command dialog
- `src/SurfaceTextureAddIn/Geometry`: small math helpers
- `src/SurfaceTextureAddIn/Models`: texture parameter and placement models

## Build notes

- Target framework: `.NET Framework 4.8`
- Expected SolidWorks interop location:
  - `C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist\SolidWorks.Interop.sldworks.dll`
- If your installation path differs, override `SolidWorksInteropDir` in the project or your build environment.

## Current limitations

- First version focuses on a single selected target face.
- UV-domain sampling is an approximation for curved faces; it is not a geodesic solver.
- Very complex freeform surfaces may still require tighter spacing, lower instance counts, or future arc-length correction.
- The UI is wrapped behind `TexturePropertyManagerPage` so it can be replaced by a native SolidWorks PropertyManagerPage later without changing the core geometry pipeline.
