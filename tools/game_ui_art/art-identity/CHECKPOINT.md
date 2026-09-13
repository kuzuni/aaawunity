# T519 art-identity checkpoint

Scope: profile portraits and lobby chapter maps only. Runtime/catalog central files are intentionally untouched.

## Completed

- `profile_01.png` — primitive / `ui.iconFoe1`
- `profile_02.png` — medieval / `ui.iconFoe2`
- `profile_03.png` — early modern / `ui.iconFoe3`
- `profile_04.png` — modern / `ui.iconFoe4`
- `profile_05.png` — cyber / `ui.face5`
- `profile_06.png` — future / `ui.face6`
- `profile_07.png` — **rejected at parent integration**: the committed branch blob is corrupt and does not match the manifest SHA256; `ui.face7` keeps its existing main asset until a valid PNG is delivered
- `profile_08.png` — immortal / `ui.face8`
- `profile_09.png` — infinite / `ui.face9`
- `chapter_autumn.png` — orange autumn forest / `ui.chapter.autumn`
- `chapter_deepForest.png` — mysterious deep forest / `ui.chapter.deepForest`
- `chapter_forest.png` — bright green forest / `ui.chapter.forest`
- `chapter_desert.png` — sand desert / `ui.chapter.desert`

The 12 accepted files were generated individually with the mandatory `character_base.png` reference attached. Each accepted file was inspected for subject/style/framing and verified against its manifest SHA256. The source branch's `profile_07.png` is not accepted or registered.

## Remaining

- Regenerate and deliver a valid `profile_07.png`, then restore the `ui.face7` catalog-fragment mapping and verify the ninth selection.

Do not mark the overall Chihuahua UI TODO complete; parent integration owns Unity catalog/runtime connection and screen verification.

## Final validation

- Profiles normalized to consistent 1254×1254 canvases with visible-alpha content kept inside at least 97 px horizontal margins; all are real RGBA with alpha extrema 0–255.
- Maps were generated individually, visually inspected in required order, then aspect-fit to exact 573×709 RGBA; maps are intentionally opaque (alpha extrema 255–255).
- Parent revalidation: 12/13 source PNG files load fully and match declared SHA256. `profile_07.png` is intentionally omitted because the remote Git blob fails PNG CRC and SHA256 checks.
