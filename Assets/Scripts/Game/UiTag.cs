using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// UI 비평 하니스(T46 · 주인 2026-09-06 «비평하면서 만든다»)의 <b>판정 요소 이름표</b>. 화면 작업자가 <see cref="UiKit.Tag"/> 로 요소에 붙이면
    /// PlayMode <c>UiShotsTests</c> 가 활성 이름표를 전부 모아 프레임 %(좌상 0 · 우하 100) 사각형을 <c>ui-screens/layout.json</c> 에 쓰고, <c>tools/ui_score.py</c> 가 <c>docs/ref-layout.md</c> 표와 대조한다.
    /// <see cref="Name"/> 은 표의 «요소» 열과 <b>글자까지 같아야</b> 한다(예: «아바타(정사각)» · «START 버튼»).
    /// <see cref="Members"/> 가 있으면 «줄(N칸)» 규약(ref-layout ⚑U03 ⓒ · ⊕ 합집합)대로 멤버 사각형의 합집합을 재고, 없으면 자기 RectTransform 의 사각형을 잰다(자식 포함 아님).
    /// 표 안에 «(참고·컨테이너)» 가 붙은 이름은 채점에서 빠진다(ui_score.py).
    /// </summary>
    public sealed class UiTag : MonoBehaviour
    {
        public string Name;
        public readonly List<RectTransform> Members = new List<RectTransform>();
        /// <summary>글자 요소(T47 ⓒ): RectTransform(조각·스트레치 rect)이 아니라 <b>글자 덩어리</b>(uGUI <see cref="Text"/> 의 preferred 크기 · 정렬 위치)를 잰다 — 표의 «챕터 제목» 은 글자 자체의 사각형이다(조각 rect 는 ±6/12 여유로 더 크다).</summary>
        public bool TextBounds;

        /// <summary>프레임 기준 % 사각형 [x, y, w, h] — 비활성 멤버는 뺀다. 잴 것이 없으면 null.</summary>
        public float[] Measure(RectTransform frame)
        {
            if (Members.Count == 0) return TextBounds ? MeasureText(frame, transform as RectTransform) : Measure(frame, transform as RectTransform);
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue; bool any = false;
            foreach (var m in Members)
            {
                if (m == null || !m.gameObject.activeInHierarchy) continue;
                var r = Measure(frame, m); if (r == null) continue; any = true;
                x0 = Mathf.Min(x0, r[0]); y0 = Mathf.Min(y0, r[1]); x1 = Mathf.Max(x1, r[0] + r[2]); y1 = Mathf.Max(y1, r[1] + r[3]);
            }
            return any ? new[] { x0, y0, x1 - x0, y1 - y0 } : null;
        }

        /// <summary>한 RectTransform 의 프레임 % 사각형 — 월드 모서리를 프레임 로컬로 옮겨 잰다(배율·회전 반영). frame 은 <c>App.Frame</c>(1080×2337 · 피벗 가운데).</summary>
        public static float[] Measure(RectTransform frame, RectTransform rt)
        {
            if (frame == null || rt == null) return null;
            var corners = new Vector3[4]; rt.GetWorldCorners(corners);
            float lx0 = float.MaxValue, ly0 = float.MaxValue, lx1 = float.MinValue, ly1 = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                var l = frame.InverseTransformPoint(corners[i]);
                lx0 = Mathf.Min(lx0, l.x); ly0 = Mathf.Min(ly0, l.y); lx1 = Mathf.Max(lx1, l.x); ly1 = Mathf.Max(ly1, l.y);
            }
            var fr = frame.rect; if (fr.width <= 0 || fr.height <= 0) return null;
            float x = (lx0 - fr.xMin) / fr.width * 100f, y = (fr.yMax - ly1) / fr.height * 100f, w = (lx1 - lx0) / fr.width * 100f, h = (ly1 - ly0) / fr.height * 100f;
            return new[] { Round(x), Round(y), Round(w), Round(h) };
        }
        /// <summary>글자 덩어리의 프레임 % 사각형 — <see cref="Text"/> 의 preferred 폭·높이(rect 안으로 클램프)를 <c>alignment</c> 자리에 놓고 잰다. Text 가 없으면 rect 그대로.</summary>
        public static float[] MeasureText(RectTransform frame, RectTransform rt)
        {
            if (frame == null || rt == null) return null;
            var text = rt.GetComponent<Text>();
            if (text == null) return Measure(frame, rt);
            var r = rt.rect;
            float w = Mathf.Clamp(text.preferredWidth, 0f, r.width), h = Mathf.Clamp(text.preferredHeight, 0f, r.height);
            int a = (int)text.alignment; int col = a % 3, row = a / 3;                                 // TextAnchor: Upper/Middle/Lower × Left/Center/Right
            float x0 = col == 0 ? r.xMin : col == 1 ? r.center.x - w / 2f : r.xMax - w;
            float y1 = row == 0 ? r.yMax : row == 1 ? r.center.y + h / 2f : r.yMin + h;
            var corners = new[] { new Vector3(x0, y1 - h), new Vector3(x0, y1), new Vector3(x0 + w, y1), new Vector3(x0 + w, y1 - h) };
            float lx0 = float.MaxValue, ly0 = float.MaxValue, lx1 = float.MinValue, ly1 = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                var l = frame.InverseTransformPoint(rt.TransformPoint(corners[i]));
                lx0 = Mathf.Min(lx0, l.x); ly0 = Mathf.Min(ly0, l.y); lx1 = Mathf.Max(lx1, l.x); ly1 = Mathf.Max(ly1, l.y);
            }
            var fr = frame.rect; if (fr.width <= 0 || fr.height <= 0) return null;
            return new[] { Round((lx0 - fr.xMin) / fr.width * 100f), Round((fr.yMax - ly1) / fr.height * 100f), Round((lx1 - lx0) / fr.width * 100f), Round((ly1 - ly0) / fr.height * 100f) };
        }
        static float Round(float v) => Mathf.Round(v * 10f) / 10f;
    }
}
