# T519 Stats Art Checkpoint

- Branch: `codex/ui-art-stats`
- Scope: `Assets/Art/ChihuahuaGameUI/Stats/`, `Assets/Art/ChihuahuaGameUI/Pattern/`, and this work folder only.
- Reference inspected: `Assets/Art/ChihuahuaEquipmentThemes/Reference/character_base.png`
- Status: generation in progress; Unity wiring and screen validation belong to the parent integration session.
- Runtime requirement for parent: preserve full icon RGB (white tint with existing alpha) for these color icons; import `pattern_paws_bones.png` with Wrap Mode Repeat.

## Completed

- `stat_attack.png` → `pi.attack`
- `stat_defense.png` → `pi.defense`
- `stat_attack_speed.png` → `pi.atk_spd`
- `stat_counter.png` → `pi.fist`
- `stat_crit_chance.png` → `pi.critical`
- `stat_dodge.png` → `ui.dodge`
- `stat_crit_damage.png` → `pi.damage`
- `stat_lifesteal.png` → `pi.drop`
- `stat_health.png` → `pi.heart` (generated heart retained; exact black outer contour added from its alpha mask after two image edits failed to preserve the requested outline)
- `stat_shield.png` → `pi.shield`
- `stat_experience.png` → `pi.star` (exact black outer contour added from generated alpha)
- `pattern_paws_bones.png` → `ui.pattern`
- All 12 assets visually inspected. Icons are real RGBA with alpha range 0..255 and centered transparent margins. The pattern is real RGBA with alpha range 0..82 for low contrast.
- Pattern 2x2 inspection: pass (`pattern-2x2-check.png`); opposite-edge RMSE is exactly 0 horizontally and vertically after edge normalization.
- The rejected checkerboard-background heart output was not copied into the repository.

## Remaining

- Parent integration only: merge this branch, apply catalog mappings, preserve color icon RGB while retaining UI alpha, set the pattern texture importer to Repeat, and validate in Unity/screenshots.
- Do not mark the overall Chihuahua UI TODO complete from this art-only branch.
