# Backlog: Mirio & Shoto: Patrulha do Potinho

## Done
- [x] 8-direction pixel-art sprites for Mirio and Shoto
- [x] Both cats playable
- [x] Character select screen with per-cat stats and critical hit system
- [x] Pixel-art insects (ant, leafcutter ant, mosquito, fly, blowfly, cockroach) with proportional sizes and distinct behaviours
- [x] Giant cockroach boss
- [x] Plush-toy barricade weapon (random plush each run)
- [x] Treat drops in pixel art
- [x] Visible spawn markers for insect entrances
- [x] Fixed left-walking animation (mirrored right-facing frames)

## Next (in order)
1. **Balancing:** make the first level winnable with both cats; review the XP-per-level curve
2. Art review: bowl and the four entrances, then furniture, floor and walls, effects and HUD
3. Scenario redesign
4. Redesign of the skill-selection screen
5. Local 2-player co-op (WASD + arrows or gamepad; revive the partner by standing next to them for a few seconds; choose who plays with whom)

## Later
- Settings menu: brightness, volume (music and effects), key/button remapping, accessibility options
- Touch controls for mobile (virtual joystick, on-screen buttons)
- Performance: object pooling (insects, projectiles, floating numbers), fewer per-frame allocations, sprite atlases, cap on active insects, profiling, replace the IMGUI HUD with uGUI or UI Toolkit
- Walk animation and facing directions for insects
- Sprites for other characters
- Remove unused sprite files
- Free key remapping per player in co-op
