using System;
using System.Collections.Generic;
using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 로비 메뉴(≡)의 «우편함»(T96-mail · 주인 2026-09-07 «`Rewards_Mailbox`·`Rewards_Mailbox_Empty` 이거 좀 써라 프리팹들 · 메뉴로 우편함 … 떠야 함»).
    ///
    /// 우편 데이터는 아직 없으므로 지시서(§2 T96-mail)대로 <b>«지금 받을 수 있는 것» 을 모아 보여 주는 함</b>이다 —
    /// 줄은 <see cref="Entries"/> 가 만들고, 그 판정과 지급은 전부 <see cref="Core.Expedition"/>·<see cref="Core.DailyGift"/> 의 <b>기존 순수 함수</b>를 그대로 부른다
    /// (우편함이 규칙을 다시 구현하지 않는다 = 이중 지급·규칙 갈림 없음). <b>광고를 봐야 받는 것</b>(데일리 기프트 광고 줄 · 빠른 탐험)은
    /// 광고 흐름이 그 팝업에 있으므로 <b>우편함에 넣지 않는다</b> — 우편함은 «바로 받을 수 있는 것» 만 담는다(결정 기록).
    ///
    /// 화면은 프리팹 <b>그대로</b>: 받을 것이 있으면 <c>ui.mailbox</c>(<c>Rewards_Mailbox</c>) · 하나도 없으면 <c>ui.mailboxEmpty</c>(<c>Rewards_Mailbox_Empty</c>).
    /// 줄은 프리팹 안 <c>ListItem_Mailbox</c> 조각을 «부품» 으로 복제해 쓰고(크기·여백은 프리팹의 레이아웃 그대로), 글자만 우리말로 바꾼다.
    /// 이름 계약(테스트): 줄 = <c>Mail:&lt;키&gt;</c> · 전체 받기 = <c>ClaimAllBtn</c>.
    /// </summary>
    public static class Mailbox
    {
        /// <summary>줄 오브젝트 이름 앞머리(테스트·이름표가 찾는다).</summary>
        public const string RowPrefix = "Mail:";
        /// <summary>«전체 받기» 버튼 이름(고정).</summary>
        public const string ClaimAllName = "ClaimAllBtn";
        /// <summary>줄 조각(데모 프리팹의 우편 한 줄).</summary>
        public const string RowPiece = "ListItem_Mailbox";

        public const string KeyExpedition = "expedition", KeyGiftFree = "giftFree", KeyGift = "gift";

        /// <summary>우편함 줄 하나 — 제목·설명·아이콘과 «받기»(지급하고 토스트 문구를 돌려준다 · 못 받으면 null).</summary>
        public sealed class Entry
        {
            public string Key, Title, Desc, Icon;
            public Func<App, string> Claim;
        }

        static double NowSec() => (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

        /// <summary>
        /// 지금 «바로 받을 수 있는 것» 줄 목록(순서 = 탐험 → 데일리 기프트 무료 → 데일리 기프트 줄).
        /// 판정은 Core 의 <c>CanClaim</c>/<c>CanFree</c> 를 그대로 쓴다 — 화면이 따로 세지 않는다.
        /// </summary>
        public static List<Entry> Entries(App app)
        {
            var list = new List<Entry>();
            if (app == null || app.Data == null || app.Save == null) return list;
            var G = app.Data; var S = app.Save; string today = SaveStore.Today();

            if (G.Expedition != null && Expedition.CanClaim(G, S, G.Expedition, NowSec(), today))
            {
                Expedition.Pending(G, S, G.Expedition, NowSec(), today, out double gold, out double gem);
                list.Add(new Entry
                {
                    Key = KeyExpedition,
                    Title = "탐험 보상",
                    Desc = "골드 " + UiKit.Fmt(gold) + " · 다이아 " + UiKit.FmtQty(gem),
                    Icon = "ui.coin",
                    Claim = a =>
                    {
                        Expedition.Claim(a.Data, a.Save, a.Data.Expedition, NowSec(), SaveStore.Today(), out double g2, out double m2);
                        if (g2 <= 0 && m2 <= 0) return null;
                        return "골드 +" + UiKit.Fmt(g2) + " · 다이아 +" + UiKit.FmtQty(m2);
                    },
                });
            }

            var D = G.DailyGift;
            if (D != null && DailyGift.CanFree(S, D, today))
            {
                list.Add(new Entry
                {
                    Key = KeyGiftFree,
                    Title = "데일리 기프트",
                    Desc = "무료 다이아 " + UiKit.FmtQty(D.FreeGem),
                    Icon = "ui.gemRed",
                    Claim = a =>
                    {
                        double g = DailyGift.ClaimFree(a.Save, a.Data.DailyGift, SaveStore.Today());
                        return g > 0 ? "다이아 " + UiKit.FmtQty(g) + " 수령!" : null;
                    },
                });
            }
            if (D != null)
            {
                for (int i = 0; i < D.Milestones.Count; i++)
                {
                    if (!DailyGift.CanClaim(S, D, i, today)) continue;
                    int idx = i;
                    list.Add(new Entry
                    {
                        Key = KeyGift + idx,
                        Title = "데일리 기프트",
                        Desc = "광고 " + D.Milestones[idx].Ads + "회 보상 · 다이아 " + UiKit.FmtQty(D.Milestones[idx].Gem),
                        Icon = "ui.gemRed",
                        Claim = a =>
                        {
                            double g = DailyGift.Claim(a.Save, a.Data.DailyGift, idx, SaveStore.Today());
                            return g > 0 ? "다이아 " + UiKit.FmtQty(g) + " 수령!" : null;
                        },
                    });
                }
            }
            return list;
        }

        /// <summary>우편함에 지금 받을 것이 있는가(빨간 점·Empty 프리팹 판정 · <see cref="Notify"/> 와 같은 판정을 화면 쪽에서 쓰는 자리).</summary>
        public static bool Any(App app) => Entries(app).Count > 0;

        /// <summary>우편함을 연다 — 받을 것이 있으면 <c>ui.mailbox</c>, 없으면 <c>ui.mailboxEmpty</c>(둘 다 주인이 지목한 데모 프리팹 그대로).</summary>
        public static void Open(App app)
        {
            if (app == null) return;
            var entries = Entries(app);
            var root = app.Overlay.OpenPrefab(entries.Count > 0 ? "ui.mailbox" : "ui.mailboxEmpty");
            var rt = (RectTransform)root.transform;
            var popup = UiKit.Find(rt, "Popup") as RectTransform;
            if (popup == null) return;                                   // 조각 구성이 바뀌면 조용히 빈 어둠(빨간 줄 0)
            UiKit.Tag(popup, "우편함 상자");
            Title(popup);

            var content = UiKit.Find(rt, "Content") as RectTransform;
            var rows = new List<RectTransform>();
            if (content != null)
            {
                // 프리팹 줄을 부품으로 — 첫 줄을 본으로 두고 필요한 만큼 복제, 남는 줄은 끈다(크기·여백은 프리팹 레이아웃 그대로)
                for (int i = 0; i < content.childCount; i++)
                {
                    var c = content.GetChild(i) as RectTransform;
                    if (c != null && c.name.StartsWith(RowPiece, StringComparison.Ordinal)) rows.Add(c);
                }
                while (rows.Count > 0 && rows.Count < entries.Count)
                {
                    var copy = UnityEngine.Object.Instantiate(rows[0].gameObject, content);
                    var crt = (RectTransform)copy.transform; crt.localScale = Vector3.one; rows.Add(crt);
                }
                for (int i = 0; i < rows.Count; i++)
                {
                    bool on = i < entries.Count;
                    rows[i].gameObject.SetActive(on);
                    if (on) Row(app, rows[i], entries[i]);
                }
            }
            // «비었음» 그림은 줄이 없을 때만(프리팹이 둘 다 들고 있다)
            UiKit.Show(rt, "Empty", entries.Count == 0);
            Buttons(app, rt, entries.Count > 0);
        }

        /// <summary>제목 «Mailbox» → 우리말. 상자 안 첫 글자 조각이 제목이다(영문 데모 글자 0 · T34 ⓒ).</summary>
        static void Title(RectTransform popup)
        {
            foreach (var t in popup.GetComponentsInChildren<Text>(true))
            {
                if (t == null || t.text != "Mailbox") continue;
                UiKit.SetText(t.transform, "", "우편함", kind: TextKind.Title);   // 빈 경로 = 이 글자 자신
                return;
            }
        }

        /// <summary>줄 하나 — 제목·설명·아이콘을 우리 것으로 바꾸고 <b>줄 자체를 «받기» 버튼으로</b> 만든다(조각에 버튼이 없다 · 결정 318). 타이머(만료)는 우리 우편에 없으니 끈다.</summary>
        static void Row(App app, RectTransform row, Entry e)
        {
            row.name = RowPrefix + e.Key;
            UiKit.SetText(row, "Text (TMP)", e.Title, kind: TextKind.Body);
            UiKit.SetText(row, "Text_Description", e.Desc, kind: TextKind.Aux);
            UiKit.Show(row, "Timer", false);
            var icon = UiKit.Find(row, "Icon");
            if (icon != null) UiKit.SetSprite(icon.parent != null ? icon.parent : row, "Icon", e.Icon, Palette.White);
            // 줄 조각(ListItem_Mailbox)에는 «받기» 버튼이 아예 없다 — 데모 줄은 제목·설명·아이콘·도장·타이머뿐이다(프리팹 실측).
            // 그래서 예전 코드는 GetComponentInChildren<Button>() 이 null 이라 조용히 돌아가 버렸고 줄이 눌리지 않았다(CI #225 빨강 · 결정 318).
            // 조각에 없는 버튼을 새로 그리지 않고 **줄 자체를 «받기» 버튼으로** 삼는다(UiKit.Clickable 이 Button 을 Ensure 하고 눌림 표시까지 붙인다).
            // 줄 안의 글자를 «받기» 로 덮어쓰지 않는다 — 첫 Text 는 우편 제목이라 덮으면 제목이 사라진다.
            // 줄 오브젝트의 이름(Mail:<키>)은 그대로 둔다 — 테스트·다음 갱신이 그 이름으로 줄을 찾는다.
            // 이름표(UiKit.Tag)도 달지 않는다 — 이름표가 붙으면 BorderAudit 이 «칸» 으로 세어 테두리를 요구한다(T69).
            UiKit.Clickable(row, () => Grant(app, e));
        }

        /// <summary>받기 — Core 가 지급하고(세이브에 바로 들어간다) 저장·토스트·화면 갱신 뒤 우편함을 다시 그린다.</summary>
        static void Grant(App app, Entry e)
        {
            string msg = e.Claim != null ? e.Claim(app) : null;
            if (string.IsNullOrEmpty(msg)) { app.Toast("지금은 받을 수 없습니다"); return; }
            app.Persist(); app.Current?.Refresh(); app.Toast(msg);
            Open(app);   // 남은 줄로 다시 그린다(하나도 안 남으면 «비었음» 프리팹)
        }

        /// <summary>프리팹의 아래 버튼 둘 — «Claim All» 은 «전체 받기» 로, «Delete All»(편지 삭제)은 우리 우편함에 뜻이 없어 끈다.</summary>
        static void Buttons(App app, RectTransform rt, bool anyRow)
        {
            var del = UiKit.Find(rt, "Button_DeleteAll");
            if (del != null) del.gameObject.SetActive(false);
            var all = UiKit.Find(rt, "Button_ClaimAll");
            if (all == null) return;
            all.gameObject.SetActive(anyRow);
            if (!anyRow) return;
            all.name = ClaimAllName;
            var label = all.GetComponentInChildren<Text>(true);
            if (label != null) UiKit.SetText(label.transform, "", "전체 받기", kind: TextKind.Button);
            UiKit.Clickable(all, () => GrantAll(app));
        }

        /// <summary>전체 받기 — 지금 목록에 있는 줄을 위에서부터 전부 받는다(광고 줄은 애초에 목록에 없다).</summary>
        static void GrantAll(App app)
        {
            var list = Entries(app);
            int n = 0; double before = app.Save.Gem + app.Save.Gold;
            foreach (var e in list) { if (e.Claim != null && !string.IsNullOrEmpty(e.Claim(app))) n++; }
            if (n == 0) { app.Toast("지금은 받을 수 없습니다"); return; }
            app.Persist(); app.Current?.Refresh();
            app.Toast("우편 " + n + "건 수령! (합계 +" + UiKit.Fmt(app.Save.Gem + app.Save.Gold - before) + ")");
            Open(app);
        }
    }
}
