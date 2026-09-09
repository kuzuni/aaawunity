using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T318 — 퀘스트 «이동» 의 목적지 표(<c>quest.json go</c>)와 손가락 힌트 값(<c>hint</c>)의 자.
    /// <para>
    /// 재는 것은 셋이다: ⓐ <b>표가 완전한가</b> — 줄마다 <c>go</c> 가 있고(로그인류만 명시적 <c>null</c>) 화면 낱말이 아는 것이며 버튼 이름이 비지 않았다
    /// ⓑ <b>빠뜨리면 읽는 순간 우는가</b> — 키 없음 · 모르는 화면 · 빈 버튼 · <c>hint</c> 없음 · 0 값. «이동이 조용히 아무 데도 안 간다» 가 이 표의 진짜 위험이다
    /// ⓒ <b>카운터 → 목적지가 지시서 표(ROUTINE T318 1항 · 주인 원문에서 푼 것)와 같은가</b> — 값을 베끼는 자가 아니라 «적 죽이기가 상점으로 데려가는» 오타를 잡는 자다.
    /// </para>
    /// </summary>
    public class QuestGoTests
    {
        static string Path_ => System.IO.Path.Combine("Assets", "KkomaKnight", "quest.json");
        static string Text() => File.ReadAllText(TestData.RepoFile(Path_));
        static QuestData Load() => QuestData.Parse(Text());

        static IEnumerable<QuestData.Quest> All(QuestData d)
        {
            foreach (var q in d.Daily.Quests) yield return q;
            foreach (var q in d.Weekly.Quests) yield return q;
        }

        // 지시서 1항의 표 — 카운터 → (화면 · 손가락이 가리키는 것). 로그인류는 «이동 없음».
        static readonly Dictionary<string, (string screen, string point)> Spec = new Dictionary<string, (string, string)>
        {
            { "kill",                ("lobby",      "Start") },
            { "chapterTry",          ("lobby",      "Start") },
            { "dungeonTry",          ("dungeon",    "EnterBtn") },
            { "dungeonClear",        ("dungeon",    "EnterBtn") },
            { "chestOpen",           ("chest",      "One") },
            { "gearFuse",            ("forge",      "FuseBtn") },
            { "petUpgrade",          ("pet",        "UpgradeAllBtn") },
            { "expeditionClaim",     ("expedition", "ClaimBtn") },
            { "expeditionFastClaim", ("expedition", "QuickBtn") },
        };
        static readonly string[] NoGo = { "login", "loginDays" };

        [Test]
        public void 모든_줄에_go_가_있고_로그인류만_null_이다()
        {
            var d = Load();
            foreach (var q in All(d))
            {
                if (Array.IndexOf(NoGo, q.Counter) >= 0) { Assert.IsNull(q.Go, "«" + q.Label + "» 는 갈 데가 없다(명시적 null)"); continue; }
                Assert.IsNotNull(q.Go, "«" + q.Label + "» 의 go — 이동이 데려갈 자리");
                Assert.IsTrue(QuestData.KnownGoScreen(q.Go.Screen), "«" + q.Label + "» 의 go.screen «" + q.Go.Screen + "» 은 GoScreens 안이어야 한다");
                Assert.Greater(q.Go.Points.Length, 0, "«" + q.Label + "» 의 go.point");
                foreach (var p in q.Go.Points) Assert.IsFalse(string.IsNullOrEmpty(p), "«" + q.Label + "» 의 go.point 에 빈 글자");
            }
        }

        [Test]
        public void 카운터별_목적지가_지시서_표와_같다()
        {
            var d = Load();
            foreach (var q in All(d))
            {
                if (q.Go == null) continue;
                Assert.IsTrue(Spec.ContainsKey(q.Counter), "지시서 표에 없는 카운터 «" + q.Counter + "» 가 go 를 갖는다 — 표를 같이 늘려라");
                var s = Spec[q.Counter];
                Assert.AreEqual(s.screen, q.Go.Screen, "«" + q.Label + "» 의 화면");
                Assert.IsTrue(Array.IndexOf(q.Go.Points, s.point) >= 0, "«" + q.Label + "» 의 버튼 후보에 «" + s.point + "» 가 있어야 한다 — 지금 " + string.Join("|", q.Go.Points));
            }
        }

        [Test]
        public void 화면_낱말_여섯은_지시서_표의_목적지_전부다()
        {
            // 표(Spec)가 쓰는 화면 낱말은 전부 GoScreens 에 있고, GoScreens 에 «아무도 안 가는 낱말» 도 없다(죽은 갈래를 화면 쪽이 안 들고 다니게).
            var used = new HashSet<string>();
            foreach (var kv in Spec) { used.Add(kv.Value.screen); Assert.IsTrue(QuestData.KnownGoScreen(kv.Value.screen), kv.Key + " 의 화면 " + kv.Value.screen); }
            foreach (var s in QuestData.GoScreens) Assert.IsTrue(used.Contains(s), "GoScreens 의 «" + s + "» 로 가는 줄이 표에 하나도 없다");
        }

        [Test]
        public void 손가락_힌트_값_다섯이_전부_0보다_크다()
        {
            var h = Load().Hint;
            Assert.Greater(h.IconPx, 0); Assert.Greater(h.BobPx, 0); Assert.Greater(h.PeriodSec, 0); Assert.Greater(h.LifeSec, 0); Assert.Greater(h.GlowSec, 0);
        }

        // ── ⓑ 빠뜨리면 읽는 순간 운다 ──
        static string Without(string text, string pattern, string replacement)
        {
            var re = new Regex(pattern);
            Assert.IsTrue(re.IsMatch(text), "자가 바꾸려는 자리가 표에 있어야 한다: " + pattern);
            return re.Replace(text, replacement, 1);
        }

        [Test]
        public void go_키를_빠뜨린_줄이_있으면_읽는_순간_운다()
        {
            var t = Without(Text(), @",\s*""go"":\s*\{[^}]*\}", "");
            Assert.Throws<FormatException>(() => QuestData.Parse(t));
        }

        [Test]
        public void 모르는_화면_낱말이면_운다()
        {
            var t = Without(Text(), @"""screen"":\s*""lobby""", "\"screen\": \"moon\"");
            Assert.Throws<FormatException>(() => QuestData.Parse(t));
        }

        [Test]
        public void 버튼_이름이_비면_운다()
        {
            var t = Without(Text(), @"""point"":\s*""Start""", "\"point\": []");
            Assert.Throws<FormatException>(() => QuestData.Parse(t));
        }

        [Test]
        public void hint_가_없거나_0이면_운다()
        {
            var none = Without(Text(), @"""hint"":\s*\{[^}]*\},", "");
            Assert.Throws<FormatException>(() => QuestData.Parse(none));
            var zero = Without(Text(), @"""lifeSec"":\s*[0-9.]+", "\"lifeSec\": 0");
            Assert.Throws<FormatException>(() => QuestData.Parse(zero));
        }

        [Test]
        public void 후보_목록도_글자_하나도_다_읽는다()
        {
            var d = Load();
            QuestData.Quest fuse = null, start = null;
            foreach (var q in All(d)) { if (q.Counter == "gearFuse" && fuse == null) fuse = q; if (q.Counter == "kill" && start == null) start = q; }
            Assert.IsNotNull(fuse); Assert.IsNotNull(start);
            Assert.AreEqual(2, fuse.Go.Points.Length, "대장간 «합성» 은 주황·회색 두 벌이라 후보 둘(켜진 첫 것)");
            Assert.AreEqual("FuseBtnOn", fuse.Go.Points[0], "주황(할 수 있다)이 먼저");
            Assert.AreEqual(1, start.Go.Points.Length, "START 는 글자 하나");
        }
    }
}
