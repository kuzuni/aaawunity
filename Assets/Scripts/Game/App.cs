using System;
using System.Collections.Generic;
using KkomaKnight.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 게임 루트 — 데이터·세이브·카탈로그·프레임·화면 전환·오버레이·토스트. index.html 의 «전역 + showScreen» 에 해당.
    /// 화면(GameScreen) 은 각자 프레임 안에 자기 RectTransform 을 세우고, App 이 하나만 켠다.
    /// </summary>
    public sealed class App : MonoBehaviour
    {
        public static App I { get; private set; }
        public GameData Data { get; private set; }
        public SaveData Save { get; private set; }
        public AssetCatalog Assets { get; private set; }
        public RectTransform Frame { get; private set; }
        /// <summary>프레임 «밖» 레터박스 띠를 채우는 바탕(T182 2단계 · 테스트가 띠 넷을 잰다).</summary>
        public FrameBackdrop Backdrop { get; private set; }
        /// <summary>T106 — 노치·펀치홀을 피한 영역(<see cref="SafeAreaRoot"/>). <see cref="Frame"/> 의 부모다.</summary>
        public RectTransform SafeArea { get; private set; }
        public Canvas UiCanvas { get; private set; }
        public Overlay Overlay { get; private set; }
        public Camera WorldCamera { get; private set; }

        readonly Dictionary<string, GameScreen> _screens = new Dictionary<string, GameScreen>();
        GameScreen _current;
        RectTransform _toastRt; TMP_Text _toastText; float _toastT;
        // T358(주인 2026-09-10 «게임 입장할 때 로딩 좀 화면 되게 하기») — 전투 입장 로딩 조각(부팅과 같은 Title_Loading · LoadingScreen).
        //   StartBattle 이 전투 화면을 세우기 «전에» 띄우고, 전투가 첫 프레임을 그린 뒤 + 최소 표시 시간(LoadingScreen.MinSeconds)이 지나면 Update 가 내린다.
        LoadingScreen _battleLoading; int _battleLoadFrames;
        /// <summary>T358 — 지금 떠 있는 전투 입장 로딩(없으면 null · 자가 «떴다 사라졌다» 를 잰다).</summary>
        public LoadingScreen BattleLoading => _battleLoading;

        public static App Create(GameData data, AssetCatalog catalog, Font font, Camera worldCamera)
        {
            var go = new GameObject("App");
            var app = go.AddComponent<App>();
            I = app;
            app.Data = data; app.Assets = catalog; app.WorldCamera = worldCamera;
            if (font == null && catalog != null) font = catalog.Font("font.ui");
            if (font != null) UiKit.DefaultFont = font;
            app.Save = SaveStore.Load(data);
            Debug.Log("[KkomaKnight] boot: save");   // T59 진단 마커 — 브라우저 콘솔에서 어디까지 왔는지(WebGL 크래시 위치 좁히기 · 릴리스에서도 무해한 한 줄)
            // T257 4항 — «접속했다». **화면을 세우기 전에** 부른다: 여기서 날·주가 밀리고(QuestRun.Roll) 로비의 빨간 점·퀘스트 줄이
            // 첫 그림부터 오늘 값으로 선다. 뒤에 두면 «켠 직후 한 번은 어제 것이 보였다가 다음 새로고침에 바뀌는» 자리가 생긴다.
            Quests.Login(app);
            Audio.Create(app);   // 배경음·효과음(T28) — 세이브(음소거)와 카탈로그(bgm.*/snd.*)를 읽는다 · BuildUi 의 첫 ShowScreen 이 로비 곡을 튼다
            Debug.Log("[KkomaKnight] boot: audio");
            app.BuildUi();
            return app;
        }

        void BuildUi()
        {
            UiKit.EnsureEventSystem();
            UiCanvas = UiKit.CreateRootCanvas("UI", 10);
            SafeArea = UiKit.CreateSafeArea(UiCanvas.transform);   // T106 — 노치를 피한 영역 · 화면 UI 는 전부 이 안(상단·하단 프레임 띠만 이 밖으로 뻗는다)
            Frame = UiKit.CreateFrame(SafeArea);
            // T182 2단계 — 프레임 «밖» 레터박스 띠를 게임 배경으로 채운다(태블릿 3:4 에서 화면의 38.4% 가 검은 띠였다 · 주인 «갤럭시 탭까지»).
            // 프레임 안은 한 픽셀도 안 덮는다 — 전투 마당은 WorldCam 이 캔버스 «뒤» 에 그리므로 통짜 배경 한 장이면 마당이 사라진다.
            Backdrop = FrameBackdrop.Create(UiCanvas.transform, Frame);
            Frame.GetComponent<Image>().enabled = false;   // 프레임 안은 각 화면이 채운다 · 전투는 카메라가 보인다
            if (WorldCamera != null)
            {
                WorldCam.Attach(WorldCamera, Frame);
                WorldCamera.clearFlags = CameraClearFlags.SolidColor; WorldCamera.backgroundColor = Palette.Hex("#86E4FF");   // GUI Pro 로비 배경 하늘색
            }
            Register(new LobbyScreen()); Register(new GearScreen()); Register(new ForgeScreen()); Register(new ShopScreen()); Register(new PetScreen()); Register(new BattleScreen()); Register(new EventsScreen());
            // T44 로비 사이드 페이지(특권 · 껍데기) · 시즌 패스 페이지는 T78(주인 2026-09-07)로 삭제 · T98 챕터 보상 페이지(레퍼런스 32)
            Register(new PrivilegeScreen()); Register(new ChapterChestScreen()); Register(new SeasonPassScreen());   // T266 — 시즌 패스는 T78 로 지웠다가 주인 2026-09-09 지시로 되살렸다
            Overlay = new Overlay(this);
            // 토스트 (GUI Pro ToastMessage_01) — 칸 세로는 본문 40 두 줄이 들어가는 Layout.Toast (T63-toast · 전 5.0% 에선 긴 문구가 bestFit 으로 32 까지 줄었다)
            _toastRt = (RectTransform)UiKit.Spawn("ui.toast", Frame).transform; UiKit.Pct(_toastRt, Layout.Toast);
            _toastText = _toastRt.GetComponentInChildren<TMP_Text>(true);
            // T219 2단계 — 이름표가 없으면 §5 하니스의 layout.json 에 `27_toast` 가 «빈 칸» 으로 남는다(자가 아무것도 못 잰다).
            // 이름은 ref-layout 표의 «요소» 열과 **글자 그대로** 같아야 `tools/ui_score.py` 가 짝을 짓는다.
            UiKit.Tag(_toastRt, "토스트 띠");
            if (_toastText != null) UiKit.Tag(_toastText.rectTransform, "토스트 문구");
            _toastRt.gameObject.SetActive(false);
            Debug.Log("[KkomaKnight] boot: ui");
            ShowScreen("lobby");
            Debug.Log("[KkomaKnight] ready lobby");   // T60 배포 스모크가 기다리는 마커(tools/webgl_smoke.js) — 문구 바꾸면 스크립트도 같이
        }

        void Register(GameScreen s) { s.App = this; _screens[s.Name] = s; }

        public void ShowScreen(string name)
        {
            if (!_screens.TryGetValue(name, out var s)) { Debug.LogError("화면 없음: " + name); return; }
            if (name != "battle") HideBattleLoading();   // T358 — 전투를 떠나면(포기·로비) 로딩이 남아 있을 수 없다
            if (_current != null && _current != s) _current.Hide();
            _current = s;
            s.Show();
            Audio.Bgm(name == "battle" ? "bgm.battle" : "bgm.lobby");   // 화면 전환 시 로비/전투 곡 자동 교체(T28 · 같은 곡이면 무시 · 보스 곡은 BattleWorld 가)
            Overlay?.Root.SetAsLastSibling();
            _toastRt.SetAsLastSibling();
        }
        public GameScreen Current => _current;
        public T GetScreen<T>() where T : GameScreen { foreach (var s in _screens.Values) if (s is T t) return t; return null; }

        /// <param name="dungeonKey">T228 ⓓ — 던전에서 들어온 판이면 그 던전 키(<c>null</c> = 일반 챕터 전투). 클리어하면 <c>DungeonSweep.Record</c> 가 이 키로 «깬 적 있다» 를 남기고, 그것이 소탕의 조건이다.</param>
        /// <param name="arenaFoe">T240 — 아레나 «도전» 으로 들어온 판이면 <b>상대 이름</b>(<c>null</c> = 아레나가 아니다).
        /// 이 한 값이 «판이 끝나면 승점을 옮기고 결과 화면을 띄운다» 를 켠다(<c>BattleScreen.EndRun</c>) — 던전의 <paramref name="dungeonKey"/> 와 같은 꼴이다.</param>
        /// <param name="arenaFoeRank">T240 3항 — 아레나 판이면 <b>상대의 순위</b>(0 = 모름). 이 값이 있어야 상대 전투력을 구해
        /// 1대1 판의 스탯을 풀 수 있다(<c>ArenaDummy.Power</c> → <c>ArenaFoe.Of</c>) — 이름만으로는 상대가 얼마나 센지 알 길이 없다.</param>
        public void StartBattle(int chapter, DungeonData.RunRule run = null, string dungeonKey = null, string arenaFoe = null, int arenaFoeRank = 0)
        {
            // T291 — **층 던전은 «얼마나 올라왔는가» 가 세기라 챕터 진행도로 깎지 않는다.**
            //   층 5 를 여는 사람은 층 4 를 깬 사람이고, 그 판의 세기는 표가 정한다(FloorChapter). 진행도로 깎으면
            //   챕터를 덜 깬 사람에게만 깊은 층이 쉬워져 **같은 층이 사람마다 다른 판**이 된다.
            //   층이 없는 던전·챕터·아레나는 예전 그대로 진행도까지가 상한이다.
            bool floorRun = !string.IsNullOrEmpty(dungeonKey) && Data != null && Data.Dungeon != null
                            && Data.Dungeon.Of(dungeonKey) != null && Data.Dungeon.Of(dungeonKey).Floors != null;
            int cap = floorRun ? Math.Max(1, Data.Tune.MaxChapter) : Math.Max(1, Save.MaxChapter);
            chapter = Mathf.Clamp(chapter, 1, cap);
            // T257 4항 — «도전했다» 를 세는 자리는 **여기 하나다**. 판에 들어오는 길이 셋(로비 START · 던전 «도전» · 아레나 «도전») 인데
            // 셋 다 이 문을 지나므로, 부르는 쪽마다 한 줄씩 박으면 길이 하나 늘 때 조용히 안 세는 자리가 생긴다(결정 787).
            // 아레나는 어느 쪽도 아니다(챕터 진행도 던전도 아닌 판이다) · 던전이면 던전 도전 · 나머지가 챕터 도전이다.
            if (arenaFoe == null) Quests.Bump(this, string.IsNullOrEmpty(dungeonKey) ? Quests.ChapterTry : Quests.DungeonTry);
            // T258 3항 — 업적은 같은 사건을 **더 잘게** 센다: 아레나 도전이 따로 있고, 던전은 지옥문·원정이 갈린다.
            // 그래서 퀘스트의 `dungeonTry` 한 줄로는 못 담는다 — 어느 던전인지 아는 자리가 여기뿐이라 여기서 이름을 댄다.
            if (arenaFoe != null) Quests.Ach(this, Quests.AchArenaTry);
            else if (dungeonKey == "hell") Quests.Ach(this, Quests.AchDungeonHell);
            else if (dungeonKey == "expedition") Quests.Ach(this, Quests.AchDungeonExpd);
            // T358 — 로딩 조각을 전투 화면을 세우기 «전에» 띄운다(부팅과 같은 Title_Loading · Frame 의 맨 위 = Overlay·토스트보다 위).
            //   세우는 일(BattleState + BattleWorld 스폰)은 이 프레임 안에서 동기로 끝나므로, 조각이 «가리는» 것은 그 스폰 프레임과
            //   첫 그린 프레임이다 — 그 뒤엔 Update(TickBattleLoading)가 MinSeconds 를 채우고 내린다. 조각이 없으면(카탈로그 결손) 옛 흐름 그대로.
            HideBattleLoading();
            _battleLoading = LoadingScreen.Show(Frame, Assets); _battleLoadFrames = 0;
            if (_battleLoading != null) LoadingScreen.LastBattleShown = true;
            ShowScreen("battle");
            GetScreen<BattleScreen>().Start(chapter, run, dungeonKey, arenaFoe, arenaFoeRank);   // T183 — run 이 null 이면 지금까지와 똑같은 일반 전투다
            if (_battleLoading != null && _battleLoading.Root != null) _battleLoading.Root.transform.SetAsLastSibling();   // ShowScreen 이 Overlay·토스트를 맨 위로 올린 뒤라 다시 맨 위로
            Debug.Log("[KkomaKnight] ready battle");   // T60 배포 스모크 마커
        }

        /// <summary>
        /// 배포 스모크 진단 훅(T60) — 브라우저 JS 가 <c>unityInstance.SendMessage("App", "DebugGo", "battle")</c> 로 부른다(GameObject 이름 = «App»).
        /// «battle» = 선택 챕터로 전투 진입 · «lobby» = 로비 · «perf» = 지금 도는 트윈 수 + Bloom 상태를 로그 한 줄로(T129 ⓑ · 화면은 안 바뀐다) ·
        /// <b>«bloom:off»·«bloom:on»·«bloom»(토글)</b> = 월드 후처리 스위치(T181 ⓐ · 같은 런 안에서 fps 를 갈라 재려고 · 화면 구도·세이브 불변). 그 외는 무시(로그 한 줄).
        /// 게임 로직은 StartBattle/ShowScreen 그대로 — 새 기능이 아니라 진입 경로만 연다.
        /// </summary>
        public void DebugGo(string what)
        {
            if (Save == null || Data == null) { Debug.LogWarning("[KkomaKnight] DebugGo: 아직 준비 전 — " + what); return; }
            switch (what)
            {
                case "battle": StartBattle(Save.SelChapter); break;
                case "lobby": Overlay?.Close(); GetScreen<BattleScreen>()?.Abort(); ShowScreen("lobby"); break;
                // T129 ⓑ — «지금 몇 개가 도나» 한 줄. 스모크가 fps 를 재기 직전에 불러 fps 옆에 같이 적는다(문구 바꾸면 tools/webgl_smoke.js 도 같이).
                case "perf": Debug.Log("[KkomaKnight] perf tweens=" + UiKit.PlayingTweens() + " screen=" + (_current != null ? _current.Name : "-") + " bloom=" + (PostFx.Enabled ? "on" : "off")); break;
                // T181 ⓐ — Bloom 을 «한 런 안에서» 껐다 켜며 재는 손잡이(«bloom:off» · «bloom:on» · «bloom» = 토글).
                // 런 사이 절대 fps 는 못 쓴다(같은 빌드가 23.8 ↔ 16.4 · 결정 524) — 이 스위치가 있어야 «Bloom 값» 을 노이즈 밖에서 잰다.
                // 화면·세이브는 한 줄도 안 바뀐다(카메라 후처리 스위치 하나다).
                case "bloom": case "bloom:on": case "bloom:off":
                    PostFx.Enabled = what == "bloom" ? !PostFx.Enabled : what == "bloom:on";
                    Debug.Log("[KkomaKnight] perf bloom=" + (PostFx.Enabled ? "on" : "off"));
                    break;
                // T300 2항 — «플레이 봇» 을 배포 빌드 안에서 돌린다(주인 «플레이해서 에러 테스트도 하라»).
                //   각본은 PlayMode 자와 **같은 한 벌**(`Playthrough.Stages`)이고, 단계마다 로그 한 줄을 찍는다.
                //   ⚠ 이 갈래는 스모크가 부를 때만 돈다 — 보통 플레이에는 한 줄도 안 걸린다(주인 폰에 아무것도 안 보인다 · 5항).
                case "play": StartCoroutine(Playthrough.Run(this)); break;
                default: Debug.Log("[KkomaKnight] DebugGo: 모르는 목적지 — " + what); break;
            }
        }

        public void Persist() => SaveStore.Save(Save);

        /// <summary>
        /// T318 — 퀘스트 «이동»: 표의 목적지(<see cref="QuestData.Go"/>)로 화면을 열고 그 버튼 위에 손가락 힌트를 세운다(<see cref="QuestGo.Open"/>).
        /// <paramref name="go"/> 가 null 이면(로그인류) 아무 일도 안 한다. 돌려주는 값 = 손가락이 선 버튼(못 찾으면 null · 경고 한 줄).
        /// </summary>
        public RectTransform Hint(QuestData.Go go) => QuestGo.Open(this, go);
        /// <summary>T318 — 지금 화면(팝업이 열려 있으면 팝업)에서 이름이 <paramref name="point"/> 인 켜진 버튼에 손가락 힌트. 못 찾으면 null.</summary>
        public RectTransform Hint(string point) => QuestGo.Hint(this, point);

        /// <summary>
        /// «데이터 삭제»(T29) — 세이브 키 삭제 → 새 세이브로 교체 → 전투 중이면 판을 버리고(골드 은행 없음) → 로비를 새로 그린다. 설정 팝업의 확인(«삭제»)에서만 부른다.
        /// 화면들은 전부 <see cref="Save"/> 를 매번 읽으므로(캐시 없음) 교체 뒤 <see cref="ShowScreen"/> 의 Refresh 가 새 값을 그린다. 음소거는 새 세이브(해제)로 바로 반영.
        /// </summary>
        public void ResetSave()
        {
            Overlay?.Close();
            GetScreen<BattleScreen>()?.Abort();
            Save = SaveStore.Reset(Data);
            Audio.ApplyMute();
            ShowScreen("lobby");
            Toast("데이터를 삭제했습니다");
        }

        /// <summary>
        /// 화면 아래 토스트 한 줄. 문구는 <see cref="TextGlyphs.Safe"/> 로 거른다 — Jua 에 없는 «·»·«×»·«→»·이모지는 유니티가 <b>폭 0</b> 으로 흘려
        /// «같은 부위·종류·등급만» 이 «같은 부위종류등급만» 으로 붙어 나왔다(T63-toast). 문구 자체는 부르는 화면 코드의 것이라 여기서 한 번에 거른다.
        /// </summary>
        public void Toast(string msg)
        {
            if (_toastText != null) _toastText.text = TextGlyphs.Safe(msg);
            _toastRt.gameObject.SetActive(true); _toastRt.SetAsLastSibling(); _toastT = 1.8f; UiKit.PopIn(_toastRt, 0.9f, 0.2f);
        }

        void Update()
        {
            if (_toastT > 0) { _toastT -= Time.unscaledDeltaTime; if (_toastT <= 0) _toastRt.gameObject.SetActive(false); }
            _current?.Tick(Time.deltaTime);
            Overlay?.Tick(Time.unscaledDeltaTime);
            TickBattleLoading();
        }

        /// <summary>
        /// T358 — 전투 입장 로딩을 내리는 조건: 전투 화면이 <b>첫 프레임을 그린 뒤</b>(StartBattle 이 든 프레임 다음 프레임부터 셈) + 최소 표시
        /// <see cref="LoadingScreen.MinSeconds"/>(깜빡임 방지 · 부팅과 같은 값). 진행 바는 실제 «단계» 가 없으므로(스폰은 한 프레임에 동기로 끝난다) 시간으로 채운다(절 1항).
        /// <para>⚠ 배치 모드(CI PlayMode·screens)에서는 첫 Update 에 바로 내린다 — 보는 사람이 없어 «깜빡임 방지» 가 뜻이 없고, 0.3초를 지키면
        /// StartBattle 직후 화면을 찍는 자·사진 수십 장이 로딩 조각을 찍는다(결정 1003). «떴다» 는 <see cref="LoadingScreen.LastBattleShown"/> 이 기록한다.</para>
        /// </summary>
        void TickBattleLoading()
        {
            if (_battleLoading == null) return;
            if (_battleLoading.Root == null || _current == null || _current.Name != "battle") { HideBattleLoading(); return; }
            _battleLoadFrames++;
            bool batch = Application.isBatchMode;
            _battleLoading.SetProgress(batch ? 1f : _battleLoading.Elapsed / LoadingScreen.MinSeconds);
            bool drawn = _battleLoadFrames >= 2;
            if (batch || (drawn && _battleLoading.Elapsed >= LoadingScreen.MinSeconds)) HideBattleLoading();
        }
        void HideBattleLoading()
        {
            if (_battleLoading == null) return;
            _battleLoading.Hide(); _battleLoading = null; _battleLoadFrames = 0;
        }

        /// <summary>전투력 표시식 (index.html `power()` · 주인 확정 2026-09-03) = 공×8 + (체+실)×1.5 — 표시 전용.</summary>
        public double Power()
        {
            var pw = GearSystem.BuildPower(Data, Save.CurBuild(Data));
            return Math.Round(pw.Atk * 8 + (pw.Hp + pw.Sh) * 1.5);
        }
    }

    /// <summary>한 화면(UnityEngine.Screen 과 이름이 겹치지 않게 GameScreen). Show 에서 처음 한 번 Build 하고, 이후 Refresh 로 다시 그린다.</summary>
    public abstract class GameScreen
    {
        public App App;
        public abstract string Name { get; }
        public RectTransform Root { get; private set; }
        bool _built;
        public void Show()
        {
            if (!_built) { Root = UiKit.Rect(App.Frame, "Screen:" + Name); UiKit.Stretch(Root); Build(); _built = true; }
            Root.gameObject.SetActive(true); Root.SetAsLastSibling();
            Refresh();
            // T167 — 탭에 들어올 때마다 하단 탭 점을 지금 상태로 맞춘다. 화면마다 부르게 하면 빠뜨리는 화면이 생기므로
            // «모든 화면이 지나는» 이 한 곳에 둔다(판정은 Core.Notify.TabAny · 여기서는 보여 주기만 한다).
            NavBar.RefreshDots(App, UiKit.FindAny(Root, "Tab_01_BottomFlushMenu", "ui.tabBar"));
        }
        public void Hide() { if (Root != null) Root.gameObject.SetActive(false); OnHide(); }
        protected abstract void Build();
        public virtual void Refresh() { }
        public virtual void Tick(float dt) { }
        protected virtual void OnHide() { }
    }
}
