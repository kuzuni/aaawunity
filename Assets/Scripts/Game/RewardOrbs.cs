using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using KkomaKnight.Core;

namespace KkomaKnight.Game
{
    /// <summary>
    /// T85 — 적을 죽이면 그 자리에서 튀어나와 HUD 로 «날아가 흡수되는» 보상 구슬(경험치 구슬 · 골드 코인) 층.
    /// 값(엔진)은 킬 순간에 이미 올라 있고(<see cref="KkomaKnight.Core.BattleState"/> 불변 · 시드 골든 불변),
    /// 여기서 옮기는 것은 «표시값이 언제 오르는가» 뿐이다 — 구슬이 목표(EXP 바 · 골드 pill)에 <b>도착할 때마다</b> 그 몫을 <see cref="Fly"/> 의 콜백으로 넘긴다.
    /// 경로(T109 · 주인 «1초 정도 머물렀다가 랜덤 곡선 그리면서 (트레일 있어야 함) 0.8초 동안 흡수») = 짧게 위로 튀었다가(<see cref="HopSec"/>) 그 자리에서 <see cref="HoldSec"/> 머물며 흔들리고,
    /// 제어점을 진행 방향의 <b>옆(구슬 번호로 좌·우 번갈아 · T461)</b>으로 민 2차 베지어로 <see cref="FlySec"/>(거리 무관 · ±<see cref="FlyJitter"/>) 만에 목표까지 — 구슬마다 <see cref="StepSec"/> 시차 · 지나간 자리에 잔상(<see cref="TrailName"/>)을 떨군다.
    /// <para>T461 ⓐ(주인 2026-09-12 «부드럽게 안 가는 느낌») — 길의 셈(제어점·진행률·크기)은 <see cref="OrbPath"/>(Core)에 있고 여기는 붙이기만 한다. 그래야 «부드러운가» 를 이 통에서 매 회차 잰다(결정 143).</para>
    /// 모든 트윈에 <c>SetLink</c>(T56 · 콘솔 노란 줄 0) · <see cref="FinishNow"/> 는 남은 값을 즉시 적립하고 비운다(무한 대기 금지).
    /// </summary>
    public sealed class RewardOrbs
    {
        /// <summary>구슬 오브젝트 이름 — PlayMode 테스트가 이 이름으로 «생겼다/사라졌다» 를 본다.</summary>
        public const string OrbName = "Orb";
        /// <summary>구슬 뒤에 남는 잔상(트레일) 오브젝트 이름 — PlayMode 테스트가 이 이름으로 «꼬리가 있다» 를 본다(T109 3항).</summary>
        public const string TrailName = "OrbTrail";
        public const float HopSec = 0.15f;                    // 적 자리에서 위로 튀는 시간
        /// <summary>T109 1항(주인 «1초 정도 머물렀다가») — 튀어오른 자리에서 머무는 시간. 그동안 살짝 위아래로 흔들린다.</summary>
        public const float HoldSec = 1.0f;
        /// <summary>
        /// T109 2항(주인 확정 «0.8초 동안 흡수») — 머무름이 끝나고 목적지까지 걸리는 시간. <b>거리와 무관하게 고정</b>이다(한꺼번에 모여 들어오는 느낌).
        /// 투사체의 «거리당 속도»(T86 4-1)와 반대인데, 그쪽은 «맞는 시점» 이 걸린 판정이고 이쪽은 순수 연출이라 주인이 시간을 직접 정했다.
        /// </summary>
        public const float FlySec = 0.8f;
        /// <summary>구슬마다 비행 시간을 이만큼 흔든다(±).</summary>
        public const float FlyJitter = 0.05f;
        /// <summary>한 구슬이 걸릴 수 있는 최대 비행 시간 — 수명 상한 계산용(<c>RewardOrbTests.OrbLifeMax</c>).</summary>
        public const float FlySecMax = FlySec + FlyJitter;
        public const float StepSec = 0.07f;                   // 구슬 사이 시차(T109 1항 «0.05~0.1s 씩 어긋나게»)
        public const float PopSec = 0.08f;                    // 도착 뒤 «작게 튀고» 사라지는 꼬리
        /// <summary>
        /// T313 — <b>리워드 팝업</b>이 한 번에 흡수하는 데 쓰는 <b>총 예산</b>(초). 주인 2026-09-09 10:0X «흡수 파티클이 느리다 — <b>1초 안에</b> 전부 흡수».
        /// <para>
        /// 종전에는 구슬 사이 시차(<see cref="StepSec"/> 0.07)가 개수만큼 그대로 쌓여서 <b>100개면 마지막 구슬이 8초쯤 뒤에 닿았다</b>
        /// (0.07 × 99 + 0.15 + 0.8 + 0.08). 개수가 늘수록 느려지는 꼴이라 «많이 받을수록 답답한» 연출이었다.
        /// </para>
        /// ⚠ <b>전투 구슬은 그대로다</b> — 예산은 <see cref="Fly"/> 에 값을 넘긴 자리(팝업)에서만 걸린다. 전투는 «죽은 자리에 1초 머물렀다가» 가 주인이 정한 연출이다(T109).
        /// </summary>
        public const float PopupBudgetSec = 1.0f;
        /// <summary>예산에 맞춰 줄일 때 <b>비행 시간의 하한</b> — 이보다 짧으면 «날아갔다» 가 아니라 «순간이동» 으로 보인다.</summary>
        public const float FlyMinSec = 0.25f;
        /// <summary>예산에서 비행이 가져가는 몫(나머지는 구슬 사이 시차) — 반씩이면 «줄지어 날아가는» 꼴이 남는다.</summary>
        public const float BudgetFlyShare = 0.5f;
        /// <summary>화면 동시 상한(기본) — 넘으면 개수를 줄인다(값은 그대로). 부르는 쪽이 <see cref="RewardOrbs(RectTransform,int)"/> 로 달리 줄 수 있다(T269 = 100 · 주인 «캡은 100개 파티클»).</summary>
        public const int MaxAlive = 40;
        /// <summary>잔상을 남기는 간격(비행 시간을 이 값으로 나눠 등분한다) · 한 장이 사라지기까지 · 화면 동시 상한.</summary>
        public const float TrailStepSec = 0.04f, TrailFadeSec = 0.26f;
        public const int MaxTrail = 140;
        /// <summary>
        /// T144(주인 2026-09-07 «그 골드랑 경험치 흡수될때 트레일 랜더러로 효과좀 줘») — 꼬리를 <c>TrailRenderer</c>(월드 렌더러)로 낸다.
        /// <para>
        /// UI 캔버스는 <c>ScreenSpaceOverlay</c> 라 월드 렌더러가 그 아래 깔린다 → 지시서가 권한 ⓒ 를 골랐다(결정 기록):
        /// <b>구슬 그림은 지금처럼 UI 층에 두고, 꼬리만 월드에 띄워 따라다니게</b> 한다. 전투 마당(화면 위쪽)에서는 꼬리가 보이고
        /// HUD 패널에 들어설 때 자연히 가려져 사라진다 — «바에 빨려 들어간다» 는 느낌이 그대로다.
        /// </para>
        /// 잔상 스프라이트(<see cref="TrailName"/>)는 두 겹이 되지 않게 <b>끈다</b>(지시서 3항).
        /// </summary>
        public const string TrailObjName = "OrbTrailWorld";
        /// <summary>꼬리 길이(초) · 시작·끝 굵기(유니티 단위) · z(캐릭터보다 뒤로 살짝) — 연출 수치라 한곳에 모은다(주인이 굵기·길이를 말하면 여기 한 줄).</summary>
        public const float TrailTime = 0.28f, TrailStartW = 0.16f, TrailEndW = 0.02f, TrailZ = 0.2f;

