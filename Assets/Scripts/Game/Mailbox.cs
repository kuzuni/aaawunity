using System;
using System.Collections.Generic;
using KkomaKnight.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 로비 메뉴(≡)의 «우편함»(T96-mail · 주인 2026-09-07 «`Rewards_Mailbox`·`Rewards_Mailbox_Empty` 이거 좀 써라 프리팹들 · 메뉴로 우편함 … 떠야 함»).
    ///
    /// <b>T243(주인 2026-09-08 11:5X)로 범위가 좁아졌다</b> — «우편함으로는 <b>아레나 보상만</b> 오게 하고 나머지는 걍 <b>즉시 지급</b>해 … 우편함은 아레나 보상만.»
    /// 그래서 이 함은 이제 «지금 받을 수 있는 것을 모아 보여 주는 함» 이 아니라 <b>세이브에 든 아레나 우편(<see cref="Core.Mail"/>)을 그대로 보여 주는 함</b>이다.
    /// 걷어낸 탐험·데일리 기프트 줄은 <b>제 팝업(<c>LobbyPopups</c>)에서 그대로 받을 수 있다</b> — 없어진 보상은 하나도 없다(실측 확인).
    /// 넣는 규칙(아레나 갈래만 · 담을 자리가 있는 보상만)은 <see cref="Core.Mail.Add"/> 가 <b>거절로</b> 지킨다.
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

        /// <summary>줄 키 = 우편 하나의 <see cref="MailItem.Id"/> 그대로(줄 이름 = <c>Mail:&lt;id&gt;</c>). 아레나 순위 보상 우편의 id 앞머리.</summary>
        public const string KeyArena = "arenaRank";

        /// <summary>우편함 줄 하나 — 제목·설명·아이콘과 «받기»(지급하고 <b>받은 우편</b>을 돌려준다 · 못 받으면 null).
        /// <para>T241 — 전에는 토스트 문구(글줄)를 돌려줬다. 리워드 팝업은 «칸» 을 그려야 해서 항목이 필요하다(<see cref="Core.Mail.Claim(SaveData, string, out MailItem)"/>).</para></summary>
        public sealed class Entry
        {
            public string Key, Title, Desc, Icon;
            public Func<App, MailItem> Claim;
        }

        /// <summary>보상 이름 → 줄 아이콘(카탈로그 키). 모르는 이름이면 코인 — 아이콘 때문에 우편이 안 뜨는 일은 없게.</summary>
        static string IconOf(MailItem m)
        {
            // 줄 아이콘은 «금화가 아닌 것» 을 앞세운다 — 골드는 거의 모든 우편에 끼어 있어 그것을 고르면 줄마다 같은 그림이 된다.
            foreach (var r in m.Rewards) { var ic = Icon(r.Item); if (ic != "ui.coin") return ic; }
            return "ui.coin";
        }

        /// <summary>
        /// 우편함 줄 목록 — <b>세이브에 든 우편 그대로</b>다(<see cref="Core.Mail"/>).
        /// <para>
        /// ⚠ <b>T243 으로 «지금 받을 수 있는 것을 모아 보여 주던» 함이 아니게 됐다</b>(주인 2026-09-08 11:5X «우편함으로는 <b>아레나 보상만</b> 오게 하고
        /// 나머지는 걍 즉시 지급해 … 우편함은 아레나 보상만»). 그래서 <b>탐험·데일리 기프트 줄을 걷어냈다</b> —
        /// 둘 다 제 팝업(<c>LobbyPopups</c>)에서 <b>여전히 그대로 받을 수 있으므로</b> 없어진 보상은 하나도 없다(실측 확인).
        /// </para>
        /// 아레나 보상이 아직 하나도 안 들어오는 것이 <b>정상</b>이다 — 우편을 넣는 자리(<see cref="Core.Mail.Add"/>)는 순위·시즌 정산이 생길 때 배선한다.
        /// 그때까지 이 함은 «비었음» 조각으로 뜬다.
        /// </summary>
        public static List<Entry> Entries(App app)
        {
            var list = new List<Entry>();
            if (app == null || app.Save == null) return list;
            foreach (var m in Core.Mail.Pending(app.Save))
            {
                string id = m.Id;
                list.Add(new Entry
                {
                    Key = id,
                    Title = string.IsNullOrEmpty(m.Title) ? "아레나 보상" : m.Title,
                    Desc = string.IsNullOrEmpty(m.Desc) ? Core.Mail.Summary(m) : m.Desc,
                    Icon = IconOf(m),
                    Claim = a => { Core.Mail.Claim(a.Save, id, out var got); return got; },
                });
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
            // closeOnDim: true — 어둠을 눌러도 닫힌다(T169 ⓑ · T139 ⓐ 가 만든 인자를 그대로 쓴다).
            // 이 창은 «닫는 길이 하나도 없어» 막혀 있었으므로 닫기 버튼(아래 Buttons)과 어둠 둘 다 넣는다.
            var root = app.Overlay.OpenPrefab(entries.Count > 0 ? "ui.mailbox" : "ui.mailboxEmpty", closeOnDim: true);
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
            HideTabs(rt);
            Buttons(app, rt, entries.Count > 0);
        }

        /// <summary>조각이 물고 오는 <b>탭 담개</b>의 이름(두 프리팹 다 이 이름으로 덮여 있다 · 실측).</summary>
        public const string TabsName = "Tab_02_BoxMenu_Text";

        /// <summary>
        /// T244 — <b>우편함 탭 셋을 끈다</b>(주인 2026-09-08 12:1X «우편에 탭 종류 3개던데 1개로 통합하기 · <b>애초에 탭 필요 x</b>» — 뒤 문장이 이긴다).
        /// <para>
        /// 그 탭은 <b>우리가 만든 것이 아니다</b> — 조각(<c>Rewards_Mailbox</c>·<c>Rewards_Mailbox_Empty</c>)이 중첩 프리팹으로 물고 오고 우리 코드는 한 번도 안 건드렸다.
        /// 즉 <b>눌러도 아무 일 없는 장식</b>이고, T243(«우편함은 아레나 보상만»)으로 나눌 갈래도 하나뿐이 됐다.
        /// </para>
        /// <b>지우지 않고 끈다</b> — 조각 원본은 남의 에셋이라 손대지 않는다(<c>Button_DeleteAll</c> 을 끄는 <see cref="Buttons"/> 와 같은 문법 · 새 꼴 금지).
        /// <para>
        /// ⚠ 이름이 덮여 있을 수 있으므로 이름 하나만 믿지 않는다 — 못 찾으면 «<c>Tab_02</c> 로 시작하는» 것들 중 <b>가장 바깥</b>(담개)을 끈다.
        /// 셋을 따로 끄지 않고 담개 하나를 끄는 까닭은 그래야 <b>자리도 같이 비기</b> 때문이다. 못 찾으면 아무것도 안 한다(창은 그대로 뜬다).
        /// </para>
        /// ⚠ <b>빈 자리는 지어내서 안 채운다</b> — 주인은 «없앤다» 만 말했다(목록을 위로 당길지는 말한 적이 없다 · §1).
        /// </summary>
        static void HideTabs(RectTransform rt)
        {
            var tabs = UiKit.Find(rt, TabsName);
            if (tabs == null)
            {
                // 이름이 덮인 경우 — 가장 얕은(= 가장 바깥) «Tab_02…» 하나가 담개다.
                int best = int.MaxValue;
                foreach (var c in rt.GetComponentsInChildren<Transform>(true))
                {
                    if (c == null || !c.name.StartsWith("Tab_02", StringComparison.Ordinal)) continue;
                    int depth = 0;
                    for (var p = c.parent; p != null && p != rt; p = p.parent) depth++;
                    if (depth < best) { best = depth; tabs = c; }
                }
            }
            if (tabs != null) tabs.gameObject.SetActive(false);
        }

        /// <summary>제목 «Mailbox» → 우리말. 상자 안 첫 글자 조각이 제목이다(영문 데모 글자 0 · T34 ⓒ).</summary>
        static void Title(RectTransform popup)
        {
            foreach (var t in popup.GetComponentsInChildren<TMP_Text>(true))
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

        /// <summary>
        /// 받기 — Core 가 지급하고(세이브에 바로 들어간다) 저장·화면 갱신 뒤 <b>리워드 팝업</b>(T241)을 띄운다.
        /// <para>
        /// 여기는 <b>팝업 안</b>이라 닫으면 우편함을 다시 그린다(<c>onClose</c> · 결정 671) — 화면에서 부르는 자리(챕터 보상)와 다른 점이다(결정 701).
        /// 못 받는 까닭은 <b>토스트 그대로</b> 남긴다 — <b>보상 팝업은 실제 지급에만 뜬다</b>(결정 715 와 같은 규약).
        /// </para>
        /// </summary>
        static void Grant(App app, Entry e)
        {
            var got = e.Claim != null ? e.Claim(app) : null;
            if (got == null) { app.Toast("지금은 받을 수 없습니다"); return; }
            app.Persist(); app.Current?.Refresh();
            Pay(app, got.Rewards);
        }

        /// <summary>
        /// 받은 것을 칸으로 띄운다 — <b>줄에 있던 것과 같은 차례·같은 아이콘</b>(결정 715).
        /// 닫으면 우편함을 다시 그린다(남은 줄로 · 하나도 안 남으면 «비었음» 프리팹).
        /// 칸이 하나도 안 나오면(표가 모르는 이름뿐) 팝업 대신 <b>글줄 토스트</b>로 물러난다 — 받은 것을 조용히 삼키지 않는다.
        /// </summary>
        static void Pay(App app, List<ArenaRankData.Reward> rewards)
        {
            var items = new List<RewardPopup.Item>();
            if (rewards != null)
                foreach (var r in rewards)
                {
                    int n = (int)System.Math.Round(r.Amount);
                    if (n > 0) items.Add(RewardPopup.Item.Of(RewardIcon(r.Item), UiKit.FmtQty(r.Amount), amount: n));
                }
            if (items.Count == 0) { app.Toast("우편을 받았습니다"); Open(app); return; }
            RewardPopup.Show(items, () => Open(app));
        }

        /// <summary>
        /// 보상 이름 → <b>칸 아이콘</b>(줄 아이콘 <see cref="IconOf"/> 는 우편 하나에 하나지만 칸은 보상마다 하나다) —
        /// 둘이 <see cref="Icon"/> 한 곳을 본다(같은 물건이 줄과 칸에서 다르게 생기지 않게).
        /// </summary>
        static string RewardIcon(string item) => Icon(item);

        /// <summary>
        /// 우편이 나를 수 있는 이름 → 그림. <b>덮는 범위는 <see cref="Core.Mail.CanPay"/> 와 같아야 한다</b> —
        /// 그 함수가 «우편함에 들어올 수 있는 것» 을 정하므로, 여기 없는 이름은 <b>조용히 금화로 그려진다</b>.
        /// <para>
        /// T290 에서 레시피를 <c>CanPay</c> 에 더하며 이 자리를 보다가, <b>이미 그렇게 새고 있던 둘</b>을 같이 잡았다 —
        /// 열쇠 3종(T255)과 부활권(T254)이 우편 칸에서 <b>금화 그림</b>으로 뜨고 있었다(둘 다 <c>CanPay</c> 는 통과한다).
        /// 아레나 우편이 그것들을 나르는 날 «금화를 받은 줄 알았는데 열쇠» 가 된다.
        /// </para>
        /// </summary>
        public static string Icon(string item)
        {
            if (item == Core.Mail.ItemGem) return "ui.gemRed";
            if (item == Core.Mail.ItemPetEgg) return "pet.egg";
            if (item == Core.Mail.ItemArenaCoin) return "ui.iconArenaCoin";
            if (item == Core.Mail.ItemRevive) return "ui.iconRevive";                       // T254
            if (item == GachaKeys.Blue) return "ui.iconKeyBlue";                            // T255 — 세 열쇠
            if (item == GachaKeys.Purple) return "ui.iconKeyPurple";
            if (item == GachaKeys.Yellow) return "ui.iconKeyGold";
            if (Recipes.IsRecipe(item)) return Recipes.Icon(Recipes.PartOf(item));          // T290 — 부위별 레시피(여섯이 같은 그림)
            return "ui.coin";
        }

        /// <summary>조각이 들고 오는 닫기 버튼의 이름 — 인스턴스는 <c>Button_Close_01</c> 로 이름이 덮여 있고 원본 조각은 <c>Button_Close_Square_01</c> 다(둘 다 찾는다 · Profile 과 같은 꼴).</summary>
        public const string CloseName = "Button_Close_01";

        /// <summary>프리팹의 아래 버튼 둘 — «Claim All» 은 «전체 받기» 로, «Delete All»(편지 삭제)은 우리 우편함에 뜻이 없어 끈다. 오른쪽 위 <b>닫기</b>는 조각이 들고 오는데 여태 배선이 없었다(T169).</summary>
        static void Buttons(App app, RectTransform rt, bool anyRow)
        {
            // T169 ⓐ — 조각(Rewards_Mailbox·Rewards_Mailbox_Empty 둘 다)이 Button_Close_01 을 들고 오는데
            // 여태 아무도 배선하지 않아 «눌러도 아무 일이 없는» 버튼이었다. 그래서 이 창은 닫을 길이 하나도 없었다.
            var close = UiKit.FindAny(rt, CloseName, "Button_Close_Square_01");
            if (close != null) UiKit.Clickable(close, () => app.Overlay.Close());

            var del = UiKit.Find(rt, "Button_DeleteAll");
            if (del != null) del.gameObject.SetActive(false);
            var all = UiKit.Find(rt, "Button_ClaimAll");
            if (all == null) return;
            all.gameObject.SetActive(anyRow);
            if (!anyRow) return;
            all.name = ClaimAllName;
            var label = all.GetComponentInChildren<TMP_Text>(true);
            if (label != null) UiKit.SetText(label.transform, "", "전체 받기", kind: TextKind.Button);
            UiKit.Clickable(all, () => GrantAll(app));
        }

        /// <summary>전체 받기 — 지금 목록에 있는 줄을 위에서부터 전부 받는다(광고 줄은 애초에 목록에 없다).</summary>
        static void GrantAll(App app)
        {
            var list = Entries(app);
            int n = 0;
            var got = new List<ArenaRankData.Reward>();
            foreach (var e in list)
            {
                if (e.Claim == null) continue;
                var m = e.Claim(app);
                if (m == null) continue;
                n++; if (m.Rewards != null) got.AddRange(m.Rewards);
            }
            if (n == 0) { app.Toast("지금은 받을 수 없습니다"); return; }
            app.Persist(); app.Current?.Refresh();
            // T241 — 여러 건을 한 번에 받아도 팝업은 하나다(칸은 우편 차례 그대로 이어 붙는다).
            Pay(app, got);
        }
    }
}
