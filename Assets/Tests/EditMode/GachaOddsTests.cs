using System;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T267 1단계 — 상자 «확률 정보» 계산(주인 2026-09-09 «상점에 상자 부분에 인포 버튼 클릭 시 이런 게 떠야 함»).
    /// 지시서 3항이 «손으로 박은 숫자를 쓰지 말 것 — <c>gacha.json</c> 의 등급 rate 와 장비 목록에서 계산» 이라
    /// 이 자가 재는 것은 <b>값이 아니라 관계</b>다(결정 555): 표가 바뀌어도 안 깨지고, 셈이 뽑기와 어긋나면 깨진다.
    /// </summary>
    public class GachaOddsTests
    {
        static GameData Load() => TestData.Load();

        [Test]
        public void SectionPercentsComeFromTheBoxRate_AndSumTo100()
        {
            var d = Load();
            foreach (var box in d.Gacha.Boxes)
            {
                var rows = GachaOdds.Of(d, box.Key);
                Assert.Greater(rows.Count, 0, box.Key + " 는 구간이 하나라도 있어야 한다");
                // 구간 확률 = 표의 rate 그대로(베끼지 않고 표를 다시 읽어 견준다)
                foreach (var r in rows)
                    Assert.AreEqual(box.Rate[r.Rar], r.Percent, 1e-9, box.Key + " 등급 " + r.Rar + " 확률 = gacha.json rate");
                Assert.AreEqual(100.0, GachaOdds.TotalPercent(d, box.Key), 1e-6, box.Key + " 구간 합 = 100%");
            }
        }

        [Test]
        public void ZeroRateGradesGetNoSection()
        {
            var d = Load();
            foreach (var box in d.Gacha.Boxes)
            {
                var rows = GachaOdds.Of(d, box.Key);
                for (int r = 0; r < box.Rate.Length; r++)
                {
                    bool has = rows.Exists(x => x.Rar == r);
                    // 없는 등급에 «0.00%» 를 그리면 «나올 수 있는데 아주 드물다» 로 읽힌다 — 아예 안 낸다.
                    Assert.AreEqual(box.Rate[r] > 0, has, box.Key + " 등급 " + r + " 은 rate > 0 일 때만 구간이 선다");
                }
            }
        }

        [Test]
        public void SectionsGoFromTheHighestGradeDown()
        {
            var d = Load();
            // 레퍼런스 36 실측 — 보라(전설) 구간이 파랑(희귀) 구간 «위» 에 있다.
            foreach (var box in d.Gacha.Boxes)
            {
                var rows = GachaOdds.Of(d, box.Key);
                for (int i = 1; i < rows.Count; i++)
                    Assert.Less(rows[i].Rar, rows[i - 1].Rar, box.Key + " 구간은 높은 등급부터 내려온다");
            }
        }

        [Test]
        public void EachItemPercentIsTheGradePercentSplitOverTheItemsThePullCanActuallyGive()
        {
            var d = Load();
            int n = GachaOdds.ItemCount(d);
            // ⚠ 이 자가 이 절의 핵심이다 — 화면이 나누는 수와 **뽑기가 실제로 고르는 목록**이 같아야 한다.
            //   GearSystem 의 Mk(rr) 이 등급과 무관하게 AllTypes 에서 균등하게 고른다(부위 × 세트).
            //   여기가 어긋나면 화면은 멀쩡히 뜨는데 적힌 확률이 거짓말이 된다 — 빨간 줄도 안 난다.
            Assert.AreEqual(d.Gear.AllTypes.Count, n, "아이템 수 = 뽑기가 고르는 목록(AllTypes)");
            Assert.AreEqual(d.Gear.Parts.Length * d.Gear.Sets.Length, n, "AllTypes = 부위 × 세트");
            Assert.Greater(n, 0, "장비 목록이 비면 나눌 수 없다");

            foreach (var box in d.Gacha.Boxes)
                foreach (var r in GachaOdds.Of(d, box.Key))
                {
                    Assert.AreEqual(n, r.Count, box.Key + " 등급 " + r.Rar + " 의 아이템 수");
                    Assert.AreEqual(r.Percent / n, r.Each, 1e-12, box.Key + " 등급 " + r.Rar + " 개별 확률 = 등급 확률 ÷ 아이템 수");
                    // 한 구간 안의 칸을 다 더하면 그 등급 확률로 돌아온다(레퍼런스가 그렇게 나뉘어 있다)
                    Assert.AreEqual(r.Percent, r.Each * r.Count, 1e-9, box.Key + " 등급 " + r.Rar + " 칸 합 = 등급 확률");
                }
        }

        [Test]
        public void GradeNamesComeFromTheTable_NotFromCode()
        {
            var d = Load();
            foreach (var r in GachaOdds.Of(d, "myth"))
                Assert.AreEqual(d.Gear.RarName[r.Rar], r.Name, "등급 이름은 gear.json rarName 에서 온다");
        }

        [Test]
        public void UnknownBoxIsEmpty_NotAThrow()
        {
            var d = Load();
            // 화면이 «없는 상자» 를 물어도 팝업이 죽으면 안 된다(GachaData.Box 는 던진다 · 그래서 이 길을 따로 뒀다).
            Assert.IsNull(GachaOdds.BoxOf(d, "없는상자"));
            Assert.AreEqual(0, GachaOdds.Of(d, "없는상자").Count);
            Assert.AreEqual(0.0, GachaOdds.TotalPercent(d, "없는상자"), 1e-12);
            Assert.AreEqual(0, GachaOdds.Of(null, "rare").Count, "데이터가 없어도 안 던진다");
            Assert.AreEqual(0, GachaOdds.Of(d, null).Count);
        }

        [Test]
        public void TheReferenceShapeHolds_GradePercentDividedByItemsIsWhatTheCellShows()
        {
            var d = Load();
            // 레퍼런스(36)의 셈: Rare 39.68% ÷ 21종 ≈ 1.89%. 우리 표는 종 수가 달라 값은 다르지만 **꼴은 같다**.
            var rows = GachaOdds.Of(d, "rare");
            var top = rows[0];
            Assert.AreEqual(top.Percent / GachaOdds.ItemCount(d), top.Each, 1e-12);
            // 희귀 상자에는 전설·신화가 없다(rate 0) — 구간 둘뿐이다.
            Assert.AreEqual(2, rows.Count, "희귀 상자는 일반·희귀 두 구간");
            Assert.AreEqual(d.Gear.RarRare, top.Rar, "맨 위 구간 = 희귀(전설 바로 아래)");
        }
    }
}
