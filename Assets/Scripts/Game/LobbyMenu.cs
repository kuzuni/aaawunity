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

        /// <summary>메뉴 항목 — 이름(줄 오브젝트 «Menu:key») · 라벨 · 새 줄이면 아이콘 키.</summary>
        public const string ItemMail = "mail", ItemSettings = "settings", ItemDailyGift = "dailyGift", ItemQuest = "quest", ItemAttendance = "attendance", ItemPrivilege = "privilege";

        /// <summary>우편함(T96-mail) 이 붙기 전까지의 자리 — 그 워커가 이 훅만 바꾸면 된다.</summary>
        public static Action<App> Mailbox;

        /// <summary>메뉴를 연다 — 로비의 ≡ 버튼이 부른다(전에는 설정 팝업을 바로 열었다).</summary>
        public static void Open(App app)
        {
            if (app == null) return;
            var root = app.Overlay.OpenPrefab("ui.lobbyMenu");
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
                (ItemMail, "우편함", null, () => { if (Mailbox != null) Mailbox(app); else app.Toast("우편함은 준비 중입니다"); }, () => false),
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
                var go = UiKit.Spawn("ui.alertDot", row); go.name = "AlertDot"; dot = go.transform;
                UiKit.Pct((RectTransform)dot, 12, 8, 9, 34);   // 아이콘(프리팹 x 58 · 128px) 오른쪽 위
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
