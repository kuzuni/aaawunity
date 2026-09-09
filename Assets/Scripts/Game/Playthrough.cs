using System;

namespace KkomaKnight.Game
{
    /// <summary>
    /// T300 — <b>플레이 봇의 각본 한 벌</b>(주인 2026-09-09 «플레이해서 에러 테스트도 하라»).
    /// <para>
    /// 이 파일은 «무엇을 어떤 순서로 놀아 보는가» 의 <b>유일한 목록</b>이다. 두 쪽이 이것을 같이 읽는다 —
    /// PlayMode <c>PlaythroughTests</c>(CI 에서 도는 자)와 배포 빌드의 <c>App.DebugGo("play")</c>(T300 2항).
    /// 목록을 두 벌 두면 한 벌이 낡고, 그러면 «봇이 도는 줄 알았는데 그 단계는 아무도 안 놀아 본» 꼴이 된다.
    /// </para>
    /// <para>
    /// ⚠ <b>봇은 규칙을 확인하지 않는다</b>(3항 ⓐ) — «값이 맞는가» 는 각 절의 자 몫이고, 봇이 재는 것은
    /// «죽지 않고 지나가는가 · 빨간 줄 0 · 도달했는가» 셋뿐이다. 봇에 값 단언을 얹기 시작하면
    /// 그 절의 자와 두 곳에서 같은 것을 재게 되고, 표가 바뀌는 날 <b>봇이 먼저 운다</b>.
    /// </para>
    /// </summary>
    public static class Playthrough
    {
        /// <summary>한 단계 — 번호(P1…)와 사람이 읽는 이름.</summary>
        public readonly struct Stage
        {
            public readonly string Id;      // "P1"
            public readonly string Name;    // "로비"
            public Stage(string id, string name) { Id = id; Name = name; }
            public override string ToString() => Id + " " + Name;
        }

        /// <summary>
        /// 각본 11단계(T300 1항 표 그대로 · <b>순서가 곧 노는 순서</b>).
        /// <para>새 화면·흐름을 만든 워커는 <b>같은 커밋에</b> 여기와 자를 같이 늘린다(절 «다른 워커» 조항).</para>
        /// </summary>
        public static readonly Stage[] Stages =
        {
            new Stage("P1", "로비"),
            new Stage("P2", "전투"),
            new Stage("P3", "장비"),
            new Stage("P4", "상점"),
            new Stage("P5", "던전"),
            new Stage("P6", "아레나"),
            new Stage("P7", "퀘스트·업적"),
            new Stage("P8", "출석·기프트·우편"),
            new Stage("P9", "탐험"),
            new Stage("P10", "펫"),
            new Stage("P11", "설정"),
        };

        /// <summary>번호로 찾는다 — 없으면 <c>null</c> 이 아니라 «못 찾았다» 를 부르는 쪽이 알게 false 를 준다.</summary>
        public static bool TryFind(string id, out Stage stage)
        {
            foreach (var s in Stages)
                if (string.Equals(s.Id, id, StringComparison.Ordinal)) { stage = s; return true; }
            stage = default; return false;
        }

        /// <summary>
        /// 배포 빌드 스모크가 세는 한 줄(T300 2항) — <c>[KkomaKnight] play P1 ok</c> / <c>… fail 까닭</c>.
        /// <para>글자를 여기서 만드는 까닭: <c>webgl_smoke.js</c> 가 이 꼴을 문자열로 찾는다. 꼴이 두 곳에 있으면 한쪽만 바뀐다.</para>
        /// </summary>
        public const string LogPrefix = "[KkomaKnight] play ";
        public static string Line(string id, bool ok, string why = null)
            => LogPrefix + id + (ok ? " ok" : " fail " + (string.IsNullOrEmpty(why) ? "(까닭 없음)" : why));
        // ─────────────────────────────────────────────────────────────────────────────
        // 배포 빌드에서 실제로 «노는» 쪽(T300 2항) — `App.DebugGo("play")` 가 이것을 돌린다.
        // ─────────────────────────────────────────────────────────────────────────────
        /// <summary>한 단계가 «게임 안에서» 하는 일. 없는 단계는 아직 아무도 안 썼다는 뜻이다(등록 안 함).</summary>
        public delegate System.Collections.IEnumerator Step(App app);