        sealed class Orb
        {
            /// <summary>구슬 오브젝트(두 모드 공통 · 사라졌으면 유니티 null) — 트윈 <c>SetLink</c>·지우기·<see cref="Prune"/> 가 이것만 본다.</summary>
            public GameObject Go;
            /// <summary>UI 모드(리워드 팝업)의 그림 — 프레임 px 로 움직인다.</summary>
            public RectTransform Rt;
            /// <summary>월드 모드(T502 ③ · 전투)의 그림 — <see cref="Px"/> 를 <see cref="WorldCam.FromFrame"/> 으로 월드에 놓는다. <see cref="Unit"/> = 프레임 px 한 칸이 이 구슬의 로컬 배율로 얼마인가(그림 크기 ÷ sizePx).</summary>
            public Transform Tr; public float Unit = 1f;
            /// <summary>지금 자리(프레임 px · 두 모드 공통) · 지금 배율 — 길의 셈은 모드와 무관하게 px 로 하고(<see cref="OrbPath"/>) 마지막에 모드가 놓는다. 꼬리(<see cref="OrbTrail"/>)도 이것을 읽는다.</summary>
            public Vector2 Px; public float S = 1f;
            public Sequence Seq; public double Value; public Action<double> OnArrive; public bool Done; public GameObject Trail;
        }

