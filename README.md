# Mirio & Shoto: Patrulha do Potinho

A top-down isometric (2.5D) survival game, made in Unity. Two cats protect their food bowl from waves of insects that sneak in through the door, window, drain and a crack in the wall.

> **Status:** playable prototype in active development. Core loop, characters and enemy roster are in place; balancing, co-op and settings are on the roadmap.

## Gameplay

- Choose between **Mirio** (tough, strong hits, more HP) and **Shoto** (fast, higher critical chance and damage).
- Survive escalating waves, collect treats dropped by insects for XP and pick an upgrade on every level-up.
- Each insect behaves differently: ants march in lines, mosquitoes zigzag, flies dart erratically, blowflies charge heavily, and a giant cockroach closes the run as a boss.
- The main weapon is a **plush-toy barricade**; a different plush appears each run.

## What I implemented

- Character select screen with per-character stats and a critical hit system (chance and multiplier per cat).
- Enemy roster of 7 types with distinct movement behaviours, sizes proportional to their real-life counterparts, and weighted spawn tables that change over time.
- Billboarded pixel-art sprites in 8 directions, with screen-space direction resolution for an isometric camera.
- Sprite scale derived from each sprite's content bounds, so characters keep consistent proportions.
- An Editor `AssetPostprocessor` that enforces pixel-art import settings (point filter, no mipmaps, uncompressed).
- Visible spawn-point markers so the player can read where each wave comes from.

## Tech

| | |
|---|---|
| Engine | Unity 6 (6000.3), Universal Render Pipeline |
| Language | C# |
| Input | Unity Input System |
| Art | Pixel art, generated and edited with Python (PIL) |

The code lives under `Assets/_MirioShoto/Scripts` (namespace `MirioShoto`).

## Run it

1. Install Unity 6000.3 (via Unity Hub).
2. Open the project folder in Unity Hub.
3. Open the main scene and press Play. Move with WASD or the arrow keys; `Tab` returns to character select.

## Roadmap

See [`Docs/BACKLOG.md`](Docs/BACKLOG.md). Highlights: difficulty tuning so the first level is winnable, local co-op with revive, settings menu (brightness, audio, key remapping, accessibility), touch controls, and performance work (object pooling, UI migration).

## Author

Erick Paz, Product Manager and developer. Game design, programming and product decisions.
