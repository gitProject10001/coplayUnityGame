# Painterly Isometric ARPG

Top-down isometric action RPG built in Unity 6 (6000.3.2f1) with a hand-painted / Tunic-inspired visual style. Uses URP 17.3, the New Input System, AI Navigation 2.0, and the Coplay multiplayer plugin (beta).

The aesthetic target is restrained and painterly — not pixel art. Toon-shaded geometry, calibrated post-processing, and geometry-based grass rather than terrain detail meshes.

---

## Project Structure

```
Assets/
├── Scripts/
│   ├── Gameplay/              # Game logic
│   │   ├── PlayerController.cs    # Movement, dodge, melee input via New Input System
│   │   ├── PlayerIK.cs            # Two-bone IK for arm/weapon aiming
│   │   ├── CameraFollow.cs        # Smooth isometric camera follow with offset
│   │   ├── Enemy.cs               # Enemy AI, health, knockback, death
│   │   ├── EnemySpawner.cs        # Wave-based enemy spawning
│   │   ├── Bullet.cs              # Projectile lifetime, hit detection
│   │   ├── SlashVFX.cs            # Procedural arc mesh for melee slash effect
│   │   ├── CombatFeedback.cs      # Screen shake, hit-stop, flash on impact
│   │   ├── StopMotionEffect.cs    # Reduces animation update rate on hit for snap feel
│   │   └── ProceduralGenerator.cs # Runtime tile-based level generation
│   ├── Rendering/             # Runtime rendering helpers
│   │   ├── WaterRippleEmitter.cs  # Pushes wake ripples into PainterlyWater shader globals
│   │   ├── WaterSplashTrigger.cs  # Trigger volume: spawns WaterSplash prefab on fast entry
│   │   ├── GrassSpawner.cs        # GPU-instanced geometry grass (Poisson-disk placement)
│   │   ├── GrassDisplacementCamera.cs  # Renders displacement mask for grass crushing
│   │   ├── GrassDisplacementObject.cs  # Tags objects that push down grass
│   │   ├── GrassGroundSetup.cs    # Links grass to terrain/ground at runtime
│   │   ├── FloatingParticles.cs   # Ambient floating dust/mote particles
│   │   ├── PixelizeFeature.cs     # URP ScriptableRendererFeature — kept DISABLED
│   │   ├── VolumetricLightFeature.cs  # URP god-ray feature — kept DISABLED
│   │   ├── CameraTexelSnap.cs     # Snaps ortho camera to pixel-grid (sub-pixel drift fix)
│   │   └── ObjectRenderSnap.cs    # Per-object position snap before render
│   ├── Art/
│   │   └── PainterlyPalette.cs    # Runtime palette/color config shared across materials
│   └── UI/
│       └── CombatHUD.cs           # Health bar, hit counter, combat state UI
│
├── Editor/                    # Editor-only utilities (not in builds)
│   │
│   ├── ── Scene & style builders ──
│   ├── PainterlyArtBibleSceneBuilder.cs  # Builds 08_ArtBible layout from scratch
│   ├── PainterlyStyleBuilder.cs          # One-shot: applies painterly materials/lighting to a scene
│   ├── PainterlyMaterialBinder.cs        # Binds Toon_* materials to imported FBX objects
│   ├── GameplayPrefabBuilder.cs          # Assembles Gameplay prefabs
│   ├── SetupForestScene.cs               # Populates a scene with grass + foliage
│   ├── RegenerateEnvironment.cs          # Re-runs procedural environment pass
│   ├── SetEnvironmentProperties.cs       # Bulk-sets lighting/fog properties
│   │
│   ├── ── Renderer feature management ──
│   ├── PainterlyDisablePixelize.cs       # Disables PixelizeFeature via API (not YAML)
│   ├── PainterlyDisableVolumetric.cs     # Disables VolumetricLightFeature via API
│   ├── PainterlyReimportRenderer.cs      # Forces renderer asset reimport after API changes
│   ├── AddPixelizeFeature.cs             # Adds PixelizeFeature to a renderer asset
│   ├── SetupNewFeatures.cs               # General renderer feature setup utility
│   │
│   ├── ── Tuning & inspection ──
│   ├── PainterlyTuneToEditorLook.cs      # Adjusts post-processing to match target look
│   ├── TweakLighting.cs                  # Tweaks key/fill light intensity and color
│   ├── TuneWaterMaterial.cs              # Live-tweaks Toon_Water.mat parameters
│   ├── PainterlyWaterSetup.cs            # Assigns WaterNormal and wires water pool
│   ├── SetupWaterNormal.cs               # Imports and assigns WaterNormal texture
│   ├── PainterlyInspectEnv.cs            # Logs current environment/lighting state
│   ├── PainterlyInspectShot.cs           # Captures and logs a framed scene screenshot
│   ├── PainterlyRenderShot.cs            # High-res scene render to file
│   ├── PainterlyDebugBG.cs               # Debug: sets camera background to solid color
│   ├── PainterlyVertexColorDiag.cs       # Diagnostic: visualizes vertex colors on meshes
│   │
│   ├── ── Import & fix utilities ──
│   ├── FixFBXImportSettings.cs           # Sets humanoid rig + smoothing on all FBX imports
│   ├── FixAnimatorSetup.cs               # Repairs broken Animator controller references
│   ├── FixPixelArtSettings.cs            # [InitializeOnLoad] Disables MSAA/AA on startup
│   ├── FixRigidbodyInterpolation.cs      # Sets interpolation on all Rigidbodies in scene
│   ├── CheckURPSettings.cs               # Validates URP pipeline config
│   ├── CheckAnimatorController.cs        # Reports missing clips/transitions
│   ├── CheckAnimatorTransitions.cs       # Flags bad transition conditions
│   └── PainterlyVerifyImport.cs          # Verifies FBX import results match expected
│   │
│   ├── ── Grass & foliage ──
│   ├── GenerateGrassTexture.cs           # Procedurally bakes GrassBlade texture to PNG
│   └── GrassDensityBrush.cs              # Scene-view brush for painting grass density
│   │
│   ├── ── Animation helpers ──
│   ├── ForceLoopAllAnimations.cs         # Sets all clips in a controller to loop
│   └── ForceRegenerate.cs                # Forces asset database refresh
│   │
│   └── ── Water splash setup ──
│       └── CreateWaterSplash.cs          # Builds WaterSplash.prefab + wires WaterSplashTrigger
│
├── Shaders/
│   ├── ToonLit.shader             # Cel-shaded surface shader for all props/characters
│   ├── ToonLighting.hlsl          # Shared toon diffuse/specular/rim functions (included by others)
│   ├── GroundToon.shader          # Toon shader variant for terrain/ground planes
│   ├── GeometryGrass.shader       # Geometry-shader grass blades with wind + displacement
│   ├── PainterlyWater.shader      # Painterly water: depth bands, caustics, wake ripples, specular
│   ├── ParticleBillboard.shader   # Simple billboard shader for particle/sprite effects
│   ├── PixelArt.shader            # Pixelize post-process — referenced by PixelizeFeature (DISABLED)
│   ├── VolumetricLight.shader     # God-ray raymarcher — referenced by VolumetricLightFeature (DISABLED)
│   └── CustomTessellation.hlsl   # Shared tessellation helper functions
│
├── Materials/
│   ├── Painterly/                 # Canonical toon material set — all use ToonLit or variants
│   │   ├── Toon_Skin.mat
│   │   ├── Toon_Stone.mat
│   │   ├── Toon_Metal.mat
│   │   ├── Toon_Wood.mat
│   │   ├── Toon_Cloth.mat
│   │   ├── Toon_Banner.mat
│   │   ├── Toon_Foliage.mat
│   │   ├── Toon_Terrain.mat
│   │   └── Toon_Water.mat         # Uses PainterlyWater.shader
│   └── (root)                     # Shared materials: GeometryGrass, Ground, BushBillboard, DustMotes, ToonLit base, character Beta mats
│
├── Settings/                  # URP pipeline and post-processing assets
│   ├── PC_RPAsset.asset           # Desktop render pipeline asset
│   ├── PC_Renderer.asset          # Main renderer — PixelizeFeature + VolumetricFeature both DISABLED
│   ├── Mobile_RPAsset.asset       # Mobile pipeline asset
│   ├── Mobile_Renderer.asset      # Mobile renderer
│   ├── PainterlyProfile.asset     # Painterly post-processing volume profile (calibrated)
│   ├── DefaultVolumeProfile.asset # URP default volume
│   ├── SampleSceneProfile.asset   # Per-scene volume override
│   └── UniversalRenderPipelineGlobalSettings.asset
│
├── Prefabs/
│   ├── Cameras/
│   │   └── IsoARPGCamera.prefab   # Isometric camera rig (ortho, 45° tilt)
│   ├── Gameplay/
│   │   ├── Player.prefab          # Player character with controller, IK, WaterRippleEmitter
│   │   ├── PlayerCamera.prefab    # Camera follow attached to player
│   │   └── GameplayBootstrap.prefab  # Scene bootstrap: spawner + HUD wiring
│   ├── Lighting/
│   │   └── PainterlyLightRig.prefab  # Key + fill + ambient light setup for painterly look
│   └── WaterSplash.prefab         # Particle burst (spray cone + ring foam) spawned on water entry
│
├── Textures/
│   ├── Water/
│   │   └── WaterNormal.png        # Scrolling normal map for water surface
│   ├── Palettes/                  # Color palette LUT textures (legacy pixel-art era)
│   │   ├── Palette_Earthy8.png
│   │   ├── Palette_Forest.png
│   │   ├── Palette_GameBoy.png
│   │   └── Palette_Retro16.png
│   ├── BushClump.png              # Billboard bush sprite
│   ├── SmallPlant.png             # Billboard small plant sprite
│   ├── GrassMask.png              # Grass density mask
│   └── WindDistortion.png         # Wind noise texture for grass animation
│
├── Animations/                # Animator controllers and animation clips
├── Models/                    # Imported FBX geometry (props, environment pieces)
├── Art/                       # Source art, concept references
├── Input/
│   └── InputSystem_Actions.inputactions  # New Input System action map
├── Scenes/                    # Numbered test/art scenes + main scene (all at root)
│   ├── 01_ToonShaders.unity   # Shader material showcase
│   ├── 02_GrassAndFoliage.unity
│   ├── 03_PostProcessing.unity
│   ├── 04_Water.unity
│   ├── 05_ProceduralWorld.unity
│   ├── 06_Combat.unity
│   ├── 07_CameraAndFeedback.unity
│   ├── 08_ArtBible.unity      # Primary art direction reference scene (active)
│   └── SampleScene.unity      # Main gameplay scene
└── Sword and Shield Pack/     # Third-party character model + animation FBX
```

