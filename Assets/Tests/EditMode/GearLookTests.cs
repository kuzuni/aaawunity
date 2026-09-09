using System.Collections.Generic;
using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>GearLook(장비 외형·아이콘 표) ↔ catalog.json — 부위×세트×등급 키가 전부 카탈로그에 실재하는가 (T7 · 승인 대기 26).</summary>
    public class GearLookTests
    {
        static HashSet<string> CatalogSprites()
        {
            var path = Path.GetFullPath(Path.Combine(TestData.Dir, "..", "..", "KkomaKnight", "catalog.json"));
            var root = new JNode(MiniJson.Parse(File.ReadAllText(path)));
            var set = new HashSet<string>(); foreach (var k in root["sprites"].Keys) set.Add(k); return set;
        }

        /// <summary>
        /// 등급마다 <b>쓸 그림 칸이 정해져 있는가</b> (T325 문 ⓑ 로 뜻을 옮겼다).
        /// <para>여기는 «등급 수 == 그림 칸 수» 를 재던 자리다. 그 단언은 <b>주인이 «영웅 등급 다시 넣고» 라고 한 순간 반드시 깨지는데</b>,
        /// 깨지는 방향이 «고쳐라» 가 아니라 «그림을 새로 만들어라» 로 읽혀서 <b>§1(새 그림 금지)과 정면으로 부딪힌다</b> —
        /// 실제로 이 한 줄이 T325 의 문 ⓑ 로 두 회차를 막았다.</para>
        /// 재는 것을 «두 수가 같은가» 에서 <b>«등급마다 실재하는 그림 칸이 배정됐는가»</b> 로 옮긴다 —
        /// 오늘(등급 넷·그림 넷·항등)은 옛 단언과 똑같은 것을 재고, 등급이 다섯이 되는 날에는
        /// «영웅은 어느 그림인가» 를 표(<c>gearOverride.json look.rarSprite</c>)가 답해야 통과한다. 그것이 §1 이 허락하는 답이다.
        /// </summary>
        [Test]
        public void EveryGradeIsAssignedAnExistingSpriteSlot()
        {
            var d = TestData.Load();
            for (int rar = 0; rar < d.Gear.RarName.Length; rar++)
            {
                int slot = d.Gear.LookRar(rar);
                Assert.That(slot, Is.InRange(0, GearLook.RarCount - 1),
                    $"«{d.Gear.RarName[rar]}» 이 쓸 그림 칸이 {slot} 인데 있는 칸은 0~{GearLook.RarCount - 1} 뿐이다 — look.rarSprite 를 보라");
            }
            // 오늘은 표가 없어 항등이다. 그 «오늘» 이 참인지도 같이 못 박는다 — 조용히 표가 생기면 여기가 먼저 말한다.
            if (d.Gear.LookRarTable == null)
                Assert.That(d.Gear.RarName.Length, Is.EqualTo(GearLook.RarCount),
                    "look.rarSprite 가 없으면 등급 수와 그림 칸 수가 같아야 한다(항등이라서) — 등급을 늘렸다면 그 표를 같이 적어라");
        }

        [Test]
        public void EveryLookPartSetRarityHasCatalogSprite()
        {
            var d = TestData.Load(); var sprites = CatalogSprites();
            // ⚠ 도는 것은 «등급» 인데 키가 받는 것은 «그림 칸» 이다 — 반드시 D.Gear.LookRar 를 거친다(T325 문 ⓑ).
            //   등급을 그대로 넘기면 등급이 다섯이 되는 날 있지도 않은 «.4» 를 찾아 이 자가 «카탈로그에 없음» 으로 운다.
            //   ⚑ GearLookWiringTests 는 자 폴더를 일부러 안 세므로(거기서는 그림 칸을 직접 다루는 것이 옳다) 이 자리는 그 자에 안 걸린다 —
            //     예외 안에서 같은 고장이 나면 아무도 안 잡는다는 것을 값 회차가 값을 치르고 알았다(결정 1033 ④).
            foreach (var part in GearLook.LookParts)
                foreach (var set in d.Gear.Sets)
                    for (int rar = 0; rar < d.Gear.RarName.Length; rar++)
                    {
                        int r = d.Gear.LookRar(rar);
                        var key = GearLook.PartKey(part, set, r);
                        Assert.That(key, Is.Not.Null, part + "/" + set + "/" + d.Gear.RarName[rar]);
                        Assert.That(sprites.Contains(key), Is.True, $"catalog.json 에 없음: {key}(등급 «{d.Gear.RarName[rar]}» → 그림 칸 {r})");
                        var icon = GearLook.IconKey(part, set, r);
                        Assert.That(icon, Is.EqualTo(GearLook.IconPrefix + part + "." + set + "." + r), "아이콘 키는 cmi.* (T31)");
                        Assert.That(sprites.Contains(icon), Is.True, "catalog.json 에 없음: " + icon);
                    }
        }

        /// <summary>
        /// T175(주인 2026-09-07 10:5X «부위 표시 부분 pictoicon 으로») — 부위 여섯의 표시 아이콘이
        /// ⓐ 빠짐 없이 있고 ⓑ 서로 다르고 ⓒ **PictoIcon(`pi.*`)** 이고 ⓓ 카탈로그에 실재하고 ⓔ 그 파일이 `PictoIcon/` 아래 있는가.
        /// 하나라도 아이템 그림(`gi.*`)으로 되돌아가면 ⓒ 에서 걸린다.
        /// </summary>
        [Test]
        public void PartIconsArePictoIconsAndAllDistinct()
        {
            var d = TestData.Load();
            var path = Path.GetFullPath(Path.Combine(TestData.Dir, "..", "..", "KkomaKnight", "catalog.json"));
            var sprites = new JNode(MiniJson.Parse(File.ReadAllText(path)))["sprites"];
            var seen = new HashSet<string>();
            foreach (var part in d.Gear.Parts)
            {
                var key = GearLook.PartIcon(part);
                Assert.That(key, Is.Not.Null.And.Not.Empty, "부위 표시 아이콘이 없다: " + part);
                Assert.That(key.StartsWith("pi."), Is.True, "부위 표시는 PictoIcon 기호여야 한다(T175 · 아이템 그림 gi.* 로 되돌아갔다): " + part + " → " + key);
                Assert.That(seen.Add(key), Is.True, "두 부위가 같은 그림을 쓴다(부위로 안 읽힌다): " + part + " → " + key);
                var file = sprites[key].Str();
                Assert.That(file, Is.Not.Null.And.Not.Empty, "catalog.json 에 없는 키: " + key);
                Assert.That(file.Contains("/PictoIcon/"), Is.True, "PictoIcon 폴더 그림이어야 한다: " + file);
                Assert.That(File.Exists(TestData.RepoFile(file)), Is.True, "그림 파일 없음: " + file);
            }
            Assert.That(seen.Count, Is.EqualTo(d.Gear.Parts.Length), "부위 수만큼 서로 다른 아이콘");
        }

        /// <summary>T31 주인 지시 «아이콘용 그림과 입는 그림은 따로» — 착용 키(cm.gear.*)는 Parts/ 의 그림, 아이콘 키(cmi.gear.*)는 Thumbnail/ 의 **같은 이름** 그림이어야 한다(투구·무기·갑옷 × 세트 × 등급 36쌍 전부).</summary>
        [Test]
        public void IconKeysAreThumbnailsOfTheSameWornPart()
        {
            var d = TestData.Load();
            var path = Path.GetFullPath(Path.Combine(TestData.Dir, "..", "..", "KkomaKnight", "catalog.json"));
            var sprites = new JNode(MiniJson.Parse(File.ReadAllText(path)))["sprites"];
            int n = 0;
            // ⚠ 여기도 «등급» 을 «그림 칸» 자리에 넘기고 있었다 — 그런데 이쪽은 **조용히 통과한다**:
            //   Suffix 의 클램프가 범위 밖 등급을 마지막 칸으로 눌러 «신화를 전설 그림으로 재고도» 초록이기 때문이다.
            //   빨개지는 쪽(EveryLookPartSetRarityHasCatalogSprite)보다 이쪽이 더 나쁘다 — 아무도 안 운다. LookRar 를 거친다(T325 문 ⓑ).
            foreach (var part in GearLook.LookParts)
                foreach (var set in d.Gear.Sets)
                    for (int rar = 0; rar < d.Gear.RarName.Length; rar++)
                    {
                        int r = d.Gear.LookRar(rar);
                        var worn = sprites[GearLook.PartKey(part, set, r)].Str(); var icon = sprites[GearLook.IconKey(part, set, r)].Str();
                        Assert.That(worn.Contains("/Parts Pack Base/Parts/"), Is.True, "착용 그림은 Parts/: " + worn);
                        Assert.That(icon.Contains("/Parts Pack Base/Thumbnail/"), Is.True, "아이콘 그림은 Thumbnail/: " + icon);
                        Assert.That(worn, Is.Not.EqualTo(icon), "아이콘과 입는 그림이 같은 파일이면 안 된다(T31)");
                        Assert.That(Path.GetFileName(icon), Is.EqualTo(Path.GetFileName(worn)), "Thumbnail 은 입는 파츠와 같은 이름: " + worn);
                        Assert.That(File.Exists(TestData.RepoFile(icon)), Is.True, "Thumbnail 파일 없음: " + icon);
                        n++;
                    }
            Assert.That(n, Is.EqualTo(GearLook.LookParts.Length * d.Gear.Sets.Length * d.Gear.RarName.Length));
        }

        [Test]
        public void PartsWithoutLookUseGuiProIcons()
        {
            var d = TestData.Load(); var sprites = CatalogSprites();
            foreach (var part in d.Gear.Parts)
            {
                if (GearLook.HasLook(part)) continue;
                foreach (var set in d.Gear.Sets)
                {
                    Assert.That(GearLook.PartKey(part, set, 0), Is.Null, part);
                    var icon = GearLook.IconKey(part, set, 3);
                    Assert.That(icon.StartsWith("gi."), Is.True, icon);
                    Assert.That(sprites.Contains(icon), Is.True, "catalog.json 에 없음: " + icon);
                }
            }
        }

        [Test]
        public void ItemKeysFollowTypeSet()
        {
            var d = TestData.Load();
            var g = new GearItem { Part = "weapon", Type = "hpsh_weapon", Rar = 2, Plus = 0 };
            // ⚑ 장착품 갈래는 «등급 → 그림 칸» 을 거친다(T325 문 ⓑ) — 그래서 기대값을 «.2» 로 박지 않고 그 관계로 적는다.
            //   등급이 넷인 오늘은 항등이라 여전히 «.2» 이고, 다섯이 되는 날에는 표가 답하는 칸을 따라간다.
            Assert.That(GearLook.PartKey(d, g), Is.EqualTo("cm.gear.weapon.hpsh." + d.Gear.LookRar(g.Rar)),
                "장착품 키의 등급 자리는 GearData.LookRar 를 거친 «그림 칸» 이어야 한다");
            Assert.That(GearLook.PartKey(d, g), Is.EqualTo(GearLook.PartKey("weapon", "hpsh", d.Gear.LookRar(g.Rar))),
                "두 갈래(장착품·문자열)가 같은 키를 내야 한다");
            Assert.That(GearLook.PartKey("weapon", "crit", 99), Is.EqualTo("cm.gear.weapon.crit.3"));
            Assert.That(GearLook.PartKey("weapon", "crit", -1), Is.EqualTo("cm.gear.weapon.crit.0"));
        }

        [Test]
        public void WeaponSlotBySet()
        {
            Assert.That(GearLook.WeaponSlot("crit"), Is.EqualTo("Sword"));
            Assert.That(GearLook.WeaponSlot("hpsh"), Is.EqualTo("Blunt"));
            Assert.That(GearLook.WeaponSlot("evade"), Is.EqualTo("Sword"));   // T17 · 창 폐기 — 근접 두 계열(검·둔기)만
            foreach (var set in TestData.Load().Gear.Sets) Assert.That(GearLook.WeaponSlot(set), Is.EqualTo("Sword").Or.EqualTo("Blunt").Or.EqualTo("Axe"), set);
        }

        /// <summary>T17 주인 지시 «무기는 전부 근접 무기 — 검(Sword)·방망이(Blunt)·도끼(Axe) 세 계열에서» — 무기 파츠 경로가 전부 HandRight/Sword·Blunt·Axe 폴더인가(활·지팡이·완드·창 0) · 착용 슬롯(WeaponSlot)이 그 폴더와 같은가.</summary>
        [Test]
        public void WeaponPartsAreMeleeSwordBluntAxeOnly()
        {
            var d = TestData.Load();
            var path = Path.GetFullPath(Path.Combine(TestData.Dir, "..", "..", "KkomaKnight", "catalog.json"));
            var sprites = new JNode(MiniJson.Parse(File.ReadAllText(path)))["sprites"];
            int n = 0;
            foreach (var set in d.Gear.Sets)
                for (int r = 0; r < d.Gear.RarName.Length; r++)
                {
                    var file = sprites[GearLook.PartKey(GearLook.Weapon, set, r)].Str();
                    Assert.That(file.Contains("/HandRight/Sword/") || file.Contains("/HandRight/Blunt/") || file.Contains("/HandRight/Axe/"), Is.True, "근접 무기(검·둔기·도끼)가 아니다: " + file);
                    Assert.That(file.Contains("/Bow/") || file.Contains("/Spear/") || file.Contains("Wand") || file.Contains("Staff"), Is.False, file);
                    Assert.That(file.Contains("/HandRight/" + GearLook.WeaponSlot(set) + "/"), Is.True, "착용 슬롯 " + GearLook.WeaponSlot(set) + " ≠ 파츠 폴더: " + file);
                    n++;
                }
            Assert.That(n, Is.EqualTo(d.Gear.Sets.Length * d.Gear.RarName.Length));
        }

        // ───────────────────────── T17 · 파츠 아이콘 맞춤(순수 계산) ─────────────────────────
        [Test]
        public void FitPartIconFillsFrameByOpaqueBounds()
        {
            // 투구 FA_Helmet_002_Brown: 120×108 캔버스 · 불투명 bbox 67×65 (x 26..93 · y 20..85) · 칸 188 · Item 스케일 0.6149
            var f = GearLook.FitPartIcon(120, 108, 26, 20, 93, 85, 188, 0.72, 0.6149);
            double k = 0.72 * 188 / 67;                      // 스프라이트 1픽셀 → 칸 픽셀
            Assert.That(f.W * 0.6149 / 120, Is.EqualTo(k).Within(1e-9), "rect 1픽셀의 실제 크기 = k");
            Assert.That(67 * f.W * 0.6149 / 120, Is.EqualTo(0.72 * 188).Within(1e-9), "bbox 긴 변 = 칸의 72%");
            Assert.That(f.H / f.W, Is.EqualTo(108.0 / 120).Within(1e-9), "sizeDelta 비율 = rect 비율(preserveAspect 여백 0)");
            Assert.That(f.PivotX, Is.EqualTo((26 + 93) / 2.0 / 120).Within(1e-9)); Assert.That(f.PivotY, Is.EqualTo((20 + 85) / 2.0 / 108).Within(1e-9));
        }

        [Test]
        public void FitPartIconUsesLongerSideAndScaleOne()
        {
            // 검 120×61 · bbox 84×33(가로가 김) · 스케일 1
            var f = GearLook.FitPartIcon(120, 61, 10, 14, 94, 47, 100, 0.72, 1);
            Assert.That(84 * f.W / 120, Is.EqualTo(72).Within(1e-9), "긴 변(가로)이 칸의 72%");
            Assert.That(33 * f.H / 61, Is.LessThan(72), "짧은 변은 그보다 작다");
            // GUI Pro 128 아이콘이 프리팹 Item(256 × 0.6149) 에서 차지하는 눈높이(≈ 0.85 × 157 ≈ 134px ≈ 칸 190 의 70%)와 같은 급이어야 한다
            double guiPro = 0.85 * 256 * 0.6149 / 190;
            Assert.That(GearLook.PartIconFill, Is.EqualTo(guiPro).Within(0.05));
            Assert.That(GearLook.PartIconFill, Is.GreaterThanOrEqualTo(0.70).And.LessThanOrEqualTo(0.75), "주인 지시 70~75%");
        }

        [Test]
        public void FitPartIconDegenerateInputsFallBackToRect()
        {
            var f = GearLook.FitPartIcon(120, 108, 0, 0, 0, 0, 188, 0.72, 0);   // bbox 없음 · 스케일 0
            Assert.That(f.PivotX, Is.EqualTo(0.5).Within(1e-9)); Assert.That(f.PivotY, Is.EqualTo(0.5).Within(1e-9));
            Assert.That(f.W, Is.EqualTo(0.72 * 188).Within(1e-9), "rect 전체를 bbox 로 · 긴 변(가로 120)이 72%");
            var z = GearLook.FitPartIcon(0, 0, 0, 0, 0, 0, 0, 0, 0);
            Assert.That(double.IsNaN(z.W) || double.IsInfinity(z.W), Is.False);
        }
    }
}