        readonly RectTransform _layer;
        /// <summary>
        /// T502 ③(주인 «재화 흡수 이펙트 전부 월드스페이스로») — 전투 구슬을 <b>월드 SpriteRenderer</b> 로 띄울 때 그 부모를 돌려주는 손. null 이면 종전대로 UI 모드(리워드 팝업은 그대로 UI · 주인 말은 전투 화면).
        /// 손으로 받는 까닭 = 전투 월드(<c>BattleWorld._root</c>)는 판마다 새로 서고 이 층은 화면과 함께 한 번만 서기 때문이다 — 띄우는 순간의 월드를 묻는다.
        /// 길의 셈(<see cref="OrbPath"/> · 홉·머무름·베지어)은 두 모드가 <b>같은 px</b> 로 하고 마지막 한 줄(<see cref="SetPos"/>)에서만 갈린다 — 그래야 자(<c>OrbPathTests</c>·<c>RewardOrbTests</c>)가 모드를 몰라도 된다.
        /// </summary>
        readonly Func<Transform> _worldRoot;
        /// <summary>월드 모드인가(전투) — 아니면 UI 모드(리워드 팝업).</summary>
        public bool WorldMode => _worldRoot != null;
        /// <summary>월드 구슬의 정렬 순서 — 꼬리(<see cref="TrailSortOrder"/> 350)·앞 소품(≤470)·팝(400) 위. 종전 UI 구슬이 «모든 것 위» 였던 것에 가장 가깝다(HUD 는 캔버스라 여전히 그 위 = 알약에 «빨려 들어간다»).</summary>
        public const int OrbSortOrder = 480;
        /// <summary>월드 구슬의 z — 캐릭터와 같은 평면(정렬은 <see cref="OrbSortOrder"/> 가 정한다).</summary>
        public const float OrbZ = 0f;
        readonly List<Orb> _alive = new List<Orb>();
        /// <summary>지금 화면에 떠 있는 잔상(T109 3항) — 상한(<see cref="MaxTrail"/>)과 한꺼번에 지우기에 쓴다.</summary>
        readonly List<RectTransform> _trails = new List<RectTransform>();
        /// <summary>T144 — 월드 꼬리(<c>TrailRenderer</c>) 들. 누수 0 을 위해 여기서 센다.</summary>
        readonly List<GameObject> _worldTrails = new List<GameObject>();
        Material _trailMat; int _sortLayer;
        static readonly int MainTex = Shader.PropertyToID("_MainTex");
        /// <summary>T144 — 꼬리를 월드 <c>TrailRenderer</c> 로 낼 것인가(끄면 T109 의 잔상 스프라이트로 돌아간다 · 되돌리는 스위치).</summary>
        public static bool UseWorldTrail = true;
        /// <summary>
        /// 꼬리의 정렬 순서 — <b>투사체와 같은 자리</b>(<c>BattleWorld</c> 의 도끼·화살이 쓰는 350).
        /// <para>
        /// ⚑ <b>T461 ⓑ 실측(2026-09-11 · 소스) — 종전 값 −50 은 전투 마당에서 «꼬리가 아예 안 보이는» 값이었다.</b>
        /// <c>BattleWorld</c> 의 월드 정렬 층은 바닥 <b>−40</b> · 길 −38 · 납작 −36 · 먼 소품 −35 · 가까운 소품 −12 ·
        /// 캐릭터 ≤300(<c>Fx.SortingOrder</c> 주석) · 투사체 350 · 앞 소품 381~470 · 이펙트 400 이다
        /// (<c>BattleWorld.OrderField</c> 줄 · <c>BattleWorld</c>:979·1043 · <c>Fx</c>:9). −50 은 그 <b>전부보다 뒤</b>라
        /// 꼬리가 <b>바닥 그림 뒤</b>에 그려진다 — 주인 «전투 화면에서 화폐 흡수되는 거는 트레일 렌더러 있게 해야 함» 의 까닭이 이 한 수다.
        /// </para>
        /// <para>
        /// ⚠ 그림을 가리는 값이 아니다 — 앞 소품(381~)은 여전히 꼬리를 덮고, UI(<c>ScreenSpaceOverlay</c>)는 늘 그 위다.
        /// 투사체와 같은 자리를 고른 것은 «날아가는 것» 끼리 같은 층에 두는 것이 다음 사람에게 읽히기 때문이다.
        /// </para>
        /// ⚠ <b>안 잰 것</b>: 이 통은 유니티를 못 돌린다(결정 143) — 위는 <b>소스에서 읽은 수</b>이고 «화면에 보인다» 는 다음 런/주인 폰이 말한다.
        /// </summary>
        public const int TrailSortOrder = 350;

        /// <summary>이 층이 한 번에 띄울 수 있는 구슬 수 — 전투는 <see cref="MaxAlive"/>(40), 리워드 팝업은 100(T269).</summary>
        readonly int _max;

        public RewardOrbs(RectTransform layer, int maxAlive = MaxAlive) : this(layer, null, maxAlive) { }
        /// <summary>월드 모드(T502 ③) — <paramref name="layer"/> 는 과녁(알약)의 프레임 px 를 재는 자로만 남고, 구슬은 <paramref name="worldRoot"/>() 아래 SpriteRenderer 로 선다.</summary>
        public RewardOrbs(RectTransform layer, Func<Transform> worldRoot, int maxAlive = MaxAlive) { _layer = layer; _worldRoot = worldRoot; _max = Mathf.Max(1, maxAlive); }

        /// <summary>날아가는 중인 구슬 수(테스트·진단용).</summary>
        public int Alive { get { Prune(); return _alive.Count; } }
        /// <summary>아직 흡수가 끝나지 않았나 — 화면은 이 동안 레벨업 특전창을 열지 않는다(주인 «다 차고 나서»).</summary>
        public bool Busy => Alive > 0;

        /// <summary>목표(EXP 바 · 골드 pill)의 한가운데를 이 층의 좌표(왼쪽 아래 0,0 = 프레임 px)로.</summary>
        public Vector2 TargetPos(RectTransform target)
        {
            if (_layer == null || target == null) return Vector2.zero;
            var world = target.TransformPoint(target.rect.center);
            return (Vector2)_layer.InverseTransformPoint(world) - _layer.rect.min;
        }

