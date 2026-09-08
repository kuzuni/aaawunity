using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using KkomaKnight.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T227 ⚑ — «버튼이 <b>실제 탭으로</b> 닿는가» 를 재는 공용 자. 이 저장소의 다른 자들은 전부
    /// <c>Button.onClick.Invoke()</c>(<c>ClickNamed</c> 계열)로 누르는데, 그것은 <b>레이캐스트를 통째로 건너뛴다</b> —
    /// 위에 무엇이 덮여 있든, 어둠이 가리든 그냥 콜백을 부른다. 그래서 «로직은 초록인데 주인 손에서는 안 눌린다» 를
    /// 레포 전체에서 아무 자도 못 봤다(주인 «도전 버튼 눌렀는데 겜 시작 안 하던데 던전» · T227).
    /// <para>
    /// ⚠ <c>onClick.Invoke()</c> 를 이 자로 <b>바꾸지 마라</b> — 기존 자들은 «로직이 도는가» 를 재고 그 값어치는 그대로다.
    /// 이것은 그 옆에 새로 세우는 «닿는가» 자다(둘은 다른 것을 잰다 · T227 4항).
    /// </para>
    /// 재는 법 = <see cref="EventSystem"/> + <see cref="GraphicRaycaster"/> 로 <b>버튼 중심의 화면 좌표</b>에 <c>RaycastAll</c> 을 쏴
    /// «맨 위 히트가 그 버튼이거나 그 자식인가» 를 본다(<c>RaycastAll</c> 결과는 위에서 아래 순서라 [0] 이 손가락에 먼저 닿는 것).
    /// </summary>
    public static class Tap
    {
        /// <summary>버튼 중심의 화면 좌표(캔버스가 Overlay 면 카메라 없이 그대로).</summary>
        public static Vector2 ScreenCenter(App app, RectTransform t)
        {
            var canvas = app != null ? app.UiCanvas : null;
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            return RectTransformUtility.WorldToScreenPoint(cam, t.TransformPoint(t.rect.center));
        }

        /// <summary>그 자리에 쌓인 히트 전부(맨 위부터). EventSystem 이 없으면 빈 목록.</summary>
        public static List<RaycastResult> Hits(App app, RectTransform t)
        {
            var res = new List<RaycastResult>();
            var es = EventSystem.current;
            if (es == null || t == null) return res;
            es.RaycastAll(new PointerEventData(es) { position = ScreenCenter(app, t) }, res);
            return res;
        }

        /// <summary>«Overlay/Popup/Border» 꼴의 읽을 수 있는 경로(캔버스까지).</summary>
        public static string PathOf(Transform t)   // ⚠ 이름이 Path 이면 System.IO.Path 와 부딪친다(같은 파일에서 Path.Combine 을 쓴다)
        {
            if (t == null) return "(없음)";
            var sb = new StringBuilder(t.name);
            for (var p = t.parent; p != null; p = p.parent) sb.Insert(0, p.name + "/");
            return sb.ToString();
        }

        /// <summary>재 본 결과 — <see cref="Ok"/>(닿는다) · <see cref="Blocked"/>(딴 것이 먹는다) · <see cref="NoHit"/>(히트 0 = 잴 수 없다).</summary>
        public enum Reach { Ok, Blocked, NoHit }

        /// <summary>버튼 중심에 탭이 닿는가. <paramref name="why"/> 에 맨 위 히트와 쌓인 순서를 담는다.</summary>
        public static Reach Reaches(App app, RectTransform btn, out string why)
        {
            why = "";
            if (btn == null) { why = "버튼이 null"; return Reach.NoHit; }
            var hits = Hits(app, btn);
            if (hits.Count == 0)
            {
                why = $"히트 0 — 화면 좌표 {ScreenCenter(app, btn)} 에 아무것도 안 잡힌다(EventSystem/GraphicRaycaster 가 없거나 버튼이 화면 밖)";
                return Reach.NoHit;
            }
            var top = hits[0].gameObject != null ? hits[0].gameObject.transform : null;
            var sb = new StringBuilder();
            for (int i = 0; i < hits.Count && i < 8; i++) sb.Append(i == 0 ? "" : " < ").Append(hits[i].gameObject != null ? hits[i].gameObject.name : "(null)");
            if (top != null && (top == btn || top.IsChildOf(btn))) { why = "맨 위 히트 = " + PathOf(top) + " · 쌓인 순서: " + sb; return Reach.Ok; }
            why = "맨 위 히트가 그 버튼이 아니다 — " + PathOf(top) + " 가 탭을 먹는다 · 쌓인 순서(위→아래): " + sb;
            return Reach.Blocked;
        }

        /// <summary>
        /// «닿는가» 를 재서 한 줄 남긴다. <paramref name="strict"/> 가 참이면 «막힘» 에서 실패시킨다.
        /// <para>
        /// ⚠ <b>새 물음을 재는 자는 처음엔 로그로 넣는다</b>(결정 627 · T226 4항) — 이 자가 빨가면 유니티 잡이 빨갛고
        /// <c>build-webgl</c> 이 <c>needs: [unity-test]</c> 로 안 돌아 <b>배포가 통째로 멈춘다</b>(오늘만 4시간 25분).
        /// T227 은 «고친 빌드에서 주인이 직접 눌러 보는 것» 이 확인 조건이라, 이 자가 배포를 막으면 <b>그 확인 자체가 불가능해진다</b>.
        /// 그래서 고침이 든 이번 회차도 로그로 두고, CI 로그에 «닿음» 이 찍힌 다음 회차에 <see cref="AssertTappable"/> 로 올린다.
        /// </para>
        /// «히트 0» 은 화면이 깨진 것이 아니라 자가 못 잰 것이라 <b>strict 여도 안 막는다</b>.
        /// </summary>
        public static void Check(App app, RectTransform btn, string what, bool strict)
        {
            var r = Reaches(app, btn, out string why);
            Row(what, btn, r, why);
            if (r == Reach.Ok) { Debug.Log($"[Tap] 닿음  «{what}» — {why}"); return; }
            if (r == Reach.NoHit) { Debug.Log($"[Tap] 못잼  «{what}» — {why}"); return; }
            string msg = $"[Tap] 막힘  «{what}» 에 탭이 안 닿는다(주인 손가락이 보는 그림 그대로 · T227) — {why}";
            if (strict) NUnit.Framework.Assert.Fail(msg); else Debug.LogWarning(msg);   // 경고는 PlayLog 가 빨강으로 안 센다
        }

        /// <summary>«닿는가» 단언 — 그 자리가 초록인 것을 CI 로그로 확인한 뒤에만 이쪽으로 올린다(위 주석).</summary>
        public static void AssertTappable(App app, RectTransform btn, string what) => Check(app, btn, what, true);

        /// <summary>«닿는가» 를 재서 로그만 남긴다(막지 않는다).</summary>
        public static void LogTappable(App app, RectTransform btn, string what) => Check(app, btn, what, false);

        /// <summary>
        /// <paramref name="root"/> 아래 켜져 있는 버튼을 전부 재서 표 한 장을 로그로 남긴다(<b>단언 안 함</b> · T226 4항 «조사 동안은 로그 한 줄 + 표»).
        /// 다음 회차가 이 표를 보고 «막는 자로 올릴 자리» 를 고른다.
        /// </summary>
        public static string Report(App app, Transform root, string where)
        {
            var sb = new StringBuilder($"[Tap] {where} — 탭이 닿는가 표(단언 아님 · T227)\n");
            if (root == null) { sb.Append("  (뿌리가 null)"); Debug.Log(sb.ToString()); return sb.ToString(); }
            foreach (var b in root.GetComponentsInChildren<Button>(false))
            {
                if (b == null) continue;
                var rt = (RectTransform)b.transform;
                var r = Reaches(app, rt, out string why);
                string note = Cover(app, rt) ? "전면 덮개(어둠·배경) — 중심이 팝업 뒤라 «막힘» 이 정상이다(잴 뜻이 없는 줄)" : "";
                Row(where + " · " + b.name, rt, r, why, note);
                sb.Append("  ").Append(r == Reach.Ok ? "닿음  " : r == Reach.Blocked ? "막힘  " : "못잼  ").Append(PathOf(b.transform))
                  .Append(note.Length > 0 ? "  (" + note + ")" : "").Append("  ← ").Append(why).Append('\n');
            }
            Debug.Log(sb.ToString());
            return sb.ToString();
        }

        // ───────────────────────── ui-screens/tap.json (워커가 읽을 수 있는 자리) ─────────────────────────
        /// <summary>
        /// ⚑ <b>찍기만 하면 아무도 못 읽는다.</b> 유니티 테스트의 <c>Debug.Log</c> 는 결과 XML(아티팩트) 안에만 남는데
        /// 워커 환경에서 그 아티팩트는 프록시가 막고(블롭 403), <c>get_job_logs</c> 는 잡 로그 <b>끝 30KB 남짓</b>만 준다 —
        /// 유니티 잡의 콘솔 출력은 그 창 앞에 있어 손이 안 닿는다(2026-09-08 실측 · 결정 636).
        /// 그래서 이 표를 <see cref="PlayShot.Dirs"/> 폴더에 <c>tap.json</c> 으로도 쓴다 — CI 가 PNG·<c>layout.json</c>·
        /// <c>overdraw.json</c> 과 같이 `screens` 브랜치로 올려 주므로 <c>git fetch origin screens</c> 한 번이면 읽힌다
        /// (<see cref="OverdrawAuditTests"/> 가 같은 까닭으로 밟은 길이다).
        /// </summary>
        static readonly List<string> _rows = new List<string>();

        static string J(string s) => (s ?? "").Replace("\\", "/").Replace("\"", "'").Replace("\n", " ");

        /// <summary>
        /// 프레임을 거의 다 덮는 칸인가(어둠 <c>Dimmed</c> · 프리팹 팝업의 <c>Background</c>). 그런 칸은 <b>중심이 팝업 상자 뒤</b>라
        /// «막힘» 이 나오는 것이 정상이고 결함이 아니다 — run 487 실측에서 `Dimmed` 한 줄이 그렇게 나왔다(T227 회차 3).
        /// 표에서 그 줄에 까닭을 붙여 다음 사람이 좇지 않게 한다.
        /// </summary>
        static bool Cover(App app, RectTransform t)
        {
            var frame = app != null ? app.Frame : null;
            if (frame == null || t == null) return false;
            float fa = frame.rect.width * frame.rect.height, ta = t.rect.width * t.rect.height;
            return fa > 1f && ta >= fa * 0.8f;
        }

        static void Row(string what, RectTransform btn, Reach r, string why, string note = "")
        {
            _rows.Add("{\"what\":\"" + J(what) + "\",\"btn\":\"" + J(btn != null ? PathOf(btn) : "(null)") + "\",\"reach\":\""
                      + (r == Reach.Ok ? "ok" : r == Reach.Blocked ? "blocked" : "nohit") + "\",\"why\":\"" + J(why) + "\",\"note\":\"" + J(note) + "\"}");
            Flush();
        }

        static void Flush()
        {
            string json = "{\"_meta\":{\"task\":\"T227\",\"rows\":" + _rows.Count + "},\"rows\":[" + string.Join(",", _rows.ToArray()) + "]}";
            foreach (var dir in PlayShot.Dirs())
            {
                try { Directory.CreateDirectory(dir); File.WriteAllText(Path.Combine(dir, "tap.json"), json); }
                catch (Exception e) { Debug.LogWarning("[Tap] tap.json 저장 실패(" + dir + "): " + e.Message); }
            }
        }
    }
}
