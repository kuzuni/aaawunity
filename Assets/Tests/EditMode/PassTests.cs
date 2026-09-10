using System;
using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T322 — 시즌 패스 보상 표(<see cref="PassData"/> · <c>Assets/KkomaKnight/pass.json</c> · 주인 «패스 부분 1~100까지 있어야»).
    /// <para>
    /// ⚠ 이 자의 요점은 <b>«표가 모르는 줄» 이 «값이 0 인 줄» 과 갈린다</b> 는 것이다.
    /// 주인이 아직 보상 값을 안 줬으므로(T266 ⓑ) 95줄은 <b>비어 있어야</b> 하고, 화면은 그것을 «?» 로 그린다.
    /// 비어 있는 것을 0 이나 빈 글자로 «채워» 두면 화면이 그것을 값으로 그리고, 주인은 그것을 «정한 값» 으로 읽는다 —
    /// 그때부터 무엇이 주인 것이고 무엇이 워커가 지어낸 것인지 아무도 못 가른다.
    /// </para>
    /// 그래서 여기서 못 박는 것은 ⓐ 100줄이라는 것 ⓑ <b>아는 줄만</b> 표에 있다는 것 ⓒ 깨진 표는 <b>읽는 순간 운다</b> 는 것 셋이다.
    /// </summary>
    public class PassTests
    {
        static PassData Load() =>
            PassData.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "pass.json"))));

        [Test]
        public void 레벨은_백까지다()
        {
            Assert.AreEqual(100, Load().MaxLevel, "주인이 말로 준 수 — «1~100까지»");
        }

        [Test]
        public void 아는_줄만_표에_있고_나머지는_모르는_채로_남는다()
        {
            var d = Load();
            // T266 ⓑ — 주인이 2026-09-10 값을 줬다(«전부 100 다이아 · 5번째마다 500 · 9,900원은 같고 비싼 것은 2배») → 100줄이 다 차 있어야 한다.
            //   (이력) 그 전에는 «100줄이 다 차 있으면 누군가 수를 지어낸 것» 을 걸었다 — 주인 미제공 시절의 자였다.
            Assert.AreEqual(d.MaxLevel, d.Levels.Count, "주인 규칙으로 1~100 이 전부 찬다(T266 ⓑ)");
            for (int lv = 1; lv <= d.MaxLevel; lv++)
            {
                int baseQty = lv % 5 == 0 ? 500 : 100;
                Assert.AreEqual(baseQty.ToString(), d.At(lv, PassData.ColFree).Qty, "무료 " + lv + " = 100 · 5의 배수는 500");
                Assert.AreEqual(baseQty.ToString(), d.At(lv, PassData.ColPaid1).Qty, "유료1(₩9,900) " + lv + " = 무료와 같다(주인 «9900원짜리는 똑같고»)");
                Assert.AreEqual((baseQty * 2).ToString(), d.At(lv, PassData.ColPaid2).Qty, "유료2 " + lv + " = 2배(주인 «더 비싼 거는 2배»)");
                for (int c = 0; c < PassData.Cols; c++) Assert.AreEqual("ui.gemRed", d.At(lv, c).Icon, "세 열 전부 다이아 " + lv);
            }

            foreach (var kv in d.Levels)
            {
                Assert.GreaterOrEqual(kv.Key, 1); Assert.LessOrEqual(kv.Key, d.MaxLevel);
                Assert.AreEqual(PassData.Cols, kv.Value.Length, "줄 " + kv.Key + " 은 세 열이다");
                Assert.IsTrue(d.Known(kv.Key), "표에 든 줄은 적어도 한 열을 안다: " + kv.Key);
                for (int c = 0; c < PassData.Cols; c++)
                {
                    var r = d.At(kv.Key, c);
                    Assert.IsFalse(string.IsNullOrEmpty(r.Icon), "줄 " + kv.Key + " 열 " + c + " 의 그림");
                    Assert.IsFalse(string.IsNullOrEmpty(r.Qty), "줄 " + kv.Key + " 열 " + c + " 의 수량 글자");
                }
            }
        }

        [Test]
        public void 표에_없는_줄은_모른다고_답한다()
        {
            var d = Load();
            int unknown = -1;
            for (int lv = 1; lv <= d.MaxLevel; lv++) if (!d.Levels.ContainsKey(lv)) { unknown = lv; break; }
            Assert.Greater(unknown, 0, "표에 없는 줄이 있어야 한다(주인 값 미제공)");
            Assert.IsFalse(d.Known(unknown), "표에 없는 줄은 «모른다»");
            for (int c = 0; c < PassData.Cols; c++)
                Assert.IsFalse(d.At(unknown, c).Known, "모르는 줄의 칸은 «모르는 칸» 이다 — 화면이 «?» 로 그린다");
            // 열 번호가 표 밖이어도 던지지 않는다(보여 주기 화면이라 «못 여는» 쪽이 더 나쁘다)
            Assert.IsFalse(d.At(unknown, -1).Known); Assert.IsFalse(d.At(unknown, PassData.Cols).Known);
        }

        [Test]
        public void 깨진_표는_읽는_순간_운다()
        {
            Assert.Throws<FormatException>(() => PassData.Parse("{\"maxLevel\":0,\"levels\":{}}"), "maxLevel 0");
            Assert.Throws<FormatException>(() => PassData.Parse("{\"maxLevel\":100,\"levels\":{\"0\":{}}}"), "레벨 0 은 밖이다");
            Assert.Throws<FormatException>(() => PassData.Parse("{\"maxLevel\":100,\"levels\":{\"101\":{}}}"), "레벨 101 은 밖이다");
            Assert.Throws<FormatException>(() => PassData.Parse("{\"maxLevel\":100,\"levels\":{\"둘\":{}}}"), "수가 아닌 열쇠");
        }
    }
}
