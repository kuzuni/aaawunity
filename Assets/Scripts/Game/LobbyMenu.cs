using System;
using System.Collections.Generic;
using KkomaKnight.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 로비 «메뉴»(≡) 팝업 — 데모 프리팹 <c>Lobby_Menu</c>(= 어둠 + <c>HambergerMenu</c> 판) **그대로** (T96-menu · 주인 2026-09-07
    /// «메뉴 버튼 로비에 있는 거 클릭 시 떠야 하고, 메뉴로 우편함·설정·데일리 기프트·퀘스트·출석·특권 이런 거 떠야 함.
    /// 그 버튼들 중복된 거는 메뉴 안으로 넣는 걸로 바꾸쇼»).
    ///
    /// **프리팹 그대로**: 줄의 크기(113.9px)·아이콘 자리(x 58 · 128px)·글자 자리·줄 간격은 프리팹의 `VerticalLayoutGroup` 이 그대로 정한다 —
    /// 우리 격자로 다시 만들지 않는다. 프리팹 줄은 넷(<c>Button_Inbox</c>·<c>Button_Settings</c>·<c>Button_Daily Login</c>·<c>Button_Achievement</c>)이라
    /// 주인이 부른 여섯 항목에 맞춰 **줄 하나를 두 번 복제**하고(복제본은 프리팹 줄의 사본이라 비례가 같다) 판 높이를 6/4 로 늘린다.
    /// 아이콘은 **프리팹이 달고 온 그림을 그대로 쓰고**(우편함·설정·데일리 기프트·퀘스트), 새로 만든 두 줄만 카탈로그 아이콘(출석 · 특권)을 넣는다.
    ///
    /// 항목 순서 = 주인이 부른 순서(우편함 · 설정 · 데일리 기프트 · 퀘스트 · 출석 · 특권 · 결정 기록).
    /// 빨간 점은 <see cref="Notify"/> 한 곳이 정한다(T96 ⓔ) — 지금 판정이 있는 것은 데일리 기프트(수령·광고)뿐이다.
    /// </summary>
    public static class LobbyMenu
    {
        /// <summary>프리팹 판(줄들을 담은 세로 레이아웃) 이름.</summary>
        public const string PanelName = "HambergerMenu";
        /// <summary>프리팹 줄 넷의 이름 — 이 순서로 항목에 배정한다(우편함 · 설정 · 데일리 기프트 · 퀘스트).</summary>
        static readonly string[] PrefabRows = { "Button_Inbox", "Button_Settings", "Button_Daily Login", "Button_Achievement" };

        /// <summary>
        /// 주인이 준 판 자리(T139 ⓑ · 2026-09-07 04:5X 인스펙터 스크린샷) — 앵커 Min/Max <b>(1,1)</b> · Pivot <b>(0.5,0.5)</b> ·
        /// <c>anchoredPosition</c> <b>(−323, −558)</b> · 크기 <b>382.62 × 688.305</b>(그때는 줄이 여섯이었다).
        /// <para>
        /// <b>T148 로 줄이 둘이 됐다</b>(주인이 넷을 로비로 도로 꺼냈다) → 판 높이가 프리팹 값 458.87 로 줄어든다.
        /// 피벗이 가운데라 <c>Pos</c> 를 그대로 두면 판이 <b>위로 114.7px 올라가</b> ≡ 버튼에서 떨어진다(결정 377 이 미리 짚어 둔 자리).
        /// 그래서 고정하는 것을 «가운데» 가 아니라 <b>«판 윗변»</b> 으로 바꿨다 — 주인 값에서 뽑은 그 윗변(<see cref="PanelTopY"/>)에
        /// 판을 걸고 높이는 줄 수가 정한다. 줄 수가 또 바뀌어도 판은 늘 ≡ 버튼 바로 아래에서 시작한다(§1 «리터럴 대신 계산»).
        /// </para>
        /// </summary>
        public const float PanelX = -323f;
        /// <summary>판 «윗변» 의 자리(px · 오른쪽 위 모서리 기준) = 주인 Pos y(−558) + 주인 높이(688.305) ÷ 2. 줄 수와 무관한 값이라 이것을 고정한다.</summary>
        public const float PanelTopY = -558f + 688.305f / 2f;
        /// <summary>같은 스크린샷의 판 가로(px) — 프리팹 값과 같으면 그대로다.</summary>
        public const float PanelWidth = 382.62f;

        /// <summary>높이가 <paramref name="height"/> 인 판의 <c>anchoredPosition</c> — 윗변을 <see cref="PanelTopY"/> 에 맞춘다.</summary>
        public static Vector2 PanelPosFor(float height) => new Vector2(PanelX, PanelTopY - height * 0.5f);

        /// <summary>메뉴 항목 — 이름(줄 오브젝트 «Menu:key») · 라벨 · 새 줄이면 아이콘 키.</summary>
        public const string ItemMail = "mail", ItemSettings = "settings", ItemDailyGift = "dailyGift", ItemQuest = "quest", ItemAttendance = "attendance", ItemPrivilege = "privilege";

        /// <summary>
        /// 줄의 빨간 점(T136) — 줄(382.6×113.9)의 % 로 재던 것을 «왼쪽 위 모서리 + px · 지름» 으로 바꿨다(전에는 34×39 로 눌렸다).
        /// 값은 종전 점 가운데(63.1 / 28.5)를 그대로 옮긴 것이라 자리는 안 움직인다.
        /// </summary>
        static readonly Vector2 DotAnchor = new Vector2(0, 1), DotOffset = new Vector2(63, -29);
        const float DotSize = 36f;


        /// <summary>메뉴를 연다 — 로비의 ≡ 버튼이 부른다(전에는 설정 팝업을 바로 열었다).</summary>
        public static void Open(App app)
        {
            if (app == null) return;
            var root = app.Overlay.OpenPrefab("ui.lobbyMenu", closeOnDim: true);   // T139 ⓐ — 주인 «딤 눌러도 꺼지게»(이 자리만 true)
            var rt = (RectTransform)root.transform;
            HideCloseButtons(rt);                                       // T162 — 주인 «메뉴 드롭다운 팝업에 클로즈 버튼 없애기»(닫는 길은 위 딤 클릭)
            var panel = UiKit.Find(rt, PanelName) as RectTransform;
            if (panel == null) return;                                  // 프리팹이 없으면(카탈로그 결손) 조용히 빈 어둠 — 빨간 줄 0
            UiKit.Tag(panel, "메뉴 판");
            // T350 — 표 ㉜ 의 나머지 두 행. T332 가 이 화면을 처음 찍고 나서야 «표는 다섯 행인데 이름표는 셋이고
            //   그중 표와 이름이 겹치는 것은 «메뉴 판» 하나» 라는 것이 보였다(§5 lobby_menu 0.0).
            //   ⚠ `UiKit.Tag` 는 그리는 것을 한 픽셀도 안 바꾼다(이름만 붙인다).
            { var dim = UiKit.Find(rt, "Dimmed"); if (dim != null) UiKit.Tag(dim, "어둠(Dimmed)"); }

            var rows = new List<RectTransform>();
            for (int i = 0; i < PrefabRows.Length; i++)
            {
                var r = Kid(panel, PrefabRows[i]) as RectTransform; if (r != null) rows.Add(r);
            }
            if (rows.Count == 0) return;
            // 항목이 줄보다 많으면 마지막 줄을 복제해 채운다(프리팹 줄의 사본 = 같은 비례·같은 조각 구성)
            var items = Items(app);
            while (rows.Count < items.Count)
            {
                var src = rows[rows.Count - 1];
                var copy = UnityEngine.Object.Instantiate(src.gameObject, panel);
                var crt = (RectTransform)copy.transform; crt.localScale = Vector3.one; rows.Add(crt);
            }
            // 판 높이 = 줄 수에 비례(프리팹은 넷 기준 496px) — 줄 크기·간격은 레이아웃이 그대로 쓴다
            if (rows.Count > PrefabRows.Length)
                panel.sizeDelta = new Vector2(panel.sizeDelta.x, panel.sizeDelta.y * rows.Count / PrefabRows.Length);
            // T139 ⓑ — 판 자리를 주인이 준 인스펙터 값으로. 앵커·피벗을 먼저 바꾸고 크기·자리를 넣는다
            // (앵커 Min == Max 면 sizeDelta 가 곧 크기고, 앵커를 나중에 바꾸면 그 값이 다시 해석돼 어긋난다).
            // T148 — 높이는 «줄 수» 가 정하고 자리는 «윗변» 을 고정한다(주석 PanelTopY 참조 · 줄이 둘로 줄어도 ≡ 버튼 바로 아래에서 시작한다).
            panel.anchorMin = panel.anchorMax = Vector2.one;
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(PanelWidth, panel.sizeDelta.y);
            panel.anchoredPosition = PanelPosFor(panel.sizeDelta.y);

            for (int i = 0; i < items.Count; i++) Row(app, rows[i], i, items[i]);
            for (int i = items.Count; i < rows.Count; i++) rows[i].gameObject.SetActive(false);
            // T350 — 표 ㉜ 은 여섯 줄을 «메뉴 줄 6» **한 행**으로 묶어 잰다(줄마다 따로 재지 않는다) ⇒ 묶음 이름을 표와 같게 한다.
            //   ⚠ 줄마다 붙는 «메뉴 «우편함» 줄» 류는 그대로 둔다 — 표에 없는 이름표지만 그것이 있어야 «어느 줄이 비었나» 를 사람이 읽는다.
            UiKit.TagGroup(panel, "메뉴 줄 6", rows.ToArray());
        }

        /// <summary>메뉴 항목 표 — 순서·라벨·아이콘(새 줄만)·누르면 할 일 · 점을 켤 조건.</summary>
        static List<(string key, string label, string icon, Action open, Func<bool> dot)> Items(App app)
        {
            // T148 로 데일리 기프트가 로비로 돌아가면서 이 자리의 점 판정(Notify.DailyGift*)도 로비 칸(_giftDot · T77)으로 갔다.
            return new List<(string, string, string, Action, Func<bool>)>
            {
                (ItemMail, "우편함", null, () => Game.Mailbox.Open(app), () => Mailbox.Any(app)),   // T96-mail
                (ItemSettings, "설정", null, () => app.Overlay.Settings(), () => false),
                // T148(주인 2026-09-07 06:0X «데일리기프트, 퀘스트, 출석, 특권은 로비에 걍 꺼내놓는게 나은듯 · 전처럼») —
                // 그 넷은 로비 사이드 기둥으로 돌아갔다(LobbyScreen ③). 메뉴에 두면 한 화면에 같은 입구가 둘이 된다.
            };
        }

        /// <summary>줄 하나 배선 — 이름 · 라벨 · (새 줄이면) 아이콘 · 클릭 · 빨간 점. 자리·크기는 손대지 않는다(프리팹 그대로).</summary>
        static void Row(App app, RectTransform row, int index, (string key, string label, string icon, Action open, Func<bool> dot) it)
        {
            row.name = "Menu:" + it.key;
            row.SetSiblingIndex(index);                                  // 세로 레이아웃 순서 = 주인이 부른 순서
            row.gameObject.SetActive(true);
            // 글자 — 조각이 달고 온 Text 하나(TMP 는 UiKit.Adopt 가 이미 uGUI 로 바꿔 놨다)
            var label = row.GetComponentInChildren<TMP_Text>(true);
            if (label != null) UiKit.SetText(row, label.name, it.label, kind: TextKind.Button);
            // 아이콘 — 새로 만든 줄만 카탈로그 그림으로 갈아 끼운다(프리팹 줄은 제 그림 그대로)
            if (!string.IsNullOrEmpty(it.icon) && Kid(row, "Icon") != null) UiKit.SetSprite(row, "Icon", it.icon);
            // 빨간 점 — 프리팹이 줄 안 어딘가에 달고 온 점(Alert_Dot_01_Red · 데모는 늘 켜 둔다)은 전부 끄고 우리 점 하나만 쓴다
            foreach (var t in row.GetComponentsInChildren<Transform>(true))
                if (t != row && t.name.StartsWith("Alert_Dot")) t.gameObject.SetActive(false);
            var dot = Kid(row, "AlertDot");
            if (dot == null)
            {
                // T136 — 줄(382.6×113.9)의 % 로 재면 34×39 로 눌린다: «왼쪽 위 모서리 + px · 지름» 으로(자리는 종전 가운데 그대로)
                var go = UiKit.AlertDot(row, "AlertDot", DotAnchor, DotOffset, DotSize);   // 아이콘(프리팹 x 58 · 128px) 오른쪽 위
                dot = go.transform;
            }
            bool on = it.dot != null && it.dot();
            dot.gameObject.SetActive(on);
            var open = it.open; var overlay = app.Overlay;
            UiKit.Clickable(row, () => { overlay.Close(); open?.Invoke(); });
            UiKit.Tag(row, "메뉴 «" + it.label + "» 줄");
        }

        /// <summary>조각이 달고 온 닫기 버튼 이름의 «앞머리»(T162) — 데모 조각은 인스턴스마다 이름을 덮어써서
        /// (<c>Button_Close</c> · 원본은 <c>Button_Close_Square_01</c>) 정확한 이름 하나로 찾으면 놓친다(워커 A 가 우편함에서 밟은 함정).</summary>
        public const string CloseNamePrefix = "Button_Close";

        /// <summary>
        /// 메뉴 팝업의 닫기 버튼을 전부 끈다(T162 · 주인 2026-09-07 «메뉴 드롭다운 팝업에 클로즈 버튼 없애기»).
        /// 조각 원본은 안 고친다(§1 «프리팹은 부품 · 원본 불변») — 세워진 인스턴스에서 끌 뿐이다.
        /// <b>닫는 길은 남아 있다</b> — T139 ⓐ 로 이 팝업은 어둠(<c>Dimmed</c>)을 눌러 닫힌다. 그 인자가 <c>true</c> 인 것과
        /// 이 줄은 한 짝이라, 하나를 되돌리면 «못 닫는 창» 이 된다(<see cref="Open"/> 의 <c>closeOnDim</c> 참조).
        /// </summary>
        static void HideCloseButtons(Transform root)
        {
            if (root == null) return;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t != root && t.name.StartsWith(CloseNamePrefix)) t.gameObject.SetActive(false);
        }

        /// <summary>이름이 같은 직계 자식(깊은 <see cref="UiKit.Find"/> 는 조각 «안»의 같은 이름을 먼저 집는다 — Overlay.Kid 와 같은 까닭).</summary>
        static Transform Kid(Transform t, string name)
        {
            if (t == null) return null;
            for (int i = 0; i < t.childCount; i++) if (t.GetChild(i).name == name) return t.GetChild(i);
            return null;
        }
    }
}
