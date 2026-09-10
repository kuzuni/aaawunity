using System.IO;
using KkomaKnight.Core;
using NUnit.Framework;

namespace KkomaKnight.Tests
{
    /// <summary>
    /// T404 ⓐ — 전투에서 펫이 플레이어 뒤에 서는 <b>간격</b>(주인 2026-09-10 «펫 너무 가까이 붙어 있고»).
    /// <para>
    /// ⚑ 셈을 <see cref="Layout.PetGap"/> 한 자리로 빼 둔 까닭이 이것이다 — 자리 잡기 전체는 PlayMode 라 워커가 못 돌리는데,
    /// <b>«얼마나 벌릴까» 는 순수한 셈</b>이라 여기서 돌릴 수 있다. 못 돌리는 자리에 규칙을 두면 그 규칙은 CI 한 회전을 기다려야 한다(결정 143).
    /// </para>
    /// 화면 폭·플레이어 자리는 레이아웃 폭 540 · <c>camera.playerX</c> 0.16 · <c>zoom</c> 1.5 실측이다.
    /// </summary>
    public class PetBattleGapTests
    {
        const float LayoutW = 540f;                              // WorldCam.LayoutW (Game 쪽 상수 · 자는 Core 만 본다)
        const float PlayerScreenX = 0.16f * LayoutW;             // 86.4 레이아웃 px
        const float Zoom = 1.5f;
        const float CharW = 40f;                                 // 캐릭터 폭(레이아웃 px · pet.json _battleNote 실측)

        // 다른 펫 자들과 같은 길 — 표를 파일에서 그대로 읽는다(그래야 gapDx 가 되돌아가면 이 자가 운다)
        static PetData Load() => PetData.Parse(File.ReadAllText(TestData.RepoFile(Path.Combine("Assets", "KkomaKnight", "pet.json"))));

        static float LastPetX(float gap, int n) => PlayerScreenX - gap * n * Zoom;

        /// <summary>표가 준 값(21 = 0.79 캐릭터 폭)이 <b>한 마리일 때는 그대로</b> 쓰인다 — 주인이 «붙어 있다» 고 한 자리가 여기다.</summary>
        [Test]
        public void OnePetKeepsTheTableGap()
        {
            var d = Load();
            float want = (float)d.BattleGapDx;
            Assert.AreEqual(want, Layout.PetGap(want, PlayerScreenX, 1, Zoom, LayoutW), 1e-4f, "한 마리면 표 값 그대로");
            // 그 값이 실제로 «전보다 벌어졌다» 를 수로 못 박는다 — 표가 16 으로 되돌아가면 이 줄이 운다.
            Assert.Greater(want * Zoom / CharW, 0.7f, "펫 간격은 캐릭터 폭의 0.7 배는 돼야 한다(주인 «너무 붙어 있다» · 종전 0.60)");
        }

        /// <summary>
        /// ⚑ 이 자가 이 절의 핵심이다 — <b>몇 마리가 서든 맨 뒤 펫이 화면 안에 있다</b>.
        /// 종전 값(16)은 세 마리째가 14.4px 에 서서 제 반폭(20px)이 화면 밖이었다.
        /// </summary>
        [Test]
        public void NoPetEverWalksOffTheLeftEdge()
        {
            var d = Load();
            float want = (float)d.BattleGapDx;
            for (int n = 1; n <= d.Slots; n++)
            {
                float gap = Layout.PetGap(want, PlayerScreenX, n, Zoom, LayoutW);
                Assert.LessOrEqual(gap, want + 1e-4f, n + "마리: 표 값보다 더 벌리지는 않는다");
                Assert.GreaterOrEqual(LastPetX(gap, n), CharW * 0.5f - 1e-3f,
                                      n + "마리: 맨 뒤 펫의 반폭이 화면 밖으로 나간다(x=" + LastPetX(gap, n) + " · 반폭 " + CharW * 0.5f + ")");
            }
        }

        /// <summary>좁히더라도 «두 마리가 한 마리로 보이는» 자리는 안 만든다 — 그리고 플레이어 앞으로는 절대 안 간다.</summary>
        [Test]
        public void CrowdedPetsShrinkButNeverStackOrOvertake()
        {
            var d = Load();
            float want = (float)d.BattleGapDx;
            float prev = float.MaxValue;
            for (int n = 1; n <= d.Slots; n++)
            {
                float gap = Layout.PetGap(want, PlayerScreenX, n, Zoom, LayoutW);
                Assert.Greater(gap, 0f, n + "마리: 간격은 양수다(0 이면 전부 플레이어 자리에 겹쳐 선다)");
                Assert.LessOrEqual(gap, prev + 1e-4f, n + "마리: 마리가 늘면 간격은 좁아지기만 한다");
                prev = gap;
            }
            // 플레이어가 왼쪽 끝에 붙어 남은 폭이 0 이어도 «간격 0» 으로 무너지지 않는다
            Assert.Greater(Layout.PetGap(want, 0f, 3, Zoom, LayoutW), 0f, "남은 폭이 없어도 간격은 양수다");
        }
    }
}