---

## Key Systems

### Painterly Rendering

The visual target is hand-painted and restrained — inspired by Tunic. The pipeline does **not** use pixelization or heavy post-processing.

- **Toon shading** — `ToonLit.shader` with stepped diffuse (2–4 bands), configurable via `ToonLighting.hlsl`. All scene props use a material from `Materials/Painterly/`.
- **Post-processing** — Calibrated via `PainterlyProfile.asset`: postExposure +0.05, contrast +8, saturation 0, vignette 0.20. Kept conservative.
- **Camera background** — `#8C9EB8` (soft cool sky).
- **Key light** — 1.05 intensity, `#FFEDC7` warm white.
- **`PixelizeFeature` and `VolumetricLightFeature`** are present on `PC_Renderer.asset` but must stay **disabled**. Disable via the `ScriptableRendererData` API (use the Editor scripts); YAML edits get overwritten by Unity's serializer.

### Water

`PainterlyWater.shader` on `Toon_Water.mat` drives the water pool in `08_ArtBible`.

- **Depth color bands** — Three-step shallow → mid → deep, controlled by `_DepthFadeDistance`, `_MidBandStart`, `_DeepBandStart`.
- **Caustic foam** — Analytic sin-field pattern (no textures). Two multiplied fields + a detail layer give organic cell-like highlights. Controlled by `_CausticScale`, `_CausticStrength`, `_CausticThreshold`.
- **Shore foam** — Depth-tested edge line where water meets geometry.
- **Wake ripples** — `WaterRippleEmitter.cs` pushes up to 16 expanding ring positions into `_WaterRipplePoints[]` each frame. Rings are computed analytically in the vertex shader. Call `WaterRippleEmitter.EmitAt(pos)` statically for one-shot impacts.
- **Splash VFX** — `WaterSplashTrigger.cs` sits on a BoxCollider trigger at the water surface. Any `Rigidbody` entering above `minSpeed` spawns `WaterSplash.prefab` (spray cone + ring foam particles) and injects a ripple.
- **Toon specular** — Posterized Blinn-Phong sun glint on the perturbed water normal.

