using System;
using KkomaKnight.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// T240 4항 — PvP 한 판의 <b>결과 화면</b>(주인 2026-09-08 «이기면 승점 올라가고 순위 올라가고 그런 느낌, 지면 승점 떨어지고» ·
    /// 레퍼런스 <c>docs/ref/34_pvp_win.jpg</c> · 자리 표 <c>docs/ref-layout.md</c> ㊻).
    /// <para>
    /// 모양은 «상자» 가 아니라 어두운 전면 위 <b>가운데 세로 한 줄</b>이다:
    /// 어둠 → 빛나는 방패 엠블럼 → «승리»/«패배» → 티어 명판 → 아바타 VS 아바타 → 이름 둘 → 승점 변화 둘 → 주황 «계속».
    /// 자리는 전부 <see cref="Layout"/> 의 <c>Arr*</c>(레퍼런스 34 실측)이고 <b>이 파일에 자리 수치를 박지 않는다</b>.
    /// </para>
    /// <para>
    /// ⚠ <b>왜 <c>Overlay</c> 안이 아니라 새 파일인가</b> — 워커 F 가 T241 에서 낸 길 그대로다(<see cref="RewardPopup"/>).
    /// 이 화면은 결과 프리팹(<c>ui.resultWin</c>)의 구도와 많이 달라 그 프리팹을 재단장하는 것보다 조각으로 세우는 편이 짧고,
    /// <c>Overlay.cs</c> 를 안 건드리면 그 파일을 쥔 회차와 절대 안 부딪힌다.
    /// </para>
    /// <para>
    /// ⚠ <b>아직 없는 것 = 컨페티</b>(지시서 4항이 «T110 ⓓ 와 같은 조각» 이라 적은 그것). 그 조각은
    /// <c>ui.resultWin</c> <b>프리팹 안</b>에 <c>SampleEffect_Confetti</c> 로 들어 있어 카탈로그 키로 따로 못 꺼낸다 —
    /// 프리팹을 통째로 띄워 자식 하나만 빼 쓰는 길은 «화면 하나 세우려고 다른 화면을 띄우는» 꼴이라 안 했다.
    /// 배선 회차가 <c>Overlay</c> 의 <c>Confetti</c> 를 <b>루트를 받는 공개 함수</b>로 한 칸 넓히면 한 줄로 붙는다(그 파일이 지금은 비어 있다).
    /// </para>
    /// </summary>
    public static class ArenaResult
    {
        /// <summary>
        /// 이 화면의 어둠 알파 — 레퍼런스 34 의 뒤 화면(콜로세움) 평균 밝기가 <b>26.9/255</b> 로 살아 있다(실측).
        /// 공통 <see cref="UiKit.DimAlpha"/>(0.985)는 뒤를 거의 지워 버린다. <see cref="RewardPopup.DimAlpha"/> 와 같은 값·같은 까닭이다.
        /// </summary>
        public const float DimAlpha = 0.65f;

        /// <summary>«승리» / «패배» 글자 — 화면이 지어내지 않게 여기 한 곳에 둔다.</summary>
        public const string WinTitle = "승리", LoseTitle = "패배";

        /// <summary>등장 연출 길이(초 · unscaled) · 줄이 하나씩 뜨는 간격.</summary>
        public const float RevealSec = 0.25f, Stagger = 0.06f;
        /// <summary>튀어 오르기 시작 배율(<see cref="UiKit.PopIn"/> 기본값과 같은 0.82).</summary>
        public const float PopFrom = 0.82f;

        /// <summary>마지막으로 띄운 결과 — 자가 «무엇이 그려졌나» 를 이걸로 본다(화면을 안 뒤져도 된다).</summary>
        public static ArenaMatch.Outcome Last { get; private set; }
        /// <summary>마지막으로 띄운 뒤 지금 떠 있는가.</summary>
        public static bool Open { get; private set; }

        /// <summary>
        /// 결과 화면을 띄운다. <paramref name="o"/> 는 <see cref="ArenaMatch.Settle"/> 가 낸 값 그대로다 —
        /// <b>이 화면은 승점을 다시 계산하지 않는다</b>(세이브가 이미 바뀐 뒤라 같은 수가 안 나온다).
        /// </summary>
        /// <param name="myFace">내 초상 스프라이트 키(없으면 기본 아바타).</param>
        /// <param name="foeFace">상대 초상 스프라이트 키.</param>
        public static void Show(ArenaMatch.Outcome o, string myName, string foeName,
                                string myFace, string foeFace, Action onContinue = null)
        {
            var app = App.I;
            if (app == null || app.Overlay == null) return;
            var ov = app.Overlay;
            ov.Close();                       // 앞 팝업의 트윈까지 깨끗이 죽인다(Overlay.cs 를 안 건드리는 길 · T241 전례)
            var root = ov.Root;
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            Last = o; Open = true;
            Audio.Sfx(o.Win ? "snd.clear" : "snd.popup");

            // ⓐ 어둠 — 프레임 밖(레터박스·노치)까지. 여기서는 «탭하면 닫힘» 이 아니다(닫는 것은 «계속» 버튼 하나 · 레퍼런스 34 에 닫기 안내가 없다).
            var dim = UiKit.Rect(root, "Dimmed");
            UiKit.Stretch(dim, -UiKit.DimOverscan, -UiKit.DimOverscan, -UiKit.DimOverscan, -UiKit.DimOverscan);
            var di = dim.gameObject.AddComponent<Image>();
            di.color = Palette.A(Palette.Dim, DimAlpha); di.raycastTarget = true;
            UiKit.FadeIn(di, DimAlpha);
            UiKit.Tag(dim, "어둠");

            // ⓑ 방패 엠블럼 + 뒤 빛무리 — 조각은 이미 있는 것(ui.iconPvp = 금빛 방패)이다. 새 그림 0(§1).
            var glow = UiKit.Rect(root, "EmblemGlow"); UiKit.Pct(glow, Layout.ArrEmblem);
            UiKit.LightBehind(glow, null, UiKit.LightKey, UiKit.LightPeriod, Palette.A(Palette.Yellow, 0.5f),
                              sidePx: UiKit.FrameW * Layout.ArrEmblem.W / 100f, clip: false);
            UiKit.Tag(glow, "엠블럼 빛무리");
            var emblem = UiKit.Icon(root, "Emblem", "ui.iconPvp");
            UiKit.Pct(emblem.rectTransform, Layout.ArrEmblem);
            UiKit.Tag(emblem.transform, "방패 엠블럼");

            // ⓒ «승리»/«패배»
            var title = UiKit.Label(root, Layout.ArrTitle.X, Layout.ArrTitle.Y, Layout.ArrTitle.W, Layout.ArrTitle.H,
                                    o.Win ? WinTitle : LoseTitle, TextSize.Title,
                                    o.Win ? Palette.Yellow : Palette.Gray, TextAnchor.MiddleCenter, true, true, TextKind.Title);
            title.name = "ResultTitle"; title.fontStyle = FontStyles.Bold;
            UiKit.Tag(title.transform, "결과 제목");

            // ⓓ 티어 명판 — 이름은 표(arenaMatch.json)에서 온 것을 그대로 쓴다(코드가 다시 짓지 않는다).
            var plate = UiKit.Panel(root, "TierPlate", "fr.r12", Palette.A(Palette.Slate, 0.85f));
            UiKit.Pct(plate.rectTransform, Layout.ArrTier);
            var tier = UiKit.Label(plate.transform, 0, 0, 100, 100, o.Tier ?? "", TextSize.Body, Palette.White);
            tier.name = "TierName"; tier.fontStyle = FontStyles.Bold;
            UiKit.Tag(plate.transform, "티어 명판");

            // ⓔ 아바타 VS 아바타
            Face(root, "MyFace", Layout.ArrMyFace, myFace, "내 초상");
            Face(root, "FoeFace", Layout.ArrFoeFace, foeFace, "상대 초상");
            var vs = UiKit.Icon(root, "VsBadge", "ui.iconPvp", Palette.A(Palette.White, 0.9f));
            UiKit.Pct(vs.rectTransform, Layout.ArrVs);
            var vsText = UiKit.Label(vs.transform, 0, 0, 100, 100, "VS", TextSize.Body, Palette.White);
            vsText.fontStyle = FontStyles.Bold;
            UiKit.Tag(vs.transform, "VS 배지");

            // ⓕ 이름 둘
            Name(root, "MyName", Layout.ArrMyName, myName, "내 이름");
            Name(root, "FoeName", Layout.ArrFoeName, foeName, "상대 이름");

            // ⓖ 승점 변화 — **표의 승/패 값이 아니라 실제로 움직인 값(Delta)** 이다.
            //    바닥(0)에 걸린 판은 −6 이 아니라 그만큼만 갔고, −6 이라 적으면 거짓말이 된다(ArenaMatch.Delta 주석과 같은 까닭).
            Delta(root, "MyDelta", Layout.ArrMyDelta, o.Delta, "내 승점 변화");
            // 상대 쪽은 내 반대다 — 더미라 저장되는 값이 없고, 레퍼런스 34 가 «+8 ↔ −6» 으로 둘을 같이 보여 준다.
            Delta(root, "FoeDelta", Layout.ArrFoeDelta, o.Win ? -Math.Abs(o.Delta) : Math.Abs(o.Delta), "상대 승점 변화");

            // ⓗ «계속» — 이 화면을 닫는 유일한 길이다.
            var cont = UiKit.Button(root, "ui.btnOrange", "계속", () => { Open = false; ov.Close(); onContinue?.Invoke(); }, Layout.ArrContinue);
            cont.name = "ContinueBtn";
            UiKit.Tag(cont, "계속 버튼");

            Reveal(emblem.rectTransform, title.rectTransform, plate.rectTransform, cont);
        }

        static void Face(RectTransform root, string name, Layout.R r, string spriteKey, string tag)
        {
            var box = UiKit.Rect(root, name); UiKit.Pct(box, r);
            var frame = UiKit.Spawn("ui.itemFrame.yellow", box); if (frame != null) UiKit.Stretch((RectTransform)frame.transform);
            if (!string.IsNullOrEmpty(spriteKey))
            {
                var img = UiKit.Icon(box, "Img", spriteKey);
                UiKit.Pct(img.rectTransform, 10, 10, 80, 80);
            }
            UiKit.Tag(box, tag);
        }

        static void Name(RectTransform root, string name, Layout.R r, string s, string tag)
        {
            var t = UiKit.Label(root, r.X, r.Y, r.W, r.H, s ?? "", TextSize.Body, Palette.White);
            t.name = name; t.fontStyle = FontStyles.Bold;
            UiKit.Tag(t.transform, tag);
        }

        static void Delta(RectTransform root, string name, Layout.R r, double v, string tag)
        {
            var t = UiKit.Label(root, r.X, r.Y, r.W, r.H, Sign(v), TextSize.Body, v >= 0 ? Palette.Green : Palette.Red);
            t.name = name; t.fontStyle = FontStyles.Bold;
            UiKit.Tag(t.transform, tag);
        }

        /// <summary>«+8» / «−6» — 0 도 «+0» 이 아니라 «0» 으로 적는다(바닥에 걸려 아무것도 안 움직인 판이 그렇다).</summary>
        public static string Sign(double v)
        {
            if (v > 0) return "+" + v.ToString("0");
            if (v < 0) return "−" + Math.Abs(v).ToString("0");   // 레퍼런스와 같은 «−»(U+2212 · 하이픈보다 굵다)
            return "0";
        }

        /// <summary>엠블럼 → 제목 → 명판 → 버튼 순서로 하나씩(unscaled · 트윈은 <c>SetLink</c> 로 붙여 화면이 사라지면 같이 죽는다).</summary>
        static void Reveal(RectTransform emblem, RectTransform title, RectTransform plate, RectTransform cont)
        {
            // ⚠ 인자 순서 — PopIn(rt, from(시작 배율), dur, delay) 다. from 자리에 길이를 넣으면 컴파일은 되고 연출만 죽는다.
            UiKit.PopIn(emblem, PopFrom, RevealSec, 0f);
            UiKit.PopIn(title, PopFrom, RevealSec, Stagger);
            UiKit.PopIn(plate, PopFrom, RevealSec, Stagger * 2f);
            UiKit.PopIn(cont, PopFrom, RevealSec, Stagger * 3f);
        }
    }
}
