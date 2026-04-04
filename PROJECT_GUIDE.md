# Unity Project Learning Guide
## Tunic-Style Action Game - Complete Technical Reference

---

## Table of Contents

1. [What is Unity? Key Concepts](#1-what-is-unity-key-concepts)
2. [Project Structure](#2-project-structure)
3. [Rendering Pipeline (URP)](#3-rendering-pipeline-urp)
4. [Custom Shaders](#4-custom-shaders)
5. [Gameplay Systems](#5-gameplay-systems)
6. [Animation & IK](#6-animation--ik)
7. [Procedural Generation](#7-procedural-generation)
8. [GPU Instancing & Performance](#8-gpu-instancing--performance)
9. [UI System](#9-ui-system)
10. [Input System](#10-input-system)
11. [Physics & Collision](#11-physics--collision)
12. [Component Interaction Map](#12-component-interaction-map)
13. [Installed Packages](#13-installed-packages)
14. [Glossary](#14-glossary)

---

## 1. What is Unity? Key Concepts

### The Entity-Component System

Unity is built around **GameObjects** and **Components**. Every object in your scene (the player, a rock, the camera, a light) is a **GameObject**. By itself, a GameObject is just an empty container with a position/rotation/scale (its **Transform**). You give it behavior by attaching **Components**.

```
GameObject "Player"
  |-- Transform          (position, rotation, scale)
  |-- MeshRenderer       (draws the 3D mesh on screen)
  |-- Rigidbody          (enables physics: gravity, forces, collisions)
  |-- Animator           (plays animations)
  |-- PlayerController   (your custom C# script - a component!)
  |-- PlayerIK           (your custom IK script - also a component!)
```

**Key principle:** Components are modular. You can mix and match them. A "Bullet" and a "Player" are both GameObjects, they just have different components attached.

### MonoBehaviour Lifecycle

Every C# script you write inherits from `MonoBehaviour`. Unity calls specific methods on it at specific times:

| Method | When it runs | Used for |
|--------|-------------|----------|
| `Awake()` | Once, when the object is created | Very early initialization |
| `Start()` | Once, before the first frame | Setup (find references, spawn children) |
| `Update()` | Every frame (~60/sec) | Input, timers, state logic |
| `FixedUpdate()` | Fixed interval (50/sec default) | Physics movement (Rigidbody) |
| `LateUpdate()` | After all Update() calls | Camera follow (needs final positions) |
| `OnAnimatorIK()` | During animation processing | Inverse Kinematics adjustments |

**This project uses all of these.** For example:
- `PlayerController.Update()` reads input and manages combat states
- `PlayerController.FixedUpdate()` moves the Rigidbody
- `CameraFollow.LateUpdate()` follows the player after movement is done
- `PlayerIK.OnAnimatorIK()` adjusts feet/hands after animation plays

### Scenes

A **Scene** is a level or screen. It contains all the GameObjects for that context. This project has one main scene: `Assets/SampleScene.unity`. When you open Unity, this scene loads and you see the game world.

### Prefabs

A **Prefab** is a saved template of a GameObject. This project creates most objects **procedurally** (via code at runtime) instead of using prefabs. The player, enemies, rocks, trees, grass - all spawned from scripts.

---

## 2. Project Structure

```
My project for codplay/
|
|-- Assets/                          <-- ALL game content lives here
|   |-- Animations/
|   |   |-- PlayerAnimator.controller   <-- State machine for player animations
|   |
|   |-- Editor/                         <-- Editor-only tools (19 scripts)
|   |   |-- CheckAnimatorController.cs
|   |   |-- ConvertPlayerToHumanoid.cs
|   |   |-- FixAnimatorSetup.cs
|   |   |-- SetupForestScene.cs
|   |   |-- ... (debug/setup utilities)
|   |
|   |-- Materials/                      <-- 9 materials (shader + settings)
|   |   |-- BushBillboard.mat
|   |   |-- DustMotes.mat
|   |   |-- GeometryGrass.mat
|   |   |-- Ground.mat
|   |   |-- PixelWater.mat
|   |   |-- SmallPlant.mat
|   |   |-- ToonLit.mat
|   |   |-- ToonLit_Beta_Joints_0.mat
|   |   |-- ToonLit_Beta_Surface_0.mat
|   |
|   |-- Scripts/
|   |   |-- Gameplay/                   <-- Game logic
|   |   |   |-- PlayerController.cs        (901 lines - player state machine)
|   |   |   |-- Enemy.cs                   (517 lines - enemy AI)
|   |   |   |-- Bullet.cs                  (89 lines - projectile)
|   |   |   |-- CombatFeedback.cs          (81 lines - screen shake/hitstop)
|   |   |   |-- EnemySpawner.cs            (46 lines - spawning)
|   |   |   |-- CameraFollow.cs            (57 lines - camera tracking)
|   |   |   |-- SlashVFX.cs                (168 lines - attack arcs)
|   |   |   |-- ProceduralGenerator.cs     (531 lines - world building)
|   |   |
|   |   |-- Rendering/                 <-- Visual systems
|   |   |   |-- PixelizeFeature.cs         (232 lines - pixel art post-process)
|   |   |   |-- VolumetricLightFeature.cs  (133 lines - god rays)
|   |   |   |-- GrassSpawner.cs            (241 lines - 4000+ grass instances)
|   |   |   |-- FloatingParticles.cs       (155 lines - dust motes)
|   |   |   |-- CameraTexelSnap.cs         (71 lines - pixel grid snap)
|   |   |   |-- GrassDisplacementCamera.cs (grass bending camera)
|   |   |   |-- GrassDisplacementObject.cs (grass bending trigger)
|   |   |   |-- GrassGroundSetup.cs        (grass initialization)
|   |   |   |-- ObjectRenderSnap.cs        (object pixel snapping)
|   |   |
|   |   |-- UI/
|   |       |-- CombatHUD.cs               (180 lines - health/stamina/pips)
|   |
|   |-- Shaders/                        <-- 8 custom shaders + 2 HLSL includes
|   |   |-- ToonLit.shader                 (main object shader)
|   |   |-- GroundToon.shader              (procedural ground)
|   |   |-- GeometryGrass.shader           (tessellated grass blades)
|   |   |-- PixelWater.shader              (animated water)
|   |   |-- PixelArt.shader                (post-process pixelation)
|   |   |-- VolumetricLight.shader         (ray-marched god rays)
|   |   |-- PixelizePosterize.shader       (color reduction)
|   |   |-- ParticleBillboard.shader       (camera-facing particles)
|   |   |-- ToonLighting.hlsl              (shared toon lighting functions)
|   |   |-- CustomTessellation.hlsl        (tessellation infrastructure)
|   |
|   |-- Settings/                       <-- URP pipeline configuration
|   |   |-- PC_RPAsset.asset               (desktop render settings)
|   |   |-- PC_Renderer.asset              (desktop renderer features)
|   |   |-- Mobile_RPAsset.asset           (mobile render settings)
|   |   |-- Mobile_Renderer.asset          (mobile renderer features)
|   |
|   |-- SampleScene.unity              <-- Main game scene (1.1 MB)
|   |-- InputSystem_Actions.inputactions   <-- Input bindings
|
|-- Packages/
|   |-- manifest.json                  <-- All installed packages
|
|-- ProjectSettings/                   <-- Engine-level configuration
    |-- ProjectSettings.asset             (player, platform, build)
    |-- QualitySettings.asset             (quality presets)
    |-- GraphicsSettings.asset            (render pipeline selection)
    |-- InputManager.asset                (legacy input axes)
    |-- TagManager.asset                  (layers & tags)
    |-- DynamicsManager.asset             (physics settings)
```

### What each folder means

| Folder | Purpose | Editable? |
|--------|---------|-----------|
| `Assets/` | Everything you create: code, art, shaders, scenes | Yes - this is YOUR content |
| `Packages/` | External libraries (URP, Input System, etc.) | Only `manifest.json` (to add/remove packages) |
| `ProjectSettings/` | Engine configuration (quality, physics, input) | Yes, usually via Unity Editor menus |
| `Library/` | Auto-generated cache. Safe to delete (Unity rebuilds it) | Never edit manually |
| `Logs/` | Runtime debug logs | Read-only reference |
| `UserSettings/` | Your personal editor layout preferences | Auto-managed by Unity |

---

## 3. Rendering Pipeline (URP)

### What is a Render Pipeline?

A render pipeline defines **how Unity draws everything on screen**. It controls:
- How lights are calculated
- How shadows are cast
- What post-processing effects are applied
- How materials/shaders work

Unity offers three pipelines:

| Pipeline | Use Case | This Project? |
|----------|----------|--------------|
| Built-in | Legacy, general purpose | No |
| **URP (Universal)** | **Optimized, cross-platform, extensible** | **YES (v17.3.0)** |
| HDRP | High-end PC/console realism | No |

### URP in This Project

**Configuration files:**
- `Assets/Settings/PC_RPAsset.asset` - Desktop quality settings
- `Assets/Settings/PC_Renderer.asset` - Desktop renderer features
- `Assets/Settings/Mobile_RPAsset.asset` - Mobile quality settings
- `Assets/Settings/Mobile_Renderer.asset` - Mobile renderer features

**Key settings (PC):**
- **Rendering:** Forward rendering (not Deferred)
- **HDR:** Enabled
- **Shadows:** 4 cascades, 2048x2048 resolution
- **SRP Batcher:** Enabled (groups draw calls for performance)
- **Render Scale:** 1.0 (full resolution)

**Key settings (Mobile):**
- **Render Scale:** 0.8 (80% resolution for performance)
- **Shadows:** Reduced quality
- **Simplified features**

### Forward vs Deferred Rendering

This project uses **Forward Rendering**:
- Each object is drawn once per light that affects it
- Simpler, better for transparency and custom shaders
- Good for the toon/pixel art style used here

**Deferred** would draw geometry first, then apply lights in a second pass. It's better for many dynamic lights but doesn't work well with toon shading.

### Renderer Features (Custom Post-Processing)

URP allows you to inject custom rendering steps called **Renderer Features**. This project has two:

1. **PixelizeFeature** - Creates the pixel art look
   - Downscales the image, applies color reduction, adds outlines, then upscales
   - Configured differently for PC (subtle) vs Mobile (aggressive pixelation)

2. **VolumetricLightFeature** - Creates god ray effects
   - Marches rays from the camera toward lights
   - Simulates light scattering through atmosphere

These are **ScriptableRendererFeatures** - C# classes that hook into URP's render loop.

### Volume Profiles (Post-Processing)

Unity URP uses **Volume Profiles** for built-in post-processing:
- **Bloom:** Glow around bright areas (intensity 0.25, subtle)
- **Vignette:** Darkened screen edges (intensity 0.2)

These are configured in `Assets/Settings/DefaultVolumeProfile.asset`.

---

## 4. Custom Shaders

### What is a Shader?

A shader is a program that runs on the **GPU** (graphics card). It decides what color each pixel of an object should be. Every material references a shader and provides it with settings (colors, textures, numbers).

```
Shader (the program)  -->  Material (the settings)  -->  Renderer (applies to object)
```

### Shader Language

This project uses **HLSL** (High-Level Shading Language) within Unity's ShaderLab framework. Shaders have two main functions:

- **Vertex Shader (`vert`)**: Runs for each vertex. Positions the geometry on screen.
- **Fragment Shader (`frag`)**: Runs for each pixel. Decides the final color.

### The 8 Custom Shaders

#### 1. ToonLit.shader (`Custom/ToonLit`)
**Purpose:** Main shader for characters and objects
**Theory - Toon/Cel Shading:**

Traditional realistic lighting calculates smooth gradients from light to shadow. Toon shading **quantizes** (rounds) the lighting into discrete bands:

```
Realistic:   [0.0 .... 0.3 .... 0.6 .... 1.0]  (smooth gradient)
Toon (3 steps): [dark] [medium] [bright]         (flat bands)
```

**How it works:**
1. Calculate dot product between surface normal and light direction (standard diffuse)
2. Multiply by number of light steps (e.g., 3)
3. Round to nearest integer (creates flat bands)
4. Divide back to 0-1 range

**Properties:**
- `_BaseColor` - Object color
- `_ShadowColor` - Shadow tint (this project uses purple: creates depth)
- `_LightSteps` - Number of shading bands (2-8)
- `_EdgeSmoothness` - Anti-aliasing between bands
- `_AmbientStrength` - Minimum light level

**Shared library:** `ToonLighting.hlsl` contains reusable functions:
- `ToonDiffuse()` - Quantized light bands
- `ToonSpecular()` - Threshold-based specular highlights
- `ToonRim()` - Edge-facing rim light (highlights silhouettes)

---

#### 2. GroundToon.shader (`Custom/GroundToon`)
**Purpose:** Procedural ground with terrain variation
**Theory - Procedural Texturing:**

Instead of using painted texture images, this shader generates terrain patterns **mathematically** using Perlin noise at different scales:

```
Large noise (scale 0.08)  -->  Stone patches
Medium noise (scale 0.3)  -->  Dirt/moss patches
Detail noise (scale 1.5)  -->  Fine variation
```

**Colors defined in material:**
- Base: Dark green (0.2, 0.28, 0.15)
- Dirt: Brown (0.25, 0.2, 0.12)
- Moss: Medium green (0.2, 0.32, 0.12)
- Path: Tan (0.3, 0.28, 0.18)
- Stone: Cool gray

The shader samples 3D Perlin noise at the world position of each pixel, blends between these colors based on noise values, then applies the same toon lighting as ToonLit.

---

#### 3. GeometryGrass.shader
**Purpose:** Realistic grass blades that sway in wind and bend when the player walks through
**Theory - Geometry Shaders & Tessellation:**

This is the most advanced shader in the project. It uses three shader stages:

```
Vertex --> Hull/Domain (Tessellation) --> Geometry --> Fragment
```

**Tessellation:** The ground mesh is subdivided into more triangles at runtime. More triangles = more grass blade spawn points.

**Geometry Shader:** For each triangle vertex, a grass blade quad is generated. The shader:
1. Rotates each blade randomly (rotation matrix)
2. Varies height/width per blade (random hash)
3. Applies wind displacement (noise texture + time)
4. Reads a **displacement render texture** to bend grass near the player
5. Colors from base (dark) to tip (light) with gradient

**Displacement System:** A top-down orthographic camera (layer 31) renders flat discs under moving objects. This render texture tells the grass shader where to bend.

---

#### 4. PixelWater.shader
**Purpose:** Stylized water surface
**Theory - Vertex Displacement & Depth Effects:**

- **Vertex stage:** Displaces vertices up/down using sine waves at two frequencies (creates wave motion)
- **Fragment stage:**
  - Reads scene depth to calculate water depth at each pixel
  - Blends shallow color into deep color based on depth
  - Adds foam at shallow edges (where depth is small)
  - Animated surface lines for pixel art look
  - Applies toon lighting

---

#### 5. PixelArt.shader (Hidden/PixelArt)
**Purpose:** Full-screen post-processing for the pixel art aesthetic
**Theory - Post-Processing Pipeline:**

This shader processes the **entire screen image** after all objects are drawn:

**Step 1 - Color Posterization:**
```
original color (millions of colors)
  --> round to N steps (e.g., 16 levels per channel)
  --> result: retro color palette
```

**Step 2 - Dithering (Bayer Matrix):**
A 4x4 pattern of threshold values creates ordered noise. This simulates more colors using patterns of fewer colors (like newspaper print dots).

**Step 3 - Edge Detection (Outlines):**
Uses two buffers that URP provides:
- **Depth buffer:** How far each pixel is from the camera
- **Normal buffer:** Which direction each surface faces

The shader checks neighboring pixels. If depth or normal changes sharply, it's an edge --> draw a black outline.

```
Roberts Cross operator:
  Compare pixel (x,y) with (x+1,y+1) and (x+1,y) with (x,y+1)
  If difference > threshold --> edge detected
```

**Step 4 - Fog:**
Blends a fog color based on distance from camera. Creates atmospheric depth.

**Step 5 - Palette LUT (optional):**
A 1D lookup texture that remaps all colors to a fixed retro palette.

---

#### 6. VolumetricLight.shader (Hidden/VolumetricLight)
**Purpose:** God rays / light shafts
**Theory - Ray Marching:**

For each screen pixel:
1. Cast a ray from camera into the scene
2. Take N steps along the ray (configurable, default 16)
3. At each step, check if the point is in shadow
4. If in light, accumulate brightness using **Mie scattering** phase function
5. Add Perlin noise to make rays look organic (not uniform)
6. Blend result additively onto the scene

**Mie scattering** simulates how light scatters through particles (dust, fog). It creates a bright halo around the light source direction.

---

#### 7. ParticleBillboard.shader
**Purpose:** Camera-facing flat quads for dust/leaf particles
**Theory - Billboarding:**

The vertex shader replaces the object's rotation so it always faces the camera. This is done by reconstructing the object's orientation using the camera's view vectors.

---

#### 8. PixelizePosterize.shader
**Purpose:** Simple color reduction pass
A simpler version of the PixelArt shader that only does color step quantization.

---

## 5. Gameplay Systems

### Player Controller (`PlayerController.cs` - 901 lines)

The player is a **Finite State Machine (FSM)** with 6 states:

```
         [Idle] <---> [Moving]
           |             |
           v             v
       [Attacking] --> [Charging]
           |
           v
       [Dodging]

       [Staggered] (entered when hit)
```

#### State Machine Pattern

```csharp
enum PlayerState { Idle, Moving, Attacking, Dodging, Charging, Staggered }

void Update() {
    switch (currentState) {
        case PlayerState.Idle:     UpdateIdle();     break;
        case PlayerState.Moving:   UpdateMoving();   break;
        case PlayerState.Attacking: UpdateAttacking(); break;
        // ... etc
    }
}
```

Each state has its own update logic, and transitions happen when conditions are met (e.g., "if player presses attack while in Idle/Moving, switch to Attacking").

#### Combat - 3-Hit Combo

```
Combo Step 0:  40 damage,  5 knockback, 0.15s active, 0.20s recovery
Combo Step 1:  55 damage,  7 knockback, 0.18s active, 0.25s recovery
Combo Step 2:  80 damage, 12 knockback, 0.22s active, 0.35s recovery
```

- **Active window:** The attack can hit enemies
- **Recovery window:** Player is committed (can't attack, but CAN dodge-cancel)
- **Combo window:** 0.6 seconds to click again for next step
- **Whiff penalty:** Recovery extends 1.3x if you miss (rewards accuracy)
- **Attack dash:** Small forward lunge on each swing (scales with combo step)

#### Charged Attack
- Hold mouse button for 0.8 seconds
- 150 damage, 20 knockback, 2.5 unit range
- **Bloodburst:** Heals 15 HP on hit (risk/reward: long windup for big payoff)
- Refills all 4 ranged charges

#### Dodge Roll
- Speed: 12 units/sec for 0.3 seconds
- **I-frames:** 0.25 seconds of invincibility (can dodge through attacks)
- Costs 25 stamina, 0.15 second cooldown
- Ghost trail: Blue afterimage capsules every 0.08 seconds

#### Stamina System
- Maximum: 100
- Regen: 30/sec after 0.8 second delay
- Costs: Attack (20), Dodge (25), Charge (40)

#### Ranged System
- 4 charges maximum
- Regain 1 charge per melee hit
- Full refill on charged attack hit
- Right-click to fire, 30 damage per bullet

### Enemy AI (`Enemy.cs` - 517 lines)

Also a Finite State Machine with 6 states:

```
[Idle] --> [Patrol] --> [Chase] --> [Attack]
  ^           ^           |           |
  |           |           v           v
  +-----------+---------[Retreat]  [Stagger]
```

#### Detection & Pursuit
- **Detection range:** 12 units (starts chasing player)
- **Lose interest:** 16 units (returns to patrol)
- **Attack range:** 2.2 units

#### Attack Patterns (random selection)
| Pattern | Chance | Description |
|---------|--------|-------------|
| Single Swipe | 50% | Quick single hit |
| Two-Hit Combo | 30% | Two swings with 0.4s gap |
| Charged Lunge | 20% | 1.5x windup, wider range |

#### Telegraph System
Enemies telegraph attacks to give the player time to react:
1. Scale up 1.15x during windup (body swells)
2. Flash orange
3. Brief pause at 85-95% windup (the "tell moment")
4. Lunge force on strike

#### Stagger Mechanics
- Consecutive hits tracked (2 second timeout between hits)
- 3+ consecutive hits = extended stagger (0.8s instead of 0.25s)
- At 25 HP or less, enemy retreats

### Combat Feedback (`CombatFeedback.cs` - 81 lines)

A **Singleton** that manages two game-feel techniques:

#### Screen Shake (Trauma-Based)
```
trauma (0-1) accumulates on hits
  --> decays at 2.5/sec
  --> shake intensity = trauma squared (quadratic falloff)
  --> offset calculated with Perlin noise (smooth randomness)
  --> max offset: 0.3 units, max rotation: 2 degrees
```

The quadratic falloff means: a small hit barely shakes, but a big hit shakes a LOT. This creates satisfying "weight" to impacts.

#### Hitstop (Time Freeze)
```
On hit:
  Time.timeScale = 0.05  (95% slow-motion)
  Wait 0.04-0.12 seconds (varies by attack)
  Time.timeScale = 1.0   (resume)
```

Hitstop creates a momentary freeze on impact. It makes hits feel powerful. Fighting games and action games (like Tunic, Hades, Dark Souls) all use this technique.

### Slash VFX (`SlashVFX.cs` - 168 lines)

Procedural arc meshes that appear during attacks:

| Combo Step | Color | Scale | Duration |
|-----------|-------|-------|----------|
| 0 | White | 0.3 to 1.4 | 0.18s |
| 1 | Light Blue | 0.4 to 1.6 | 0.20s |
| 2 | Orange | 0.5 to 2.0 | 0.25s |
| 3 (Charged) | Gold | 0.5 to 2.5 | 0.35s |

The mesh is generated with code: inner/outer radius, arc angle (120-180 degrees), 12 segments. It grows, then fades out with quadratic easing.

---

## 6. Animation & IK

### Animator Controller (`PlayerAnimator.controller`)

Unity's **Animator** is a visual state machine for playing animation clips. It has:

**Parameters** (variables that scripts set to control transitions):
- `MoveX`, `MoveY` (float) - Direction of movement
- `ComboStep` (int) - Which attack in the combo (0-3)
- `ChargeLevel` (float) - Charge progress (0-1)
- `Slash`, `Dodge`, `Stagger` (trigger) - One-shot events
- `ChargeStart`, `ChargeRelease`, `ChargeCancelled` (trigger)
- `TurnLeft`, `TurnRight`, `Turn180` (trigger)

**States:**
- **Locomotion:** 2D Blend Tree that blends between idle/walk animations based on MoveX/MoveY
- **Attack states:** Selected by ComboStep parameter
- **Dodge, Charge, Stagger:** Triggered states

### What is a Blend Tree?

A Blend Tree smoothly interpolates between multiple animation clips based on parameter values:

```
MoveX = 0, MoveY = 0  -->  Idle animation
MoveX = 1, MoveY = 0  -->  Walk Right animation
MoveX = 0, MoveY = 1  -->  Walk Forward animation
MoveX = 1, MoveY = 1  -->  Walk Forward-Right (blended)
```

This creates seamless directional movement without needing an animation for every angle.

### Inverse Kinematics (`PlayerIK.cs` - 165 lines)

**IK (Inverse Kinematics)** adjusts bone positions AFTER the animation plays. Instead of the animation fully controlling where hands/feet go, IK overrides specific bones to reach target positions.

#### Head Look-At
```
Target: Mouse cursor position (projected onto ground plane)
Weight: 1.0 (fully overrides animation)
Result: Character's head tracks the mouse cursor
```

#### Arm Aiming (Right-Click)
```
When holding right mouse button:
  - Right hand reaches toward aim direction (3 units ahead)
  - Left hand offsets 0.15 units left
  - Smooth blend in/out at 8 units/sec
Result: Character visually aims before shooting
```

#### Foot Placement
```
For each foot:
  1. Raycast downward from foot bone position
  2. If ground is found within range:
     - Move foot to ground + 0.02 unit offset
     - Rotate foot to match ground surface normal
     - Lower body by the larger foot adjustment
  3. Reject adjustments > 0.5 units (too extreme)
Result: Feet plant on uneven terrain (rocks, slopes)
```

**Only active during Idle/Moving states** - disabled during combat to not interfere with attack animations.

### Stop-Motion Effect (`StopMotionEffect.cs`)

Reduces animation playback to 8 FPS (frames per second) for a retro stop-motion look:
```csharp
animator.speed = 0;           // Pause normal playback
animator.Update(1f / fps);    // Manually step by fixed interval
```

This creates the choppy, hand-drawn feel common in pixel art games.

---

## 7. Procedural Generation

### ProceduralGenerator.cs (531 lines)

Instead of placing objects by hand in the editor, this script generates the entire environment at runtime using mathematical algorithms.

### Icosphere Mesh Generation

An **icosphere** is a sphere made of triangles. The process:
1. Start with an **icosahedron** (20 triangles)
2. **Subdivide:** Split each triangle into 4 smaller triangles
3. **Normalize:** Push all vertices to the same distance from center (makes it spherical)
4. Result: A smooth sphere with evenly-distributed triangles

This base mesh is then deformed with noise to create rocks and bushes.

### Perlin Noise

**Perlin noise** is a smooth random function that returns values between -1 and 1. Unlike pure random numbers (which are jagged), Perlin noise has smooth gradients - nearby points have similar values.

```
Pure random:  ///\/\\\///\\\/\///   (jagged)
Perlin noise: ~~^~~~v~~~^~~v~~^~   (smooth hills and valleys)
```

This project uses Perlin noise for:
- Rock vertex displacement (lumpy surfaces)
- Tree canopy deformation (organic shapes)
- Bush shape variation
- Ground terrain color blending
- Grass density distribution
- Floating particle movement
- Screen shake offsets

### Rock Generation (20 rocks)
1. Generate icosphere mesh
2. Displace each vertex along its normal by Perlin noise (0.15-0.3 strength)
3. **Flat shading:** Recalculate normals per-face (not smoothed) for angular look
4. Random scale: 0.4-1.6 width, 0.4-0.7 height ratio
5. Cluster placement: 5-8 cluster centers, rocks placed near clusters

### Tree Generation (8 trees)
1. **Trunk:** Procedural cylinder mesh (0.1-0.2 radius, 1-2 height)
2. **Canopy:** 2-3 deformed icospheres overlapping
3. Edge-biased placement (trees tend toward map edges)
4. Brown trunk material, 3 shades of green for foliage

### Bush Generation (25 bushes)
1. Noisy icospheres (smaller than rocks)
2. Placed near rocks and trees (proximity-biased)
3. Various green tones

### Material Property Blocks

Instead of creating a unique material for every object (expensive), the code uses **MaterialPropertyBlocks**:
```csharp
MaterialPropertyBlock props = new MaterialPropertyBlock();
props.SetColor("_BaseColor", randomGreen);
renderer.SetPropertyBlock(props);
```

This lets each rock/tree have a unique color while sharing the same material. The GPU can still batch draw calls efficiently.

---

## 8. GPU Instancing & Performance

### What is GPU Instancing?

Drawing thousands of objects normally means thousands of **draw calls** (CPU tells GPU to draw each one). GPU Instancing sends one draw call with a list of positions/colors for all instances:

```
Without instancing: 4000 draw calls (CPU bottleneck!)
With instancing:    ~40 draw calls (batched in groups of 1023)
```

### GrassSpawner.cs (241 lines)

Spawns **4000+ foliage instances** using `Graphics.DrawMeshInstanced()`:

- **1500 bushes:** Scale 0.6-1.4, darker green
- **2500 small plants:** Scale 0.15-0.35, lighter green

**Density mapping with noise:**
```
Perlin noise value at (x, z) position:
  High noise --> dense placement (0.12 unit spacing)
  Low noise  --> sparse placement (0.5 unit spacing)
```

**Per-instance color variation:**
```
For each instance:
  Sample Perlin noise at position
  Shift hue slightly (-0.05 to +0.05)
  Shift brightness slightly (-0.1 to +0.1)
  Store in MaterialPropertyBlock array
```

**Mesh:** Procedural cross-quad (3 intersecting vertical quads) - looks 3D from any angle.

### FloatingParticles.cs (155 lines)

80 camera-facing billboard quads for atmospheric dust:
- Sinusoidal vertical drift + Perlin noise horizontal drift
- Respawn when too far from camera
- GPU instanced with `DrawMeshInstanced()`

### Grass Displacement System

A clever technique for making grass bend when the player walks through:

```
1. GrassDisplacementObject.cs
   - Spawns a flat disc mesh under the player (on layer 31)
   - Layer 31 is invisible to main camera

2. GrassDisplacementCamera.cs
   - Orthographic top-down camera, only sees layer 31
   - Renders to a 256x256 RenderTexture
   - Sets global shader variable: _DisplacementRT

3. GeometryGrass.shader
   - Reads _DisplacementRT
   - Where the texture is white (disc present), bends grass away
```

---

## 9. UI System

### CombatHUD.cs (180 lines)

The UI is built **entirely in code** (no UI prefabs or drag-and-drop layout):

```
Screen Layout (bottom-left):
+------------------------------------------+
|                                          |
|                                          |
|                                          |
|  [====HEALTH BAR=======] 220x16px       |
|  [==STAMINA BAR====]     176x10.4px     |
|  [*] [*] [*] [*]         charge pips    |
+------------------------------------------+
```

**Components used:**
- `Canvas` - Root UI container (Screen Space Overlay)
- `CanvasScaler` - Scales UI to match screen resolution
- `Image` - Colored rectangles for bars and pips
- `RectTransform` - Positioning and sizing

**Health Bar:**
- Red fill over dark background
- Width scales with `currentHealth / maxHealth`
- White flash for 0.15s on damage

**Stamina Bar:**
- Green fill, same flash behavior
- Positioned below health bar

**Ranged Pips:**
- 4 small squares (12x12 pixels)
- Yellow = charge available, Dark gray = empty

### Unity UI Theory

Unity UI uses a **Canvas** system:
- **Canvas:** Container that defines how UI renders (screen overlay, world space, etc.)
- **RectTransform:** Like Transform but for 2D. Has anchors (where it's pinned on screen) and offsets.
- **Image:** Draws a colored rectangle or sprite.
- **Anchoring:** `(0,0)` = bottom-left, `(1,1)` = top-right. This project anchors everything to bottom-left.

---

## 10. Input System

### Two Input Systems in Unity

Unity has two input systems:

1. **Legacy Input Manager** (`Input.GetKey()`, `Input.GetAxis()`) - Simple but limited
2. **New Input System** (`Keyboard.current`, `Mouse.current`, `InputAction`) - Modern, flexible

**This project uses a hybrid approach:**
- The `InputSystem_Actions.inputactions` asset defines bindings (WASD, gamepad, etc.)
- But `PlayerController.cs` directly reads `Keyboard.current` and `Mouse.current`

### How Input Flows

```
1. Player presses "W" key
2. Unity's Input System detects Keyboard.current.wKey.isPressed
3. PlayerController.Update() reads this every frame
4. Constructs movement vector: new Vector3(horizontal, 0, vertical)
5. Transforms to camera-relative direction
6. In FixedUpdate(), applies to Rigidbody.MovePosition()
```

### Mouse Aiming

```
1. Read Mouse.current.position
2. Create ray from camera through mouse screen position
3. Raycast against ground plane (y=0)
4. Hit point = world position the player aims at
5. Player rotates toward this point
6. PlayerIK uses this for head/arm look-at
```

### Input Bindings (`InputSystem_Actions.inputactions`)

| Action | Keyboard | Gamepad |
|--------|----------|---------|
| Move | WASD / Arrows | Left Stick |
| Look | Mouse Delta | Right Stick |
| Attack | Left Mouse | West Button (X/Square) |

---

## 11. Physics & Collision

### Rigidbody

A **Rigidbody** makes a GameObject participate in physics (gravity, forces, collisions).

**Player Rigidbody settings:**
- Interpolation: Enabled (smooth movement between physics steps)
- Rotation constraints: Freeze X and Z (only rotate around Y - prevents tipping over)
- Movement: `rb.MovePosition()` in FixedUpdate (kinematic-style but still collides)

### Hit Detection Methods

This project uses three approaches:

#### 1. OverlapSphere (Melee Attacks)
```csharp
Collider[] hits = Physics.OverlapSphere(attackPoint, radius);
// Returns all colliders within a sphere
// Used for: player melee, enemy melee, charged attacks
```

#### 2. Raycast (Bullets, Ground Detection)
```csharp
Physics.Raycast(origin, direction, out hit, maxDistance);
// Casts an invisible line, returns first thing it hits
// Used for: bullet hit detection, foot IK ground finding, mouse aim
```

#### 3. SphereCast (Bullet Fallback)
```csharp
Physics.SphereCast(origin, radius, direction, out hit, maxDistance);
// Like raycast but with thickness (catches near-misses)
// Used for: bullet hit detection (more forgiving than thin raycast)
```

### Knockback

```csharp
enemyRb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
```

`ForceMode.Impulse` applies force instantly (like being hit), vs `ForceMode.Force` which applies gradually (like wind).

---

## 12. Component Interaction Map

### How Everything Connects

```
                    INPUT
                      |
                      v
              [PlayerController]
               /      |       \
              /       |        \
             v        v         v
        [Animator] [Bullet]  [SlashVFX]
             |        |
             v        v
        [PlayerIK] [Enemy.TakeDamage()]
                       |
                       v
               [CombatFeedback]
                (screen shake)
                (hitstop)
                       |
                       v
               [CameraFollow]
                       |
                       v
              [CameraTexelSnap]
                       |
                       v
          === URP RENDER PIPELINE ===
                       |
              [PixelizeFeature]
                (outlines, posterize, fog)
                       |
           [VolumetricLightFeature]
                (god rays)
                       |
                       v
                   SCREEN
```

### Detailed Flow: Player Attacks Enemy

```
1. Player clicks left mouse button
2. PlayerController.Update() detects click
3. State changes to Attacking, comboStep increments
4. Animator.SetTrigger("Slash") + SetInteger("ComboStep", step)
5. SlashVFX.Spawn() creates arc mesh at player position
6. After active delay, Physics.OverlapSphere() checks for enemies
7. If enemy hit:
   a. Enemy.TakeDamage(damage, knockback) called
   b. Enemy Rigidbody receives impulse force
   c. Enemy enters Stagger state, flashes white
   d. CombatFeedback.AddTrauma(0.3) shakes camera
   e. CombatFeedback.DoHitstop(0.06) freezes time briefly
   f. Player regains 1 ranged charge
   g. CombatHUD updates health/stamina bars
8. If enemy health <= 0: enemy destroyed
9. Recovery window begins (player committed to animation)
10. Player can dodge-cancel during recovery, or wait for combo window
```

### Detailed Flow: One Frame of Rendering

```
1. Update() runs for all scripts (gameplay logic)
2. FixedUpdate() runs (physics, movement)
3. LateUpdate() runs:
   a. CameraFollow positions camera behind player
   b. CameraTexelSnap snaps to pixel grid
4. Animation updates (Animator evaluates blend trees)
5. OnAnimatorIK() runs (foot/hand adjustments)
6. URP begins rendering:
   a. Shadow pass (casts shadows from lights)
   b. Depth + Normals prepass (for outlines)
   c. Forward pass (draws all objects with their materials)
      - ToonLit shader: toon-shaded characters/objects
      - GroundToon shader: procedural ground
      - GeometryGrass shader: tessellated grass
      - PixelWater shader: animated water
   d. GrassDisplacementCamera renders layer 31 (displacement map)
   e. GPU Instancing: DrawMeshInstanced for 4000+ foliage
   f. Transparent pass (SlashVFX arcs, particles)
   g. PixelizeFeature pass:
      - Downscale to pixel grid
      - Posterize colors
      - Detect and draw outlines
      - Apply fog
      - Upscale with point filtering
   h. VolumetricLightFeature pass:
      - Ray march god rays
      - Additive blend onto scene
   i. Built-in post-processing (Bloom, Vignette)
7. Final image sent to screen
```

---

## 13. Installed Packages

From `Packages/manifest.json`:

| Package | Version | Purpose |
|---------|---------|---------|
| **com.unity.render-pipelines.universal** | 17.3.0 | URP - the entire rendering system |
| **com.unity.inputsystem** | 1.17.0 | Modern input handling |
| **com.coplaydev.coplay** | beta | AI-assisted game development tool |
| com.unity.timeline | 1.8.9 | Cutscene/sequence editor |
| com.unity.visualscripting | 1.9.9 | Node-based scripting (not used in this project) |
| com.unity.ai.navigation | 2.0.9 | NavMesh pathfinding (available but enemies use custom AI) |
| com.unity.2d.sprite | - | Sprite handling for 2D assets |
| com.unity.2d.tilemap | - | Tilemap system (not actively used) |
| com.unity.ugui | - | Unity UI system (used by CombatHUD) |

**Unity Version:** 6000.3.2f1 (Unity 6 LTS)

---

## 14. Glossary

| Term | Definition |
|------|-----------|
| **Blend Tree** | Animator node that smoothly blends multiple animations based on parameters |
| **Draw Call** | One command from CPU to GPU to draw a mesh. Fewer = better performance |
| **Forward Rendering** | Render technique where each object is lit in a single pass |
| **FSM** | Finite State Machine - pattern where an object is always in exactly one state |
| **GPU Instancing** | Drawing many copies of the same mesh in one draw call |
| **HLSL** | High-Level Shading Language - the language shaders are written in |
| **I-Frames** | Invincibility frames - brief period where player can't take damage |
| **IK** | Inverse Kinematics - calculating joint positions to reach a target |
| **MaterialPropertyBlock** | Per-object shader data without creating new materials |
| **Mie Scattering** | Physics model for how light scatters through particles |
| **MonoBehaviour** | Base class for all Unity scripts attached to GameObjects |
| **Perlin Noise** | Smooth pseudo-random function used for natural-looking variation |
| **Posterization** | Reducing colors to discrete steps (retro palette effect) |
| **Ray Marching** | Stepping along a ray to accumulate visual effects |
| **Renderer Feature** | URP extension point for custom render passes |
| **RenderTexture** | A texture that a camera can render into (used for grass displacement) |
| **Rigidbody** | Component that enables physics simulation on a GameObject |
| **ScriptableObject** | Data container that exists as an asset (not on a GameObject) |
| **SRP Batcher** | URP optimization that groups compatible draw calls |
| **Tessellation** | Subdividing mesh triangles for more detail at runtime |
| **Trauma** | Accumulated screen shake intensity (quadratic decay) |
| **URP** | Universal Render Pipeline - Unity's modern, extensible render system |
| **Volume Profile** | Asset containing post-processing settings (bloom, vignette, etc.) |

---

*Document generated from project analysis. Unity version 6000.3.2f1, URP 17.3.0.*
*Last updated: April 2026*
