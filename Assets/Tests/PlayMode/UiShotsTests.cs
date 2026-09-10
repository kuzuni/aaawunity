using System.Collections;
using System.Collections.Generic;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T46 «UI 비평 하니스» — UiSmokeTests 와 같은 순서로 화면·팝업을 열어 <b>PNG(540×1170)</b> 와 <b>layout.json</b>(활성 <see cref="UiTag"/> 의 프레임 %) 을 <c>ui-screens/</c> 에 남긴다.
    /// CI(unity-test 잡)가 main push 때 그 폴더를 <c>screens</c> 브랜치로 올리고, 워커가 <c>tools/ui_score.py</c> 로 <c>docs/ref-layout.md</c> 표와 대조해 채점한다(ROUTINE §5).
    /// 파일 이름 = <c>docs/ref/</c> 번호와 같게(01_lobby · 02_battle …). 아직 없는 화면은 건너뛰고 <c>_missing</c> 에 «없음» 으로 적는다.
    /// 검사는 빨간 줄 0(<see cref="PlayLog"/>)과 «PNG·layout 이 하나라도 남았는가» 뿐 — 구도 단언은 UiSmokeTests 가, 채점은 워커가 한다.
    /// </summary>
    public class UiShotsTests
    {
        /// <summary>가장 긴 실제 토스트 문구(T161 · 대장간 재료 안내 · 최악의 이름 «암살자의 목걸이») — `TextSizeGateTests` 와 <b>같은 글</b>이라 두 자가 같은 화면을 본다(T216).</summary>
        const string LongToast = "같은 부위·종류·등급만 재료가 됩니다 (목걸이 · 암살자의 목걸이 · 신화)";

        /// <summary>
        /// T349 — <c>screens</c> 06·07 셋업이 들고 갈 <b>가장 높은 강화 값</b>. 신화 +13 = 표시 등급 «무한 +1»(T316 표: 3 갓 · 6 초월 · 9 불멸 · 12 무한).
        /// <para>12 가 아니라 13 인 까닭 — 12 는 «무한 <b>+0</b>» 이라 화면에 «+N» 이 안 붙는다. 한 칸 더 올려야 «표시 등급 + 다시 센 강화» 둘 다 사진에 나온다.</para>
        /// </summary>
        const int MythPlusForShot = 13;

        App _app; PlayLog _log;
        readonly Dictionary<string, object> _layout = new Dictionary<string, object>();
        readonly List<object> _missing = new List<object>();
        int _saved;

        [SetUp] public void SetUp() { _log = new PlayLog(); _layout.Clear(); _missing.Clear(); _saved = 0; }
        [TearDown] public void TearDown() { _log?.Dispose(); _log = null; Time.timeScale = 1f; }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다(데이터 로드)");
            _app = App.I;
            yield return Frames(2);
        }
        IEnumerator Shutdown()
        {
            Time.timeScale = 1f;
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null;
            yield return Frames(3);
        }
        /// <summary>n 프레임 — HeroView(RenderTexture 타깃) 카메라만 강제로 그린다(배치 모드 · CI #34 규약).</summary>
        IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++)
            {
                foreach (var hv in Object.FindObjectsByType<HeroView>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    if (hv != null && hv.Cam != null && hv.Cam.isActiveAndEnabled) hv.Cam.Render();
                yield return null;
            }
        }
        IEnumerator RealSeconds(float sec) { float t = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t < sec) yield return Frames(1); }

        /// <summary>한 장 — 등장 연출을 끝까지 돌리고(T49 · 비평 PNG 가 연출 중간을 찍지 않게) 레이아웃을 한 번 더 굳힌 뒤(2프레임) PNG + 이름표 사각형.</summary>
        IEnumerator Shot(string name)
        {
            UiKit.CompleteAllTweens();
            yield return Frames(2);
            Canvas.ForceUpdateCanvases();
            if (PlayShot.Save(_app, name))
            {
                _saved++;
                // T58: 프레임이 PNG(RenderTexture) 의 ≥ 95% 를 채워야 눈 비평이 된다 — 첫 screens 배포(CI #83)는 34.8% 띠였다(CopyFrom 이 WorldCam letterbox rect 를 복사)
                Assert.GreaterOrEqual(PlayShot.LastFrameFill, 0.95f, name + ": 프레임이 PNG 의 95% 미만 — " + PlayShot.LastFrameInfo);
            }
            _layout[name] = PlayShot.Layout(_app);
            yield return Frames(1);
        }
        /// <summary>이름이 <paramref name="prefix"/> 로 시작하는 첫 조각(`UiKit.Find` 는 «똑같은 이름» 만 찾는다 — 확률 팝업 칸은 «Odds:등급:번호» 라 등급을 미리 모른다).</summary>
        static Transform FirstNamed(Transform root, string prefix)
        {
            if (root == null) return null;
            if (root.name.StartsWith(prefix)) return root;
            for (int i = 0; i < root.childCount; i++) { var r = FirstNamed(root.GetChild(i), prefix); if (r != null) return r; }
            return null;
        }
        /// <summary>이름으로 버튼을 누른다(onClick 직접 호출 · 입력 장치 없이).</summary>
        static bool Press(Transform root, string name) { if (root == null) return false; var t = UiKit.Find(root, name); var b = t != null ? t.GetComponent<UnityEngine.UI.Button>() : null; if (b == null) return false; b.onClick.Invoke(); return true; }
        GearItem Give(string part, int rar = 0, int plus = 0)
        {
            foreach (var t in _app.Data.Gear.AllTypes) if (t.Part == part) { var g = _app.Save.NewGear(t.Part, t.Type, rar, plus); _app.Save.Inv.Add(g); return g; }
            return null;
        }

        [UnityTest]
        public IEnumerator AllScreensToPngAndLayoutJson()
        {
            yield return Boot();
            var S = _app.Save; var D = _app.Data;
            S.Gold = 11540; S.Gem = 543;   // 재화 pill 에 숫자가 보이게(레퍼런스 느낌 · 수치는 표시용)

            // 01 로비 · 12 설정
            Assert.AreEqual("lobby", _app.Current.Name);
            yield return Shot("01_lobby");
            _app.Overlay.Settings(); yield return Frames(2); yield return Shot("12_settings"); _app.Overlay.Close(); yield return Frames(1);
            // T332 — §5 꼬리가 일곱 시간째 «아무도 안 찍는다» 고 부르던 표 셋 중 둘(㉜ 로비 메뉴 · ㉟ 프로필 둘).
            //   셋 다 «프리팹 그대로 · 레퍼런스 그림 없음» = **회귀 자**인데, 찍히지 않는 동안은 그 회귀 자가 **한 번도 안 돈 것과 같다**.
            //   레퍼런스 번호가 없는 화면이라 이름으로 남긴다(`ev_*`·`res_*` 와 같은 규약).
            LobbyMenu.Open(_app); yield return Frames(2); yield return Shot("lobby_menu"); _app.Overlay.Close(); yield return Frames(1);
            //   ㉟ 는 **한 표에 두 팝업**이라 행 앞머리(«아바타 …»·«이름 …»)로 갈린다 — `ui_score` 의 SCREENS 가 그 갈래를 안다(⑤·⑦ 과 같은 꼴).
            Profile.OpenAvatar(_app); yield return Frames(2); yield return Shot("profile_avatar"); _app.Overlay.Close(); yield return Frames(1);
            Profile.OpenNickname(_app); yield return Frames(2); yield return Shot("profile_nick"); _app.Overlay.Close(); yield return Frames(1);

            // 11 특권 · 15 퀘스트 · 16 출석 · 17 데일리 기프트 · 18 7일 챌린지 · 19 시즌 패스 (T44 로비 사이드 껍데기 — 페이지 2 + 팝업 4)
            // T78(주인 2026-09-07) — 18_challenge7 · 19_pass 는 화면째 삭제돼 촬영 대상이 아니다
            _app.ShowScreen("privilege"); yield return Frames(3); yield return Shot("11_shop_special"); _app.ShowScreen("lobby"); yield return Frames(1);
            LobbyPopups.Quest(_app); yield return Frames(2); yield return Shot("15_quest"); _app.Overlay.Close(); yield return Frames(1);
            // T258 4항 — 같은 팝업의 «업적» 판. 주인이 보는 길이 `screens` PNG 라 판마다 한 장씩 남긴다
            //   (한 장만 찍으면 «업적 탭이 어떻게 생겼나» 를 아무도 못 본다). §5 채점 행은 아직 «일일» 판만 잰다(ref-layout ⑳).
            LobbyPopups.Achievements(_app); yield return Frames(2); yield return Shot("15b_quest_ach"); _app.Overlay.Close(); yield return Frames(1);
            // T305 — «아무것도 안 받음» 으로 찍으면 이 화면의 핵심(받은 칸이 어떻게 갈리는가)이 그림에 안 나온다.
            //   주인이 «거의 구분이 안 감» 이라고 한 것도 그 그림으로는 확인이 안 되던 자리다. ⇒ 1~3일차 받음 · 오늘은 4일차로 찍는다.
            //   찍고 나서 되돌린다 — 뒤에 오는 장들이 «출석 안 받음» 을 전제로 서 있을 수 있다.
            int attDone0 = S.AttDone; string attDay0 = S.AttDay;
            S.AttDone = 3; S.AttDay = "2000-01-01";   // 어제까지 셋 받았다 = 오늘(4일차)은 받을 수 있다
            LobbyPopups.Attendance(_app); yield return Frames(2); yield return Shot("16_attendance"); _app.Overlay.Close(); yield return Frames(1);
            S.AttDone = attDone0; S.AttDay = attDay0;
            LobbyPopups.DailyGift(_app); yield return Frames(2); yield return Shot("17_daily_gift"); _app.Overlay.Close(); yield return Frames(1);
            // 35 공통 «리워드» 획득 팝업(T241 · 표 ㊹) — 어느 지급 자리에서 뜨든 그림은 같으므로 «팝업 그 자체» 를 찍는다.
            // 레퍼런스 35 도 두 칸이라 칸 수를 둘로 맞춘다(칸 수가 바뀌면 칸 폭·묶음 폭이 달라져 표와 어긋난다).
            RewardPopup.Show(new List<RewardPopup.Item> { RewardPopup.Item.Of("ui.coin", "1,000"), RewardPopup.Item.Of("ui.gemRed", "50") });
            yield return Frames(2); yield return Shot("35_reward"); _app.Overlay.Close(); yield return Frames(1);
            // 30 탐험 · 31 빠른 탐험 (T97 — 방치·오프라인 보상 · 표 ㉕·㉖) — 8시간이 쌓인 상태로 찍어야 칸 숫자·«받기» 가 레퍼런스처럼 보인다
            if (_app.Data != null && _app.Data.Expedition != null) _app.Save.ExpSettle = LobbyPopups.NowSec() - _app.Data.Expedition.MaxHours * 3600.0;
            LobbyPopups.Expedition(_app); yield return Frames(2); yield return Shot("30_expedition");
            LobbyPopups.QuickExplore(_app, null); yield return Frames(2); yield return Shot("31_expedition_fast"); _app.Overlay.Close(); yield return Frames(1);
            // 32 챕터 보상 (T137 · 표 ㉝) — 챕터 1 을 깬 상태로 열어야 «받을 수 있는 첫 단» 이 가운데에 온다(레퍼런스 32 의 주황 Claim)
            S.MaxChapter = Mathf.Max(S.MaxChapter, 2); S.SelChapter = S.MaxChapter;
            ChapterChestScreen.Open(_app); yield return Frames(3); yield return Shot("32_lobby_clear");
            _app.ShowScreen("lobby"); yield return Frames(1);

            // 19 시즌 패스 (T266 · 표 ㊼ · 주인 2026-09-09 «걍 다시 넣기» 가 T78 삭제를 뒤집었다 · 지금은 «디자인만»)
            // T322 ⓓ — 레벨이 «세이브에서 온다» 가 되면서(옛 const 32 폐지) **새 세이브는 1 레벨**이다.
            //   그대로 찍으면 이 사진이 «전부 어두운 트랙» 이 되어 주인 레퍼런스 19(29~33 이 밝다)와 견줄 수 없고 §5 도 그 상태를 잰다.
            //   ⇒ 찍기 직전에 세이브를 그 자리로 놓는다(T349 가 장비 사진에 한 것과 같은 손짓 · «셋업이 그 축의 한 값만 담으면 사진이 그 축을 못 본다»).
            //   ⚠ 되돌린다 — 이 세이브는 뒤에 찍는 화면들도 읽는다(T299 ⓑ 가 «승점을 굴리지 않는다» 로 데인 자리).
            int passLv0 = _app.Save.PassLv; bool paid1_0 = _app.Save.PassPaid1;
            var passClaimed0 = new System.Collections.Generic.Dictionary<int, int>(_app.Save.PassClaimed);
            _app.Save.PassLv = 32; _app.Save.PassPaid1 = true;                    // 레퍼런스와 같은 자리(29~33 이 보인다) · 유료 1 열도 열어 «산 열» 꼴을 보여 준다
            for (int lv = 29; lv < 32; lv++) _app.Save.PassClaimed[lv] = Pass.Bit(PassData.ColFree);   // 지난 줄 무료 칸은 «받음»
            SeasonPassScreen.Open(_app); yield return Frames(3); yield return Shot("19_pass");
            _app.Save.PassLv = passLv0; _app.Save.PassPaid1 = paid1_0;
            _app.Save.PassClaimed.Clear(); foreach (var kv in passClaimed0) _app.Save.PassClaimed[kv.Key] = kv.Value;
            _app.ShowScreen("lobby"); yield return Frames(1);

            // 13 펫 탭 · 14 펫 세부 (T42 껍데기)
            _app.ShowScreen("pet"); yield return Frames(3); yield return Shot("13_pet");
            (_app.Current as PetScreen)?.OpenDetail(0); yield return Frames(2); yield return Shot("14_pet_detail"); _app.Overlay.Close(); yield return Frames(1);

            // 20 던전 · 21 던전 세부 · 22 PvP · 23 아레나 입장 · 24 도전 · 25 순위 보상 · 26 상인 (T43 껍데기 · 한 화면 «events» 의 페이지 4 + 팝업 3)
            EventsScreen.Open(_app, EventsScreen.PageDungeon); yield return Frames(3); yield return Shot("20_dungeon");
            var ev = _app.GetScreen<EventsScreen>(); var evRoot = _app.Current.Root;
            if (Press(UiKit.Find(evRoot, "Card:hell"), "EnterBtn")) { yield return Frames(2); yield return Shot("21_dungeon_detail"); _app.Overlay.Close(); yield return Frames(1); } else _missing.Add("21_dungeon_detail (던전 카드 입장 버튼 없음)");
            ev.ShowPage(EventsScreen.PagePvp); yield return Frames(2); yield return Shot("22_arena");
            ev.ShowPage(EventsScreen.PageArena); yield return Frames(3); yield return Shot("23_arena_enter");
            if (Press(evRoot, "ChallengeBtn")) { yield return Frames(2); yield return Shot("24_arena_challenge"); _app.Overlay.Close(); yield return Frames(1); } else _missing.Add("24_arena_challenge (도전 버튼 없음)");
            if (Press(evRoot, "RewardsBtn")) { yield return Frames(2); yield return Shot("25_arena_rank_reward"); _app.Overlay.Close(); yield return Frames(1); } else _missing.Add("25_arena_rank_reward (보상 버튼 없음)");
            ev.ShowPage(EventsScreen.PageMerchant); yield return Frames(3); yield return Shot("26_arena_shop");
            _app.ShowScreen("lobby"); yield return Frames(1);

            // 06 장비(전부 장착 + 인벤 10) · 07 세부 · 08 대장간 · 09 상점
            GearItem firstFree = null;
            foreach (var p in D.Gear.Parts) { var g = Give(p, rar: 1, plus: 1); S.Eq[p] = g.Uid; }
            for (int i = 0; i < 10; i++) { var g = Give(D.Gear.Parts[i % D.Gear.Parts.Length], rar: i % 3, plus: i % 2); if (firstFree == null) firstFree = g; }
            // T349 — **사진이 등급 축의 «한 값» 만 담고 있었다.** 07 이 열던 것은 `firstFree`(= 위 루프의 i=0 = **일반 +0**)라
            //   T324(«모든 등급 제목을 갈색으로»)가 고침 «전에도» 갈색인 사진을 남겼고, 신화가 아예 없어 T316 의 표시 등급(«무한 +1»)은
            //   06·07 어디에도 안 나왔다. 자는 초록이고 사진은 아무 말이 없는 자리다 — 등급·강화는 이 게임에서 가장 자주 바뀌는 축인데.
            //   ⇒ **사진 장수는 안 늘리고**(같은 06·07 이 더 많은 것을 말하게 한다) 셋업에 «가장 높은 값» 하나를 더 얹고, 07 이 그것을 연다.
            var mythPlus = Give(D.Gear.Parts[0], rar: D.Gear.RarMyth, plus: MythPlusForShot);
            {
                // T349 3항 — 재는 것은 **사진이 아니라 셋업**이다. 사진은 사람이 보고, 이 둘은 «사람이 볼 것이 사진에 들어 있는가» 를 지킨다.
                //   누가 이 셋업을 «정리» 하면(옛 두 줄로 되돌리면) 사진은 여전히 찍히고 자도 전부 초록이라 **아무도 못 알아챈다** — 그때 우는 것이 이 둘이다.
                int topRar = 0, topPlus = -1;
                foreach (var g in S.Inv) { if (g.Rar > topRar) topRar = g.Rar; if (g.Rar >= D.Gear.RarMyth && g.Plus > topPlus) topPlus = g.Plus; }
                Assert.GreaterOrEqual(topRar, 1, "06 셋업에 «일반» 말고도 등급이 있어야 한다 — 등급색·배지가 한 값뿐이면 그 축의 회귀는 사진으로 안 잡힌다(T349)");
                Assert.GreaterOrEqual(topPlus, 12, "06 셋업에 «신화 +12 이상» 이 있어야 T316 의 표시 등급(갓·초월·불멸·무한)이 사진에 나온다(T349)");
            }
            _app.ShowScreen("gear"); yield return Frames(3); yield return Shot("06_gear");
            // 07 은 **신화 +13** 을 연다 — 그 한 장이 주인 지시 둘을 같이 보여 준다(T324 = 제목이 갈색인가 · T316 = 배지가 «무한 +1» 인가).
            //   그것이 없으면(표가 등급을 못 주면) 예전처럼 첫 장비로 물러난다 — 사진이 통째로 빠지는 것보다 낫다.
            var detail = mythPlus != null ? mythPlus : firstFree;
            if (detail != null) { GearUi.OpenDetail(_app, detail, null); yield return Frames(2); yield return Shot("07_gear_detail"); _app.Overlay.Close(); yield return Frames(1); }
            _app.ShowScreen("forge"); yield return Frames(3); yield return Shot("08_gear_fuse");
            // 상점(T40) = 세로 스크롤 한 화면 — 레퍼런스 10 = 맨 위(상자 배너 · 상자 카드 2) · 09 = 끝까지 내린 상태(다이아 · 골드)
            _app.ShowScreen("shop"); yield return Frames(3); yield return Shot("10_shop_2");
            // 36 상자 «확률 정보» 팝업 (T267 · 표 ㊾ · 큰 카드의 (i) 를 눌러 연다 — 37 은 같은 화면의 스크롤 다른 구간이라 안 찍는다)
            {
                var big = UiKit.Find(_app.Current.Root, "Box:" + ShopScreen.BigBox(_app.Data).Key);
                var info = big != null ? UiKit.Find(big, "Info") : null;
                var ib = info != null ? info.GetComponent<UnityEngine.UI.Button>() : null;
                if (ib != null)
                {
                    ib.onClick.Invoke(); yield return Frames(3); yield return Shot("36_box_rates");
                    // 38 확률 팝업 안 «아이템 세부» 팝업 (T267 6단계 · 표 ㊿ · 첫 칸을 눌러 연다 — 닫으면 확률 팝업으로 돌아온다)
                    var cell = FirstNamed(_app.Overlay.Root, "Odds:");
                    var cb = cell != null ? cell.GetComponent<UnityEngine.UI.Button>() : null;
                    if (cb != null) { cb.onClick.Invoke(); yield return Frames(3); yield return Shot("38_box_item_detail"); }
                    else _missing.Add("38_box_item_detail (확률 팝업에 칸이 없다)");
                    _app.Overlay.Close(); yield return Frames(1);
                }
            }
            (_app.Current as ShopScreen)?.ScrollTo(0f); yield return Frames(2); yield return Shot("09_shop_1");
            // T332 — ㉞ 소환(뽑기) 결과 창. **09 를 찍은 «뒤»** 에 뽑는다: 뽑으면 세이브가 바뀌어(인벤·Pulls·무료 배지) 앞 장들이 흔들린다.
            //   ⚠ 손으로 `ChestResult` 를 부르지 않는다 — 그러면 «상점에서 뽑으면 결과 창이 뜬다» 는 배선이 끊겨 있어도 초록이다(T280 이 값 주고 세운 규칙).
            //      카드의 «1회» 를 실제로 눌러 화면이 **스스로** 열게 한다.
            //   ⚠ 이 화면은 방금 T315(푸딩 착지 · 상자 피벗을 바닥으로)가 손댄 자리다 — 표 ㉞ 의 «상자 묶음(Chest)» 행이
            //      그 피벗 보정이 **보이는 자리를 안 옮겼는지**를 처음으로 채점한다(그 회귀는 정지 그림에서만 보인다).
            {
                double gem0 = _app.Save.Gem; _app.Save.Gem = 999999;   // 값이 모자라 안 눌리면 그림이 아예 안 나온다(찍는 판의 재화는 표시용이다)
                _app.Current.Refresh(); yield return Frames(1);
                var box = UiKit.Find(_app.Current.Root, "Box:" + ShopScreen.BigBox(_app.Data).Key);
                var one = box != null ? UiKit.Find(box, "One") : null;
                var ob = one != null ? one.GetComponent<UnityEngine.UI.Button>() : null;
                if (ob != null && ob.interactable)
                {
                    ob.onClick.Invoke(); yield return Frames(3);
                    if (_app.Overlay.IsOpen) { yield return Shot("shop_chest_open"); _app.Overlay.Close(); yield return Frames(1); }
                    else _missing.Add("shop_chest_open («1회» 를 눌렀는데 결과 창이 안 열렸다)");
                }
                else _missing.Add("shop_chest_open (큰 상자 카드의 «1회» 버튼을 못 찾았거나 안 눌린다)");
                _app.Save.Gem = gem0; _app.Current?.Refresh(); yield return Frames(1);
            }

            // 02 전투(3초) · 03 적 조우(8초 안에 Engaged 가 되면) · 04 레벨업 · 05 보유 특전
            _app.StartBattle(1); yield return RealSeconds(3f);
            Assert.AreEqual("battle", _app.Current.Name);
            var bs = _app.GetScreen<BattleScreen>(); var G = bs != null ? bs.G : null; Assert.IsNotNull(G, "전투 상태");
            // T47 ⓐ — 3초 안에 레벨업 팝업이 뜨면 02 가 특전 카드 화면(04 와 같은 그림)으로 찍힌다(CI #83) → 03 루프와 같이 닫고 찍는다
            Time.timeScale = 0f;   // 촬영 동안 엔진 정지 — 닫은 뒤 다음 프레임에 또 레벨업이 뜨지 않게(찍고 나서 1 로 되돌린다)
            if (_app.Overlay.IsOpen) { _app.Overlay.Close(); G.Pending = null; yield return Frames(1); }
            yield return Shot("02_battle");
            {   // T47 ⓑ — 월드 행이 layout.json 에 들어간다(BattleWorld.MeasureLayout · ref-layout ② 이름 그대로)
                var l02 = (Dictionary<string, object>)_layout["02_battle"];
                Assert.IsTrue(l02.ContainsKey("플레이어 발밑 y") && l02.ContainsKey("플레이어 높이") && l02.ContainsKey("지면(길) 띠"), "02_battle layout 에 월드 행(플레이어 발밑 y · 플레이어 높이 · 지면(길) 띠)이 있어야 한다");
                Assert.IsTrue(l02.ContainsKey("챕터 제목") && l02.ContainsKey("HP 바"), "02_battle 은 팝업이 아니라 HUD 를 찍어야 한다(챕터 제목 · HP 바 이름표)");
                // T215 — 지면(길) 띠는 표 ②(레퍼런스 실측 y30.0 h21.0)의 자리다. 데모 씬 스케일 그대로면 h16.2 라 §5 02·03 이 0.5점씩 깎였다.
                // 기댓값은 표에서 읽은 수를 그대로 적는다(코드 상수를 부르지 않는다 · 결정 555) · 판정 여유는 §5 와 같은 ±3%p 보다 좁게 잡는다.
                { var band = (List<object>)l02["지면(길) 띠"]; Assert.AreEqual(21.0, (double)band[3], 1.0, "지면(길) 띠 높이 = 표 ② 21.0%"); Assert.AreEqual(30.0, (double)band[1], 1.5, "지면(길) 띠 윗변 = 표 ② 30.0%"); }
            }
            Time.timeScale = 1f;
            bool engaged = false; float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 8f) { if (bs.World != null && bs.World.Engaged && !_app.Overlay.IsOpen) { engaged = true; break; } if (_app.Overlay.IsOpen) { _app.Overlay.Close(); G.Pending = null; } yield return Frames(1); }
            if (engaged) yield return Shot("03_battle_enemy"); else _missing.Add("03_battle_enemy (8초 안에 적 조우 없음)");
            Time.timeScale = 0f; _app.Overlay.Close(); G.Pending = null; yield return Frames(1);
            var rng = new Mulberry32(7u);
            var offer = Perks.Offer(D, G.Taken, false, rng);
            if (offer.Count > 0)
            {
                G.Pending = new PendingDecision { Kind = PendingKind.LevelUp, Offer = offer };
                _app.Overlay.LevelUp(G, pick => G.ResolveLevelUp(pick)); yield return Frames(2); yield return Shot("04_perks");
                _app.Overlay.Close(); G.Pending = null; yield return Frames(1);
                for (int i = 0; i < offer.Count && i < 3; i++) G.Taken.Add(offer[i]);
            }
            _app.Overlay.PerkBook(G, null); yield return Frames(2); yield return Shot("05_perks_list"); _app.Overlay.Close(); yield return Frames(1);

            // T240 1항 — PvP 인게임(33). **아레나 «판» 으로 다시 연다** — 같은 챕터라도 아레나면 무대(모래)·상대 외형(기사)·머리(VS 바)가 전부 갈리므로
            //   02_battle 을 다시 쓰면 안 된다. 순위 1 을 주는 까닭은 그래야 상대 전투력이 «—» 가 아닌 실제 수로 찍히기 때문이다(표 ㊺ «전투력 줄»).
            Time.timeScale = 1f;
            _app.StartBattle(1, null, null, "도전자 1", 1); yield return RealSeconds(2f);
            Time.timeScale = 0f; _app.Overlay.Close(); yield return Frames(2);
            yield return Shot("33_pvp_battle");
            {   // ⚠ 가드(ContainsKey)를 두지 않는다 — Shot 이 _layout[name] 을 **늘** 채우므로 가드는 «못 찍었을 때 조용히 넘어가는» 구멍만 만든다(02_battle 과 같은 꼴).
                var l33 = (Dictionary<string, object>)_layout["33_pvp_battle"];
                Assert.IsTrue(l33.ContainsKey("빨간 VS 바") && l33.ContainsKey("VS 배지(금·원)"), "33 은 PvP 머리를 찍어야 한다(빨간 VS 바 · VS 배지)");
                Assert.IsFalse(l33.ContainsKey("챕터 제목"), "PvP 판에는 «챕터 N» 제목이 없다(웨이브가 없어 뜻이 없다)");
            }
            // T299 ⓑ — PvP 결과(34). **표 ㊻ 는 2026-09-08 12:2X 에 섰고 화면(`ArenaResult`)도 14:4X 에 섰는데
            //   여기 찍는 자리가 없어 §5 가 스무 시간 동안 이 화면을 한 번도 안 셌다** — 요약이 «찍힌 화면» 만 돌기 때문이고,
            //   그래서 «안 찍힌 화면 0개» 라는 초록말이 계속 나왔다(그 눈은 이 회차 ⓐ 가 고쳤다 · ui_score.unscored_tables).
            //   ⚠ **승점을 실제로 굴리지 않는다**(`ArenaMatch.Settle` 은 세이브를 고친다) — 여기서 굴리면
            //     뒤에 찍는 아레나 화면(22·23·24)이 이 판 때문에 흔들려 «그 화면이 바뀌었나» 를 못 읽게 된다.
            //     화면은 «받은 Outcome 을 그대로 그리고 다시 계산하지 않는다» 니(ArenaResult 주석) 여기서는 값을 들려 보낸다.
            {
                var oc = new ArenaMatch.Outcome
                {
                    Win = true, Before = 1000, After = 1008, Delta = 8,
                    RankBefore = 12, RankAfter = 11, BestImproved = true,
                    Tier = ArenaMatch.TierOf(D != null ? D.ArenaMatch : null, 1008),
                };
                ArenaResult.Show(oc, "나", "도전자 1", null, null, null);
                yield return Frames(2);
                yield return Shot("34_pvp_win");
                {   // ⚠ 33 과 같은 까닭으로 가드(ContainsKey)를 두지 않는다 — Shot 은 _layout[name] 을 늘 채우므로
                    //    가드는 «못 찍었을 때 조용히 넘어가는» 구멍만 만든다.
                    var l34 = (Dictionary<string, object>)_layout["34_pvp_win"];
                    Assert.IsTrue(l34.ContainsKey("방패 엠블럼") && l34.ContainsKey("티어 명판"),
                                  "34 는 결과 화면을 찍어야 한다(방패 엠블럼 · 티어 명판)");
                }
                // ⚠ 여기서 `Overlay.Close()` 로 지우면 **`ArenaResult.Open` 이 true 로 남는다**(그 static 은 «계속» 만 내린다) —
                //    같은 판에서 뒤에 도는 자가 그 값을 물으면 «안 눌렀는데 떠 있다» 는 거짓을 읽는다. 닫는 길로 닫는다.
                var contBtn = UiKit.Find(_app.Overlay.Root, "ContinueBtn")?.GetComponent<UnityEngine.UI.Button>();
                Assert.IsNotNull(contBtn, "34 를 닫는 길은 «계속» 버튼 하나다");
                contBtn.onClick.Invoke(); yield return Frames(1);
            }
            // ev_devil · ev_angel — T141 6항 «워커가 한 장 찍어 04 와 나란히 눈으로»(판 없이 어둠 위인가).
            // 레퍼런스 그림이 없는 화면이라 번호 대신 이름으로 남긴다.
            // ⚠ T213 — «표가 없어 채점 대상이 아니다» 는 반쪽이었다: 이 둘은 **이름표(UiTag)조차 없어** layout.json 에
            //    빈 칸 `{}` 으로 남았고, `ui_score --all` 이 그 자리에 «—» 를 찍고 지나갔다(자 «밖» 의 화면 둘).
            //    Overlay.Devil/Angel 에 이름표를 달았으니 이제 재진다 — 아래 단언이 그 상태를 지킨다.
            {
                var devilPerk = Perks.OfferDevil(D, G.Taken, rng);
                G.Pending = new PendingDecision { Kind = PendingKind.Devil, DevilPerk = devilPerk };
                _app.Overlay.Devil(G, _ => { }); yield return Frames(2); yield return Shot("ev_devil");
                _app.Overlay.Close(); G.Pending = null; yield return Frames(1);
                G.Pending = new PendingDecision { Kind = PendingKind.Angel };
                _app.Overlay.Angel(G, _ => { }); yield return Frames(2); yield return Shot("ev_angel");
                _app.Overlay.Close(); G.Pending = null; yield return Frames(1);
                // T216 1단계 — 여기까지 «자는 드나드는데 사진이 없던» 화면 여섯(+ 아래 로비에서 둘).
                // 이 여섯은 `TextSizeGateTests` 가 이미 한 줄씩 열고 있었다(글자 하한·잘림은 실패로 세는 화면이다) —
                // 그런데 PNG 가 없어 **§5 도 워커 눈도 닿은 적이 없다**. 특히 결과 팝업 셋(res_*)은
                // `BorderAudit.StrictScreens` 안에까지 들어 있으면서(테두리는 실패로 센다) 그림은 한 번도 안 봤고,
                // 주인이 **판마다 보는 화면**이다. 여는 코드는 그쪽에서 그대로 옮겼다(같은 순서 · 같은 인자).
                G.Gold = 12750; G.Kills = 137;
                _app.Overlay.Rest(G, _ => { }, () => { }); yield return Frames(2); yield return Shot("ev_rest");
                _app.Overlay.Close(); yield return Frames(1);
                _app.Overlay.DevilGift(devilPerk, null); yield return Frames(2); yield return Shot("ev_devil_gift");
                _app.Overlay.Close(); yield return Frames(1);
                _app.Overlay.AdCountdown(9, () => { }); yield return Frames(2); yield return Shot("ev_ad");
                _app.Overlay.Close(); yield return Frames(1);
                _app.Overlay.Clear(G, false, () => { }, () => { }); yield return Frames(2); yield return Shot("res_win");
                _app.Overlay.Close(); yield return Frames(1);
                _app.Overlay.Clear(G, true, () => { }, () => { }); yield return Frames(2); yield return Shot("res_win_last");
                _app.Overlay.Close(); yield return Frames(1);
                _app.Overlay.Dead(G, () => { }); yield return Frames(2); yield return Shot("res_lose");
                _app.Overlay.Close(); yield return Frames(1);
                // T274 1단계 — 같은 팝업의 «부활권» 갈래. 위 한 줄로는 이 갈래가 **영영 안 찍힌다**:
                // `ReviveBtn`·`ReviveHint` 는 `canRevive: true` 일 때만 서는데 이 화면을 여는 네 자리
                // (여기 · BorderGateTests:547 · TextSizeGateTests:158 · PercentGateTests:179)가 전부 기본값이라
                // T254 1항이 세운 버튼을 **글자 하한도 테두리도 자리 표도 한 번도 본 적이 없다**(결정 724).
                // ⚠ 개수를 0 으로 주는 것이 «인색한 사진» 이 아니라 **가장 많이 보이는 화면**이다 —
                //   `BattleScreen:345` 의 canRevive 는 «이 판에 아직 안 썼나» 뿐이라 티켓 0 이어도 버튼이 서고,
                //   그때만 안내 줄(`ReviveHint`)이 같이 뜬다. 즉 0 으로 찍어야 **새 요소 둘이 한 장에 다 든다**
                //   (2 로 찍으면 안내 줄이 없어 그 줄은 여전히 아무도 못 본다).
                _app.Overlay.Dead(G, () => { }, () => { }, 0, true); yield return Frames(2); yield return Shot("res_lose_revive");
                // T274 5b — 여기 있던 «[T274] 글자 크기» 임시 로그는 **할 일을 마치고 걷었다**:
                // 그 수(run 579 실측 · 버튼 44/44 · 안내 40/40 · 잘림 0)를 보고 `TextSizeGateTests` 로 올렸으므로
                // 이제 그 자가 매 런 같은 것을 **막는 자로** 잰다. 로그를 남겨 두면 자와 같은 말을 두 번 하는 자리가 된다.
                _app.Overlay.Close(); yield return Frames(1);
            }
            Time.timeScale = 1f; _app.ShowScreen("lobby"); yield return Frames(2);

            // T216 1단계 — 나머지 둘. 토스트는 «가장 긴 실제 문구»(대장간 재료 안내 · 최악의 이름 = 암살자의 목걸이 · T161)로 찍는다 —
            // 짧은 글로 찍으면 사진이 있어도 «칸이 모자란가» 를 못 본다(`TextSizeGateTests` 가 같은 문구를 쓰는 까닭이다).
            _app.Toast(LongToast); yield return Frames(2); yield return Shot("27_toast");
            // ⚠ 토스트는 1.8초를 살고 스스로 꺼진다(`App.Toast` · `_toastT`). 그대로 다음 장을 찍으면
            // «데이터 삭제» 확인 팝업 사진에 토스트가 얹혀 나온다 — 사진을 남기는 자에게 그것은 «틀린 사진» 이다.
            // 게이트라면 몇 프레임이 아깝지만 이 자의 결과물은 **사람이 보는 그림**이라 꺼질 때까지 기다린다.
            yield return RealSeconds(2f);
            _app.Overlay.ConfirmReset(); yield return Frames(2); yield return Shot("28_confirm_reset");
            _app.Overlay.Close(); yield return Frames(1);

            // 20~26 은 T43 · 11·15~19 는 T44 가 위에서 찍는다 — 이제 «없음» 화면이 없다(_missing 은 03 조우 실패 때만)
            PlayShot.WriteLayout(_layout, _missing);
            Assert.Greater(_saved, 0, "PNG 가 하나도 안 남았다(RenderTexture 캡처 실패)");
            Assert.IsTrue(_layout.ContainsKey("01_lobby") && ((Dictionary<string, object>)_layout["01_lobby"]).Count > 0, "로비 이름표(UiTag)가 layout.json 에 있어야 한다");
            // T213 — «찍히기는 하는데 아무것도 안 재는» 화면을 막는다. 빈 칸이면 §5 자가 그 화면을 통째로 못 본다.
            // T219 1단계 — 결과 팝업 셋을 같은 자로 지킨다(주인이 판마다 보는 화면이고 `BorderAudit.StrictScreens` 안이다).
            // T219 3단계 — 남은 다섯(ev_rest·ev_devil_gift·ev_ad·27_toast·28_confirm_reset)까지 채웠다. **이로써 T216 이 낸 여덟 장이 전부 재진다.**
            // T274 1단계 — `res_lose_revive` 도 같은 자로 지킨다(이름표가 비면 §5 가 그 화면을 «—» 로 지나친다).
            foreach (var evName in new[] { "ev_devil", "ev_angel", "res_win", "res_win_last", "res_lose", "res_lose_revive",
                                           "ev_rest", "ev_devil_gift", "ev_ad", "27_toast", "28_confirm_reset" })
                Assert.IsTrue(_layout.ContainsKey(evName) && ((Dictionary<string, object>)_layout[evName]).Count > 0,
                    evName + " 에 이름표(UiTag)가 하나도 없다 — layout.json 이 빈 칸이면 `ui_score` 가 그 화면을 «—» 로 지나친다(T213)");
            // ⚠ 27_toast 만 «비었나» 로는 못 지킨다 — 토스트는 팝업이 아니라 로비 «위» 라 팝업 층이 안 열려 있고,
            //   `PlayShot.Layout` 은 그때 캔버스 전체를 잰다(팝업이 열렸을 때만 Overlay 층으로 좁힌다).
            //   그래서 이 칸에는 토스트 이름표가 하나도 없어도 **로비 이름표 13개가 이미 들어가 있어** 위 단언이 그냥 통과한다(실측 run 447).
            //   토스트 제 이름표를 이름으로 콕 집어야 이 자가 뜻이 있다 — 표(㊴)를 세울 워커도 그 13개는 «로비 것» 임을 알아야 한다.
            var toast = (Dictionary<string, object>)_layout["27_toast"];
            foreach (var n in new[] { "토스트 띠", "토스트 문구" })
                Assert.IsTrue(toast.ContainsKey(n), "27_toast 에 «" + n + "» 이름표가 없다 — 이 칸의 나머지는 로비 이름표라 «비었나» 로는 안 잡힌다(T219 3단계)");
            _log.AssertNoRed("스크린샷 회차(전 화면)");
            yield return Shutdown();
        }
    }
}
