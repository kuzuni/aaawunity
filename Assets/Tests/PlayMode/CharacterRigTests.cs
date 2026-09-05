using System.Collections;
using KkomaKnight.Core;
using KkomaKnight.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace KkomaKnight.Tests.Play
{
    /// <summary>
    /// T14 — 전투 캐릭터 ⓐ 크기 = 표 % × 2/3(발밑 바 폭도 같은 배율) ⓑ 공격 애니 속도 = 클립 ÷ 간격(상한 없음) ⓒ 사망·승리 클립(루프 에셋)이 끝나면 Animator 가 마지막 프레임에서 멈춘다.
    /// 실제 씬(SampleScene · Bootstrap → App)에서 전투를 돌리고, 단계마다 빨간 줄 0(<see cref="PlayLog"/> · T11 규약 · LogAssert.NoUnexpectedReceived 금지).
    /// 배치 모드라 월드 카메라는 수동 렌더하지 않는다(T11 · CI #34).
    /// </summary>
    public class CharacterRigTests
    {
        PlayLog _log; App _app;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static IEnumerator RealSeconds(float sec) { float t = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t < sec) yield return null; }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다(데이터 로드)");
            _app = App.I; Assert.IsNotNull(_app.Assets, "AssetCatalog 이 씬에 연결돼 있어야 한다");
            yield return Frames(2);
            _log.AssertNoRed("부팅");
        }
        IEnumerator Shutdown()
        {
            Time.timeScale = 1f;
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            _app = null; yield return Frames(3);
            _log.AssertNoRed("종료");
        }

        static CharacterRig FindRig(string name)
        {
            foreach (var r in Object.FindObjectsByType<CharacterRig>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) if (r != null && r.name == name) return r;
            return null;
        }
        static Animator AnimOf(CharacterRig r) => r.GetComponentInChildren<Animator>(true);

        // ───────────────────────── ⓐ 크기 2/3 · 발밑 바 폭 ─────────────────────────
        [UnityTest]
        public IEnumerator BattleCharactersAreTwoThirdsOfTableHeightAndBarsScaled()
        {
            yield return Boot();
            _app.StartBattle(1);
            yield return RealSeconds(1.0f);
            Assert.AreEqual("battle", _app.Current.Name);
            var player = FindRig("Player"); Assert.IsNotNull(player, "플레이어 CharacterRig");
            float expectP = WorldCam.PctH(Layout.CharHeightPct(Layout.PlayerHeight)) / BattleWorld.CharBaseHeight;
            Assert.AreEqual(expectP, player.transform.localScale.y, 1e-4f, "플레이어 키 = PlayerHeight(9%) × 2/3");
            Assert.AreEqual(expectP, Mathf.Abs(player.transform.localScale.x), 1e-4f, "가로도 같은 배율(반전만)");
            Assert.AreEqual(WorldCam.PctH(Layout.PlayerHeight) * 2f / 3f / BattleWorld.CharBaseHeight, player.transform.localScale.y, 1e-4f, "= 예전 키의 2/3");

            // 적 — 화면에 들어온 첫 웨이브(보스 아님)
            var D = _app.Data; float expectE = WorldCam.PctH(Layout.CharHeightPct(Layout.EnemyHeight)) / BattleWorld.CharBaseHeight;
            float expectB = WorldCam.PctH(Layout.CharHeightPct(Layout.EnemyHeight * (float)D.Enemies.BossSizeMul)) / BattleWorld.CharBaseHeight;
            int enemies = 0;
            foreach (var r in Object.FindObjectsByType<CharacterRig>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (r == null || !r.name.StartsWith("Enemy")) continue;
                enemies++;
                float sy = r.transform.localScale.y;
                Assert.IsTrue(Mathf.Abs(sy - expectE) < 1e-4f || Mathf.Abs(sy - expectB) < 1e-4f, $"{r.name} 키 {sy} = 일반 {expectE} 또는 보스 {expectB}");
                Assert.AreEqual(sy, Mathf.Abs(r.transform.localScale.x), 1e-4f);
            }
            Assert.Greater(enemies, 0, "1초 뒤 화면 안에 적이 있어야 한다(첫 웨이브)");

            // 발밑 바 폭 — 플레이어 = 표 폭 × 2/3 · 적 = ui.json enemyBarW × 2/3 (높이는 그대로)
            float pBar = WorldCam.PctW(Layout.PlayerFootBarW) * Layout.CharScale, eBar = (float)D.Ui.EnemyBarW / WorldCam.PPU * Layout.CharScale;
            bool foundP = false, foundE = false;
            foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (sr == null || sr.name != "BarBg") continue;
                if (Mathf.Abs(sr.size.x - pBar) < 1e-4f) foundP = true;
                if (Mathf.Abs(sr.size.x - eBar) < 1e-4f) foundE = true;
                Assert.IsFalse(Mathf.Abs(sr.size.x - WorldCam.PctW(Layout.PlayerFootBarW)) < 1e-4f, "예전(배율 1) 플레이어 바 폭이 남아 있으면 안 된다");
            }
            Assert.IsTrue(foundP, "플레이어 발밑 바 폭 = 10.3% × 2/3"); Assert.IsTrue(foundE, "적 발밑 바 폭 = enemyBarW × 2/3");
            _log.AssertNoRed("전투 1초(크기·바)");

            _app.ShowScreen("lobby"); yield return Frames(2);
            yield return Shutdown();
        }

        // ───────────────────────── ⓑ 공격 애니 속도 · ⓒ 사망/승리 정지 (단독 리그) ─────────────────────────
        [UnityTest]
        public IEnumerator DeadAndVictoryFreezeAtLastFrameAndAttackSpeedUncapped()
        {
            yield return Boot();
            var prefab = _app.Assets.Prefab("cm.character"); Assert.IsNotNull(prefab, "cm.character");
            var go = Object.Instantiate(prefab); go.name = "RigUnderTest"; go.transform.position = new Vector3(200f, 0, 0);   // 화면 밖
            var rig = CharacterRig.Attach(go); rig.Apply(CharacterRig.PlayerSkin(_app.Data, _app.Save, false));
            var anim = AnimOf(rig); Assert.IsNotNull(anim, "Animator");
            Assert.Greater(rig.AttackClipLength, 1f, "Attack 클립 길이를 컨트롤러에서 읽어야 한다(조사값 1.83초)");
            rig.Play(CharacterRig.Idle); yield return Frames(2);
            Assert.IsFalse(rig.Frozen); Assert.AreEqual(1f, rig.AnimSpeed, 1e-4f);

            // ⓑ 공속이 빠르면(간격 0.3초) 클립 ÷ 간격 배속 — 예전 상한 ×3 없음 · 타격 순간도 같은 배율
            rig.PlayAttack(0.3);
            float expectSpeed = Layout.AttackAnimSpeed(rig.AttackClipLength, 0.3);
            Assert.Greater(expectSpeed, 3f, "상한 ×3 폐기");
            Assert.AreEqual(expectSpeed, rig.AnimSpeed, 1e-3f, "Animator 속도 = 클립 ÷ 간격");
            Assert.LessOrEqual(rig.HitDelay, 0.3f + 1e-3f, "타격 순간(OnAttackHit 1.0초 지점)이 간격 안으로 앞당겨진다");
            rig.PlayAttack(5.0); Assert.AreEqual(1f, rig.AnimSpeed, 1e-4f, "간격이 길면 1배(느리게 돌리지 않는다)");
            yield return Frames(1);

            // ⓒ 사망 — Dead1(1.0초 · 루프) 이 끝나면 마지막 프레임에서 정지 · 그 뒤로도 움직이지 않는다
            rig.Play(CharacterRig.Dead, true);
            yield return RealSeconds(1.8f);
            Assert.IsTrue(rig.Frozen, "사망 클립이 끝난 뒤 Frozen");
            Assert.AreEqual(0f, rig.AnimSpeed, 1e-6f, "Animator.speed = 0");
            var st = anim.GetCurrentAnimatorStateInfo(0);
            Assert.IsTrue(st.IsName(CharacterRig.Dead), "여전히 Dead1 상태");
            float ntFrozen = st.normalizedTime;
            Assert.IsTrue(ntFrozen >= 0.9f && ntFrozen < 1.0f, $"마지막 프레임(normalizedTime {ntFrozen}) — 1.0 이면 처음으로 감긴 것");
            rig.Tick(0.1f);   // 월드 틱이 와도 속도를 되살리지 않는다
            yield return RealSeconds(0.6f);
            Assert.AreEqual(0f, rig.AnimSpeed, 1e-6f); Assert.AreEqual(ntFrozen, anim.GetCurrentAnimatorStateInfo(0).normalizedTime, 1e-3f, "멈춘 뒤 진행 없음(다시 일어나지 않는다)");

            // 다른 상태를 틀면 풀린다
            rig.Play(CharacterRig.Idle); yield return Frames(2);
            Assert.IsFalse(rig.Frozen); Assert.AreEqual(1f, rig.AnimSpeed, 1e-4f);

            // 승리(1.33초 · 루프)도 같은 처리
            rig.Play(CharacterRig.Victory, true);
            yield return RealSeconds(2.0f);
            Assert.IsTrue(rig.Frozen, "승리 클립 끝에서 정지"); Assert.AreEqual(0f, rig.AnimSpeed, 1e-6f);
            Assert.IsTrue(anim.GetCurrentAnimatorStateInfo(0).IsName(CharacterRig.Victory));
            // Idle/Walk/Stun(루프가 의도) 은 멈추지 않는다
            rig.Play(CharacterRig.Walk, true); yield return RealSeconds(1.2f);
            Assert.IsFalse(rig.Frozen, "Walk 는 계속 돈다"); Assert.AreEqual(1f, rig.AnimSpeed, 1e-4f);
            _log.AssertNoRed("단독 리그(공격·사망·승리)");

            Object.Destroy(go); yield return Frames(2);
            yield return Shutdown();
        }

        // ───────────────────────── ⓒ 실제 전투에서 사망 → 사망 팝업 아래에서도 정지 ─────────────────────────
        [UnityTest]
        public IEnumerator PlayerDeathInBattleFreezesUnderDeadPopup()
        {
            yield return Boot();
            _app.StartBattle(1);
            var bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs); var G = bs.G; Assert.IsNotNull(G);
            yield return RealSeconds(0.5f);
            G.P.Hp = 1; G.P.Sh = 0; G.P.MaxSh = 0;   // 첫 타격에 죽는다(엔진은 적의 타격에서만 사망 판정)
            Time.timeScale = 3f;                      // 첫 웨이브까지 걷는 시간을 줄인다(엔진 틱은 dt 로 돈다)
            float t0 = Time.realtimeSinceStartup;
            while (!G.Dead && Time.realtimeSinceStartup - t0 < 40f) yield return null;
            Time.timeScale = 1f;
            Assert.IsTrue(G.Dead, "40초 안에 첫 타격으로 죽어야 한다");
            var player = FindRig("Player"); Assert.IsNotNull(player, "플레이어 CharacterRig");
            // 타격 연출(칼이 내려오는 순간) 뒤 Dead1 재생 → 사망 팝업 → 클립 끝에서 정지
            float t1 = Time.realtimeSinceStartup;
            while (!player.Frozen && Time.realtimeSinceStartup - t1 < 6f) yield return null;
            Assert.IsTrue(player.Frozen, "사망 클립이 끝나면 정지(Frozen)");
            Assert.AreEqual(0f, player.AnimSpeed, 1e-6f);
            var st = AnimOf(player).GetCurrentAnimatorStateInfo(0);
            Assert.IsTrue(st.IsName(CharacterRig.Dead), "Dead1 상태에서 멈춤"); Assert.Less(st.normalizedTime, 1.0f, "처음으로 감기지 않음");
            Assert.IsTrue(_app.Overlay.IsOpen, "사망 팝업이 열려 있다");
            float nt = st.normalizedTime;
            yield return RealSeconds(1.0f);
            Assert.AreEqual(0f, player.AnimSpeed, 1e-6f); Assert.AreEqual(nt, AnimOf(player).GetCurrentAnimatorStateInfo(0).normalizedTime, 1e-3f, "팝업 아래에서도 다시 일어나지 않는다");
            _log.AssertNoRed("전투 사망 → 팝업");

            _app.Overlay.Close(); _app.ShowScreen("lobby"); yield return Frames(2);
            _log.AssertNoRed("로비 복귀");
            yield return Shutdown();
        }
    }
}
