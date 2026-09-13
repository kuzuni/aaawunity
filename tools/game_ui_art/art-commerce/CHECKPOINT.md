# T519 commerce art checkpoint

Updated: 2026-09-13 UTC

## Completed and preserved

- `chest_medieval_open.png` — RGBA passed; lower baseline aligned to closed source at y=1055.
- `chest_modern_open.png` — RGBA passed; meaningful lower baseline aligned to closed source at y≈1011 (closed y≈1010).
- `chest_cyber_open.png` — RGBA passed; lower baseline aligned to closed source at y=1028.
- `recipe_weapon.png`, `recipe_armor.png`, `recipe_helmet.png`, `recipe_shoes.png`, `recipe_ring.png`, `recipe_necklace.png` — six distinct colored equipment recipes, RGBA passed.
- `recipe_random.png` — general rolled parchment with ribbon and seal, RGBA passed.
- `gem_pack_1.png` → `gem_pack_6.png` — progression from three loose matching blue diamonds, pouch, tray pile, compact chest, medium chest, to maximum ornate chest; RGBA passed.
- Unity `.meta` files exist for all 16 final PNGs.

The open images retain the theme, camera angle, central body placement, and lower-body scale of their corresponding closed references. Alignment was a canvas translation only after generation; no closed source was overwritten.

## Failed attempts retained outside Assets

- `chest_cyber_open_failed_checkerboard.png` — RGB with painted checkerboard; rejected.
- `recipe_weapon_failed_checkerboard.png`, `recipe_armor_failed_checkerboard.png`, `recipe_helmet_failed_checkerboard.png` — RGB with painted checkerboards; rejected. Background-extraction retries also returned painted checkerboards and were not accepted.

## Remaining

- Commit the six gem packs and refreshed records.
- Push the local branch through the connected GitHub write tool; Git CLI itself has no credentials.
- Create a downloadable ZIP fallback containing both owned folders.
- Parent session must integrate the catalog fragment and validate in Unity. This session must not mark the overall TODO complete.

Do not edit runtime C#, central catalog files, progress/claims/TODO files, or anything outside this session's two owned folders. Current local branch commits are preserved; Git CLI push lacked credentials, so the GitHub connector or final ZIP is required.
