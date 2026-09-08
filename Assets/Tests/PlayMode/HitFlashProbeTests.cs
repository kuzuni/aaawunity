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
    /// T242 회차 1 — <b>주인이 «피격 시 하얗게 번쩍이 안 보인다» 고 한 자리를 «재는» 자</b>(고치는 자가 아니다).
    /// <para>
    /// 등재문이 원인 후보로 «0.1s · 세기 부족 · 경로 누락» 셋을 적었는데 <b>«세기» 는 이미 지웠다</b> —
    /// <c>Assets/KkomaKnight/HitFlash.mat</c> 이 <c>_HitEffectBlend 1</c> · <c>_HitEffectGlow 5</c> · 흰색 α1 로 <b>최대</b>다.
    /// 남은 셋을 가르는 물음은 하나씩이다:
    /// <list type="bullet">
    /// <item><b>부르는 경로가 있나</b> — 전투를 돌리는 동안 <see cref="CharacterRig.FlashCount"/> 가 0 이면 그 유닛은 «아예 안 켜진다».</item>
    /// <item><b>얼마나 켜지나</b> — <see cref="CharacterRig.LastFlashSeconds"/> 와 «켜진 채로 지나간 프레임 수» 를 같이 센다.
    ///       0.1초를 요청해도 프레임이 한둘이면 «짧아서 안 보인다» 가 답이고, 프레임이 여섯인데도 주인 눈에 없으면 답은 다른 데 있다.</item>
    /// <item><b>그 유닛이 이 리그인가</b> — 씬의 <see cref="CharacterRig"/> 수와 그중 «한 번이라도 켠» 수를 같이 적는다.</item>
    /// </list>
    /// </para>
    /// <para>
    /// ⚠ <b>이 자는 막지 않는다</b>(결정 627 · T226) — 새로 묻는 물음이라 처음엔 <see cref="Debug.Log"/> 로만 적고,
    /// 값이 0 이 아닌 것이 확인된 회차에 단언으로 올린다. 지금 막으면 «아직 답을 모르는 물음» 이 배포를 세운다.
    /// ⚠ <b>손잡이는 한 칸도 안 돌렸다</b> — 길이·세기·경로 어느 것도 이 회차에서 안 바꾼다(결정 622: 모르는 채 움직이면 관측도 못 얻는다).
    /// </para>
    /// </summary>
    public class HitFlashProbeTests
    {
        App _app; PlayLog _log;

        [SetUp] public void SetUp() { _log = new PlayLog(); }
        [TearDown] public void TearDown() { Time.timeScale = 1f; _log?.Dispose(); _log = null; try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { } }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        IEnumerator Boot()
        {
            try { PlayerPrefs.DeleteKey(SaveStore.Key); } catch { }
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
            float t0 = Time.realtimeSinceStartup;
            while (App.I == null && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            Assert.IsNotNull(App.I, "Bootstrap 이 60초 안에 App 을 세워야 한다");
            _app = App.I;
            yield return Frames(2);
        }

        [UnityTest]
        public IEnumerator HitFlashIsMeasuredOnEveryRigThatTakesAHit()
        {
            yield return Boot();
            _app.StartBattle(1);
            var bs = _app.GetScreen<BattleScreen>();
            Assert.IsNotNull(bs, "전투 화면");
            yield return Frames(2);

            // 리그마다 «켜진 채로 지나간 프레임» 을 센다 — 요청 길이(초)와 따로 재야 «짧아서 안 보인다» 를 가를 수 있다.
            var onFrames = new Dictionary<CharacterRig, int>();
            var seen = new List<CharacterRig>();
            int frames = 0;
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 8f)
            {
                foreach (var rig in Object.FindObjectsByType<CharacterRig>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (rig == null) continue;
                    if (!onFrames.ContainsKey(rig)) { onFrames[rig] = 0; seen.Add(rig); }
                    if (rig.Flashing) onFrames[rig]++;
                }
                frames++;
                yield return null;
            }

            int rigs = 0, flashed = 0, totalOn = 0, totalCount = 0; float lastSec = -1f;
            foreach (var rig in seen)
            {
                if (rig == null) continue;
                rigs++;
                if (rig.FlashCount > 0) { flashed++; totalCount += rig.FlashCount; if (rig.LastFlashSeconds > 0f) lastSec = rig.LastFlashSeconds; }
                totalOn += onFrames[rig];
            }
            float onPerFlash = totalCount > 0 ? (float)totalOn / totalCount : 0f;
            Debug.Log(string.Format(
                "[T242] 전투 {0}프레임 · 리그 {1}개 중 한 번이라도 번쩍인 것 {2}개 · 번쩍 {3}회 · 켜진 프레임 합 {4} · 회당 {5:0.0}프레임 · 요청 길이 {6:0.000}s",
                frames, rigs, flashed, totalCount, totalOn, onPerFlash, lastSec));
            Debug.Log("[T242] 읽는 법 — 번쩍 0회면 «부르는 경로가 없다»(그 유닛은 CharacterRig 가 아니거나 그 이벤트가 안 온다) · "
                      + "회당 프레임이 한둘이면 «짧아서 안 보인다» · 여섯 이상인데도 주인 눈에 없으면 남은 것은 «그림이 안 바뀐다»(머티리얼 교체가 실제로 안 먹는 자리)다.");

            _log.AssertNoRed("T242 피격 플래시 관측");
            if (_app != null) { if (_app.UiCanvas != null) Object.Destroy(_app.UiCanvas.gameObject); Object.Destroy(_app.gameObject); }
            yield return Frames(3);
        }
    }
}
