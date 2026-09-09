using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T325 ⓐ 회차 «자리» — <b>덮어쓰기 표 셋(gear·gacha·tune)이 실제로 먹는가, 그리고 오늘은 한 톨도 안 바꾸는가.</b>
    /// <para>
    /// 이 회차는 <b>값을 0줄</b> 넣었다. 까닭은 §2 T325 마지막 줄이 못 박은 것이다 —
    /// «ⓐ 가 들어가면 주인 폰의 전투가 바로 쉬워지거나 어려워진다 · ⓐ·ⓑ 를 같은 초록 런에 넣는 것이 옳다».
    /// 그래서 <b>기계만 먼저 세우고</b>, 그 기계가 ① 오늘 아무것도 안 바꾸고 ② 다음 회차의 진짜 값을 실제로 실어 나르는지를 여기서 잰다.
    /// 기계를 자 없이 놓아 두면 값을 넣는 회차에 «값이 틀렸나 길이 틀렸나» 를 못 가른다(워커 B 가 결정 918 에서 자리와 값을 가른 그 까닭).
    /// </para>
    /// <para>⚠ 여기 있는 «정본 값» 들은 <c>data/*.json</c> 에서 직접 읽는다 — 베껴 적으면 정본이 바뀌는 날 이 자가 거짓말을 한다.</para>
    /// </summary>
    public class GameDataOverrideTests
    {
        static GameData Fresh() => GameData.LoadFromDirectory(TestData.Dir);
        static JNode Canon(string file) => new JNode(MiniJson.Parse(File.ReadAllText(Path.Combine(TestData.Dir, file))));

        // ───────────────────── ① 오늘은 아무것도 안 바뀐다 ─────────────────────

        /// <summary>
        /// 새 덮어쓰기 파일 셋이 들어왔는데 <b>실린 값이 정본과 한 톨도 다르지 않아야</b> 한다.
        /// 이 자가 빨개지는 날 = 누가 «값 0줄» 약속을 깬 날이고, 그 순간 주인 폰의 밸런스가 움직인다.
        /// </summary>
        [Test]
        public void TodayTheNewOverridesChangeNothingAtAll()
        {
            var D = Fresh();
            var gear = Canon("gear.json");
            Assert.AreEqual(gear["rarName"].StrArray(), D.Gear.RarName, "등급 이름이 정본과 달라졌다 — gearOverride.json 에 값이 들어갔다");
            Assert.AreEqual(gear["rarLegend"].Int(), D.Gear.RarLegend, "rarLegend 가 움직였다");
            Assert.AreEqual(gear["rarMyth"].Int(), D.Gear.RarMyth, "rarMyth 가 움직였다");
            // RarRare 의 «유도하는 식» 을 바꿨다(전설 아래 → 일반 위) — 오늘 값은 그대로 1 이어야 한다(T261 천장이 가리키는 등급이 안 움직였다)
            Assert.AreEqual(1, D.Gear.RarRare, "오늘의 «희귀» 인덱스는 옛 식과 같은 1 이어야 한다");
            Assert.AreEqual("희귀", D.Gear.RarName[D.Gear.RarRare], "그 칸이 실제로 «희귀» 여야 한다");
            var c = gear["contribution"];
            Assert.AreEqual(c["atk"].NumArray(), D.Gear.Atk, "기여(공)가 움직였다");
            Assert.AreEqual(c["hp"].NumArray(), D.Gear.Hp, "기여(체)가 움직였다");
            Assert.AreEqual(c["sh"].NumArray(), D.Gear.Sh, "기여(실)가 움직였다");

            var tune = Canon("tune.json")["tune"];
            Assert.AreEqual(tune["maxChapter"].Int(), D.Tune.MaxChapter, "챕터 수가 움직였다 — tuneOverride.json 에 값이 들어갔다");
            Assert.AreEqual(tune["eBaseHp"].Num(), D.Tune.EBaseHp, 1e-12, "적 기저 체력이 움직였다");
            Assert.AreEqual(tune["eBaseDmg"].Num(), D.Tune.EBaseDmg, 1e-12, "적 기저 공격이 움직였다");

            var boxes = Canon("gacha.json")["boxes"];
            foreach (var b in D.Gacha.Boxes)
                Assert.AreEqual(boxes[b.Key]["rate"].NumArray(), b.Rate, $"상자 «{b.Key}» 확률이 움직였다 — gachaOverride.json 에 값이 들어갔다");
        }

        /// <summary>
        /// 덮어쓰기 파일 목록에 이름을 더하고 <b>카탈로그에 등록을 잊는</b> 것이 이 기계의 제일 조용한 고장이다 —
        /// 게임 쪽(Bootstrap)은 경고 한 줄만 찍고 그냥 지나가므로 «하니스에서는 먹고 폰에서는 안 먹는» 두 규칙이 생긴다.
        /// 그래서 <b>목록 · 파일 · 카탈로그 셋을 맞대어</b> 잰다.
        /// </summary>
        [Test]
        public void EveryOverrideFileExistsAndIsRegisteredInTheCatalog()
        {
            var texts = new JNode(MiniJson.Parse(File.ReadAllText(TestData.RepoFile("Assets/KkomaKnight/catalog.json"))))["texts"];
            Assert.That(GameData.OverrideFiles.Length, Is.GreaterThanOrEqualTo(4), "덮어쓰기 표는 넷 이상이다(combat·gear·gacha·tune)");
            foreach (var file in GameData.OverrideFiles)
            {
                var path = TestData.RepoFile("Assets/KkomaKnight/" + file);
                Assert.IsTrue(File.Exists(path), $"{file} 이 Assets/KkomaKnight/ 에 없다 — 하니스는 조용히 안 먹는다");
                var key = GameData.OverrideCatalogKey(file);
                Assert.IsTrue(texts.Has(key), $"catalog.json texts 에 «{key}» 가 없다 — 폰에서는 이 표가 안 먹고 경고 한 줄만 찍힌다");
                Assert.AreEqual("Assets/KkomaKnight/" + file, texts[key].Str(), $"«{key}» 가 가리키는 경로가 파일 이름과 다르다");
            }
        }

        // ───────────────────── ② 그런데 기계는 진짜로 먹는다 ─────────────────────

        /// <summary>장비 덮어쓰기가 <b>적힌 칸만</b> 바꾸고 나머지는 정본 그대로 둔다.</summary>
        [Test]
        public void GearOverrideChangesOnlyTheKeysItNames()
        {
            var D = Fresh();
            var slotStep = D.Gear.SlotStep; var plusStep = D.Gear.PlusStep; var parts = D.Gear.Parts.Length;
            D.ApplyGearOverride("{ \"contribution\": { \"atk\": [1, 2, 3, 4] } }");
            Assert.AreEqual(new double[] { 1, 2, 3, 4 }, D.Gear.Atk, "기여(공)는 덮여야 한다");
            Assert.AreEqual(Canon("gear.json")["contribution"]["hp"].NumArray(), D.Gear.Hp, "안 적은 기여(체)는 정본 그대로여야 한다");
            Assert.AreEqual(slotStep, D.Gear.SlotStep, 1e-12, "슬롯 배율은 이 표의 칸이 아니다(주인이 노강 값만 줬다)");
            Assert.AreEqual(plusStep, D.Gear.PlusStep, 1e-12, "강화 배율도 이 표의 칸이 아니다");
            Assert.AreEqual(parts, D.Gear.Parts.Length, "부위는 안 건드린다");
        }

        /// <summary>손잡이 덮어쓰기 — 정본과 같은 «tune» 껍질을 써도 되고 안 써도 된다(둘 다 같은 곳에 닿는다).</summary>
        [Test]
        public void TuneOverrideWorksWithOrWithoutTheTuneWrapper()
        {
            var a = Fresh(); a.ApplyTuneOverride("{ \"maxChapter\": 100 }");
            var b = Fresh(); b.ApplyTuneOverride("{ \"tune\": { \"maxChapter\": 100 } }");
            Assert.AreEqual(100, a.Tune.MaxChapter, "껍질 없이도 닿아야 한다");
            Assert.AreEqual(100, b.Tune.MaxChapter, "정본과 같은 껍질로도 닿아야 한다");

            b.ApplyTuneOverride("{ \"eHpSeg\": [[0, 1.1], [5, 1.2], [10, 1.05]] }");
            Assert.AreEqual(3, b.Tune.EHpSeg.Length, "구간 표가 통째로 덮여야 한다");
            Assert.AreEqual(5, b.Tune.EHpSeg[1][0], 1e-12, "구간 시작 챕터");
            Assert.AreEqual(1.05, b.Tune.EHpSeg[2][1], 1e-12, "구간 배율");
        }

        /// <summary>
        /// <b>cum 은 표에 안 적고 코드가 rate 에서 계산한다</b> — 그 계산이 정본이 적어 둔 cum 과 <b>같은 뜻</b> 인지 여기서 맞대어 본다.
        /// (다르면 확률표는 새 값인데 굴림은 옛 값인 채로 아무 자도 안 우는 고장이 된다.)
        /// </summary>
        [Test]
        public void TheRecomputedCumMatchesWhatTheCanonicalTableWrote()
        {
            var boxes = Canon("gacha.json")["boxes"];
            foreach (var k in boxes.Keys)
            {
                var rate = boxes[k]["rate"].NumArray();
                var canonCum = boxes[k]["cum"].NumArray();
                var mine = GameData.CumOf(rate);
                Assert.AreEqual(canonCum.Length, mine.Length, $"상자 «{k}» 의 cum 칸 수");
                for (int i = 0; i < mine.Length; i++)
                    Assert.AreEqual(canonCum[i], mine[i], 1e-9, $"상자 «{k}» 의 cum[{i}] — 정본이 적은 뜻과 계산이 어긋난다");
            }
        }

        /// <summary>상자 덮어쓰기는 <c>rate</c> 를 바꾸면서 <c>cum</c> 도 같이 다시 세워, 굴림이 새 확률로 돈다.</summary>
        [Test]
        public void GachaOverrideRebuildsTheCumSoTheRollFollows()
        {
            var D = Fresh();
            D.ApplyGachaOverride("{ \"boxes\": { \"rare\": { \"rate\": [50, 50, 0, 0], \"cost\": 90 } } }");
            var box = D.Gacha.Box("rare");
            Assert.AreEqual(90, box.Cost, 1e-12, "비용도 덮인다");
            Assert.AreEqual(50, box.Cum[1], 1e-9, "cum 이 새 rate 로 다시 서야 한다");
            Assert.AreEqual(100, box.Cum[0], 1e-9, "cum[0] 은 늘 100");
            // 굴림은 «작은 수가 높은 등급» 이다(정본 cum 의 뜻 그대로) — 50/50 이면 [0,50) 이 희귀다
            Assert.AreEqual(1, box.RarRoll(0), "0 은 희귀 — 굴림이 새 확률을 본다");
            Assert.AreEqual(1, box.RarRoll(49.999), "49.999 까지 희귀");
            Assert.AreEqual(0, box.RarRoll(50), "50 부터 일반 — 옛 cum(33.3) 이 남아 있으면 여기가 빨개진다");
            Assert.AreEqual(Canon("gacha.json")["boxes"]["legend"]["rate"].NumArray(), D.Gacha.Box("legend").Rate, "안 적은 상자는 정본 그대로");
        }

        /// <summary>없는 상자 키는 <b>조용히 안 넘긴다</b> — 오타 하나로 «확률을 바꿨는데 안 바뀌는» 것이 제일 찾기 어려운 고장이다.</summary>
        [Test]
        public void AnUnknownBoxKeyThrowsInsteadOfBeingIgnored()
        {
            var D = Fresh();
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(
                () => D.ApplyGachaOverride("{ \"boxes\": { \"legned\": { \"rate\": [50, 50, 0, 0] } } }"),
                "상자 키 오타는 던져야 한다");
        }

        // ───────────────────── ③ 어긋난 표는 부팅에서 운다 ─────────────────────

        /// <summary>
        /// 등급을 늘리면서 <b>한쪽만</b> 늘리는 것이 이 개편에서 제일 나기 쉬운 실수다.
        /// 옛날엔 그런 표로도 부팅이 되고 «신화 장비를 낀 순간» 에야 터졌다 — 이제 표를 다 먹인 자리에서 먼저 운다.
        /// </summary>
        [Test]
        public void HalfWidenedTablesThrowRightAfterTheOverridesInsteadOfMuchLater()
        {
            var D = Fresh();
            D.ApplyGearOverride("{ \"rarName\": [\"일반\", \"희귀\", \"영웅\", \"전설\", \"신화\"] }");
            var e = Assert.Throws<System.FormatException>(() => D.ValidateOverridden(), "등급만 다섯이고 기여가 넷이면 던져야 한다");
            StringAssert.Contains("contribution", e.Message, "무엇이 안 맞는지 이름을 대야 한다");
        }

        /// <summary>상자 확률 칸 수가 등급 수와 다르면 <b>그 등급이 영영 안 나온다</b> — 예외도 로그도 없던 자리라 여기서 던진다.</summary>
        [Test]
        public void ABoxWhoseRateHasTooFewSlotsThrows()
        {
            var D = Fresh();
            D.Gear.RarName = new[] { "일반", "희귀", "영웅", "전설", "신화" };
            D.Gear.Atk = new double[] { 30, 60, 90, 120, 150 };
            D.Gear.Hp = new double[] { 60, 120, 180, 240, 300 };
            D.Gear.Sh = new double[] { 90, 180, 270, 360, 450 };
            D.Gear.OptCountByRar = new[] { 0, 1, 2, 3, 4 };
            D.Gear.RarLegend = 3; D.Gear.RarMyth = 4;
            var e = Assert.Throws<System.FormatException>(() => D.ValidateOverridden(), "상자 rate 가 넷인 채로는 지나가면 안 된다");
            StringAssert.Contains("rate", e.Message);
        }

        /// <summary>확률 합이 100 이 아니거나 등급 인덱스가 어긋나거나 챕터가 정본보다 많으면 던진다.</summary>
        [Test]
        public void TheOtherBrokenShapesThrowToo()
        {
            var a = Fresh();
            a.ApplyGachaOverride("{ \"boxes\": { \"rare\": { \"rate\": [50, 40, 0, 0] } } }");
            StringAssert.Contains("합", Assert.Throws<System.FormatException>(() => a.ValidateOverridden()).Message, "합 90 은 못 지나간다");

            var b = Fresh();
            b.ApplyGearOverride("{ \"rarLegend\": 3, \"rarMyth\": 2 }");
            StringAssert.Contains("등급 인덱스", Assert.Throws<System.FormatException>(() => b.ValidateOverridden()).Message, "전설이 신화보다 위면 못 지나간다");

            var c = Fresh();
            c.ApplyTuneOverride("{ \"maxChapter\": 99999 }");
            StringAssert.Contains("maxChapter", Assert.Throws<System.FormatException>(() => c.ValidateOverridden()).Message, "정본에 없는 챕터는 못 지나간다");

            var d = Fresh();
            d.ApplyTuneOverride("{ \"eHpSeg\": [[0, 1.1], [5, 1.2], [3, 1.05]] }");
            StringAssert.Contains("오름차순", Assert.Throws<System.FormatException>(() => d.ValidateOverridden()).Message, "구간 경계가 거꾸로면 못 지나간다");
        }

        /// <summary>
        /// 챕터를 <b>줄이는</b> 것은 통과해야 한다 — 주인 지시가 «챕터 수는 100» 인데 <c>enemies.json</c> 은 420 줄 그대로이기 때문이다(§1).
        /// 로드 때의 «같은가» 검사를 그대로 다시 돌리면 여기서 막혔을 자리다.
        /// </summary>
        [Test]
        public void ShrinkingTheChapterCountIsAllowedBecauseTheCanonKeepsAllOfThem()
        {
            var D = Fresh();
            D.ApplyTuneOverride("{ \"maxChapter\": 100 }");
            Assert.DoesNotThrow(() => D.ValidateOverridden(), "정본이 더 많이 들고 있는 것은 어긋난 것이 아니다");
            Assert.That(D.Enemies.Chapters.Count, Is.GreaterThan(100), "정본은 100 보다 많은 챕터를 그대로 들고 있다");
        }

        // ───────────────────── ④ 다음 회차의 진짜 값을 실어 나를 수 있는가 ─────────────────────

        /// <summary>
        /// <b>다음 회차가 넣을 값 전부를 미리 한 번 태워 본다</b>(파일에는 안 넣는다 · 여기서만).
        /// 이 자가 서면 «값을 넣는 회차» 는 JSON 두 개를 채우는 일이 되고, 안 서면 그 회차가 기계와 값을 동시에 의심하게 된다.
        /// 값의 출처는 §2 T325 1·2·3항(주인 원문)이다 — 지어낸 수는 없다.
        /// </summary>
        [Test]
        public void TheFiveGradeTableTheOwnerAskedForFitsThroughThisMachine()
        {
            var D = Fresh();
            D.ApplyGearOverride(@"{
              ""rarName"": [""일반"", ""희귀"", ""영웅"", ""전설"", ""신화""],
              ""rarLegend"": 3, ""rarMyth"": 4,
              ""contribution"": { ""atk"": [30, 60, 90, 120, 150], ""hp"": [60, 120, 180, 240, 300], ""sh"": [90, 180, 270, 360, 450] },
              ""optionLadder"": { ""optCount"": [0, 1, 2, 3, 4], ""mythPlusAt"": [3, 6] }
            }");
            D.ApplyGachaOverride(@"{ ""boxes"": {
              ""rare"":   { ""rate"": [66.7, 33.3, 0, 0, 0] },
              ""legend"": { ""rate"": [0, 66, 30, 4, 0] },
              ""myth"":   { ""rate"": [0, 65.2, 30, 4, 0.8] }
            } }");
            D.ApplyTuneOverride("{ \"maxChapter\": 100 }");
            Assert.DoesNotThrow(() => D.ValidateOverridden(), "주인이 준 표가 이 기계를 통과해야 한다");

            // 주인 «30씩 증가» — 등차인지 표에서 직접 잰다(칸을 옮겨 적다 틀리는 것을 여기서 잡는다)
            for (int r = 1; r < D.Gear.Atk.Length; r++)
            {
                Assert.AreEqual(30, D.Gear.Atk[r] - D.Gear.Atk[r - 1], 1e-9, "공 기여는 30씩 는다");
                Assert.AreEqual(60, D.Gear.Hp[r] - D.Gear.Hp[r - 1], 1e-9, "체 기여는 60씩 는다");
                Assert.AreEqual(90, D.Gear.Sh[r] - D.Gear.Sh[r - 1], 1e-9, "실 기여는 90씩 는다");
            }

            // 주인 «전설 상자 = 66% 희귀 · 30% 영웅 · 4% 전설» — 굴림이 실제로 그 등급을 돌려주는가(cum 을 손으로 안 적었다)
            var lg = D.Gacha.Box("legend");
            Assert.AreEqual(3, lg.RarRoll(0), "0 은 전설(4%)");
            Assert.AreEqual(3, lg.RarRoll(3.999), "3.999 까지 전설");
            Assert.AreEqual(2, lg.RarRoll(4), "4 부터 영웅(30%)");
            Assert.AreEqual(2, lg.RarRoll(33.999), "33.999 까지 영웅");
            Assert.AreEqual(1, lg.RarRoll(34), "34 부터 희귀(66%)");
            Assert.AreEqual(1, lg.RarRoll(99.999), "전설 상자에서 일반은 안 나온다");

            // 옵션 칸 수 — «일반은 안 열린다» 가 그대로 서고 신화 강화 단계가 표에서 온다
            // (여기 쓴 칸 수는 «기계가 실어 나르는가» 를 재는 보기다 — 최종 값은 §2 T325 4항 ⓓ 가 정한다)
            Assert.AreEqual(0, D.Gear.OptCount(0, 0), "일반은 옵션이 안 열린다(주인 2026-09-07)");
            Assert.AreEqual(2, D.Gear.OptCount(2, 0), "영웅 줄이 가운데에 섰다");
            Assert.AreEqual(4, D.Gear.OptCount(4, 0), "신화 노강");
            Assert.AreEqual(6, D.Gear.OptCount(4, 6), "신화 +6 은 두 단계가 열린다 — mythPlusAt 이 표에서 온다");
            Assert.AreEqual("영웅", D.Gear.RarName[2], "가운데가 영웅이다");

            // ⚑ 여기가 이 회차에서 실제로 고친 «자리» 다 — 자세한 까닭은 GearData.RarRare 주석.
            Assert.AreEqual("희귀", D.Gear.RarName[D.Gear.RarRare],
                "«희귀 확정» 천장(T261)이 가리키는 등급이 영웅으로 밀리면 안 된다 — 컴파일도 되고 아무 자도 안 우는 자리였다");
        }
    }
}
