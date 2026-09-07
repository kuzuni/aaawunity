using System;
using System.Collections.Generic;
using KkomaKnight.Core;
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
        /// <c>anchoredPosition</c> <b>(−323, −558)</b> · 가로 <b>382.62</b>. 세로는 지금 코드가 «줄 수 ÷ 프리팹 줄 수» 로 만드는 값과
        /// 주인 값(688.305 = 프리팹 458.87 × 6/4)이 <b>같아서</b> 계산을 그대로 둔다 — 즉 주인이 바꾼 것은 «자리» 다.
        /// <para>⚠ 항목 수가 바뀌면(예 <b>T148</b> 이 넷을 로비로 도로 꺼내 메뉴가 둘이 되면) 세로가 458.87 로 줄어 판이 위로 짧아진다.
        /// 그때는 «가운데 피벗 + 고정 Pos» 라 판이 ≡ 버튼에서 떨어져 보이므로, 그 작업을 잡는 워커가 <see cref="PanelPos"/> 의 y 를 다시 잰다(높이 절반만큼 올린다).</para>
        /// </summary>
        public static readonly Vector2 PanelPos = new Vector2(-323f, -558f);
        /// <summary>같은 스크린샷의 판 가로(px) — 프리팹 값과 같으면 그대로다.</summary>
        public const float PanelWidth = 382.62f;

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
            var panel = UiKit.Find(rt, PanelName) as RectTransform;
            if (panel == null) return;                                  // 프리팹이 없으면(카탈로그 결손) 조용히 빈 어둠 — 빨간 줄 0
            UiKit.Tag(panel, "메뉴 판");

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
            // 높이는 위 계산값을 그대로 쓴다 — 주인 값 688.305 와 같다(주석 PanelPos 참조).
            panel.anchorMin = panel.anchorMax = Vector2.one;
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(PanelWidth, panel.sizeDelta.y);
            panel.anchoredPosition = PanelPos;

            for (int i = 0; i < items.Count; i++) Row(app, rows[i], i, items[i]);
            for (int i = items.Count; i < rows.Count; i++) rows[i].gameObject.SetActive(false);
            UiKit.TagGroup(panel, "메뉴 항목 줄", rows.ToArray());
        }

        /// <summary>메뉴 항목 표 — 순서·라벨·아이콘(새 줄만)·누르면 할 일 · 점을 켤 조건.</summary>
        static List<(string key, string label, string icon, Action open, Func<bool> dot)> Items(App app)
        {
            var G = app.Data; var S = app.Save;
            Func<bool> giftDot = () => G != null && S != null
                && (Notify.DailyGiftClaimable(S, G.DailyGift, SaveStore.Today()) || Notify.DailyGiftAd(S, G.DailyGift, SaveStore.Today()));
            return new List<(string, string, string, Action, Func<bool>)>
            {
                (ItemMail, "우편함", null, () => Game.Mailbox.Open(app), () => Mailbox.Any(app)),   // T96-mail
                (ItemSettings, "설정", null, () => app.Overlay.Settings(), () => false),
                (ItemDailyGift, "데일리 기프트", null, () => LobbyPopups.DailyGift(app), giftDot),
                (ItemQuest, "퀘스트", null, () => LobbyPopups.Quest(app), () => false),
                (ItemAttendance, "출석", "ui.iconCalendar", () => LobbyPopups.Attendance(app), () => false),
                (ItemPrivilege, "특권", "ui.iconCrown", () => app.ShowScreen("privilege"), () => false),
            };
        }

        /// <summary>줄 하나 배선 — 이름 · 라벨 · (새 줄이면) 아이콘 · 클릭 · 빨간 점. 자리·크기는 손대지 않는다(프리팹 그대로).</summary>
        static void Row(App app, RectTransform row, int index, (string key, string label, string icon, Action open, Func<bool> dot) it)
        {
            row.name = "Menu:" + it.key;
            row.SetSiblingIndex(index);                                  // 세로 레이아웃 순서 = 주인이 부른 순서
            row.gameObject.SetActive(true);
            // 글자 — 조각이 달고 온 Text 하나(TMP 는 UiKit.Adopt 가 이미 uGUI 로 바꿔 놨다)
            var label = row.GetComponentInChildren<Text>(true);
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

        /// <summary>이름이 같은 직계 자식(깊은 <see cref="UiKit.Find"/> 는 조각 «안»의 같은 이름을 먼저 집는다 — Overlay.Kid 와 같은 까닭).</summary>
        static Transform Kid(Transform t, string name)
        {
            if (t == null) return null;
            for (int i = 0; i < t.childCount; i++) if (t.GetChild(i).name == name) return t.GetChild(i);
            return null;
        }
    }
}
