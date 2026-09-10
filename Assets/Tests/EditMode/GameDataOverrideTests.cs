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
        static JNode Over(string file) => new JNode(MiniJson.Parse(File.ReadAllText(TestData.RepoFile("Assets/KkomaKnight/" + file))));

        static JNode Walk(JNode n, string path) { foreach (var seg in path.Split('.')) n = n[seg]; return n; }

        /// <summary>
        /// <b>그 칸에 실려 있어야 할 값</b> — 덮어쓰기 파일이 그 칸을 <i>적었으면</i> 그 값이고, <i>안 적었으면</i> 정본 값이다.
        /// 이것이 이 기계가 약속한 규칙 전부이고(«적힌 키만 바뀐다»), 그래서 이 자는 값이 0줄인 오늘도 값이 다 찬 뒤에도 같은 말을 한다.
        /// </summary>
        static JNode Want(JNode over, JNode canon, string path)
        {
            var o = Walk(over, path);
            return o.IsNull ? Walk(canon, path) : o;
        }

        /// <summary>«_note» 처럼 밑줄로 시작하는 글칸은 값이 아니다 — 나머지 잎사귀 칸의 점 경로를 모은다.</summary>
        static void CollectPaths(JNode n, string prefix, System.Collections.Generic.List<string> into)
        {
            foreach (var k in n.Keys)
            {
                if (k.Length > 0 && k[0] == '_') continue;
                var child = n[k];
                string path = prefix.Length == 0 ? k : prefix + "." + k;
                if (child.IsObject) CollectPaths(child, path, into);
                else into.Add(path);
            }
        }

        static void OnlyKnownKeys(string file, params string[] known)
        {
            var got = new System.Collections.Generic.List<string>();
            CollectPaths(Over(file), "", got);
            foreach (var p in got)
                Assert.Contains(p, known,
                    $"{file} 이 «{p}» 를 적었는데 기계가 안 읽는 칸이다 — 오타 하나로 «값을 바꿨는데 안 바뀌는» 제일 조용한 고장이 된다(있는 칸: {string.Join(" · ", known)})");
        }

        // ───────────────────── ① 적힌 키만 바뀌고, 나머지는 정본 그대로다 ─────────────────────

        /// <summary>
        /// 덮어쓰기 표 셋이 <b>제가 적은 칸만</b> 옮기고 <b>안 적은 칸은 정본 그대로</b> 두는가.
        /// <para>
        /// ⚑ 이 자는 «오늘은 안 바뀐다» 를 재지 않는다 — 그러면 주인 값이 들어가는 날 <b>설계대로</b> 빨개져서
        /// 그날 «값이 틀렸나 기계가 틀렸나» 를 못 가르게 된다. 대신 <b>기계가 약속한 규칙 자체</b>를 재므로
        /// 값이 0줄인 오늘도, 다섯 등급 표가 다 들어간 뒤에도 같은 말을 한다.
        /// </para>
        /// <para>그래서 이 자가 빨개지는 뜻은 하나뿐이다 — <b>파일에 적은 것과 실제로 실린 것이 다르다</b>.</para>
        /// </summary>
        [Test]
        public void EachOverrideMovesExactlyTheKeysItDeclaresAndLeavesTheRestAtCanon()
        {
            var D = Fresh();

            var gc = Canon("gear.json");
            var go = Over(GameData.GearOverrideFile);
            Assert.AreEqual(Want(go, gc, "rarName").StrArray(), D.Gear.RarName, "등급 이름이 gearOverride.json 이 적은 것과 다르다");
            Assert.AreEqual(Want(go, gc, "rarLegend").Int(), D.Gear.RarLegend, "rarLegend 가 적은 것과 다르다");
            Assert.AreEqual(Want(go, gc, "rarMyth").Int(), D.Gear.RarMyth, "rarMyth 가 적은 것과 다르다");
            Assert.AreEqual(Want(go, gc, "contribution.atk").NumArray(), D.Gear.Atk, "기여(공)가 적은 것과 다르다");
            Assert.AreEqual(Want(go, gc, "contribution.hp").NumArray(), D.Gear.Hp, "기여(체)가 적은 것과 다르다");
            Assert.AreEqual(Want(go, gc, "contribution.sh").NumArray(), D.Gear.Sh, "기여(실)가 적은 것과 다르다");

            // T261 천장이 가리키는 등급 — 표가 몇 칸이 되든 «희귀 확정» 은 희귀를 가리켜야 한다(영웅이 끼면 조용히 «영웅 확정» 이 된다)
            Assert.AreEqual("희귀", D.Gear.RarName[D.Gear.RarRare], "RarRare 가 «희귀» 가 아닌 칸을 가리킨다 — T261 천장의 뜻이 바뀐다");

            var to = Over(GameData.TuneOverrideFile); if (to.Has("tune")) to = to["tune"];
            var tc = Canon("tune.json")["tune"];
            Assert.AreEqual(Want(to, tc, "maxChapter").Int(), D.Tune.MaxChapter, "챕터 수가 tuneOverride.json 이 적은 것과 다르다");
            Assert.AreEqual(Want(to, tc, "eBaseHp").Num(), D.Tune.EBaseHp, 1e-12, "적 기저 체력이 적은 것과 다르다");
            Assert.AreEqual(Want(to, tc, "eBaseDmg").Num(), D.Tune.EBaseDmg, 1e-12, "적 기저 공격이 적은 것과 다르다");

            var ac = Canon("gacha.json");
            var ao = Over(GameData.GachaOverrideFile);
            foreach (var b in D.Gacha.Boxes)
            {
                Assert.AreEqual(Want(ao, ac, "boxes." + b.Key + ".rate").NumArray(), b.Rate, $"상자 «{b.Key}» 확률이 적은 것과 다르다");
                // 굴림이 보는 것은 rate 가 아니라 cum 이다 — rate 만 맞고 cum 이 옛것이면 아무 자도 안 운다
                Assert.AreEqual(GameData.CumOf(b.Rate), b.Cum, $"상자 «{b.Key}» 의 cum 이 rate 에서 다시 계산되지 않았다");
            }
        }

        /// <summary>
        /// 덮어쓰기 파일이 <b>기계가 안 읽는 칸</b>을 적지 않았는가 — <c>rarNam</c> 같은 오타 하나면
        /// «값을 바꿨는데 게임이 그대로» 가 되고, 장비·손잡이·적 표는 모르는 키를 <b>조용히 흘린다</b>(상자만 던진다).
        /// <para>이 자도 값이 아니라 <b>칸 이름</b>을 재므로 값이 들어간 뒤에도 그대로 산다.</para>
        /// </summary>
        [Test]
        public void NoOverrideFileDeclaresAKeyTheMachineDoesNotRead()
        {
            OnlyKnownKeys(GameData.CombatOverrideFile,
                "range.spearReach", "range.waveReach", "pierce.spear", "pierce.wave", "pierce.waveBig");
            OnlyKnownKeys(GameData.GearOverrideFile,
                "rarName", "rarLegend", "rarMyth",
                "contribution.atk", "contribution.hp", "contribution.sh",
                "optionLadder.optCount", "optionLadder.mythPlusAt", "look.rarSprite",
                "enhance.plusStep", "enhance.legendToMythPlus", "enhance.legendMaxPlus");
            OnlyKnownKeys(GameData.TuneOverrideFile,
                "maxChapter", "eBaseHp", "eBaseDmg", "eHpSeg", "eDmgSeg",
                "tune.maxChapter", "tune.eBaseHp", "tune.eBaseDmg", "tune.eHpSeg", "tune.eDmgSeg");
            OnlyKnownKeys(GameData.EnemiesOverrideFile, "hpSeg", "dmgSeg");

            var known = new System.Collections.Generic.List<string>();
            foreach (var b in Fresh().Gacha.Boxes)
                foreach (var k in new[] { "rate", "cost", "pityMyth", "pityLegend", "pityRare" })
                    known.Add("boxes." + b.Key + "." + k);
            OnlyKnownKeys(GameData.GachaOverrideFile, known.ToArray());
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
            Assert.That(GameData.OverrideFiles.Length, Is.GreaterThanOrEqualTo(5), "덮어쓰기 표는 다섯 이상이다(combat·gear·gacha·tune·enemies)");
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
            // ⚠ «안 적은 칸» 의 대조는 **덮기 직전의 값**이지 «정본 파일» 이 아니다 —
            //   이 레포는 주인 지시로 정본 위에 값을 덮으므로(T325) 정본과 비교하면 «내가 안 적은 칸» 도 다르게 나온다.
            var hpBefore = (double[])D.Gear.Hp.Clone();
            var atk = new double[D.Gear.RarName.Length];
            for (int i = 0; i < atk.Length; i++) atk[i] = i + 1;          // 칸 수는 표에서 낸다(등급이 늘어도 이 자가 산다)
            D.ApplyGearOverride("{ \"contribution\": { \"atk\": [" + string.Join(", ", atk) + "] } }");
            Assert.AreEqual(atk, D.Gear.Atk, "기여(공)는 덮여야 한다");
            Assert.AreEqual(hpBefore, D.Gear.Hp, "안 적은 기여(체)는 덮기 전 그대로여야 한다");
            Assert.AreEqual(slotStep, D.Gear.SlotStep, 1e-12, "슬롯 배율은 이 표의 칸이 아니다(주인이 노강 값만 줬다)");
            Assert.AreEqual(plusStep, D.Gear.PlusStep, 1e-12, "«enhance» 를 안 적었으면 강화 배율은 덮기 전 그대로여야 한다");
            Assert.AreEqual(parts, D.Gear.Parts.Length, "부위는 안 건드린다");
        }

        /// <summary>T405 가 연 <c>enhance</c> 칸 — 강화 배율이 <b>진짜로 덮인다</b>(적어 놓고 안 먹는 것이 제일 조용한 고장이다).</summary>
        [Test]
        public void GearOverrideCarriesTheEnhanceBlock()
        {
            var D = Fresh();
            var atkBefore = (double[])D.Gear.Atk.Clone();
            D.ApplyGearOverride("{ \"enhance\": { \"plusStep\": 0.25, \"legendToMythPlus\": 4, \"legendMaxPlus\": 3 } }");
            Assert.AreEqual(0.25, D.Gear.PlusStep, 1e-12, "강화 배율이 덮여야 한다(T405 · 주인 «그 전설 2강보다 신화 0강이 세야 함»)");
            Assert.AreEqual(4, D.Gear.LegendToMythPlus, "전설→신화 강화 단계도 같은 칸이다 — 반만 열면 조용히 안 먹는다");
            Assert.AreEqual(3, D.Gear.LegendMaxPlus, "전설 최대강도 같은 칸이다");
            Assert.AreEqual(atkBefore, D.Gear.Atk, "안 적은 기여는 그대로여야 한다");
        }

        /// <summary>그리고 <b>레포에 실제로 든 표</b>가 주인 제약 안이다 — 이 자가 «파일에 값이 있나» 를 지킨다(위 자는 «기계가 먹나» 다).</summary>
        [Test]
        public void TheRepoGearTableActuallyReducesThePlusStep()
        {
            var D = Fresh();
            Assert.Less(D.Gear.PlusStep, 0.125,
                "gearOverride.json 의 enhance.plusStep 이 0.125 미만이어야 한다 — 경계는 1 + 2·plusStep < 신화/전설 비율 1.25(T405)");
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
            // 칸 수를 표에서 낸다 — 앞 둘이 50/50 이고 나머지는 0(합 100). 등급이 늘어도 이 자가 그대로 산다.
            var rate = new double[D.Gear.RarName.Length]; rate[0] = 50; rate[1] = 50;
            D.ApplyGachaOverride("{ \"boxes\": { \"rare\": { \"rate\": [" + string.Join(", ", rate) + "], \"cost\": 90 } } }");
            var box = D.Gacha.Box("rare");
            Assert.AreEqual(90, box.Cost, 1e-12, "비용도 덮인다");
            Assert.AreEqual(50, box.Cum[1], 1e-9, "cum 이 새 rate 로 다시 서야 한다");
            Assert.AreEqual(100, box.Cum[0], 1e-9, "cum[0] 은 늘 100");
            // 굴림은 «작은 수가 높은 등급» 이다(정본 cum 의 뜻 그대로) — 50/50 이면 [0,50) 이 희귀다
            Assert.AreEqual(1, box.RarRoll(0), "0 은 희귀 — 굴림이 새 확률을 본다");
            Assert.AreEqual(1, box.RarRoll(49.999), "49.999 까지 희귀");
            Assert.AreEqual(0, box.RarRoll(50), "50 부터 일반 — 옛 cum(33.3) 이 남아 있으면 여기가 빨개진다");
            Assert.AreEqual(Fresh().Gacha.Box("legend").Rate, D.Gacha.Box("legend").Rate, "안 적은 상자는 덮기 전 그대로");
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
            // 이름만 한 칸 늘리고 기여는 그대로 둔다 — «반만 넓힌 표» (넓힌 이름은 지금 표에서 낸다)
            var names = new System.Collections.Generic.List<string>(D.Gear.RarName);
            names.Insert(D.Gear.RarLegend, "새등급");
            D.ApplyGearOverride("{ \"rarName\": [\"" + string.Join("\", \"", names) + "\"] }");
            var e = Assert.Throws<System.FormatException>(() => D.ValidateOverridden(), "등급만 한 칸 늘고 기여가 그대로면 던져야 한다");
            StringAssert.Contains("contribution", e.Message, "무엇이 안 맞는지 이름을 대야 한다");
        }

        /// <summary>상자 확률 칸 수가 등급 수와 다르면 <b>그 등급이 영영 안 나온다</b> — 예외도 로그도 없던 자리라 여기서 던진다.</summary>
        [Test]
        public void ABoxWhoseRateHasTooFewSlotsThrows()
        {
            var D = Fresh();
            // 장비 쪽만 한 칸 넓힌다(상자는 그대로) — 그러면 남는 어긋남은 «상자 칸 수» 하나뿐이다
            D.ApplyGearOverride(TestData.WidenByOneGrade(D));
            var e = Assert.Throws<System.FormatException>(() => D.ValidateOverridden(), "상자 rate 가 한 칸 모자란 채로는 지나가면 안 된다");
            StringAssert.Contains("rate", e.Message);
        }

        /// <summary>확률 합이 100 이 아니거나 등급 인덱스가 어긋나거나 챕터가 정본보다 많으면 던진다.</summary>
        [Test]
        public void TheOtherBrokenShapesThrowToo()
        {
            var a = Fresh();
            var bad = new double[a.Gear.RarName.Length]; bad[0] = 50; bad[1] = 40;   // 합 90 — 칸 수는 표에서 낸다
            a.ApplyGachaOverride("{ \"boxes\": { \"rare\": { \"rate\": [" + string.Join(", ", bad) + "] } } }");
            StringAssert.Contains("합", Assert.Throws<System.FormatException>(() => a.ValidateOverridden()).Message, "합 90 은 못 지나간다");

            var b = Fresh();
            b.ApplyGearOverride("{ \"rarLegend\": " + b.Gear.RarMyth + ", \"rarMyth\": " + b.Gear.RarLegend + " }");   // 둘을 맞바꾼다
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

        // ───────────────────── ③-ⓑ 적 세기 손잡이가 «전투에» 걸려 있는가 ─────────────────────

        /// <summary>
        /// ⚑ <b>이 절이 한 회차를 통째로 값을 치른 자리다(결정 985).</b>
        /// <para>지시서 8항은 «적 세기 = <c>eBaseHp</c> × 구간 성장률» 이라는 <c>tune.json</c> 손잡이로 챕터 밸런스를 잡으라 했는데,
        /// <b>이 레포의 전투는 그 식을 안 읽는다</b> — <c>Battle</c> 는 <c>enemies.json</c> 에 <b>챕터마다 구워진</b> 값을 그대로 쓰고,
        /// 그 식을 부르는 것은 «JSON 이 그 공식과 맞는가» 를 재는 <c>LayoutTests</c> 뿐이다.
        /// 곡선을 끝까지 돌려 놓고도 «왜 아무 일도 안 나지» 로 세 번을 헤맸다.</para>
        /// 그래서 그 사실을 <b>자로 못 박는다</b> — 다음 사람이 같은 자리에서 또 헤매지 않게, 그리고 누가 두 손잡이를 헷갈려 tune 쪽에 곡선을 적으면 여기가 말하게.
        /// </summary>
        [Test]
        public void TheTuneCurveDoesNotReachTheBattleButTheEnemiesOverrideDoes()
        {
            var a = Fresh();
            double waveHp0 = a.Enemies.Chapter(3).Waves[0].Hp, bossHp0 = a.Enemies.Chapter(3).Boss.Hp;

            // ⓐ tune 쪽 곡선을 끝까지 돌려도 «전투가 읽는 값» 은 한 자도 안 움직인다
            a.ApplyTuneOverride("{ \"eBaseHp\": 0.001, \"eBaseDmg\": 0.001, \"eHpSeg\": [[0, 1.0]], \"eDmgSeg\": [[0, 1.0]] }");
            Assert.AreEqual(waveHp0, a.Enemies.Chapter(3).Waves[0].Hp, 1e-9,
                "tune 의 곡선은 전투가 읽는 적 수치에 안 닿는다 — 닿게 되었다면 이 자를 지우지 말고 «어디서 닿는가» 를 여기 적어라");
            Assert.AreEqual(bossHp0, a.Enemies.Chapter(3).Boss.Hp, 1e-9, "보스도 마찬가지다");

            // ⓑ 적 표 덮어쓰기는 닿는다 — 3챕터의 배수는 1챕터부터 두 번 곱한 2² 이어야 한다(1챕터는 늘 1배)
            var b = Fresh();
            b.ApplyEnemiesOverride("{ \"hpSeg\": [[0, 2.0]], \"dmgSeg\": [[0, 3.0]] }");
            Assert.AreEqual(waveHp0 * 4, b.Enemies.Chapter(3).Waves[0].Hp, 1e-6, "3챕터 체력은 2² 배여야 한다");
            Assert.AreEqual(bossHp0 * 4, b.Enemies.Chapter(3).Boss.Hp, 1e-6, "보스도 같은 배수를 받는다");
            Assert.AreEqual(a.Enemies.Chapter(1).Waves[0].Hp, b.Enemies.Chapter(1).Waves[0].Hp, 1e-9, "1챕터는 늘 1배다(누적이 아직 없다)");

            // ⓒ 체력과 공격은 따로 움직인다
            Assert.AreEqual(Fresh().Enemies.Chapter(3).Waves[0].Dmg * 9, b.Enemies.Chapter(3).Waves[0].Dmg, 1e-6, "3챕터 공격은 3² 배여야 한다");
        }

        /// <summary>
        /// 적 표에 실제로 실린 수가 <b>정본 × 제 파일이 적은 곡선</b> 과 정확히 같은가 — 챕터마다 다시 센다.
        /// <para>
        /// ⚑ 여기도 «오늘은 배수가 1 이다» 를 재지 않는다. 곡선이 들어가는 날 설계대로 빨개지면
        /// 그날 «곡선이 틀렸나 곱하는 자리가 틀렸나» 를 못 가른다. 대신 <b>같은 셈(<c>ChapterLayout.SegGrow</c>)으로 다시 재서 맞대므로</b>
        /// 곡선이 0줄인 오늘은 «전부 1배» 를, 곡선이 들어간 뒤에는 «챕터마다 그 배수» 를 말한다.
        /// </para>
        /// <para>이 자가 빨개지는 뜻 — 파일이 적은 곡선과 실제로 곱해진 배수가 다르다(경계를 한 칸 밀었거나 1챕터부터 곱했거나).</para>
        /// </summary>
        [Test]
        public void TheEnemiesOverrideMultipliesCanonByExactlyTheCurveItDeclares()
        {
            var D = Fresh();
            var over = Over(GameData.EnemiesOverrideFile);
            double[][] hpSeg = over.Has("hpSeg") ? Seg(over["hpSeg"]) : null;
            double[][] dmgSeg = over.Has("dmgSeg") ? Seg(over["dmgSeg"]) : null;

            var canon = new JNode(MiniJson.Parse(File.ReadAllText(Path.Combine(TestData.Dir, "enemies.json"))));
            int c = 0;
            foreach (var ch in canon["chapters"].Items())
            {
                int cc = ch["c"].Int();
                var mine = D.Enemies.Chapter(cc);
                double kh = hpSeg != null ? ChapterLayout.SegGrow(hpSeg, cc) : 1;
                double kd = dmgSeg != null ? ChapterLayout.SegGrow(dmgSeg, cc) : 1;
                var w0 = ch["waves"][0];
                Near(w0["hp"].Num() * kh, mine.Waves[0].Hp, $"{cc}챕터 첫 물결 체력이 «정본 × {kh}» 이 아니다");
                Near(w0["dmg"].Num() * kd, mine.Waves[0].Dmg, $"{cc}챕터 첫 물결 공격이 «정본 × {kd}» 이 아니다");
                Near(ch["boss"]["hp"].Num() * kh, mine.Boss.Hp, $"{cc}챕터 보스 체력이 «정본 × {kh}» 이 아니다");
                Near(ch["boss"]["dmg"].Num() * kd, mine.Boss.Dmg, $"{cc}챕터 보스 공격이 «정본 × {kd}» 이 아니다");
                if (++c >= 30) break;                       // 앞 서른 챕터면 곡선의 앞 경계 몇 개를 넘기에 넉넉하다(420 을 다 도는 것은 느리다)
            }
            Assert.AreEqual(1.0, hpSeg != null ? ChapterLayout.SegGrow(hpSeg, 1) : 1.0, 1e-12, "1챕터는 늘 1배여야 한다 — 아니면 곡선이 한 칸 일찍 곱해진 것이다");
        }

        static double[][] Seg(JNode a)
        {
            var r = new double[a.Count][];
            for (int i = 0; i < a.Count; i++) r[i] = a[i].NumArray();
            return r;
        }

        /// <summary>큰 수일수록 오차가 커지므로 상대 오차로 잰다(정본 체력은 수만까지 간다).</summary>
        static void Near(double want, double got, string msg) => Assert.AreEqual(want, got, 1e-9 * System.Math.Max(1, System.Math.Abs(want)), msg);

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
              ""optionLadder"": { ""optCount"": [0, 1, 2, 3, 4], ""mythPlusAt"": [3, 6] },
              ""look"": { ""rarSprite"": [0, 1, 1, 2, 3] }
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

            // 문 ⓑ — «영웅은 어느 그림인가» 에 새 그림 0 으로 답한다(희귀 그림을 같이 쓴다 · 회차 1 이 남긴 답).
            //   그림 칸은 넷뿐인데 등급이 다섯이 되므로, 이 표가 없으면 GearLook 이 조용히 마지막 칸으로 눌러
            //   **신화가 전설 그림을 입는다**(컴파일도 되고 빨간 줄도 안 난다).
            Assert.AreEqual(1, D.Gear.LookRar(2), "영웅은 희귀 그림을 같이 쓴다 — 새 그림 0");
            Assert.AreEqual(2, D.Gear.LookRar(3), "전설은 옛 전설 그림 그대로");
            Assert.AreEqual(3, D.Gear.LookRar(4), "신화는 옛 신화 그림 그대로 — 여기가 밀리면 주인 눈에 바로 보인다");

            // ⚑ 여기가 이 회차에서 실제로 고친 «자리» 다 — 자세한 까닭은 GearData.RarRare 주석.
            Assert.AreEqual("희귀", D.Gear.RarName[D.Gear.RarRare],
                "«희귀 확정» 천장(T261)이 가리키는 등급이 영웅으로 밀리면 안 된다 — 컴파일도 되고 아무 자도 안 우는 자리였다");
        }
    }
}
