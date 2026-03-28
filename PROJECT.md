# Pixel Art Action Game

Top-down action game built in Unity (URP) with a custom pixel art rendering pipeline, toon shading, and procedural environment features.

## Project Structure

```
Assets/
├── Animations/              # Animator controllers
│   └── PlayerAnimator.controller
├── Editor/                  # Editor-only utility scripts (not included in builds)
│   ├── CheckURPSettings.cs        # Validates URP pipeline configuration
│   ├── GenerateGrassTexture.cs    # Procedurally generates grass blade textures
│   ├── SetupNewFeatures.cs        # Sets up new rendering features on renderers
│   ├── SetupPixelArtShaders.cs    # Assigns toon materials and creates palette textures
│   ├── Fix*.cs / Check*.cs        # Various editor fixes for animations, imports, etc.
│   └── ...
├── Input/                   # Input system configuration
│   └── InputSystem_Actions.inputactions
├── Materials/               # Shared materials
│   ├── GrassBillboard.mat         # Billboard grass material (alpha cutout)
│   ├── PixelWater.mat             # Stylized pixel water material
│   ├── ToonLit.mat                # Base toon-lit material
│   └── ToonLit_Beta_*.mat         # Character-specific toon materials
├── Scenes/
│   └── SampleScene.unity          # Main game scene
├── Scripts/
│   ├── Gameplay/                  # Game logic scripts
│   │   ├── PlayerController.cs        # Player movement, input, attack logic
│   │   ├── PlayerIK.cs               # Inverse kinematics for arm aiming
│   │   ├── CameraFollow.cs           # Smooth camera follow with offset
│   │   ├── Enemy.cs                   # Enemy AI, health, knockback
│   │   ├── EnemySpawner.cs           # Wave-based enemy spawning
│   │   ├── Bullet.cs                 # Projectile behavior
│   │   ├── SlashVFX.cs               # Melee slash visual effects
│   │   ├── CombatFeedback.cs         # Hit feedback (screenshake, flash)
│   │   ├── StopMotionEffect.cs       # Reduces animation framerate for pixel art feel
│   │   └── ProceduralGenerator.cs    # Procedural level generation
│   └── Rendering/                 # Rendering pipeline scripts
│       ├── PixelizeFeature.cs         # URP ScriptableRendererFeature — main pixel art post-process
│       ├── VolumetricLightFeature.cs  # URP feature — volumetric god rays
│       ├── CameraTexelSnap.cs        # Snaps orthographic camera to pixel grid
│       ├── GrassSpawner.cs           # GPU-instanced billboard grass placement
│       └── ObjectRenderSnap.cs       # Snaps object positions to pixel grid before render
├── Settings/                # URP pipeline assets and profiles
│   ├── PC_RPAsset.asset / PC_Renderer.asset       # PC quality settings
│   ├── Mobile_RPAsset.asset / Mobile_Renderer.asset # Mobile quality settings
│   ├── DefaultVolumeProfile.asset                  # Post-processing volume
│   └── SampleSceneProfile.asset                    # Scene-specific volume profile
├── Shaders/                 # All shader files
│   ├── PixelArt.shader            # Post-process: pixelization, outlines, dithering, palette LUT
│   ├── PixelizePosterize.shader   # Legacy posterize shader (unused)
│   ├── ToonLit.shader             # Cel-shaded surface shader for objects
│   ├── ToonLighting.hlsl          # Shared toon lighting functions (diffuse, specular, rim)
│   ├── GrassBillboard.shader      # Billboard grass with wind animation
│   ├── PixelWater.shader          # Water with waves, depth fade, foam, refraction
│   └── VolumetricLight.shader     # Fullscreen volumetric light (raymarched shadows, Mie phase)
├── Sword and Shield Pack/   # 3rd-party character model and animations (FBX)
├── Textures/
│   ├── GrassBlade.png             # Single grass blade (procedurally generated)
│   ├── GrassBladeMulti.png        # Multi-blade grass cluster
│   └── Palettes/                  # Color palette LUT textures
│       ├── Palette_GameBoy.png
│       ├── Palette_Retro16.png
│       └── Palette_Earthy8.png
└── TutorialInfo/            # Unity template readme (can be removed)
```

## Key Systems

### Pixel Art Rendering Pipeline

The rendering pipeline converts 3D scenes into a pixel art style through multiple stages:

1. **Downscale** — Scene renders at 1/pixelScale resolution (default 4x) via `PixelizeFeature.cs`
2. **Dithering** — Bayer 4x4 ordered dither adds texture to color banding (`PixelArt.shader`)
3. **Posterization** — Reduces color depth to N steps per channel
4. **Palette LUT** — Optional mapping to a specific retro palette (GameBoy, NES-style, etc.)
5. **Outline Detection** — Roberts Cross edge detection on depth + normal buffers
6. **Upscale** — Point-filtered upscale back to screen resolution

Key files: `Shaders/PixelArt.shader`, `Scripts/Rendering/PixelizeFeature.cs`

### Outline System

Outlines use post-process edge detection on the depth and normal buffers:
- **Depth outlines** — Detect silhouettes where objects meet the background
- **Normal outlines** — Detect surface orientation changes (cube edges, etc.)
- **Convex-only mode** — Optional filter to only show outlines on outer silhouettes
- Thresholds are tunable in the PixelizeFeature inspector

### Toon Shading

Objects use `ToonLit.shader` for cel-shaded lighting:
- Stepped diffuse bands (configurable 2-4 steps)
- Optional specular highlight and rim lighting
- Shadow caster and DepthNormals passes for outline detection

### Volumetric Lighting

`VolumetricLightFeature.cs` + `VolumetricLight.shader` add god ray effects:
- Raymarched shadow sampling along camera rays
- Mie phase scattering
- 3D noise for density variation
- Posterized output for pixel art consistency

### Environment

- **Grass** — GPU-instanced billboards placed via Poisson-disk sampling (`GrassSpawner.cs`)
- **Water** — Vertex-displaced waves with depth fade, foam lines, and refraction (`PixelWater.shader`)

### Camera System

- `CameraFollow.cs` — Smooth follow with configurable offset
- `CameraTexelSnap.cs` — Snaps camera to pixel grid to prevent pixel swimming
- `ObjectRenderSnap.cs` — Per-object position snapping for consistent pixel rendering

## Tech Stack

- **Unity 6** (URP — Universal Render Pipeline)
- **Render Graph API** for custom render passes
- **Input System** package for player controls
- **Orthographic camera** (top-down, orthoSize=7)