        /// <summary>
        /// 구슬 <paramref name="count"/> 개를 <paramref name="from"/>(프레임 px)에서 <paramref name="target"/> 으로 날린다 —
        /// 값 <paramref name="total"/> 은 개수만큼 나눠 담고(나머지는 마지막 구슬), 도착할 때마다 <paramref name="onArrive"/> 로 그 몫을 넘긴다.
        /// 실제로 띄운 개수를 돌려준다(0 이면 호출자가 값을 바로 반영해야 한다).
        /// </summary>
        /// <param name="sprite">T473 — 구슬 그림을 <b>키가 아니라 스프라이트로</b> 줄 때(칸이 런타임 썸네일(펫 메이커 캡처)처럼 카탈로그에 없는 그림을 쓰면 키로는 못 찾아 엉뚱한 그림이 뜬다). null 이면 <paramref name="spriteKey"/>.</param>
        public int Fly(Vector2 from, RectTransform target, string spriteKey, Color tint, int count, double total, float sizePx, float timeScale, Action<double> onArrive, float holdSec = HoldSec, float budgetSec = 0f, Sprite sprite = null)
        {
            Prune();
            if (_layer == null || target == null || count <= 0 || total <= 0) return 0;
            var root = WorldMode ? _worldRoot() : null;
            if (WorldMode && root == null) return 0;   // 월드가 없으면(판이 끝난 뒤) 구슬 없이 — 호출자가 값을 바로 반영한다(종전 «화면 밖» 갈래와 같은 손)
            count = Mathf.Min(count, Mathf.Max(0, _max - _alive.Count));
            if (count <= 0) return 0;
            float sc = Mathf.Max(0.5f, timeScale);
            var to = TargetPos(target);
            double each = total / count;
            Pace(count, Mathf.Max(0f, holdSec), budgetSec, out float step, out float flyBase, out float flyJit);
            for (int i = 0; i < count; i++)
            {
                double val = i == count - 1 ? total - each * (count - 1) : each;
                Make(root, from, to, spriteKey, tint, sizePx, i, count, sc, val, onArrive, Mathf.Max(0f, holdSec), step, flyBase, flyJit, sprite);
            }
            return count;
        }

        /// <summary>구슬을 프레임 px 자리에 놓는다 — 모드가 갈리는 유일한 자리(UI 는 anchoredPosition · 월드는 <see cref="WorldCam.FromFrame"/>). 꼬리는 <see cref="Orb.Px"/> 를 읽는다.</summary>
        static void SetPos(Orb o, Vector2 px)
        {
            o.Px = px;
            if (o.Rt != null) o.Rt.anchoredPosition = px;
            else if (o.Tr != null) o.Tr.position = WorldCam.FromFrame(px, OrbZ);
        }
        /// <summary>구슬 배율(1 = sizePx 한 변) — UI 는 localScale 그대로, 월드는 그림 크기로 맞춘 <see cref="Orb.Unit"/> 을 곱한다.</summary>
        static void SetScale(Orb o, float s)
        {
            o.S = s;
            if (o.Rt != null) o.Rt.localScale = Vector3.one * s;
            else if (o.Tr != null) o.Tr.localScale = Vector3.one * (o.Unit * s);
        }

        /// <summary>
        /// T313 — <b>예산 안에 다 들어오게</b> 시차와 비행 시간을 정한다(예산 0 = 종전 그대로).
        /// <para>
        /// <b>필요할 때만 줄인다</b> — 개수가 적어 예산 안에 이미 들어오면 종전 값을 그대로 쓴다(주인이 좋아한 «0.8초 곡선» 이 적게 받을 때는 안 바뀐다).
        /// 줄일 때는 튀어오름(<see cref="HopSec"/>)과 머무름은 건드리지 않고 <b>비행 + 시차</b>만 남은 예산에 맞춘다 — 그 둘이 «개수에 비례해 길어지는» 유일한 자리다.
        /// </para>
        /// 마지막 구슬의 <b>도착</b>이 예산 안이다(뒤에 붙는 <see cref="PopSec"/> 꼬리는 값이 이미 들어간 뒤의 장식이라 예산 밖으로 센다).
        /// </summary>
        public static void Pace(int count, float holdSec, float budgetSec, out float step, out float flyBase, out float flyJit)
        {
            step = StepSec; flyBase = FlySec; flyJit = FlyJitter;
            if (budgetSec <= 0f || count <= 0) return;
            // «평소» 로 재고 지터는 안 센다 — 그래야 구슬 하나짜리 판이 지터 때문에 압축되지 않는다(주인이 좋아한 0.8초 곡선이 그대로 남는다).
            // 안 줄인 판의 최악값도 예산 안이다: 안 줄이는 것은 count 1 · hold 0 뿐이고 그때 최악은 HopSec + FlySecMax = 1.00 이다.
            float natural = (count - 1) * StepSec + HopSec + holdSec + FlySec;
            if (natural <= budgetSec) return;                       // 이미 예산 안 — 종전 연출 그대로
            float room = Mathf.Max(0f, budgetSec - HopSec - holdSec);
            flyBase = Mathf.Clamp(room * BudgetFlyShare, Mathf.Min(FlyMinSec, room), FlySec);
            flyJit = 0f;                                            // 예산에 맞추는 판에서는 흔들지 않는다(흔들면 마지막 구슬이 예산을 넘는다)
            step = count > 1 ? Mathf.Max(0f, (room - flyBase) / (count - 1)) : 0f;
            // ⚠ 시차를 «늘리지는» 않는다 — 반씩 나눈 몫이 종전 시차보다 크면(구슬이 두셋뿐일 때) 종전 시차를 쓰고
            //    남은 시간을 전부 비행에 준다. 그러면 조금 받을 때는 주인이 좋아한 0.8초 곡선이 거의 그대로 남는다
            //    (구슬 둘이면 시차 0.07 · 비행 0.78 — 눈으로는 종전과 같다).
            if (step > StepSec)
            {
                step = StepSec;
                flyBase = Mathf.Clamp(room - step * (count - 1), Mathf.Min(FlyMinSec, room), FlySec);
            }
        }