### Geometry Grass

Grass is geometry-based, not terrain detail meshes.

- `GrassSpawner.cs` — Places GPU-instanced grass blades via Poisson-disk sampling on a ground mesh. Driven by `GeometryGrass.shader` with wind animation via `WindDistortion.png`.
- `GrassDisplacementCamera.cs` / `GrassDisplacementObject.cs` — Render a displacement mask from objects near the ground; `GeometryGrass.shader` reads this to crush blades underfoot.
- `GrassGroundSetup.cs` — Connects the grass system to the active ground mesh at runtime.
- Density can be painted in the scene view using `GrassDensityBrush.cs`.

### Combat

Top-down melee combat with sword + enemy waves.

- `PlayerController.cs` — Movement via New Input System, dodge roll, melee attack trigger.
- `SlashVFX.cs` — Procedurally builds an arc mesh for the sword swing.
- `CombatFeedback.cs` — Hit-stop (time scale dip), screen shake, material flash on damage.
- `StopMotionEffect.cs` — Drops animation update rate on hit for a snappy, punchy feel.
- `Enemy.cs` / `EnemySpawner.cs` — Simple AI with knockback, health, wave spawning.

### Camera

Isometric (orthographic) camera at a fixed 45° angle.

- `CameraFollow.cs` — Smooth follow with configurable offset and dead zone.
- `CameraTexelSnap.cs` — Snaps the orthographic camera to the sub-pixel grid each frame to prevent pixel swimming.
- `ObjectRenderSnap.cs` — Companion to `CameraTexelSnap`; snaps individual object positions before rendering for consistent pixel alignment.

