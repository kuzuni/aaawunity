# T519 commerce art checkpoint

Updated: 2026-09-13T18:25:05Z

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

## Delivery status

- Scope complete: 16/16 requested individual final PNGs, plus Unity `.meta`, prompts, manifest, and catalog fragment.
- Pushed non-force to `codex/ui-art-commerce` through the connected GitHub write tool. Remote content tree `52691bc47bf9919db7692ebfb2cb071c7226a520` exactly matched the local content tree after fetch verification.
- Remote art commits: `d70e4e7d`, `1847fdc3`, `bbc0abaa`; complete large-PNG correction: `03726042`.
- No ZIP fallback was needed because the remote branch push succeeded.

## Remaining outside this session

- Parent integration session must consume `catalog-fragment.json`, connect the assets, and validate them in Unity screens.
- The overall T519 TODO remains incomplete; this independent art session does not mark it complete.

Do not edit runtime C#, central catalog files, progress/claims/TODO files, or anything outside this session's two owned folders.