        /// <summary>마지막 구슬이 <b>닿기까지</b> 걸리는 시간 — 자와 부르는 쪽이 «예산 안인가» 를 같은 셈으로 볼 수 있게 한 곳에 둔다.</summary>
        public static float LastArrivalSec(int count, float holdSec = 0f, float budgetSec = 0f)
        {
            if (count <= 0) return 0f;
            Pace(count, holdSec, budgetSec, out float step, out float flyBase, out float flyJit);
            return (count - 1) * step + HopSec + holdSec + flyBase + flyJit;
        }

        void Make(Transform root, Vector2 from, Vector2 to, string spriteKey, Color tint, float sizePx, int i, int count, float sc, double value, Action<double> onArrive, float holdSec, float stepSec, float flyBase, float flyJit, Sprite sprite = null)
        {
            var orb = new Orb { Value = value, OnArrive = onArrive };
            if (root != null)
            {
                // T502 ③ — 월드 SpriteRenderer. 그림 크기는 «한 변 = sizePx(프레임 px)» 가 화면에서 같게 보이도록 그림의 bounds 로 나눠 맞춘다(BattleWorld.PopIcon 과 같은 셈).
                var go = new GameObject(OrbName); go.transform.SetParent(root, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite != null ? sprite : (App.I != null && App.I.Assets != null ? App.I.Assets.Sprite(spriteKey) : null);
                sr.color = tint; sr.sortingOrder = OrbSortOrder;
                float b = sr.sprite != null ? Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y) : 0f;
                orb.Unit = b > 1e-4f ? sizePx * BattleWorld.WorldPerPx / b : BattleWorld.WorldPerPx;
                orb.Go = go; orb.Tr = go.transform;
            }
            else
            {
                var img = UiKit.Icon(_layer, OrbName, spriteKey, tint);
                // T473 — 칸이 든 «그 그림» 으로(키가 카탈로그에 없어도 · 펫 메이커 썸네일 같은 런타임 스프라이트)
                if (sprite != null) img.sprite = sprite;
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = Vector2.zero; rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(sizePx, sizePx);
                orb.Go = rt.gameObject; orb.Rt = rt;
            }
            float spread = sizePx * 1.6f;
            var start = from + new Vector2(UnityEngine.Random.Range(-spread, spread), UnityEngine.Random.Range(-spread * 0.4f, spread * 0.4f));
            SetPos(orb, start);
            SetScale(orb, 0.7f);
            var hop = start + new Vector2(UnityEngine.Random.Range(-spread, spread), sizePx * UnityEngine.Random.Range(1.4f, 2.6f));
            // T109 2항 — 비행 시간은 거리와 무관하게 고정(구슬마다 ±FlyJitter 만 흔든다)
            float fly = flyBase + (flyJit > 0f ? UnityEngine.Random.Range(-flyJit, flyJit) : 0f);   // T313 — 값은 Pace 가 정한다(예산이 있으면 줄어든 값)
            // T109 2항 «랜덤 곡선» — 제어점을 진행 방향의 «옆»으로 밀어 구슬마다 다른 활을 그린다.
            // 옆으로 벌어졌다 목적지에서 다시 모이므로 여러 개가 한꺼번에 날 때 겹쳐 보이지 않는다.
            // T461 ⓐ — 그 «랜덤» 을 구슬 번호로 바꿨다(좌·우 번갈아 · 곡률 세 갈래). 무작위가 빠지니 Core 의 자가 이 길을 잰다.
            var ctrl = Vec(OrbPath.Ctrl(Pt(hop), Pt(to), i, sizePx));
            // 트윈은 전부 «px 자리·배율» 을 돌리고 SetPos/SetScale 이 모드대로 놓는다(T502 ③) — UI 모드에서는 종전 DOAnchorPos/DOScale 과 같은 값이 같은 시간에 간다.
            var seq = DOTween.Sequence().SetLink(orb.Go);   // SetLink(T56) — 전투 종료로 층·월드가 먼저 파괴돼도 경고 0
            if (i > 0 && stepSec > 0f) seq.AppendInterval(i * stepSec / sc);
            seq.Append(DOTween.To(() => orb.S, v => SetScale(orb, v), 1f, HopSec / sc).SetEase(Ease.OutBack));
            seq.Join(DOTween.To(() => orb.Px, v => SetPos(orb, v), hop, HopSec / sc).SetEase(Ease.OutQuad));
            // T109 1항 «1초 정도 머물렀다가» — 그 자리에서 살짝 위아래로 흔들며 기다린다(요요라 끝나면 hop 자리로 정확히 돌아온다)
            // (T269) 머무름은 부르는 쪽이 정한다 — 전투는 «죽은 그 자리에 잠깐 남는» 1초이고, 리워드 팝업은 그 자리가 닫히며 사라지므로 거의 0 이다.
            if (holdSec > 0.001f) seq.Append(DOTween.To(() => orb.Px.y, y => SetPos(orb, new Vector2(orb.Px.x, y)), hop.y + sizePx * 0.35f, holdSec * 0.5f / sc).SetEase(Ease.InOutSine).SetLoops(2, LoopType.Yoyo));
            int trailSteps = Mathf.Max(4, Mathf.RoundToInt(fly / TrailStepSec));
            int lastTrail = -1;
            // T461 ⓐ — 진행률을 트윈의 ease 가 아니라 OrbPath.Ease(InOutSine)가 정한다(그래서 여기는 Linear 다).
            //   왜 옮겼나: 종전 Ease.InQuad 는 «도착하는 순간이 가장 빠른» 꼴이라(실측 평균의 2.44배) 흡수가 끊기듯 보였다.
            //   셈을 Core 에 두면 이 통에서 매 회차 돌아 «부드러운가» 가 눈이 아니라 수로 지켜진다(OrbPathTests).
            seq.Append(DOVirtual.Float(0f, 1f, fly / sc, p =>
            {
                if (orb.Go == null) return;
                var pos = Bezier(hop, ctrl, to, OrbPath.Ease(p));
                SetPos(orb, pos);
                SetScale(orb, OrbPath.Scale(p));      // 도착 직전 0.8배 — 과녁으로 «빨려 들어가는» 꼴
                // T109 3항 트레일 — 새 그림을 만들지 않고 «같은 스프라이트의 잔상» 을 일정 간격으로 떨군다
                // ⚠ 잔상 간격은 ease 를 안 먹인 p(=시간)로 센다 — 그래야 감속 구간에서 잔상이 뭉치지 않는다.
                int k = Mathf.FloorToInt(p * trailSteps);
                if (k > lastTrail) { lastTrail = k; SpawnTrail(pos, spriteKey, tint, sizePx, sc); }
            }).SetEase(Ease.Linear));
            seq.AppendCallback(() => Arrive(orb));
            seq.Append(DOTween.To(() => orb.S, v => SetScale(orb, v), 1.15f, PopSec * 0.4f / sc));
            seq.Append(DOTween.To(() => orb.S, v => SetScale(orb, v), 0f, PopSec * 0.6f / sc));
            seq.OnComplete(() => Kill(orb));
            AttachWorldTrail(orb, tint);
            orb.Seq = seq;
            _alive.Add(orb);
        }