### Editor Utilities

`Assets/Editor/` contains a large collection of **one-shot** setup and fix scripts. They run via Unity menu items or `[InitializeOnLoad]` — they are not included in builds.

Key groups:
- **Painterly\*** — Scene builders, material binders, renderer feature toggles, post-processing tuners, visual inspection tools.
- **Fix\*** / **Check\*** — Import fixers, Animator repairers, URP config validators.
- **Setup\*** — Renderer feature wiring, grass/water setup, prefab assembly.
- **CreateWaterSplash.cs** — Builds `WaterSplash.prefab` and wires `WaterSplashTrigger` onto the water pool in the active scene.

---

## Tech Stack

- **Engine** — Unity 6 (6000.3.2f1)
- **Render pipeline** — URP 17.3, `PC_RPAsset` / `PC_Renderer` for desktop
- **Shaders** — Shader Graph toon shaders + hand-written HLSL
- **Input** — New Input System 1.17
- **Navigation** — AI Navigation 2.0
- **Multiplayer** — Coplay plugin (beta)
- **Camera** — Orthographic, isometric 45°

---

## Important Rules

1. **Never enable PixelizeFeature or VolumetricLightFeature** — they break the painterly look. Disable via `ScriptableRendererData` API (see `PainterlyDisablePixelize.cs`), not YAML.
2. **Active art direction scene is `08_ArtBible.unity`** — treat it as the visual ground truth.
3. **Grass is geometry, not terrain detail** — do not use Unity terrain detail meshes.
4. **All water changes go through `PainterlyWater.shader` or `Toon_Water.mat`**.
5. **Short commit messages** — project convention.