        static readonly System.Collections.Generic.Dictionary<string, Step> Steps =
            new System.Collections.Generic.Dictionary<string, Step> { { "P1", P1Lobby } };

        /// <summary>이 단계가 게임 안에서 놀 수 있는가(= 누가 각본을 붙였는가).</summary>
        public static bool HasStep(string id) => Steps.ContainsKey(id);

        /// <summary>
        /// P1 로비 — 탭 다섯을 왕복하고 챕터 ◀▶ 를 눌러 본다. <b>단언은 하나도 없다</b>(3항 ⓐ):
        /// 봇은 «죽지 않고 지나가는가» 만 본다. 못 찾은 자리는 <see cref="MissingException"/> 로 알린다.
        /// </summary>
        static System.Collections.IEnumerator P1Lobby(App app)
        {
            app.ShowScreen("lobby");
            yield return null; yield return null;
            Need(app, "Start"); Need(app, "ChapterCard");
            foreach (var key in NavBar.Keys)
            {
                Tap(app, "Tab:" + key);
                yield return null; yield return null;
            }
            app.ShowScreen("lobby");
            yield return null;
            for (int i = 0; i < 2; i++) { Tap(app, "ArrowR"); yield return null; }
            for (int i = 0; i < 3; i++) { Tap(app, "ArrowL"); yield return null; }
        }

        /// <summary>각본이 «있어야 한다» 고 여기는 자리가 없을 때 — 봇은 이것을 <c>fail</c> 로 적고 다음 단계로 간다.</summary>
        public class MissingException : System.Exception
        {
            public MissingException(string name) : base("못 찾았다: " + name) { }
        }
        static UnityEngine.Transform Need(App app, string name)
        {
            var root = app.Current != null ? app.Current.Root : null;
            var t = root != null ? UiKit.Find(root, name) : null;
            if (t == null) throw new MissingException(name);
            return t;
        }
        static void Tap(App app, string name)
        {
            var b = Need(app, name).GetComponent<UnityEngine.UI.Button>();
            if (b == null || !b.interactable) throw new MissingException(name + "(눌리지 않는다)");
            b.onClick.Invoke();
        }

        /// <summary>
        /// 각본을 처음부터 끝까지 돌린다(T300 2항). 단계마다 <see cref="Line"/> 한 줄을 찍고,
        /// <b>어느 단계가 터져도 다음 단계로 간다</b> — 봇이 게임을 멈추면 그것이 더 나쁜 고장이다.
        /// <para>마지막 줄은 늘 <c>[KkomaKnight] play done &lt;성공&gt;/&lt;돈 것&gt; fail &lt;실패&gt;</c> 다 —
        /// 스모크가 꼬리에서 그 한 줄만 찾으면 되게(T239 결정 678 과 같은 계약).</para>
        /// </summary>
        public static System.Collections.IEnumerator Run(App app)
        {
            int ok = 0, bad = 0, ran = 0;
            foreach (var st in Stages)
            {
                Step step;
                if (!Steps.TryGetValue(st.Id, out step)) continue;   // 아직 아무도 안 쓴 단계는 조용히 건너뛴다
                ran++;
                var it = step(app);
                bool alive = true;
                while (alive)
                {
                    try { alive = it.MoveNext(); }
                    catch (System.Exception e)
                    {
                        UnityEngine.Debug.Log(Line(st.Id, false, e.GetType().Name + " " + e.Message));
                        bad++; alive = false; it = null;
                    }
                    if (it == null) break;
                    if (alive) yield return it.Current;
                }
                if (it != null) { UnityEngine.Debug.Log(Line(st.Id, true)); ok++; }
            }
            UnityEngine.Debug.Log(DoneLine(ok, ran, bad));
        }

        /// <summary>스모크가 꼬리에서 찾는 마지막 한 줄.</summary>
        public const string DonePrefix = LogPrefix + "done ";
        public static string DoneLine(int ok, int ran, int bad) => DonePrefix + ok + "/" + ran + " fail " + bad;
    }
}
