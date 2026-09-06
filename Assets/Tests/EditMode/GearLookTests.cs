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

        [Test]
        public void RarCountMatchesGearJson()
        {
            var d = TestData.Load();
            Assert.That(d.Gear.RarName.Length, Is.EqualTo(GearLook.RarCount));
        }

        [Test]
        public void EveryLookPartSetRarityHasCatalogSprite()
        {
            var d = TestData.Load(); var sprites = CatalogSprites();
            foreach (var part in GearLook.LookParts)
                foreach (var set in d.Gear.Sets)
                    for (int r = 0; r < d.Gear.RarName.Length; r++)
                    {
                        var key = GearLook.PartKey(part, set, r);
                        Assert.That(key, Is.Not.Null, part + "/" + set + "/" + r);
                        Assert.That(sprites.Contains(key), Is.True, "catalog.json 에 없음: " + key);
                        Assert.That(GearLook.IconKey(part, set, r), Is.EqualTo(key));
                    }
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
            Assert.That(GearLook.PartKey(d, g), Is.EqualTo("cm.gear.weapon.hpsh.2"));
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
