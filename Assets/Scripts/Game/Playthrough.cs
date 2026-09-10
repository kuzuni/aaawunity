using System;
using KkomaKnight.Core;

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

        /// <summary>
        /// 같은 줄 <b>뒤에 «몇 초 놀았는지» 를 붙인다</b>(T406) — <c>[KkomaKnight] play P1 ok 1.3s</c>.
        /// <para>
        /// ⚑ <b>왜</b>: T300 1항이 봇에 «5분 이내» 예산을 걸어 뒀고 결정 1148·1165 가 «판을 굴리는 단계(P5·P6)는 그 예산을 <b>재 보고</b> 붙여라» 고 두 번 적었는데,
        /// <b>재는 길이 없었다</b> — 줄이 «ok» 만 말하니 어느 단계가 몇 초 먹는지 아무 데도 안 남는다. 그러면 다음 사람도 <b>짐작</b>으로 차례를 정한다.
        /// </para>
        /// <para>
        /// ⚠ <b>초는 <see cref="System.Globalization.CultureInfo.InvariantCulture"/> 로 찍는다</b> — 러너의 문화권이 «1,3s» 를 쓰면
        /// <c>webgl_smoke.js</c> 의 수 읽기가 <b>조용히</b> 어긋난다(아무 자도 안 운다). 오늘 결정 1162 ⑤ 가 옆 파일에서 같은 노출을 미리 막은 그 까닭이다.
        /// </para>
        /// <para>⚠ <b>꼬리를 붙이는 것이지 꼴을 바꾸는 것이 아니다</b> — 스모크의 정규식은 앵커가 없으므로(결정 1065) 옛 줄도 새 줄도 같이 읽힌다.</para>
        /// </summary>
        public static string Secs(double s) => " " + s.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + "s";
        // ─────────────────────────────────────────────────────────────────────────────
        // 배포 빌드에서 실제로 «노는» 쪽(T300 2항) — `App.DebugGo("play")` 가 이것을 돌린다.
        // ─────────────────────────────────────────────────────────────────────────────
        /// <summary>한 단계가 «게임 안에서» 하는 일. 없는 단계는 아직 아무도 안 썼다는 뜻이다(등록 안 함).</summary>
        public delegate System.Collections.IEnumerator Step(App app);

        static readonly System.Collections.Generic.Dictionary<string, Step> Steps =
            new System.Collections.Generic.Dictionary<string, Step> { { "P1", P1Lobby }, { "P2", P2Battle }, { "P3", P3Gear }, { "P4", P4Shop }, { "P6", P6Arena }, { "P7", P7Quest }, { "P8", P8Boxes }, { "P9", P9Expedition }, { "P10", P10Pet } };

        /// <summary>
        /// 단계 하나만 돌린다 — <b>자가 «배포 갈래도 실제로 도는가» 를 재는 입구</b>(T300 2항 · 결정 1101).
        /// <para>
        /// ⚑ <b>왜 필요한가</b>: 배포 갈래는 <c>webgl_smoke</c> 안에서만 돌아서, 낡으면 <b>CI 는 초록인 채</b>로 배포 로그에만 fail 이 뜬다.
        /// 그 로그는 «마지막 초록 커밋» 이 있어야 나오므로 main 이 빨간 동안은 몇 시간이고 안 나온다(2026-09-10 새벽에 실제로 그랬다).
        /// ⇒ 같은 각본을 PlayMode 자가 한 번 돌려 주면 <b>낡음이 CI 한 회전 안에서 잡힌다</b>.
        /// </para>
        /// <para>⚠ <see cref="Run"/> 과 달리 <b>예외를 안 삼킨다</b> — 자에서는 «못 찾았다: Background» 같은 줄이 그대로 빨강이 되는 것이 맞다.</para>
        /// </summary>
        public static System.Collections.IEnumerator RunOne(App app, string id)
        {
            Step step;
            if (!Steps.TryGetValue(id, out step)) throw new MissingException("배포 갈래 " + id + "(등록 안 됨)");
            yield return step(app);
        }

        /// <summary>이 단계가 게임 안에서 놀 수 있는가(= 누가 각본을 붙였는가).</summary>
        public static bool HasStep(string id) => Steps.ContainsKey(id);

        /// <summary>
        /// P1 로비 — 탭 다섯을 왕복하고 챕터 ◀▶ 를 눌러 본다. <b>단언은 하나도 없다</b>(3항 ⓐ):
        /// 봇은 «죽지 않고 지나가는가» 만 본다. 못 찾은 자리는 <see cref="MissingException"/> 로 알린다.
        /// </summary>
        static System.Collections.IEnumerator P1Lobby(App app)
        {
            app.ShowScreen("lobby");
            yield return null; yield return null;
            Need(app, "Start"); Need(app, "ChapterCard");
            foreach (var key in NavBar.Keys)
            {
                Tap(app, "Tab:" + key);
                yield return null; yield return null;
            }
            app.ShowScreen("lobby");
            yield return null;
            for (int i = 0; i < 2; i++) { Tap(app, "ArrowR"); yield return null; }
            for (int i = 0; i < 3; i++) { Tap(app, "ArrowL"); yield return null; }
        }

        /// <summary>
        /// P3 장비(T300 1항 · 배포 갈래 · T398) — PlayMode <c>PlaythroughTests.P3_…</c> 와 <b>같은 길</b>을 게임 안에서 누른다. 단언은 없다(3항 ⓐ):
        /// 탭 «장비» → 인벤 첫 칸 → 세부 팝업 «장착»(<c>BtnL</c>) → 슬롯 칸 → «슬롯 강화»(<c>BtnR</c>) → 어둠으로 닫기 →
        /// 여섯 슬롯에서 «해제» 를 찾아 누르기 → «대장간» → 재료 셋을 골라 «합성 (3/3)» → «뒤로» → 로비.
        /// <para>⚠ <b>«해제» 는 글자로 집는다</b> — 그 버튼은 «장착» 과 <b>이름이 같다</b>(<c>BtnL</c> · <see cref="GearUi.OpenDetail"/> 이 한 자리에 둘 중 하나를 세운다).
        /// 게다가 어느 슬롯이 어느 부위인지는 표(<c>D.Gear.Parts</c>)가 정하고 주인 지시로 바뀌므로 <b>자리로 굳히지 않는다</b>(결정 956 이 자 쪽에 세운 그 규칙).</para>
        /// <para>⚠ <b>세이브(인벤·골드·레시피)는 봇이 제 조건을 만든다</b>(1항) — 값은 표(<c>AllTypes</c>·<c>Parts</c>)에서 읽는다. 스모크의 브라우저는 일회용이라 누구의 세이브도 아니다.</para>
        /// <para>⚠ <b>«레시피가 모자라면 안 오른다» 갈래는 안 논다</b> — 그것은 «규칙이 맞는가» 라 <c>PlaythroughTests.P3_…</c> ⓒ 와 <c>GearSystem</c> 자의 몫이다(3항 ⓐ).</para>
        /// </summary>
        static System.Collections.IEnumerator P3Gear(App app)
        {
            var D = app.Data; var S = app.Save;
            if (D == null || D.Gear == null || D.Gear.AllTypes.Count == 0) throw new MissingException("장비 표(data.gear)");
            // 조건은 이 단계가 만든다 — 인벤을 비우고 «같은 종류 넷»(셋은 합성거리 · 하나는 입어 볼 것) · 골드 · 레시피.
            S.Inv.Clear(); S.Eq.Clear();
            var t0 = D.Gear.AllTypes[0];
            for (int i = 0; i < 4; i++) S.Inv.Add(S.NewGear(t0.Part, t0.Type, 0, 0));
            S.Gold += 1e9;
            foreach (var pt in D.Gear.Parts) Recipes.Add(S, pt, 999);
            app.Persist();

            app.ShowScreen("lobby"); yield return Frames(2);
            Tap(app, "Tab:gear"); yield return Frames(3);
            Reach(app, "gear");

            // ⓐ 인벤 첫 칸 → 세부 팝업 → «장착»(팝업은 스스로 닫힌다)
            TapChild(app.Current.Root, "Content", 0, "인벤 첫 칸"); yield return Frames(2);
            if (!app.Overlay.IsOpen) throw new MissingException("장비 세부 팝업(인벤 칸 뒤)");
            TapIn(app.Overlay.Root, "BtnL", true); yield return Frames(3);
            if (app.Overlay.IsOpen) throw new MissingException("«장착» 뒤 팝업 닫힘(아직 열려 있다)");

            // ⓑ 슬롯 칸 → «슬롯 강화». 강화는 팝업을 제 손으로 다시 연다(GearUi.OpenDetail/OpenSlot) → 어둠으로 닫는다.
            //    MAX 면 그 버튼이 꺼져 있다 — 없는 것을 «눌렀다» 고 적지 않고 지나간다(P10 ⓒ 와 같은 결).
            var slots = Slots(app); if (slots.Count == 0) throw new MissingException("슬롯 칸(이름 계약 Group_Slot/Slot:<부위>)");
            TapIn(app.Current.Root, slots[0], true); yield return Frames(2);
            if (!app.Overlay.IsOpen) throw new MissingException("슬롯 팝업(슬롯 칸 뒤)");
            var up = UiKit.Find(app.Overlay.Root, "BtnR"); if (up == null) throw new MissingException("«슬롯 강화» 버튼");
            var upBtn = up.GetComponent<UnityEngine.UI.Button>();
            if (upBtn != null && upBtn.interactable) { upBtn.onClick.Invoke(); yield return Frames(3); }
            if (app.Overlay.IsOpen) yield return CloseByDim(app, "슬롯 팝업");

            // ⓒ 해제 — 여섯 칸을 돌며 «해제» 글자를 찾는다(자리로도 이름으로도 못 집는다 · 위 ⚠)
            bool off = false;
            for (int i = 0; i < 6 && !off; i++)
            {
                var six = Slots(app); if (i >= six.Count) break;
                TapIn(app.Current.Root, six[i], true); yield return Frames(2);
                if (!app.Overlay.IsOpen) continue;
                UiKit.CompleteAllTweens(); yield return Frames(1);
                if (TapLabel(app.Overlay.Root, "해제")) { yield return Frames(3); off = true; }
                else yield return CloseByDim(app, "슬롯 팝업(" + six[i] + ")");
            }
            if (!off) throw new MissingException("여섯 슬롯 어디에서도 «해제» — 장착한 것이 슬롯에 안 닿았다");
            if (app.Overlay.IsOpen) yield return CloseByDim(app, "해제 뒤 팝업");

            // ⓓ 대장간 — 재료 셋을 «골라» 합성한다(«자동»(AutoBtn)은 한 번에 다 태워서 «3 → 1» 을 안 논다 · 결정 956)
            Reach(app, "gear");
            Tap(app, "ForgeBtn"); yield return Frames(3);
            Reach(app, "forge");
            for (int i = 0; i < 3; i++) { TapChild(app.Current.Root, "Content", i, "대장간 재료 " + (i + 1)); yield return Frames(2); }
            TapIn(app.Current.Root, "FuseBtnOn", true); yield return Frames(3);
            Tap(app, "BackBtn"); yield return Frames(3);
            Reach(app, "gear");
            app.ShowScreen("lobby"); yield return null;
        }

        /// <summary>슬롯 칸 이름들(<c>Group_Slot</c> 의 <c>Slot:&lt;부위&gt;</c>) — <b>이름 계약으로 센다</b>(T348: 그 묶음에는 비평 이름표 <c>Tag:…</c> 도 자식으로 붙어 있어 <c>childCount</c> 는 6 이 아니다).</summary>
        static System.Collections.Generic.List<string> Slots(App app)
        {
            var list = new System.Collections.Generic.List<string>();
            var g = app.Current != null ? UiKit.Find(app.Current.Root, "Group_Slot") : null;
            if (g == null) return list;
            for (int i = 0; i < g.childCount; i++)
            {
                var n = g.GetChild(i).name;
                if (n.StartsWith("Slot:", StringComparison.Ordinal)) list.Add(n);
            }
            return list;
        }

        /// <summary>격자(<c>Content</c> 류)의 <paramref name="index"/> 번째 칸을 누른다 — 칸 이름이 없는 격자(인벤·대장간)를 집는 유일한 길이다.</summary>
        static void TapChild(UnityEngine.Transform root, string name, int index, string what)
        {
            var grid = root != null ? UiKit.Find(root, name) : null;
            if (grid == null) throw new MissingException(name);
            if (grid.childCount <= index) throw new MissingException(what + "(" + name + " 에 " + (index + 1) + "번째 칸이 없다)");
            var b = grid.GetChild(index).GetComponentInChildren<UnityEngine.UI.Button>();
            if (b == null || !b.interactable) throw new MissingException(what + "(눌리지 않는다)");
            b.onClick.Invoke();
        }

        /// <summary>
        /// <b>글자로</b> 버튼을 집는다 — 이름이 같은 자리에 글자만 갈리는 버튼(«장착»↔«해제» 가 둘 다 <c>BtnL</c>)이 있을 때만 쓴다.
        /// <para>못 찾으면 던지지 않고 <c>false</c> 를 준다 — 부르는 쪽이 «여섯 칸 가운데 하나» 처럼 <b>돌면서</b> 찾기 때문이다.</para>
        /// </summary>
        static bool TapLabel(UnityEngine.Transform root, string text)
        {
            if (root == null) return false;
            foreach (var b in root.GetComponentsInChildren<UnityEngine.UI.Button>(false))
            {
                if (!b.interactable) continue;
                foreach (var t in b.GetComponentsInChildren<TMPro.TMP_Text>(false))
                    if ((t.text ?? "") == text) { b.onClick.Invoke(); return true; }
            }
            return false;
        }

        /// <summary>
        /// P4 상점(T300 1항 · 배포 갈래) — PlayMode <c>PlaythroughTests.P4_…</c> 와 <b>같은 길</b>을 게임 안에서 누른다. 단언은 없다(3항 ⓐ):
        /// 탭 «상점» → 큰 상자 다이아 1회·10회(결과 창은 배경 탭으로) → 열쇠 «캡+7» 을 쥐고 탭 왕복으로 다시 열어 10회 자리·1회 자리 열쇠 옷 →
        /// 무료 보급 다이아·골드(표에 있을 때만) → 상자 ⓘ → 확률 칸 → 아이템 세부 → 어둠 탭(확률로 돌아옴) → 어둠 탭(닫힘).
        /// <para>⚠ 겹친 두 옷(<c>One</c>/<c>OneKey</c> · <c>Ten</c>/<c>TenKey</c>) 중 <b>켜진</b> 것만 누른다(<see cref="TapIn"/> · 결정 941 ①) —
        /// 꺼진 버튼도 <c>onClick.Invoke</c> 는 돌아서, 안 가리면 봇이 사람 눈에 없는 버튼을 누르고 «ok» 를 찍는다.</para>
        /// <para>⚠ 세이브(다이아·열쇠)는 봇이 제 조건을 만든다(1항) — 스모크의 브라우저는 일회용이라 누구의 세이브도 아니다.</para>
        /// </summary>
        static System.Collections.IEnumerator P4Shop(App app)
        {
            var D = app.Data; var S = app.Save;
            var big = ShopScreen.BigBox(D); if (big == null) throw new MissingException("큰 상자(가장 비싼 상자)");
            string keyItem = GachaKeys.KeyOf(big.Key); if (keyItem == null) throw new MissingException("큰 상자를 여는 열쇠");
            int cap = D.Gacha.TenPullCount;
            // 표 값으로 «1회 + 캡 회 + 여유» 만큼 — 수를 안 박는다. 열쇠는 아직 0(다이아 옷부터 논다).
            S.Gem = big.Cost * (cap + 1) * 2; app.Persist();

            app.ShowScreen("lobby"); yield return Frames(2);
            Tap(app, "Tab:shop"); yield return Frames(3);
            Reach(app, "shop");
            var card = Need(app, "Box:" + big.Key);
            TapIn(card, "One", true); yield return CloseChest(app, "다이아 1회");
            TapIn(card, "Ten", true); yield return CloseChest(app, "다이아 " + cap + "회");

            // 열쇠 «캡+7»(주인 예의 17) — 옷은 Refresh 가 갈아입히므로 사람이 하듯 화면을 다시 연다(탭 왕복)
            GachaKeys.Add(S, keyItem, cap + 7); app.Persist();
            Tap(app, "Tab:battle"); yield return Frames(2);
            Tap(app, "Tab:shop"); yield return Frames(3);
            Reach(app, "shop");
            card = Need(app, "Box:" + big.Key);
            TapIn(card, "TenKey", true); yield return CloseChest(app, "열쇠 " + (cap + 7) + "/" + cap);
            TapIn(card, "OneKey", true); yield return CloseChest(app, "열쇠 나머지");

            // 무료 보급 — 표가 지목한 줄이 있을 때만(없으면 지어내지 않고 지나간다)
            string today = SaveStore.Today();
            var gp = D.Shop != null ? D.Shop.FreeGemPack : null;
            if (gp != null && ShopFree.Can(S, ShopFree.Gem, today)) { TapIn(Need(app, "GemPack:" + D.Shop.GemPacks.IndexOf(gp)), "Button_Price", false); yield return Frames(2); }
            var gd = D.Shop != null ? D.Shop.FreeGoldPack : null;
            if (gd != null && ShopFree.Can(S, ShopFree.Gold, today)) { TapIn(Need(app, "GoldPack:" + D.Shop.GoldPacks.IndexOf(gd)), "Button_Price", false); yield return Frames(2); }

            // 상자 ⓘ → 확률 팝업 → 칸 → 세부 → 어둠 탭(확률로 돌아온다) → 어둠 탭(닫힌다)
            card = Need(app, "Box:" + big.Key);
            TapIn(card, "Info", false); yield return Frames(2);
            if (!app.Overlay.IsOpen) throw new MissingException("확률 팝업(ⓘ 뒤)");
            var rows = GachaOdds.Of(D, big.Key); if (rows.Count == 0) throw new MissingException("확률 구간(" + big.Key + ")");
            TapIn(app.Overlay.Root, "Odds:" + rows[0].Rar + ":0", false); yield return Frames(2);
            if (!app.Overlay.IsOpen) throw new MissingException("아이템 세부 팝업(칸 뒤)");
            TapIn(app.Overlay.Root, "Dimmed", false); yield return Frames(2);
            if (!app.Overlay.IsOpen) throw new MissingException("세부를 닫으면 돌아올 확률 팝업");
            TapIn(app.Overlay.Root, "Dimmed", false); yield return Frames(2);
            if (app.Overlay.IsOpen) throw new MissingException("확률 팝업 닫힘(어둠 탭 뒤에도 열려 있다)");
            app.ShowScreen("lobby"); yield return null;
        }

        /// <summary>
        /// P8 출석·데일리 기프트·우편(T300 1항 · 배포 갈래 · T399) — PlayMode <c>PlaythroughTests.P8_…</c> 와 <b>같은 길</b>. 단언은 없다(3항 ⓐ):
        /// 사이드 «출석» → (오늘 몫이 있으면) 1일차 칸 → 닫기 → 사이드 «데일리 기프트» → (그냥 받는 칸이 있으면) «받기» → 닫기 →
        /// ≡ → «우편» → «전체 받기»(없으면 줄의 «받기») → 닫기 → 로비.
        /// <para>⚠ <b>팝업은 손으로 안 연다</b> — <c>LobbyPopups.Attendance(app)</c> 를 직접 부르면 사이드 칸·메뉴 줄의 배선이 끊겨도 봇이 «ok» 를 찍는다(결정 922 ③ · 979).</para>
        /// <para>⚠ <b>«받을 것이 있다» 와 «지금 그 버튼이 있다» 는 다른 말이다</b>(결정 979) — 기프트의 칸은 «광고 보기» 일 수 있고 출석은 오늘 몫을 이미 받았을 수 있다.
        /// 그런 자리는 <b>지나간다</b>. 없는 것을 «잡았다» 고 적지 않는다 — 다만 <b>팝업이 서고 닫히는가</b> 는 늘 잰다.</para>
        /// <para>⚠ <b>우편 한 통은 이 단계가 넣는다</b>(1항) — 새 세이브의 우편함은 비어 있어 그냥 열면 «받기» 가 없다.</para>
        /// </summary>
        static System.Collections.IEnumerator P8Boxes(App app)
        {
            var D = app.Data; var S = app.Save;
            string today = SaveStore.Today();

            // 조건은 이 단계가 만든다 — 우편 한 통(아레나 갈래만 들어간다 · T243). 수(1000)는 표의 값이 아니라 이 각본이 정한 값이라 안 낡는다.
            var mail = new MailItem { Id = "p8-play", Kind = Core.Mail.KindArena, Title = "아레나 순위 보상", Desc = "" };
            mail.Rewards.Add(new ArenaRankData.Reward { Item = Core.Mail.ItemGold, Amount = 1000 });
            Core.Mail.Add(S, mail);
            app.Persist();

            app.ShowScreen("lobby"); yield return Frames(3);
            Reach(app, "lobby");

            // ⓐ 출석 — 사이드 칸을 눌러 연다. 오늘 몫이 남아 있을 때만 1일차 칸을 누른다.
            Tap(app, "Side:" + LobbyScreen.SideAttendance); yield return Frames(2);
            yield return UntilOpen(app, "출석 팝업");
            if (D != null && D.Attendance != null && Core.Attendance.Can(S, D.Attendance, today))
            {
                TapDeep(app.Overlay.Root, "Day:1", "출석 1일차 칸");
                yield return Frames(3);
            }
            // ⚠ 받고 나면 «결과 팝업 → (닫으면) 출석 팝업» 두 겹이 선다(ClaimAttendance 의 onClose) — 그래서 한 번이 아니라 다 닫는다.
            yield return CloseAll(app, "출석 팝업");

            // ⓑ 데일리 기프트 — «받기» 글자가 있을 때만 누른다(광고로 여는 칸은 «광고 보기» 다 · 결정 979)
            Tap(app, "Side:" + LobbyScreen.SideDailyGift); yield return Frames(2);
            yield return UntilOpen(app, "데일리 기프트 팝업");
            if (UiKit.Find(app.Overlay.Root, "DailyGiftBox") == null) throw new MissingException("기프트 상자(DailyGiftBox)");
            if (TapLabel(app.Overlay.Root, "받기")) yield return Frames(3);
            yield return CloseAll(app, "데일리 기프트 팝업");

            // ⓒ 우편 — ≡ 메뉴 줄로 연다(사이드 칸이 없는 유일한 자리라 이 갈래가 메뉴 배선도 같이 잰다)
            Tap(app, "Button_Menu"); yield return Frames(2);
            yield return UntilOpen(app, "≡ 메뉴");
            TapIn(app.Overlay.Root, "Menu:" + LobbyMenu.ItemMail, true); yield return Frames(3);
            if (!app.Overlay.IsOpen) throw new MissingException("우편함(메뉴 줄 뒤)");
            UiKit.CompleteAllTweens(); yield return Frames(1);
            if (UiKit.Find(app.Overlay.Root, Mailbox.ClaimAllName) != null) TapIn(app.Overlay.Root, Mailbox.ClaimAllName, true);
            else if (!TapLabel(app.Overlay.Root, "받기")) throw new MissingException("우편의 «전체 받기»·«받기»(이 단계가 넣은 한 통이 있는데 받을 자리가 없다)");
            yield return Frames(3);
            if (Mailbox.Any(app)) throw new MissingException("받고 나면 비는 우편함(아직 그 통이 남아 있다)");
            if (app.Overlay.IsOpen) yield return CloseAll(app, "우편함");

            app.ShowScreen("lobby"); yield return null;
        }

        /// <summary>
        /// P7 퀘스트·업적(T300 1항 표 · 배포 갈래 · T407) — 사이드 «퀘스트» → 일일 «전부 받기» → 주간 탭 → 업적 탭.
        /// <b>단언은 하나도 없다</b>(3항 ⓐ): 재는 것은 «배선이 이어져 있는가» 뿐이고, 못 찾은 자리만 <see cref="MissingException"/> 로 알린다.
        /// <para>
        /// ⚑ <b>팝업 안 «탭» 이 이 갈래의 값이다</b> — 세 판(일일·주간·업적)은 <see cref="LobbyPopups.Quest"/>·<see cref="LobbyPopups.Achievements"/> 로
        /// <b>팝업을 통째로 다시 여는</b> 배선이라(T257·T258 4항), 화면 자들은 판마다 제 손으로 열어 재므로 그 탭이 끊겨도 아무도 안 운다(T280).
        /// </para>
        /// <para>
        /// ⚠ <b>조건은 이 단계가 만든다</b>(P8 우편 한 통과 같은 자리) — 갓 켠 세이브에는 받을 칸이 없어 «전부 받기» 가 회색이고,
        /// 그러면 이 갈래가 <b>아무것도 안 누르고 초록</b>이 된다(결정 771 이 미워하는 그 꼴의 자 판). <b>수는 표에서 읽는다</b>
        /// (<c>quest.Goal</c>) — 표가 바뀌어도 이 각본은 안 낡는다. <see cref="QuestRun.Bump"/> 는 일일·주간에 같이 쌓으므로 두 판이 함께 열린다.
        /// </para>
        /// </summary>
        static System.Collections.IEnumerator P7Quest(App app)
        {
            var D = app.Data; var S = app.Save;
            var q = D != null ? D.Quest : null;
            if (q == null) throw new MissingException("퀘스트 표(data.quest)");

            QuestRun.Roll(S, q, System.DateTime.Now);                     // 날·주를 먼저 민다(민 뒤에 쌓아야 그 셈이 안 지워진다)
            foreach (var quest in q.Daily.Quests) QuestRun.Bump(S, quest.Counter, quest.Goal);
            foreach (var quest in q.Weekly.Quests) QuestRun.Bump(S, quest.Counter, quest.Goal);
            app.Persist();

            app.ShowScreen("lobby"); yield return Frames(3);
            Reach(app, "lobby");

            // ⓐ 일일 — 사이드 «퀘스트» 칸으로 연다
            Tap(app, "Side:" + LobbyScreen.SideQuest); yield return Frames(2);
            yield return UntilOpen(app, "퀘스트 팝업(일일)");
            if (!TapIfLive(app.Overlay.Root, "QuestClaimAll"))
                throw new MissingException("일일 «전부 받기»(칸을 다 채웠는데 눌리지 않는다)");
            yield return Frames(3);
            // 받으면 «리워드 팝업 → (닫으면) 퀘스트 팝업» 두 겹이 선다(결정 671) — 그래서 한 번이 아니라 다 닫는다.
            yield return CloseAll(app, "퀘스트 팝업(일일)");

            // ⓑ 주간 — **탭으로** 판을 갈아탄다(팝업을 새로 여는 것이 아니라 그 배선을 잰다)
            Tap(app, "Side:" + LobbyScreen.SideQuest); yield return Frames(2);
            yield return UntilOpen(app, "퀘스트 팝업");
            TapIn(app.Overlay.Root, "Tab:1", true); yield return Frames(3);
            if (!TapIfLive(app.Overlay.Root, "QuestClaimAll"))
                throw new MissingException("주간 «전부 받기»(칸을 다 채웠는데 눌리지 않는다)");
            yield return Frames(3);
            yield return CloseAll(app, "퀘스트 팝업(주간)");

            // ⓒ 업적 — 셋째 탭. ⚠ 여기는 «받을 것이 있을 때만» 누른다: 업적은 이 각본이 조건을 만들지 않는다
            //   (누적이라 초기화되지 않고, 켠 것만으로 하나가 열려 있기도 하다 · QuestClaimDotTests 가 그 값을 치렀다).
            //   그래서 «상자가 섰는가» 를 자리로 잡고, 단추는 살아 있으면 누른다(T401 이 그 판에 같은 이름의 단추를 세웠다).
            Tap(app, "Side:" + LobbyScreen.SideQuest); yield return Frames(2);
            yield return UntilOpen(app, "퀘스트 팝업");
            TapIn(app.Overlay.Root, "Tab:2", true); yield return Frames(3);
            if (UiKit.Find(app.Overlay.Root, "QuestBox") == null) throw new MissingException("업적 판의 상자(QuestBox)");
            if (TapIfLive(app.Overlay.Root, "QuestClaimAll")) yield return Frames(3);
            yield return CloseAll(app, "퀘스트 팝업(업적)");

            app.ShowScreen("lobby"); yield return null;
        }

        /// <summary>
        /// P9 탐험(T300 1항 표 · 배포 갈래 · T407) — 보조 버튼 «탐험» → «받기» → «빠른 탐험» 팝업.
        /// <b>단언은 하나도 없다</b>(3항 ⓐ).
        /// <para>
        /// ⚠ <b>조건은 이 단계가 만든다</b> — 갓 켠 세이브는 쌓인 것이 0 이라 «받기» 가 회색이다. 시계(<see cref="SaveData.ExpSettle"/>)를
        /// 표가 말하는 <b>최대 시간</b>만큼 뒤로 돌려 «가득 쌓인» 자리를 만든다(수를 안 적는다 · <c>d.MaxSeconds</c> 를 읽는다).
        /// </para>
        /// <para>
        /// ⚑ <b>«광고 보고 무료» 는 누르지 않는다</b> — 그 길은 <see cref="Overlay.AdCountdown"/> 로 <b>몇 초를 세고 서 있다</b>.
        /// T300 2항의 시간 예산(전부 합쳐 5분)에서 한 단계가 초를 그냥 먹는 것은 값이 안 맞는다(T406 이 그 예산을 재게 해 두었다).
        /// 그래서 여기서는 <b>그 단추가 서 있고 눌리는가</b> 까지만 보고 닫는다 — 끊기면 그것으로 빨개진다.
        /// </para>
        /// </summary>
        /// <summary>
        /// P2 전투가 <b>한 판이 끝나기를</b> 기다리는 프레임 예산 — <see cref="WaitFrames"/>(팝업 하나를 기다리는 값)와 <b>따로 둔다</b>.
        /// <para>⚠ 공통값을 이만큼 올리면 «팝업이 안 뜬다» 같은 진짜 고장이 열 배 느리게 드러난다 — 오래 걸리는 것은 이 한 단계뿐이다.</para>
        /// <para>60fps 로 90초쯤. 판이 그보다 길면 «못 찾았다» 가 아니라 <b>예산을 다시 정해야 한다</b>는 뜻이라 메시지에 그렇게 적는다.</para>
        /// </summary>
        public const int BattleFrames = 5400;

        /// <summary>
        /// P2 전투(T300 1항 표 · 배포 갈래 · T410) — 로비 START → 배속 → 판이 끝날 때까지 돌리며 <b>레벨업 특전 팝업이 뜨면 하나 고른다</b> → 결과 팝업 → 로비.
        /// <b>단언은 하나도 없다</b>(3항 ⓐ): 봇은 «죽지 않고 지나가는가» 만 본다.
        /// <para>
        /// ⚑ <b>이 각본이 유일하게 «판» 을 굴린다</b> — 다른 갈래는 전부 팝업을 열고 닫는다. 그래서 여기서만 잡히는 것이 있다:
        /// START 배선 · 전투 화면이 서는가 · 배속 단추 · <b>레벨업이 실제로 3택을 열고 그 선택이 엔진에 닿는가</b> · 판이 끝나고 로비로 돌아오는 길.
        /// </para>
        /// <para>
        /// ⚠ <b>배속은 <c>Time.timeScale</c> 을 손으로 안 만진다</b> — 사람이 누르는 그 단추(<c>SpeedBtn</c>)를 누른다.
        /// 손으로 만지면 «단추가 끊겨도 각본은 빨라진다» 가 되어 그 배선을 못 잰다(T280 이 값을 치른 자리).
        /// </para>
        /// <para>
        /// ⚠ <b>그리고 끝에서 되돌린다</b>(결정 1177 · 워커 C 가 P6 에서 값을 치른 자리) — 이 단추는 <c>Time.timeScale</c> 이 아니라
        /// <b><see cref="SaveData.Speed"/> 를 쓰고 저장한다</b>. 안 되돌리면 <b>뒤 단계와 그 뒤의 판이 전부 그 배속으로 흐르고</b>,
        /// T406 이 만든 «몇 초» 눈금까지 거짓이 된다 — 그런데 각 단계는 여전히 <c>ok</c> 를 찍으므로 <b>어느 자도 안 잡는다</b>.
        /// </para>
        /// <para>
        /// ⚠ <b>이벤트(천사·악마·휴식·광고)는 «지나가면 지나가는» 것으로 둔다</b> — T300 1항 표는 «하나 이상 지난다» 라고 적었지만
        /// 그것은 <b>난수</b>다. 각본이 그것을 <b>단언</b>하면 시드가 다른 날 애먼 빨강이 뜨고, 그 빨강에 이 갈래가 잡으려는 진짜 고장이 묻힌다
        /// (T278·결정 930 이 값을 치른 그 손). 이벤트 팝업이 뜨면 <see cref="CloseAll"/> 로 닫고 계속 논다 — 곧 <b>막히지 않는가</b>만 잰다.
        /// </para>
        /// </summary>
        static System.Collections.IEnumerator P2Battle(App app)
        {
            app.ShowScreen("lobby"); yield return Frames(3);
            Reach(app, "lobby");

            Tap(app, "Start"); yield return Frames(4);
            Reach(app, "battle");
            var bs = app.Current as BattleScreen;
            if (bs == null) throw new MissingException("전투 화면(BattleScreen)");

            // 배속 — 사람이 누르는 그 단추로 올린다(위 ⚠). 끝에서 되돌리려고 «원래 값» 을 쥐고 간다.
            int speed0 = app.Save.Speed;
            Tap(app, "SpeedBtn"); yield return Frames(2);

            int picked = 0, closed = 0;
            for (int i = 0; ; i++)
            {
                var G = bs.G;
                if (G == null) throw new MissingException("전투 상태(BattleScreen.G)");
                if (G.Over) break;
                if (app.Current != bs) break;                      // 화면이 스스로 로비로 갔다(결과 팝업을 닫은 뒤)

                if (app.Overlay.IsOpen)
                {
                    // 레벨업 3택이면 카드 하나를 «누른다» — 카드는 프리팹 조각이라 제 이름이 없다.
                    // 그 대신 담는 자리(`Group_Card`)가 계약이다(팝업을 세우는 쪽이 프리팹에서 찾는 그 이름).
                    var group = UiKit.Find(app.Overlay.Root, "Group_Card");
                    if (group != null && TapFirstButtonIn(group)) { picked++; yield return Frames(3); continue; }
                    // 그 밖의 팝업(이벤트·악마의 거래 …)은 닫고 계속 논다(위 ⚠).
                    yield return CloseAll(app, "전투 중 팝업"); closed++;
                    continue;
                }

                if (i >= BattleFrames)
                    throw new MissingException("한 판이 " + BattleFrames + "프레임 안에 안 끝났다 — 각본이 막힌 것이 아니라 «예산» 을 다시 정해야 하는 자리일 수도 있다(P2 주석)");
                yield return null;
            }

            // 결과 팝업(승리 `ui.resultWin` · 사망 `Dead`) — 둘 다 «어둠 탭 = 연출 스킵 → 로비로» 라 CloseAll 이 두 단을 다 민다.
            // ⚠ 판이 끝난 «그 프레임» 에는 아직 안 서 있다(화면이 다음 틱에 스스로 연다 · T280) — 잠깐 기다렸다가 닫아야
            //   이 갈래가 «결과 팝업 → 로비» 배선까지 잰다. 안 뜨면 그냥 넘어간다(화면이 스스로 로비로 갔을 수도 있다).
            if (!app.Overlay.IsOpen && app.Current == bs)
                for (int k = 0; k < WaitFrames && !app.Overlay.IsOpen && app.Current == bs; k++) yield return null;
            if (app.Overlay.IsOpen) yield return CloseAll(app, "전투 결과 팝업");
            if (app.Current == null || app.Current.Name != "lobby") { app.ShowScreen("lobby"); yield return Frames(2); }
            Reach(app, "lobby");
            // 배속 되돌림(위 ⚠ · 결정 1177) — 화면을 나온 뒤라 단추가 없으므로 세이브를 원래대로 돌린다.
            if (app.Save.Speed != speed0) { app.Save.Speed = speed0; app.Persist(); }
            if (picked == 0 && closed == 0)
            {
                // 아무 팝업도 안 떴다 = 레벨업이 한 번도 안 났다. 1챕터에서도 경험치는 오르므로 이것은 «배선이 끊겼다» 쪽이 훨씬 그럴듯하다.
                throw new MissingException("판이 끝나도록 팝업이 한 번도 안 떴다(레벨업 3택이 안 열렸다 — 특전 배선을 보라)");
            }
            yield return null;
        }

        /// <summary>담는 자리 안에서 <b>처음으로 눌리는</b> 버튼 하나를 누른다 — 조각이 프리팹에서 와 제 이름이 없을 때(특전 카드) 쓴다.</summary>
        static bool TapFirstButtonIn(UnityEngine.Transform group)
        {
            if (group == null) return false;
            foreach (var b in group.GetComponentsInChildren<UnityEngine.UI.Button>(false))
                if (b.interactable && b.gameObject.activeInHierarchy) { b.onClick.Invoke(); return true; }
            return false;
        }

        static System.Collections.IEnumerator P9Expedition(App app)
        {
            var D = app.Data; var S = app.Save;
            var ex = D != null ? D.Expedition : null;
            if (ex == null) throw new MissingException("탐험 표(data.expedition)");

            S.ExpSettle = LobbyPopups.NowSec() - ex.MaxSeconds;           // 가득 쌓인 자리(표가 말하는 최대 시간)
            app.Persist();

            app.ShowScreen("lobby"); yield return Frames(3);
            Reach(app, "lobby");

            // ⓐ 탐험 — 보조 버튼 줄(사이드 열이 아니라 «SubRow» 다 · 이 갈래가 그 줄의 배선도 같이 잰다)
            Tap(app, "Side:" + LobbyScreen.SideExplore); yield return Frames(2);
            yield return UntilOpen(app, "탐험 팝업");
            if (UiKit.Find(app.Overlay.Root, "ExpCellGold") == null) throw new MissingException("탐험의 쌓인 골드 칸(ExpCellGold)");
            if (!TapIfLive(app.Overlay.Root, "ClaimBtn"))
                throw new MissingException("탐험 «받기»(시계를 최대까지 돌렸는데 눌리지 않는다)");
            yield return Frames(3);
            // 받으면 «리워드 팝업 → (닫으면) 탐험 팝업» 두 겹이다(결정 671·997 · 출석과 같은 꼴)
            yield return CloseAll(app, "탐험 팝업(받기 뒤)");

            // ⓑ 빠른 탐험 — 파란 단추로 그 팝업을 열고, 안의 «광고 보고 무료» 가 눌리는 자리인지만 본다(위 ⚑).
            Tap(app, "Side:" + LobbyScreen.SideExplore); yield return Frames(2);
            yield return UntilOpen(app, "탐험 팝업");
            if (!TapIfLive(app.Overlay.Root, "QuickBtn"))
                throw new MissingException("«빠른 탐험» 단추(횟수가 남았는데 눌리지 않는다)");
            yield return Frames(3);
            if (UiKit.Find(app.Overlay.Root, "QuickExploreBox") == null) throw new MissingException("빠른 탐험 팝업(QuickExploreBox)");
            var free = UiKit.Find(app.Overlay.Root, "QxFreeBtn");
            if (free == null) throw new MissingException("«광고 보고 무료» 단추(QxFreeBtn)");
            var freeBtn = free.GetComponent<UnityEngine.UI.Button>();
            if (freeBtn == null || !freeBtn.interactable)
                throw new MissingException("«광고 보고 무료»(횟수가 남았는데 눌리지 않는다)");
            yield return CloseAll(app, "빠른 탐험 팝업");

            app.ShowScreen("lobby"); yield return null;
        }

        /// <summary>
        /// 이름으로 찾아 <b>켜져 있고 눌리는</b> 것만 누른다 — 없거나 회색이면 <b>던지지 않고</b> false.
        /// <para><see cref="TapIn"/> 과 갈리는 자리: «받을 것이 없으면 회색» 이 정상인 단추(«전부 받기»·«받기»)에서 쓴다.
        /// 부르는 쪽이 «없어도 되는 자리» 인지 «있어야 하는 자리» 인지를 정한다 — 뒤엣것은 false 를 받아 제 말로 던진다.</para>
        /// </summary>
        static bool TapIfLive(UnityEngine.Transform root, string name)
        {
            var t = root != null ? UiKit.Find(root, name) : null;
            if (t == null || !t.gameObject.activeInHierarchy) return false;
            var b = t.GetComponent<UnityEngine.UI.Button>();
            if (b == null || !b.interactable) return false;
            b.onClick.Invoke();
            return true;
        }

        /// <summary>
        /// P6 아레나(T300 1항 · 배포 갈래 · T409) — PlayMode <c>PlaythroughTests.P6_…</c> 와 <b>같은 길</b>. 단언은 없다(3항 ⓐ):
        /// 아레나 페이지 → «도전» 팝업(24) → 줄 «도전» → <b>아레나 판을 굴린다</b> → 결과 화면(34) «계속» → 순위 보상 팝업(25) → 상인(26) → «뒤로».
        /// <para>
        /// ⚑ <b>판을 굴리는 첫 배포 갈래다</b> — 여기까지는 어느 각본도 전투를 안 열었다. 그래서 <b>시간이 이 각본의 유일한 새 위험</b>이고,
        /// T408 이 «다섯 단계 41.4s / 예산 300s» 를 재 둔 덕에 붙일 수 있었다(결정 1172). 다음 배포의 <c>시간 …s/300s</c> 줄이 그 판단을 되짚는다.
        /// </para>
        /// <para>
        /// ⚠ <b>판은 «이기게» 해 준다</b> — 봇이 재는 것은 «결과 화면까지 지나가는가» 지 «이 판을 이길 수 있는가» 가 아니다(밸런스는 그 절의 몫 · 3항 ⓐ).
        /// 그래서 배속을 올리고 체력을 받쳐 주다가, 상한 안에 안 끝나면 <c>Cleared</c> 로 끝을 낸다 — PlayMode 자가 쓰는 그 손잡이 그대로다.
        /// </para>
        /// </summary>
        static System.Collections.IEnumerator P6Arena(App app)
        {
            EventsScreen.Open(app, EventsScreen.PageArena); yield return Frames(3);
            var pg = Page(app, EventsScreen.PageArena);

            // ⓐ 도전 팝업(24) → 첫 상대 줄
            TapIn(pg, "ChallengeBtn", true); yield return Frames(2);
            if (!app.Overlay.IsOpen) throw new MissingException("아레나 도전 팝업(24)");
            TapIn(app.Overlay.Root, "FoeBtn:0", true); yield return Frames(2);
            Reach(app, "battle");
            var bs = app.GetScreen<BattleScreen>();
            if (bs == null || bs.G == null) throw new MissingException("전투 화면·전투 상태");
            if (!bs.IsArena) throw new MissingException("«아레나 판» 표식(IsArena) — 이 표식이 없으면 끝났을 때 승점 갈래가 안 켜진다(T240)");

            // ⓑ 굴린다 → 결과 화면(34) → «계속» → 아레나 페이지
            yield return WinTheRun(app, bs.G, 1.0f, "아레나 판");
            yield return UntilOpen(app, "PvP 결과 화면(34)");
            if (!ArenaResult.Open) throw new MissingException("PvP 결과 화면(아레나 판은 클리어 팝업이 아니라 승점 결과로 끝난다 · T240)");
            TapIn(app.Overlay.Root, "ContinueBtn", true); yield return Frames(3);
            if (ArenaResult.Open) throw new MissingException("«계속» 뒤 결과 화면 닫힘");
            pg = Page(app, EventsScreen.PageArena);

            // ⓒ 순위 보상 팝업(25) → 닫기 → 상인(26) → 상품 한 칸(표시만) → 뒤로
            TapIn(pg, "RewardsBtn", true); yield return Frames(2);
            if (!app.Overlay.IsOpen) throw new MissingException("순위 보상 팝업(25)");
            if (UiKit.Find(app.Overlay.Root, "RewardRow:0") == null) throw new MissingException("순위 보상 줄");
            yield return CloseAll(app, "순위 보상 팝업");

            pg = Page(app, EventsScreen.PageArena);
            TapIn(pg, "MerchantBtn", true); yield return Frames(2);
            pg = Page(app, EventsScreen.PageMerchant);
            TapIn(pg, "Goods:0", true); yield return Frames(2);   // ⚠ 상인의 «구매» 는 아직 배선이 없다 — «눌러도 죽지 않는가» 까지만
            TapIn(Page(app, EventsScreen.PageMerchant), "BackBtn", true); yield return Frames(3);

            app.ShowScreen("lobby"); yield return null;
        }

        /// <summary>
        /// 판을 <paramref name="maxSec"/> 초 안에 <b>이기게</b> 끝낸다 — 배속을 올리고 체력을 받쳐 주다가, 그 안에 안 끝나면 <c>Cleared</c> 로 끝을 낸다.
        /// <para>
        /// ⚠ <b>«이기는 것» 은 봇이 재는 것이 아니다</b>(3항 ⓐ) — 봇이 보는 것은 «판이 열리고 결과까지 지나가는가» 뿐이고,
        /// «이 판을 이길 수 있는가» 는 밸런스 절(T325)의 몫이다. 그것을 봇이 재면 표가 바뀌는 날 <b>봇이 먼저 운다</b>.
        /// </para>
        /// <para>⚠ <b>배속은 반드시 되돌린다</b> — 여기서 <c>timeScale</c> 을 3 으로 두고 나가면 <b>뒤 단계가 전부 세 배로 흐른다</b>(그 단계들이 재는 «몇 초» 도 거짓이 된다).</para>
        /// </summary>
        static System.Collections.IEnumerator WinTheRun(App app, BattleState g, float maxSec, string what)
        {
            UnityEngine.Time.timeScale = 3f;
            float t0 = UnityEngine.Time.realtimeSinceStartup;
            while (UnityEngine.Time.realtimeSinceStartup - t0 < maxSec && !g.Over && !app.Overlay.IsOpen)
            {
                if (g.P.Hp < g.P.MaxHp * 0.5) g.P.Hp = g.P.MaxHp;
                yield return null;
            }
            UnityEngine.Time.timeScale = 1f;
            if (g.T <= 0.0) throw new MissingException(what + "(엔진이 한 틱도 안 돌았다)");
            if (!g.Over)
            {
                // 엔진이 스스로 연 레벨업 팝업이면 여기서 정리한다 — 각본 순서대로 놀아야 잡은 고장을 이름으로 말할 수 있다(결정 922).
                if (app.Overlay.IsOpen) { app.Overlay.Close(); yield return Frames(1); }
                g.Pending = null; g.PendingLevelUps = 0;
                g.Cleared = true;
            }
        }

        /// <summary>던전·아레나 화면(<see cref="EventsScreen"/>)의 페이지 루트 — 도달 확인을 겸한다(꺼진 형제 페이지에서 이름을 집지 않으려고 · 결정 936).</summary>
        static UnityEngine.Transform Page(App app, string page)
        {
            Reach(app, "events");
            var ev = app.GetScreen<EventsScreen>();
            if (ev == null || ev.Page != page) throw new MissingException("페이지 «" + page + "»(지금 " + (ev != null ? ev.Page : "없음") + ")");
            var pg = UiKit.Find(app.Current.Root, "Page:" + page);
            if (pg == null) throw new MissingException("페이지 «" + page + "» 의 루트");
            return pg;
        }

        /// <summary>팝업이 설 때까지 기다린다 — <see cref="WaitFrames"/> 를 넘기면 «무엇을 기다리다 죽었나» 를 남긴다.</summary>
        static System.Collections.IEnumerator UntilOpen(App app, string what)
        {
            var w = new Waiter(what);
            while (!app.Overlay.IsOpen && w.Tick()) yield return null;
            yield return Frames(2);
        }

        /// <summary>
        /// 사람이 닫는 길(닫기 <b>X</b> → 없으면 <b>어둠</b>)로 <b>겹쳐 선 팝업을 다 닫는다</b>.
        /// <para>
        /// ⚑ <b>«한 번 닫으면 끝» 이 아니다</b> — 이 게임에는 <b>닫히면서 다른 팝업을 여는</b> 자리가 있다:
        /// 출석 보상 팝업의 <c>onClose</c> 가 출석 팝업을 <b>다시 그리고</b>(<see cref="LobbyPopups"/> · «✅ 가 붙은 채로»),
        /// 빠른 탐험 상자도 같은 꼴이다(결정 997). 게다가 결과 팝업의 첫 탭은 연출 «건너뛰기» 라 <b>닫는 탭이 아니다</b>(T202).
        /// ⇒ <c>IsOpen</c> 이 거짓이 됐는가를 <b>한 번만</b> 보면 그 자리에서 «안 닫힌다» 로 잘못 운다 — 배포 갈래 P8 1회차가 정확히 그렇게 빨갰다(결정 1150 뒤).
        /// </para>
        /// <para>⚠ <c>Overlay.Close()</c> 를 손으로 부르지 않는다 — 그러면 «X 도 어둠도 아무 데도 안 이어져 있어도» 봇이 «닫았다» 고 적는다(T280).</para>
        /// <para>⚠ 닫기 X 는 <b>앞머리</b>로 고른다(<c>Button_Close_01</c> ↔ <c>Button_Close_Square_01</c>). 꺼진 X 는 «안 쓰는 것» 이라 <b>켜진 것만</b> 센다(결정 168).</para>
        /// </summary>
        static System.Collections.IEnumerator CloseAll(App app, string what)
        {
            for (int i = 0; i < CloseTaps && app.Overlay.IsOpen; i++)
            {
                var b = CloseHandle(app);
                if (b == null) throw new MissingException(what + " 를 닫을 것(켜진 닫기 X·눌리는 어둠)");
                b.onClick.Invoke();
                yield return Frames(2);
            }
            if (app.Overlay.IsOpen) throw new MissingException(what + " 닫힘(" + CloseTaps + "번 눌러도 팝업이 서 있다)");
        }

        /// <summary>겹쳐 선 팝업을 다 닫는 데 쓸 탭 수의 상한 — 지금 가장 긴 사슬은 «결과 팝업(건너뛰기 + 닫기) → 다시 선 팝업(닫기)» 셋이다. 봇의 인내심이라 표에 두지 않는다.</summary>
        const int CloseTaps = 8;

        /// <summary>지금 선 팝업을 닫는 손잡이 — 켜진 <c>Button_Close*</c> 가 먼저고, 없으면 눌리는 어둠(<c>Dimmed</c> → <c>Background</c>)이다.</summary>
        static UnityEngine.UI.Button CloseHandle(App app)
        {
            var root = app.Overlay.Root;
            foreach (var b in root.GetComponentsInChildren<UnityEngine.UI.Button>(false))
                if (b.name.StartsWith("Button_Close", StringComparison.Ordinal) && b.interactable) return b;
            var dim = UiKit.Find(root, "Dimmed");
            if (dim == null) dim = UiKit.Find(root, "Background");
            var db = dim != null ? dim.GetComponent<UnityEngine.UI.Button>() : null;
            return db != null && db.interactable ? db : null;
        }

        /// <summary>이름으로 찾은 자리 «안»의 첫 손잡이를 누른다 — 칸 자신이 아니라 자식이 눌리는 자리(출석 칸 <c>Day:N</c>)에 쓴다.</summary>
        static void TapDeep(UnityEngine.Transform root, string name, string what)
        {
            var t = root != null ? UiKit.Find(root, name) : null;
            if (t == null) throw new MissingException(name);
            var b = t.GetComponentInChildren<UnityEngine.UI.Button>(true);
            if (b == null || !b.interactable) throw new MissingException(what + "(눌리지 않는다)");
            b.onClick.Invoke();
        }

        /// <summary>
        /// 뽑기 결과 창(<c>ui.chestOpen</c>)이 서기를 기다렸다가 <b>배경 탭</b>으로 닫는다 — 첫 탭은 연출 «건너뛰기», 다음 탭이 «닫기»(T202).
        /// 두 번 안에 안 닫히면 그 단계는 «죽었다» 로 적힌다.
        /// </summary>
        /// <summary>
        /// P10 펫(T300 1항 · 배포 갈래) — PlayMode <c>PlaythroughTests.P10_…</c> 와 <b>같은 길</b>을 게임 안에서 누른다. 단언은 없다(3항 ⓐ):
        /// 탭 «펫» → <b>소환</b>(결과 창은 배경 탭으로) → <b>가진 펫</b>의 칸 → 세부(14) → 강화(잠겨 있지 않을 때만) → 장착 → 로비.
        /// <para>⚠ <b>여는 칸은 «가진 펫»</b> 이다 — 소환은 표의 확률이라 0번이 뽑힌다는 보장이 없고, 안 가진 펫의 세부는 강화·장착이 둘 다 잠겨 있어
        /// 봇이 «아무것도 안 누르고» ok 를 찍는다(결정 1049 ④ 와 같은 까닭 · 여기서는 단언이 없으니 더 조용하다).</para>
        /// <para>⚠ 세이브(다이아·펫알)는 봇이 제 조건을 만든다(1항) — 값은 <see cref="Pets.Offer"/> 가 말하는 그대로라 표가 바뀌어도 안 운다.</para>
        /// </summary>
        static System.Collections.IEnumerator P10Pet(App app)
        {
            var D = app.Data; var S = app.Save;
            if (D == null || D.Pet == null) throw new MissingException("펫 표(data.pet)");
            int cap = D.Gacha != null ? D.Gacha.TenPullCount : 10;
            var offer = Pets.Offer(D.Pet, false, S.PetEgg, cap);
            if (offer.ByEgg) S.PetEgg += offer.Egg; else S.Gem += offer.Diamond;
            app.Persist();

            app.ShowScreen("lobby"); yield return Frames(2);
            Tap(app, "Tab:pet"); yield return Frames(3);
            Reach(app, "pet");
            TapIn(app.Current.Root, "SummonBtn", true);
            yield return CloseByDim(app, "펫 소환 결과");

            // ⛑ T293 ⓘ 4회차(주인 5항 ⓙ) 뒤 — 격자는 **가진 펫만** 앞에서부터 켜므로 칸 이름 `Pet:N` 의 N 은 «표의 몇 번째» 가 아니라 «화면의 몇 번째» 다.
            //    표 자리로 집으면 꺼진 칸을 누르고, 꺼진 것도 `onClick` 은 돌아서 봇이 «사람 눈에 없는 것» 을 누른 채 ok 를 찍는다(결정 941 ① · 1071).
            //    ⇒ **켜진 첫 칸**을 집는다 — 켜져 있다는 것이 곧 «가진 펫» 이다(자리 대신 뜻으로 집는다).
            string slot = null;
            for (int i = 0; i < D.Pet.Pets.Count && slot == null; i++)
            {
                var c = UiKit.Find(app.Current.Root, "Pet:" + i);
                if (c != null && c.gameObject.activeInHierarchy) slot = "Pet:" + i;
            }
            if (slot == null) throw new MissingException("소환하고도 격자에 켜진 칸이 없다(뽑은 것이 화면에 안 닿았다)");
            TapIn(app.Current.Root, slot, true); yield return Frames(2);
            if (!app.Overlay.IsOpen) throw new MissingException("펫 세부 팝업(칸 뒤)");

            // 강화 — 갓 뽑은 펫은 조각이 모자랄 수 있다. 잠겨 있으면 «없는 것을 눌렀다» 고 적지 않고 지나간다.
            var up = UiKit.Find(app.Overlay.Root, "PetUpgradeBtn");
            if (up == null) throw new MissingException("«강화» 버튼");
            var upBtn = up.GetComponent<UnityEngine.UI.Button>();
            if (upBtn != null && upBtn.interactable) { upBtn.onClick.Invoke(); yield return Frames(3); }

            // 장착 — 강화는 팝업을 닫았다 다시 연다(PetScreen.Upgrade) · 닫혀 있으면 칸을 다시 누른다
            if (!app.Overlay.IsOpen) { TapIn(app.Current.Root, slot, true); yield return Frames(2); }
            TapIn(app.Overlay.Root, "PetEquipBtn", true); yield return Frames(3);

            if (app.Overlay.IsOpen) yield return CloseByDim(app, "펫 세부 팝업");
            app.ShowScreen("lobby"); yield return null;
        }

        /// <summary>
        /// 어둠(<c>Dimmed</c>)을 눌러 닫는 팝업 — 결과 팝업(<see cref="RewardPopup"/>)·펫 세부가 이 꼴이다.
        /// <para>
        /// ⚠ <b><see cref="CloseChest"/> 의 «Background» 와 다른 이름이다.</b> 그쪽은 상자 결과 <b>프리팹</b>이 갖고 온 이름이고,
        /// 코드가 세우는 팝업의 어둠은 <c>Dimmed</c> 다(<c>RewardPopup:158</c> · <c>UiKit.Clickable(dim, …)</c> 로 눌린다).
        /// 배포 갈래 P10 1회차가 이것을 «닫는 이름은 하나겠지» 로 읽어 <c>CloseChest</c> 를 그대로 부르다 «못 찾았다: Background» 로 죽었다(결정 1065).
        /// </para>
        /// </summary>
        static System.Collections.IEnumerator CloseByDim(App app, string what)
        {
            var w = new Waiter("팝업(" + what + ")");
            while (!app.Overlay.IsOpen && w.Tick()) yield return null;
            yield return Frames(2);
            var dim = UiKit.Find(app.Overlay.Root, "Dimmed");
            if (dim == null) dim = UiKit.Find(app.Overlay.Root, "Background");
            if (dim == null) throw new MissingException(what + " 를 닫을 어둠(Dimmed·Background)");
            var b = dim.GetComponent<UnityEngine.UI.Button>();
            if (b == null) throw new MissingException(what + " 의 어둠이 눌리는 것이 아니다");
            b.onClick.Invoke(); yield return Frames(2);
            if (app.Overlay.IsOpen) { b.onClick.Invoke(); yield return Frames(2); }
            if (app.Overlay.IsOpen) throw new MissingException(what + " 닫힘(어둠 두 번 뒤에도 열려 있다)");
        }

        static System.Collections.IEnumerator CloseChest(App app, string what)
        {
            var w = new Waiter("결과 창(" + what + ")");
            while (!app.Overlay.IsOpen && w.Tick()) yield return null;
            yield return Frames(2);
            TapIn(app.Overlay.Root, "Background", false); yield return Frames(2);
            if (app.Overlay.IsOpen) { TapIn(app.Overlay.Root, "Background", false); yield return Frames(2); }
            if (app.Overlay.IsOpen) throw new MissingException("결과 창 닫힘(" + what + " · 배경 탭 두 번 뒤에도 열려 있다)");
        }

        /// <summary>
        /// 봇이 무엇을 «기다리는» 프레임 상한(3항 ⓓ 타임아웃). 게임 수치가 아니라 봇 자신의 인내심이라 표에 두지 않았다 —
        /// 30fps 에서 20초. 넘으면 <see cref="MissingException"/> 으로 «무엇을 기다리다 죽었나» 를 남기고 다음 단계로 간다.
        /// </summary>
        public const int WaitFrames = 600;
        /// <summary>«아직 안 왔다» 를 세는 자 — <c>while (!cond && w.Tick()) yield return null;</c> 꼴로 쓴다(상한을 넘기면 던진다).</summary>
        sealed class Waiter
        {
            readonly string _what; int _n;
            public Waiter(string what) { _what = what; }
            public bool Tick() { if (++_n > WaitFrames) throw new MissingException(_what + "(" + WaitFrames + "프레임 안에 안 왔다)"); return true; }
        }
        static System.Collections.IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        /// <summary>도달 — 지금 화면이 그 이름이 아니면 «죽었다».</summary>
        static void Reach(App app, string screen)
        {
            if (app.Current == null || app.Current.Name != screen) throw new MissingException("화면 «" + screen + "»(지금 " + (app.Current != null ? app.Current.Name : "없음") + ")");
        }
        /// <summary>
        /// 주어진 뿌리 아래의 이름을 누른다. <paramref name="live"/> 면 <b>켜진</b> 것만 — 같은 rect 에 옷 두 벌이 겹친 자리(상점 1회·10회)에서
        /// 꺼진 옷을 누르는 것은 노는 것이 아니다(<c>UiKit.Find</c> 는 꺼진 것도 집고 <c>onClick.Invoke</c> 는 꺼진 버튼에서도 돈다 · 결정 941 ①).
        /// </summary>
        static void TapIn(UnityEngine.Transform root, string name, bool live)
        {
            var t = root != null ? UiKit.Find(root, name) : null;
            if (t == null) throw new MissingException(name);
            if (live && !t.gameObject.activeInHierarchy) throw new MissingException(name + "(꺼진 옷 — 지금 켜진 옷이 아니다)");
            var b = t.GetComponent<UnityEngine.UI.Button>();
            if (b == null || !b.interactable) throw new MissingException(name + "(눌리지 않는다)");
            b.onClick.Invoke();
        }

        /// <summary>각본이 «있어야 한다» 고 여기는 자리가 없을 때 — 봇은 이것을 <c>fail</c> 로 적고 다음 단계로 간다.</summary>
        public class MissingException : System.Exception
        {
            public MissingException(string name) : base("못 찾았다: " + name) { }
        }
        static UnityEngine.Transform Need(App app, string name)
        {
            var root = app.Current != null ? app.Current.Root : null;
            var t = root != null ? UiKit.Find(root, name) : null;
            if (t == null) throw new MissingException(name);
            return t;
        }
        static void Tap(App app, string name)
        {
            var b = Need(app, name).GetComponent<UnityEngine.UI.Button>();
            if (b == null || !b.interactable) throw new MissingException(name + "(눌리지 않는다)");
            b.onClick.Invoke();
        }

        /// <summary>
        /// 각본을 처음부터 끝까지 돌린다(T300 2항). 단계마다 <see cref="Line"/> 한 줄을 찍고,
        /// <b>어느 단계가 터져도 다음 단계로 간다</b> — 봇이 게임을 멈추면 그것이 더 나쁜 고장이다.
        /// <para>마지막 줄은 늘 <c>[KkomaKnight] play done &lt;성공&gt;/&lt;돈 것&gt; fail &lt;실패&gt;</c> 다 —
        /// 스모크가 꼬리에서 그 한 줄만 찾으면 되게(T239 결정 678 과 같은 계약).</para>
        /// </summary>
        public static System.Collections.IEnumerator Run(App app)
        {
            int ok = 0, bad = 0, ran = 0;
            float t0All = UnityEngine.Time.realtimeSinceStartup;
            foreach (var st in Stages)
            {
                Step step;
                if (!Steps.TryGetValue(st.Id, out step)) continue;   // 아직 아무도 안 쓴 단계는 조용히 건너뛴다
                ran++;
                float t0 = UnityEngine.Time.realtimeSinceStartup;
                // 단계 안의 «작은 걸음»(Frames · CloseChest …)은 여기서 손으로 돌린다 — 유니티에 그대로 넘기면(중첩 코루틴) 그 안에서 난
                //   예외를 이 try 가 못 잡고, 봇이 «fail 한 줄» 대신 조용히 멈춘다(그러면 done 줄이 안 와서 «돌다 죽었다» 만 남는다).
                var stack = new System.Collections.Generic.Stack<System.Collections.IEnumerator>();
                stack.Push(step(app));
                bool failed = false;
                while (stack.Count > 0)
                {
                    var top = stack.Peek();
                    bool alive;
                    try { alive = top.MoveNext(); }
                    catch (System.Exception e)
                    {
                        UnityEngine.Debug.Log(Line(st.Id, false, e.GetType().Name + " " + e.Message) + Secs(UnityEngine.Time.realtimeSinceStartup - t0));
                        bad++; failed = true; break;
                    }
                    if (!alive) { stack.Pop(); continue; }
                    if (top.Current is System.Collections.IEnumerator sub) stack.Push(sub);
                    else yield return top.Current;
                }
                if (!failed) { UnityEngine.Debug.Log(Line(st.Id, true) + Secs(UnityEngine.Time.realtimeSinceStartup - t0)); ok++; }
            }
            UnityEngine.Debug.Log(DoneLine(ok, ran, bad) + Secs(UnityEngine.Time.realtimeSinceStartup - t0All));
        }

        /// <summary>스모크가 꼬리에서 찾는 마지막 한 줄.</summary>
        public const string DonePrefix = LogPrefix + "done ";
        public static string DoneLine(int ok, int ran, int bad) => DonePrefix + ok + "/" + ran + " fail " + bad;
    }
}
