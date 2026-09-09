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
    }
}