        /// <summary>
        /// T109 3항 — 구슬이 지나간 자리에 <b>같은 스프라이트</b> 한 장을 옅게 깔고 곧 지운다(잔상 = 꼬리).
        /// 새 그림을 만들지 않는다(§1 «에셋은 주인 에셋만»). uGUI 라 <c>TrailRenderer</c>(월드 렌더러)는 쓸 수 없다 — 지시서 3항이 허용한 «잔상 스프라이트» 쪽이다.
        /// 층의 <b>맨 뒤</b>(첫 형제)에 넣어 구슬 아래로 깔리고, 클릭은 받지 않는다.
        /// </summary>
        void SpawnTrail(Vector2 pos, string spriteKey, Color tint, float sizePx, float sc)
        {
            // T144 ⓒ — 월드 꼬리(TrailRenderer)가 붙었으면 잔상은 안 뿌린다(두 겹이면 지저분하다 · 지시서 3항)
            if (UseWorldTrail || _layer == null || _trails.Count >= MaxTrail) return;
            var img = UiKit.Icon(_layer, TrailName, spriteKey, tint);
            if (img == null) return;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.SetAsFirstSibling();
            rt.anchorMin = rt.anchorMax = Vector2.zero; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(sizePx, sizePx);
            rt.anchoredPosition = pos;
            rt.localScale = Vector3.one * 0.62f;
            var c = img.color; c.a *= 0.55f; img.color = c;
            _trails.Add(rt);
            var seq = DOTween.Sequence().SetLink(rt.gameObject);
            seq.Append(img.DOFade(0f, TrailFadeSec / sc).SetEase(Ease.OutQuad));
            seq.Join(rt.DOScale(0.28f, TrailFadeSec / sc).SetEase(Ease.OutQuad));
            seq.OnComplete(() => { _trails.Remove(rt); if (rt != null) UnityEngine.Object.Destroy(rt.gameObject); });
        }

