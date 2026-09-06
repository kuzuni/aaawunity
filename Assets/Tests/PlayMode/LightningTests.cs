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
    /// T70 — 번개 특전(<see cref="EvKind.Bolt"/>) 이펙트. 주인 지시(2026-09-06) «번개 이펙트 뭐 인터넷에서 에셋 다운받아서 되게 해줘».
    /// 시트는 CC0 에셋(`fx.lightning` · Superpowers Asset Packs · 출처 `Assets/KkomaKnight/Fx/Lightning/LICENSES.md`)이고
    /// <see cref="Fx.PlaySheet"/> 가 <see cref="SheetAnim"/> 으로 한 번 재생한 뒤 스스로 파괴한다.
    /// 계약: ① 적마다 «Lightning» 하나 ② 적마다 <see cref="BattleWorld.BoltStagger"/> 만큼 어긋난다 ③ 새까만 칸 2 는 안 쓴다
    /// ④ 하늘에서 발밑까지(적 키 × <see cref="BattleWorld.BoltHeightMul"/> · 수직이 되게 기울임) ⑤ 끝나면 사라지고 그 자리에 종전 전기 튀김(fx.bolt) ⑥ 빨간 줄 0.
    /// 빨간 줄은 <see cref="PlayLog"/> 로 본다(T11 규약 · LogAssert.NoUnexpectedReceived 금지).
    /// </summary>
    public class LightningTests
    {
        PlayLog _log; App _app;
        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }
        static IEnumerator RealSeconds(float sec) { float t = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t < sec) yield return null; }
        const string SparkName = "CFXR3 Hit Electric C (Air)(Clone)";   // catalog fx.bolt 프리팹의 인스턴스 이름

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

        static List<GameObject> Bolts(BattleWorld world)
        {
            var o = new List<GameObject>();
            if (world == null || world.Root == null) return o;
            foreach (Transform t in world.Root) if (t.name == BattleWorld.LightningName) o.Add(t.gameObject);
            return o;
        }

        [UnityTest]
        public IEnumerator BoltEventDropsOneLightningPerEnemyThenSparksAndDisappears()
        {
            yield return Boot();
            _app.StartBattle(1);
            var bs = _app.GetScreen<BattleScreen>(); Assert.IsNotNull(bs, "전투 화면");
            var G = bs.G; Assert.IsNotNull(G, "전투 상태");
            var world = bs.World; Assert.IsNotNull(world, "BattleWorld");
            yield return Frames(2);

            var sheet = _app.Assets.Sprite("fx.lightning");
            Assert.IsNotNull(sheet, "카탈로그에 번개 시트(fx.lightning · CC0 에셋)가 있어야 한다");
            Assert.AreEqual(0, sheet.texture.width % Fx.LightningCols, "시트 폭이 칸 수(" + Fx.LightningCols + ")로 나뉘어야 한다");
            int fw = sheet.texture.width / Fx.LightningCols;

            var alive = new List<EnemyState>();
            // 보스는 키가 ×BossSizeMul 라 아래 «길이 = 적 키 × 1.8» 단언에서 뺀다
            foreach (var e in G.AliveList()) if (!e.IsBoss) alive.Add(e);
            Assert.GreaterOrEqual(alive.Count, 2, "번개는 «보이는 적 전부» 라 잡몹이 둘 이상인 판이어야 시험이 성립한다");
            var e0 = alive[0]; var e1 = alive[1];
            Assert.AreEqual(0, Bolts(world).Count, "번개 이벤트 전에는 번개가 없다");

            // 엔진이 «처치 시 번개»·«치명타 번개» 로 보내는 것과 같은 이벤트(적마다 하나)
            world.Handle(new BattleEvent { Kind = EvKind.Bolt, Enemy = e0 });
            world.Handle(new BattleEvent { Kind = EvKind.Bolt, Enemy = e1 });
            var bolts = Bolts(world);
            Assert.AreEqual(2, bolts.Count, "① 적마다 번개 하나");

            var a0 = bolts[0].GetComponent<SheetAnim>(); var a1 = bolts[1].GetComponent<SheetAnim>();
            Assert.IsNotNull(a0, "번개는 SheetAnim 으로 재생한다"); Assert.IsNotNull(a1);
            // ② 같은 틱에 여럿이면 적마다 시차 — 첫 줄기는 0, 다음 줄기는 BoltStagger
            Assert.AreEqual(0f, a0.Delay, 1e-4f, "첫 번개는 바로 떨어진다");
            Assert.AreEqual(BattleWorld.BoltStagger, a1.Delay, 1e-4f, "다음 적의 번개는 " + BattleWorld.BoltStagger + "초 어긋난다");

            // ③ 새까만 반전 섬광(칸 2)은 안 쓴다 — 재생 칸은 0·1·3·4
            Assert.AreEqual(Fx.LightningFrames.Length, a0.Frames.Length, "재생 칸 수");
            for (int i = 0; i < Fx.LightningFrames.Length; i++)
                Assert.AreEqual(Fx.LightningFrames[i] * fw, a0.Frames[i].rect.x, 0.5f, "재생 칸 " + i + " 은 시트의 " + Fx.LightningFrames[i] + "번 칸");
            foreach (var f in a0.Frames) Assert.AreNotEqual(2 * fw, f.rect.x, "칸 2(새까만 실루엣)는 안 쓴다");

            // ④ 하늘에서 발밑까지 — 적 발 위에 서고, 세로 길이는 적 키 × BoltHeightMul, 시트를 세워 수직으로 내리꽂는다
            float eh = WorldCam.PctH(Layout.EnemyHeight), span = eh * BattleWorld.BoltHeightMul;
            float footY = WorldCam.ToWorld(0f, Layout.PlayerFootY / 100f).y;   // 발 줄은 x 와 무관하다
            Assert.AreEqual(footY + span * 0.5f, bolts[0].transform.position.y, 1e-3f, "번개는 적 발 위 «반 길이» 자리에 선다(= 아래 끝이 발밑)");
            Assert.AreEqual(Fx.LightningTiltDeg, bolts[0].transform.rotation.eulerAngles.z, 0.5f, "시트를 세워 수직으로 내리꽂는다");
            Assert.AreEqual(span / Fx.LightningSpanAtScale1, bolts[0].transform.localScale.x, 1e-3f, "세로 길이 = 적 키 × " + BattleWorld.BoltHeightMul);
            Assert.AreEqual(Fx.SortingOrder, bolts[0].GetComponent<SpriteRenderer>().sortingOrder, "적 리그 앞에 그린다");

            // 재생 — 첫 프레임이 지나면 그림이 켜지고 시트 칸이 붙는다
            yield return Frames(2);
            var sr0 = bolts[0] != null ? bolts[0].GetComponent<SpriteRenderer>() : null;
            if (sr0 != null) { Assert.IsTrue(sr0.enabled, "첫 번개는 바로 보인다"); Assert.IsNotNull(sr0.sprite, "시트 칸이 붙는다"); }

            // ⑤ 0.31초(4칸/13fps)면 끝나고 사라진다 + 그 자리에 종전 전기 튀김
            yield return RealSeconds(1.2f);
            Assert.AreEqual(0, Bolts(world).Count, "재생이 끝나면 번개는 스스로 사라진다");
            Assert.IsNotNull(GameObject.Find(SparkName), "번개가 닿는 순간 종전 전기 튀김(fx.bolt)이 뿌려진다");
            _log.AssertNoRed("번개 재생");

            _app.ShowScreen("lobby"); yield return Frames(3);
            Assert.AreEqual(0, Bolts(world).Count, "전투를 나가면 번개도 남지 않는다");
            _log.AssertNoRed("로비 복귀");
            yield return Shutdown();
        }
    }
}
