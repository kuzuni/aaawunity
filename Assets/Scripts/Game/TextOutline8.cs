using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 글자 테를 <b>여덟 방향 같은 반경</b>으로 두른다 — 주인 2026-09-07 17:0X
    /// «게임 전반적인 텍스트 아웃라인이 … 굉장히 이상함 · <b>메테리얼 사용해서 검정 아웃라인 한 느낌이 아님</b>»(T204).
    /// <para>
    /// <b>uGUI <see cref="Outline"/> 을 왜 버리나</b> — 그것은 테를 «그리는» 것이 아니라 글자 사본을
    /// <b>대각 네 방향</b>(±d, ±d)으로 민 것이다. 그래서 두 가지가 어긋난다(등재 세션 실측 · T204 2항):
    /// <list type="bullet">
    /// <item><b>테가 마름모다</b> — 정면 도달 거리는 d 인데 대각은 <b>d√2 = 1.41배</b>라 네 귀퉁이만 뾰족하다.
    /// «두른 테» 가 아니라 별 모양으로 보이는 것이 주인이 말한 «괴상함» 의 절반이다.</item>
    /// <item><b>정면(상·하·좌·우)이 비어 있다</b> — 대각 넷만 있으니 획의 위·아래·양옆은 사본이 안 덮는다.
    /// 곡선 글자(ㅇ·ㅎ·0·8)에서 테가 끊겨 보인다.</item>
    /// </list>
    /// 여덟 방향(45°씩)을 <b>모두 반경 d</b> 로 찍으면 정면·대각이 같은 거리라 테가 고르게 두른 꼴이 된다.
    /// </para>
    /// <para>
    /// <b>진짜 «머티리얼 아웃라인»(TMP SDF)은 이 회차에서 안 한다</b> — 이 프로젝트는 조각의 TMP 를 전부
    /// uGUI <see cref="Text"/>(Jua)로 바꿔 쓰고 Jua SDF 폰트 애셋이 저장소에 없다(T204 4항).
    /// 그쪽은 주인이 «그렇게 해라» 고 답한 뒤 별 번호로 연다 — 이 컴포넌트는 <b>uGUI 안에서 갈 수 있는 가장 가까운 자리</b>다.
    /// </para>
    /// <para>
    /// <b>비용</b> — 사본 여덟 장이라 정점이 9배가 된다(uGUI <see cref="Outline"/> 은 5배였다).
    /// 글자는 화면당 수십 개 규모라 실측에서 문제가 없었고, 늘어난 것은 <b>정점뿐</b>(드로콜·머티리얼 불변)이다.
    /// </para>
    /// </summary>
    [AddComponentMenu("UI/Effects/Text Outline 8", 16)]
    public class TextOutline8 : BaseMeshEffect
    {
        [SerializeField] Color m_EffectColor = Color.black;
        [SerializeField] float m_Radius = 1f;
        [SerializeField] bool m_UseGraphicAlpha = true;

        /// <summary>테 색(<see cref="UiKit.OutlineColor"/>).</summary>
        public Color effectColor
        {
            get { return m_EffectColor; }
            set { m_EffectColor = value; MarkDirty(); }
        }

        /// <summary>테 반경(px) — <b>여덟 방향 모두 이 거리</b>다(uGUI Outline 의 <c>effectDistance</c> 와 달리 방향마다 다르지 않다).</summary>
        public float radius
        {
            get { return m_Radius; }
            set { m_Radius = value; MarkDirty(); }
        }

        /// <summary>글자 알파를 테에 곱한다(uGUI <see cref="Outline"/> 의 같은 이름 옵션과 같은 뜻).</summary>
        public bool useGraphicAlpha
        {
            get { return m_UseGraphicAlpha; }
            set { m_UseGraphicAlpha = value; MarkDirty(); }
        }

        void MarkDirty()
        {
            if (graphic != null) graphic.SetVerticesDirty();
        }

        /// <summary>여덟 방향(45°씩) 단위 벡터 — 반경을 곱해 쓴다. 정면 넷과 대각 넷이 <b>같은 거리</b>인 것이 이 컴포넌트의 요점이다.</summary>
        public static readonly Vector2[] Dirs = BuildDirs();

        static Vector2[] BuildDirs()
        {
            var d = new Vector2[8];
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f;
                d[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            }
            return d;
        }

        static readonly List<UIVertex> Buf = new List<UIVertex>();

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;

            Buf.Clear();
            vh.GetUIVertexStream(Buf);
            int count = Buf.Count;
            if (count == 0) return;

            vh.Clear();
            // 테 여덟 장을 먼저 깔고 원래 글자를 «맨 위» 에 올린다 — 순서가 바뀌면 테가 글자를 덮는다.
            for (int dir = 0; dir < Dirs.Length; dir++)
            {
                var off = Dirs[dir] * m_Radius;
                for (int i = 0; i < count; i++)
                {
                    var v = Buf[i];
                    var p = v.position;
                    p.x += off.x; p.y += off.y;
                    v.position = p;
                    var c = m_EffectColor;
                    if (m_UseGraphicAlpha) c.a = m_EffectColor.a * (Buf[i].color.a / 255f);
                    v.color = c;
                    vh.AddVert(v);
                }
            }
            for (int i = 0; i < count; i++) vh.AddVert(Buf[i]);

            int total = (Dirs.Length + 1) * count;
            for (int i = 0; i < total; i += 3) vh.AddTriangle(i, i + 1, i + 2);
        }
    }
}