        /// <summary>잔상을 한꺼번에 지운다 — 화면 전환·새 판·즉시 적립에서 층에 꼬리만 남지 않게.</summary>
        void ClearTrails()
        {
            var list = new List<RectTransform>(_trails);
            _trails.Clear();
            foreach (var t in list) { if (t != null) { DOTween.Kill(t.gameObject); UnityEngine.Object.Destroy(t.gameObject); } }
        }


        /// <summary>
        /// T144 ⓒ — 구슬 하나에 <b>월드</b> <c>TrailRenderer</c> 를 붙인다. 구슬 그림(UI)은 그대로 두고 꼬리만 월드에 뜬다.
        /// 따라다니는 일은 붙인 오브젝트 자신이 한다(<see cref="OrbTrail"/>) — <see cref="RewardOrbs"/> 는 MonoBehaviour 가 아니라 매 프레임 도는 자리가 없다.
        /// 머티리얼은 <b>새로 만들지 않고</b> 씬에 이미 있는 월드 스프라이트의 것을 빌려 쓴다 — URP 2D 라 <c>Shader.Find("Sprites/Default")</c> 는
        /// 빌드에서 스트리핑될 수 있고(§1 «주인 에셋만»), 빌린 것은 이미 빌드에 들어 있는 물건이라 확실하다. 못 찾으면 꼬리 없이 간다(구슬은 그대로 난다).
        /// </summary>
        void AttachWorldTrail(Orb orb, Color tint)
        {
            if (!UseWorldTrail || orb == null || orb.Go == null) return;
            var mat = TrailMaterial();
            if (mat == null) return;
            var go = new GameObject(TrailObjName);
            go.transform.position = WorldCam.FromFrame(orb.Px, TrailZ);
            var tr = go.AddComponent<TrailRenderer>();
            tr.time = TrailTime; tr.startWidth = TrailStartW; tr.endWidth = TrailEndW;
            tr.numCapVertices = 4; tr.minVertexDistance = 0.02f; tr.autodestruct = false; tr.emitting = true;
            tr.sharedMaterial = mat;
            // 색은 구슬 색 그대로(경험치 초록 · 골드 흰/노랑) — 끝으로 갈수록 투명해진다
            var c0 = tint; var c1 = tint; c1.a = 0f;
            tr.startColor = c0; tr.endColor = c1;
            tr.sortingLayerID = SortLayer(); tr.sortingOrder = TrailSortOrder;
            var follow = go.AddComponent<OrbTrail>(); follow.Target = orb;
            orb.Trail = go;
            _worldTrails.Add(go);
        }

        /// <summary>
        /// 꼬리 머티리얼을 한 번 만들어 캐시한다(위 설명).
        /// <para>
        /// ⚠ <b>«씬에서 빌린다» 만으로는 부족하다 — 빌릴 것이 하나도 없는 화면이 실제로 생겼다.</b>
        /// 이 자리는 <c>Shader.Find("Sprites/Default")</c> 를 피하려고 «씬에 이미 있는 월드 스프라이트의 머티리얼» 을 빌려 썼고,
        /// <b>못 찾으면 조용히 꼬리 없이</b> 갔다. 그런데 T262 ⓐ 가 로비 아바타의 <see cref="HeroView"/> 를 초상 아이콘으로 갈아 끼우자
        /// <b>로비의 활성 <see cref="SpriteRenderer"/> 가 0</b> 이 됐고, 로비에서 나는 흡수 구슬의 꼬리가 <b>빨간 줄 하나 없이 사라졌다</b>
        /// (런 645 의 <c>RewardOrbTests</c> 가 그것을 잡았다 · 워커 B 실측 · 결정 816).
        /// </para>
        /// <para>
        /// 그래서 <b>빌릴 것이 없을 때는 «기본 스프라이트 머티리얼» 을 유니티에게 직접 받는다</b> —
        /// 빈 <see cref="GameObject"/> 에 <see cref="SpriteRenderer"/> 를 하나 붙이면 유니티가 기본 머티리얼을 넣어 주고, 그 사본만 챙긴 뒤 바로 버린다.
        /// <b>이름으로 셰이더를 찾지 않으므로</b>(§1 · 스트리핑) 종전 규칙을 그대로 지키면서 «빌릴 것이 없는 화면» 을 덮는다.
        /// </para>
        /// 곁들여 <b>꺼져 있는</b> 스프라이트도 빌릴 대상에 넣는다(<see cref="FindObjectsInactive.Include"/>) — 머티리얼은 켜져 있든 아니든 같은 물건이다.
        /// </summary>
        Material TrailMaterial()
        {
            if (_trailMat != null) return _trailMat;
            var sr = UnityEngine.Object.FindFirstObjectByType<SpriteRenderer>(FindObjectsInactive.Include);
            var src = sr != null ? sr.sharedMaterial : null;
            if (sr != null) _sortLayer = sr.sortingLayerID;

            GameObject probe = null;
            if (src == null)
            {
                probe = new GameObject("~OrbTrailMatProbe") { hideFlags = HideFlags.HideAndDontSave };
                var psr = probe.AddComponent<SpriteRenderer>();
                src = psr.sharedMaterial;          // 유니티가 넣어 주는 기본 스프라이트 머티리얼 — Shader.Find 를 안 쓴다
                _sortLayer = psr.sortingLayerID;
            }

            if (src != null)
            {
                // 빌린 셰이더를 그대로 쓰되 «그림» 은 흰 텍스처로 바꾼 사본을 하나 만든다 — 안 그러면 꼬리가 그 스프라이트의 그림을 물고 늘어진다.
                // (스프라이트 셰이더는 텍스처를 SpriteRenderer 가 넣어 주는데 TrailRenderer 는 안 넣어 주므로 머티리얼의 것이 그대로 쓰인다.)
                _trailMat = new Material(src) { name = "OrbTrailMat" };
                if (_trailMat.HasProperty(MainTex)) _trailMat.SetTexture(MainTex, Texture2D.whiteTexture);
            }
            if (probe != null) UnityEngine.Object.Destroy(probe);
            return _trailMat;
        }
        int SortLayer() { return _sortLayer; }

