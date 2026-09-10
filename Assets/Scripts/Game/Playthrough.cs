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
        // ─────────────────────────────────────────────────────────────────────────────
        // 배포 빌드에서 실제로 «노는» 쪽(T300 2항) — `App.DebugGo("play")` 가 이것을 돌린다.
        // ─────────────────────────────────────────────────────────────────────────────
        /// <summary>한 단계가 «게임 안에서» 하는 일. 없는 단계는 아직 아무도 안 썼다는 뜻이다(등록 안 함).</summary>
        public delegate System.Collections.IEnumerator Step(App app);

        static readonly System.Collections.Generic.Dictionary<string, Step> Steps =
            new System.Collections.Generic.Dictionary<string, Step> { { "P1", P1Lobby }, { "P4", P4Shop }, { "P10", P10Pet } };

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
            foreach (var st in Stages)
            {
                Step step;
                if (!Steps.TryGetValue(st.Id, out step)) continue;   // 아직 아무도 안 쓴 단계는 조용히 건너뛴다
                ran++;
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
                        UnityEngine.Debug.Log(Line(st.Id, false, e.GetType().Name + " " + e.Message));
                        bad++; failed = true; break;
                    }
                    if (!alive) { stack.Pop(); continue; }
                    if (top.Current is System.Collections.IEnumerator sub) stack.Push(sub);
                    else yield return top.Current;
                }
                if (!failed) { UnityEngine.Debug.Log(Line(st.Id, true)); ok++; }
            }
            UnityEngine.Debug.Log(DoneLine(ok, ran, bad));
        }

        /// <summary>스모크가 꼬리에서 찾는 마지막 한 줄.</summary>
        public const string DonePrefix = LogPrefix + "done ";
        public static string DoneLine(int ok, int ran, int bad) => DonePrefix + ok + "/" + ran + " fail " + bad;
    }
}
