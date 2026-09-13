# T519 art-identity checkpoint

Scope: profile portraits and lobby chapter maps only. Runtime/catalog central files are intentionally untouched.

## Completed

- `profile_01.png` — primitive / `ui.iconFoe1`
- `profile_02.png` — medieval / `ui.iconFoe2`
- `profile_03.png` — early modern / `ui.iconFoe3`
- `profile_04.png` — modern / `ui.iconFoe4`
- `profile_05.png` — cyber / `ui.face5`
- `profile_06.png` — future / `ui.face6`
- `profile_07.png` — space / `ui.face7`
- `profile_08.png` — immortal / `ui.face8`
- `profile_09.png` — infinite / `ui.face9`
- `chapter_autumn.png` — orange autumn forest / `ui.chapter.autumn`
- `chapter_deepForest.png` — mysterious deep forest / `ui.chapter.deepForest`
- `chapter_forest.png` — bright green forest / `ui.chapter.forest`
- `chapter_desert.png` — sand desert / `ui.chapter.desert`

All completed files were generated individually with the mandatory `character_base.png` reference attached. Each file was inspected for subject/style/framing and verified as a 1254×1254 RGBA PNG with alpha extrema 0–255.

## Remaining

- Parent integration: merge this branch, insert `catalog-fragment.json` mappings into the central catalog, regenerate derived catalog files, connect/verify screens.

Do not mark the overall Chihuahua UI TODO complete; parent integration owns Unity catalog/runtime connection and screen verification.

## Final validation

- Profiles normalized to consistent 1254×1254 canvases with visible-alpha content kept inside at least 97 px horizontal margins; all are real RGBA with alpha extrema 0–255.
- Maps were generated individually, visually inspected in required order, then aspect-fit to exact 573×709 RGBA; maps are intentionally opaque (alpha extrema 255–255).
- All 13 PNG files load fully in Pillow; no text or watermark observed; Unity sprite `.meta` files exist with deterministic repository GUIDs.