        /// <summary>꼬리 하나를 거둔다 — 구슬이 사라지면 더 이상 늘리지 않고, 이미 그려진 꼬리는 <see cref="TrailTime"/> 만큼 남았다가 없어진다.</summary>
        void RetireTrail(Orb o, bool immediate)
        {
            if (o == null || o.Trail == null) return;
            var go = o.Trail; o.Trail = null;
            _worldTrails.Remove(go);
            var f = go.GetComponent<OrbTrail>(); if (f != null) f.Target = null;
            var tr = go.GetComponent<TrailRenderer>(); if (tr != null) tr.emitting = false;
            if (immediate) { if (tr != null) tr.Clear(); UnityEngine.Object.Destroy(go); }
            else UnityEngine.Object.Destroy(go, TrailTime);
        }

        /// <summary>남은 꼬리를 즉시 없앤다 — 화면 전환·새 판(누수 0).</summary>
        void ClearWorldTrails()
        {
            var list = new List<GameObject>(_worldTrails);
            _worldTrails.Clear();
            foreach (var go in list) { if (go == null) continue; var f = go.GetComponent<OrbTrail>(); if (f != null) f.Target = null; UnityEngine.Object.Destroy(go); }
        }

        /// <summary>구슬을 월드에서 따라다니는 꼬리 — 자기 <c>LateUpdate</c> 로 따라간다(구슬 트윈이 끝난 «뒤» 라 한 프레임도 안 밀린다). 구슬의 px 자리(<see cref="Orb.Px"/>)를 읽으므로 UI·월드 두 모드가 같다(T502 ③).</summary>
        sealed class OrbTrail : MonoBehaviour
        {
            public Orb Target;
            void LateUpdate()
            {
                if (Target == null || Target.Go == null) return;
                transform.position = WorldCam.FromFrame(Target.Px, TrailZ);
            }
        }

        /// <summary>T461 ⓐ — 셈은 <see cref="OrbPath"/>(Core · 자가 매 회차 돈다)가 하고 여기는 <see cref="Vector2"/> 로 바꾸기만 한다.</summary>
        static Vector2 Bezier(Vector2 a, Vector2 c, Vector2 b, float t) => Vec(OrbPath.Bezier(Pt(a), Pt(c), Pt(b), t));
        static OrbPath.P Pt(Vector2 v) => new OrbPath.P(v.x, v.y);
        static Vector2 Vec(OrbPath.P p) => new Vector2(p.X, p.Y);

        void Arrive(Orb o)
        {
            if (o.Done) return;
            o.Done = true;
            var cb = o.OnArrive; o.OnArrive = null;
            if (cb != null) cb(o.Value);
        }
        void Kill(Orb o)
        {
            Arrive(o);
            RetireTrail(o, false);
            Drop(o);
            _alive.Remove(o);
        }
        /// <summary>구슬 오브젝트를 지운다(두 모드 공통).</summary>
        static void Drop(Orb o)
        {
            if (o.Go != null) UnityEngine.Object.Destroy(o.Go);
            o.Go = null; o.Rt = null; o.Tr = null;
        }

        /// <summary>남은 구슬의 값을 즉시 적립하고 없앤다 — 사망·클리어 팝업이 흡수를 오래 기다리지 않게(0.6초 상한 · 주인 «무한 대기 금지»).</summary>
        public void FinishNow()
        {
            var list = new List<Orb>(_alive);
            _alive.Clear();
            foreach (var o in list) { if (o.Seq != null) { o.Seq.Kill(); o.Seq = null; } Arrive(o); RetireTrail(o, true); Drop(o); }
            ClearTrails(); ClearWorldTrails();
        }
        /// <summary>값 적립 없이 비운다 — 화면 전환·새 판(호출자가 표시값을 엔진 값으로 맞춘다).</summary>
        public void Clear()
        {
            var list = new List<Orb>(_alive);
            _alive.Clear();
            foreach (var o in list) { if (o.Seq != null) { o.Seq.Kill(); o.Seq = null; } o.OnArrive = null; RetireTrail(o, true); Drop(o); }
            ClearTrails(); ClearWorldTrails();
        }

        void Prune()
        {
            for (int i = _alive.Count - 1; i >= 0; i--) if (_alive[i].Go == null) _alive.RemoveAt(i);
        }
    }
}
