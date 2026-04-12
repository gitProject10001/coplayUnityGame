# Project: Painterly Isometric ARPG

Unity 6 (6000.3.2f1) isometric action RPG with a hand-painted / Tunic-inspired visual style.
Uses URP 17.3, Input System 1.17, AI Navigation 2.0, and the Coplay multiplayer plugin (beta).

## Project structure

```
Assets/
  Scripts/
    Gameplay/    PlayerController, Enemy, EnemySpawner, Bullet, SlashVFX,
                 CombatFeedback, CameraFollow, ProceduralGenerator,
                 PlayerIK, StopMotionEffect
    Rendering/   PixelizeFeature, VolumetricLightFeature, CameraTexelSnap,
                 ObjectRenderSnap, FloatingParticles, GrassSpawner,
                 GrassDisplacementCamera, GrassDisplacementObject,
                 GrassGroundSetup, WaterRippleEmitter
    Art/         PainterlyPalette
    UI/          CombatHUD
  Editor/        Large collection of editor utilities — painterly setup scripts,
                 import fixers, scene builders, debug inspectors
  Settings/      URP renderer/pipeline assets, volume profiles
                 PC_Renderer.asset — main renderer (shared across all scenes)
                 PainterlyProfile.asset — painterly volume profile
  Materials/
    Painterly/   Toon_* material set (Skin, Stone, Metal, Wood, Water, etc.)
    (root)       Geometry grass, billboard bush, toon-lit, pixel water, etc.
  Scenes (root)  Numbered test scenes: 01_ToonShaders … 08_ArtBible, SampleScene
  Prefabs/       Cameras, Gameplay, Lighting, Palette, ArtBible
  Art/, Models/, Animations/
```

## Key conventions

- **Painterly look** — the target aesthetic is restrained, hand-painted, not pixel-art.
  PixelizeFeature and VolumetricLightFeature in PC_Renderer.asset must stay **disabled**.
  If they reactivate, run the disable scripts in Editor/ via Unity API (YAML edits get reverted).
- **Calibrated post-processing** — see PainterlyProfile.asset. Prefer small, conservative
  adjustments (postExposure +0.05, contrast +8, saturation 0, vignette 0.20).
  Camera background: #8C9EB8 (soft cool sky), key light: 1.05 intensity, #FFEDC7.
- **Renderer features** — disable via `ScriptableRendererData` API, not YAML. Unity
  re-serializes from memory and overwrites direct file edits.
- **Editor scripts** live in `Assets/Editor/`. Many are one-shot setup/fix utilities
  (Painterly*, Fix*, Setup*). They run via menu items or `[InitializeOnLoad]`.

## Working with this project

- Open in Unity 6000.3.2f1. The active scene for art direction work is `08_ArtBible`.
- The render pipeline is URP with PC_RPAsset / PC_Renderer for desktop.
- Materials use Shader Graph toon shaders; the Painterly/ folder has the canonical set.
- Grass is geometry-based (GrassSpawner + displacement system), not terrain detail.
- Combat: top-down sword combat with enemy spawning, slash VFX, and stop-motion hit feedback.

## Git

- Short commit messages are the norm in this repo.
- Don't commit Library/, Temp/, or other Unity-generated folders (already gitignored).
