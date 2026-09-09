using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T325 4항 ⓔ — <b>등급이 가운데에 하나 늘 때 옛 세이브의 장비를 한 칸 올려 읽는다.</b>
    /// <para>
    /// 주인이 «영웅 등급 다시 넣고» 라고 했다. 영웅은 희귀와 전설 <b>사이</b>에 들어가므로 옛 세이브의 <c>rar 2</c>(옛 전설)·<c>3</c>(옛 신화)이
    /// 새 표에서는 <b>영웅·전설</b> 을 가리킨다 — 손대지 않으면 <b>주인 폰의 장비가 한 등급씩 떨어진다.</b>
    /// </para>
    /// <para>⚑ 그리고 그것보다 나쁜 갈래가 하나 더 있다 — <c>Normalize</c> 의 다음 줄이 «전설인데 +N 이상이면 신화로 승격하고 <b>강화를 0 으로</b>» 다.
    /// 옛 신화 장비가 «전설» 로 읽히는 순간 그 줄이 <b>강화를 통째로 지운다.</b> 그래서 이관은 그 줄보다 <b>먼저</b> 돌아야 하고, 여기서 그 차례를 잰다.</para>
    /// <para><b>오늘은 등급이 넷이라 이 자가 재는 것은 «아무 일도 안 난다» 쪽이다</b> — 값이 들어가는 회차에 나머지 절반이 켜진다.</para>
    /// </summary>
    public class SaveRarMigrationTests
    {
        static GameData Canon() => GameData.LoadFromDirectory(TestData.Dir);

        static SaveData WithGear(GameData D, params int[] rars)
        {
            var s = SaveData.NewSave(D);
            s.Inv.Clear();
            foreach (var r in rars) s.Inv.Add(s.NewGear(D.Gear.Parts[0], D.Gear.Types[D.Gear.Parts[0]][0], r, 0));
            return s;
        }

        /// <summary>오늘(표가 그대로)은 등급이 한 칸도 안 움직인다 — 이관이 «켜져 있지만 안 돈다».</summary>
        [Test]
        public void TodayNothingMoves()
        {
            var D = Canon();
            var s = WithGear(D, 0, 1, D.Gear.RarLegend, D.Gear.RarMyth);
            s.RarN = D.Gear.RarName.Length;               // «지금 표로 적힌» 세이브
            var before = new[] { s.Inv[0].Rar, s.Inv[1].Rar, s.Inv[2].Rar, s.Inv[3].Rar };
            s.Normalize(D);
            Assert.AreEqual(before, new[] { s.Inv[0].Rar, s.Inv[1].Rar, s.Inv[2].Rar, s.Inv[3].Rar },
                "등급 수가 그대로면 이관은 아무 일도 안 해야 한다");
            Assert.AreEqual(D.Gear.RarName.Length, s.RarN, "이관이 돌든 안 돌든 «이 표로 적혔다» 는 남는다");
        }

        /// <summary>
        /// ⚑ <b>이 자가 이 절에서 제일 중요하다</b> — 진짜 옛 세이브에는 <c>rarN</c> 칸이 <b>아예 없다</b>.
        /// 처음 쓴 코드는 «칸이 없으면 지금 표로 적힌 것으로 보자» 였는데, 그러면 <b>정작 옮겨야 할 세이브만 안 옮겨진다.</b>
        /// 그래서 «없음» 은 <see cref="SaveData.RarNLegacy"/>(칸이 생기기 전의 등급 수)로 읽는다.
        /// <para>⚠ <b>표가 넷인 오늘은 두 규칙이 같은 답을 낸다</b>(<c>now</c> 도 4 · <c>RarNLegacy</c> 도 4) —
        /// 그래서 이 자는 <b>표를 다섯으로 넓힌 뒤에</b> 잰다. 안 그러면 «내가 쓴 값을 내가 읽는» 거울이라 어느 쪽으로 고쳐도 초록이다(결정 906 의 그 꼴).</para>
        /// </summary>
        [Test]
        public void ASaveWithNoRarNAtAllIsTreatedAsTheOldTable()
        {
            var D = Canon();
            int oldLegend = D.Gear.RarLegend, oldMyth = D.Gear.RarMyth;
            var s = WithGear(D, oldLegend, oldMyth);      // 옛 표의 전설 · 신화
            var json = s.ToJson().Replace("\"rarN\":", "\"_rarN\":");
            Assert.IsFalse(json.Contains("\"rarN\":"), "이 자가 재려는 «칸이 없는 세이브» 를 실제로 만들었는지부터 본다");

            // 읽는 쪽의 표를 한 칸 넓혀 둔다 — FromJson 이 그 표로 Normalize 를 돌린다(넓히는 표는 지금 표에서 낸다)
            D.ApplyGearOverride(TestData.WidenByOneGrade(D));

            var back = SaveData.FromJson(json, D);
            Assert.AreEqual(new[] { oldLegend + 1, oldMyth + 1 }, new[] { back.Inv[0].Rar, back.Inv[1].Rar },
                "칸이 없는 옛 세이브가 안 옮겨졌다 — «없음» 을 «지금 표» 로 읽으면 주인 폰의 장비가 한 등급 떨어진다");
            Assert.AreEqual(D.Gear.RarName.Length, back.RarN, "옮긴 뒤에는 새 표로 도장을 찍는다");
        }

        /// <summary>적고 다시 읽으면 도장이 따라온다 — 안 따라오면 옮긴 세이브가 다음 로드에 또 옮겨진다.</summary>
        [Test]
        public void TheStampSurvivesTheRoundTrip()
        {
            var D = Canon();
            var s = WithGear(D, 1);
            s.Normalize(D);
            var back = SaveData.FromJson(s.ToJson(), D);
            Assert.AreEqual(s.RarN, back.RarN, "rarN 이 저장에 안 실렸다");
            Assert.AreEqual(D.Gear.RarName.Length, back.RarN);
        }

        /// <summary>등급이 가운데에 하나 늘면 «전설 바로 아래» 부터 한 칸씩 밀린다 — 그 아래(일반·희귀)는 그대로다.</summary>
        [Test]
        public void OneGradeInsertedInTheMiddleShiftsEverythingAboveIt()
        {
            var D = Canon();
            int oldN = D.Gear.RarName.Length;
            int at = D.Gear.RarLegend;                    // 새 등급이 끼는 자리 = 이 칸부터 한 칸씩 밀린다
            var s = WithGear(D, 0, 1, at, D.Gear.RarMyth);
            s.RarN = oldN;                                // 옛 표로 적힌 세이브

            // ⚑ «한 칸 늘어난 표» 를 **지금 표에서 낸다**(TestData.WidenByOneGrade) — 다섯을 손으로 적으면
            //   주인 값이 들어가 표가 이미 다섯인 날 이 자가 통째로 뜻을 잃는다.
            D.ApplyGearOverride(TestData.WidenByOneGrade(D));
            s.Normalize(D);

            Assert.AreEqual(0, s.Inv[0].Rar, "맨 아래 등급은 그대로(끼는 자리보다 아래다)");
            Assert.AreEqual(1, s.Inv[1].Rar, "그 위 등급도 그대로");
            Assert.AreEqual(at + 1, s.Inv[2].Rar, "끼는 자리에 있던 등급은 한 칸 올라간다 — 안 옮기면 새 등급으로 떨어진다");
            Assert.AreEqual(D.Gear.RarMyth, s.Inv[3].Rar, "옛 신화는 새 신화가 된다(새 표의 rarMyth)");
            Assert.AreEqual(oldN + 1, s.RarN, "옮긴 뒤에는 새 표로 도장을 찍어 두 번 안 옮긴다");
        }

        /// <summary>두 번 돌려도 한 번만 옮긴다 — 도장(<c>RarN</c>)이 그것을 막는다.</summary>
        [Test]
        public void MigratingTwiceMovesNothingTheSecondTime()
        {
            var D = Canon();
            var s = WithGear(D, D.Gear.RarLegend, D.Gear.RarMyth);
            s.RarN = D.Gear.RarName.Length;
            D.ApplyGearOverride(TestData.WidenByOneGrade(D));
            s.Normalize(D);
            var once = new[] { s.Inv[0].Rar, s.Inv[1].Rar };
            s.Normalize(D);
            Assert.AreEqual(once, new[] { s.Inv[0].Rar, s.Inv[1].Rar }, "두 번째 Normalize 는 등급을 또 올리면 안 된다");
        }

        /// <summary>
        /// ⚑ <b>차례가 이 절의 알맹이다</b> — 이관이 «전설 → 신화 승격» 보다 늦게 돌면 옛 신화 장비의 <b>강화가 0 이 된다.</b>
        /// 그 승격 줄은 «전설인데 +N 이상» 을 신화로 올리면서 <c>Plus</c> 를 0 으로 지우는데, 옛 신화(3)가 새 표에서 «전설» 이기 때문이다.
        /// </summary>
        [Test]
        public void TheOldMythicKeepsItsPlusBecauseTheMigrationRunsFirst()
        {
            var D = Canon();
            int oldMyth = D.Gear.RarMyth;
            var s = SaveData.NewSave(D); s.Inv.Clear();
            var g = s.NewGear(D.Gear.Parts[0], D.Gear.Types[D.Gear.Parts[0]][0], oldMyth, D.Gear.LegendToMythPlus + 4);
            s.Inv.Add(g);
            s.RarN = D.Gear.RarName.Length;
            D.ApplyGearOverride(TestData.WidenByOneGrade(D));
            s.Normalize(D);
            Assert.AreEqual(D.Gear.RarMyth, s.Inv[0].Rar, "옛 신화는 새 신화여야 한다(새 표의 rarMyth)");
            Assert.AreEqual(D.Gear.LegendToMythPlus + 4, s.Inv[0].Plus,
                "강화가 0 이 됐다 — 이관이 «전설 → 신화 승격» 줄보다 늦게 돌았다는 뜻이다(차례를 되돌리지 마라)");
        }
    }
}
