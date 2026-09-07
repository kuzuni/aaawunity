using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// T85 — 적을 죽이면 그 자리에서 튀어나와 HUD 로 «날아가 흡수되는» 보상 구슬(경험치 구슬 · 골드 코인) 층.
    /// 값(엔진)은 킬 순간에 이미 올라 있고(<see cref="KkomaKnight.Core.BattleState"/> 불변 · 시드 골든 불변),
    /// 여기서 옮기는 것은 «표시값이 언제 오르는가» 뿐이다 — 구슬이 목표(EXP 바 · 골드 pill)에 <b>도착할 때마다</b> 그 몫을 <see cref="Fly"/> 의 콜백으로 넘긴다.
    /// 경로(T109 · 주인 «1초 정도 머물렀다가 랜덤 곡선 그리면서 (트레일 있어야 함) 0.8초 동안 흡수») = 짧게 위로 튀었다가(<see cref="HopSec"/>) 그 자리에서 <see cref="HoldSec"/> 머물며 흔들리고,
    /// 제어점을 진행 방향의 <b>옆(좌·우 랜덤)</b>으로 민 2차 베지어로 <see cref="FlySec"/>(거리 무관 · ±<see cref="FlyJitter"/>) 만에 목표까지 — 구슬마다 <see cref="StepSec"/> 시차 · 지나간 자리에 잔상(<see cref="TrailName"/>)을 떨군다.
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
        public const int MaxAlive = 40;                       // 화면 동시 상한 — 넘으면 개수를 줄인다(값은 그대로)
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
            public RectTransform Rt; public Sequence Seq; public double Value; public Action<double> OnArrive; public bool Done; public GameObject Trail;
        }

        readonly RectTransform _layer;
        readonly List<Orb> _alive = new List<Orb>();
        /// <summary>지금 화면에 떠 있는 잔상(T109 3항) — 상한(<see cref="MaxTrail"/>)과 한꺼번에 지우기에 쓴다.</summary>
        readonly List<RectTransform> _trails = new List<RectTransform>();
        /// <summary>T144 — 월드 꼬리(<c>TrailRenderer</c>) 들. 누수 0 을 위해 여기서 센다.</summary>
        readonly List<GameObject> _worldTrails = new List<GameObject>();
        Material _trailMat; int _sortLayer;
        static readonly int MainTex = Shader.PropertyToID("_MainTex");
        /// <summary>T144 — 꼬리를 월드 <c>TrailRenderer</c> 로 낼 것인가(끄면 T109 의 잔상 스프라이트로 돌아간다 · 되돌리는 스위치).</summary>
        public static bool UseWorldTrail = true;
        /// <summary>꼬리의 정렬 순서 — 캐릭터(0)보다 뒤에 깔아 그림을 안 가린다.</summary>
        public const int TrailSortOrder = -50;

        public RewardOrbs(RectTransform layer) { _layer = layer; }

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
        public int Fly(Vector2 from, RectTransform target, string spriteKey, Color tint, int count, double total, float sizePx, float timeScale, Action<double> onArrive)
        {
            Prune();
            if (_layer == null || target == null || count <= 0 || total <= 0) return 0;
            count = Mathf.Min(count, Mathf.Max(0, MaxAlive - _alive.Count));
            if (count <= 0) return 0;
            float sc = Mathf.Max(0.5f, timeScale);
            var to = TargetPos(target);
            double each = total / count;
            for (int i = 0; i < count; i++)
            {
                double val = i == count - 1 ? total - each * (count - 1) : each;
                Make(from, to, spriteKey, tint, sizePx, i, count, sc, val, onArrive);
            }
            return count;
        }

        void Make(Vector2 from, Vector2 to, string spriteKey, Color tint, float sizePx, int i, int count, float sc, double value, Action<double> onArrive)
        {
            var img = UiKit.Icon(_layer, OrbName, spriteKey, tint);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(sizePx, sizePx);
            float spread = sizePx * 1.6f;
            var start = from + new Vector2(UnityEngine.Random.Range(-spread, spread), UnityEngine.Random.Range(-spread * 0.4f, spread * 0.4f));
            rt.anchoredPosition = start;
            rt.localScale = Vector3.one * 0.7f;
            var hop = start + new Vector2(UnityEngine.Random.Range(-spread, spread), sizePx * UnityEngine.Random.Range(1.4f, 2.6f));
            // T109 2항 — 비행 시간은 거리와 무관하게 고정(구슬마다 ±FlyJitter 만 흔든다)
            float fly = FlySec + UnityEngine.Random.Range(-FlyJitter, FlyJitter);
            // T109 2항 «랜덤 곡선» — 제어점을 진행 방향의 «옆»(좌·우 랜덤)으로 밀어 구슬마다 다른 활을 그린다.
            // 옆으로 벌어졌다 목적지에서 다시 모이므로 여러 개가 한꺼번에 날 때 겹쳐 보이지 않는다.
            var seg = to - hop; float segLen = seg.magnitude;
            var perp = segLen > 0.001f ? new Vector2(-seg.y, seg.x) / segLen : Vector2.up;
            float side = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            float bow = Mathf.Max(sizePx * 2.5f, segLen * UnityEngine.Random.Range(0.22f, 0.45f)) * side;
            var ctrl = (hop + to) * 0.5f + perp * bow + Vector2.up * UnityEngine.Random.Range(sizePx, sizePx * 3f);
            var orb = new Orb { Rt = rt, Value = value, OnArrive = onArrive };
            var seq = DOTween.Sequence().SetLink(rt.gameObject);   // SetLink(T56) — 전투 종료로 층이 먼저 파괴돼도 경고 0
            if (i > 0) seq.AppendInterval(i * StepSec / sc);
            seq.Append(rt.DOScale(1f, HopSec / sc).SetEase(Ease.OutBack));
            seq.Join(rt.DOAnchorPos(hop, HopSec / sc).SetEase(Ease.OutQuad));
            // T109 1항 «1초 정도 머물렀다가» — 그 자리에서 살짝 위아래로 흔들며 기다린다(요요라 끝나면 hop 자리로 정확히 돌아온다)
            seq.Append(rt.DOAnchorPosY(hop.y + sizePx * 0.35f, HoldSec * 0.5f / sc).SetEase(Ease.InOutSine).SetLoops(2, LoopType.Yoyo));
            int trailSteps = Mathf.Max(4, Mathf.RoundToInt(fly / TrailStepSec));
            int lastTrail = -1;
            seq.Append(DOVirtual.Float(0f, 1f, fly / sc, p =>
            {
                if (rt == null) return;
                var pos = Bezier(hop, ctrl, to, p);
                rt.anchoredPosition = pos;
                // T109 3항 트레일 — 새 그림을 만들지 않고 «같은 스프라이트의 잔상» 을 일정 간격으로 떨군다
                int k = Mathf.FloorToInt(p * trailSteps);
                if (k > lastTrail) { lastTrail = k; SpawnTrail(pos, spriteKey, tint, sizePx, sc); }
            }).SetEase(Ease.InQuad));
            seq.AppendCallback(() => Arrive(orb));
            seq.Append(rt.DOScale(1.15f, PopSec * 0.4f / sc));
            seq.Append(rt.DOScale(0f, PopSec * 0.6f / sc));
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
            if (!UseWorldTrail || orb == null || orb.Rt == null) return;
            var mat = TrailMaterial();
            if (mat == null) return;
            var go = new GameObject(TrailObjName);
            go.transform.position = WorldCam.FromFrame(orb.Rt.anchoredPosition, TrailZ);
            var tr = go.AddComponent<TrailRenderer>();
            tr.time = TrailTime; tr.startWidth = TrailStartW; tr.endWidth = TrailEndW;
            tr.numCapVertices = 4; tr.minVertexDistance = 0.02f; tr.autodestruct = false; tr.emitting = true;
            tr.sharedMaterial = mat;
            // 색은 구슬 색 그대로(경험치 초록 · 골드 흰/노랑) — 끝으로 갈수록 투명해진다
            var c0 = tint; var c1 = tint; c1.a = 0f;
            tr.startColor = c0; tr.endColor = c1;
            tr.sortingLayerID = SortLayer(); tr.sortingOrder = TrailSortOrder;
            var follow = go.AddComponent<OrbTrail>(); follow.Follow = orb.Rt;
            orb.Trail = go;
            _worldTrails.Add(go);
        }

        /// <summary>씬의 월드 스프라이트에서 머티리얼을 한 번 빌려 캐시한다(위 설명).</summary>
        Material TrailMaterial()
        {
            if (_trailMat != null) return _trailMat;
            var sr = UnityEngine.Object.FindFirstObjectByType<SpriteRenderer>(FindObjectsInactive.Exclude);
            if (sr == null || sr.sharedMaterial == null) return null;
            _sortLayer = sr.sortingLayerID;
            // 빌린 셰이더를 그대로 쓰되 «그림» 은 흰 텍스처로 바꾼 사본을 하나 만든다 — 안 그러면 꼬리가 그 스프라이트의 그림을 물고 늘어진다.
            // (스프라이트 셰이더는 텍스처를 SpriteRenderer 가 넣어 주는데 TrailRenderer 는 안 넣어 주므로 머티리얼의 것이 그대로 쓰인다.)
            _trailMat = new Material(sr.sharedMaterial) { name = "OrbTrailMat" };
            if (_trailMat.HasProperty(MainTex)) _trailMat.SetTexture(MainTex, Texture2D.whiteTexture);
            return _trailMat;
        }
        int SortLayer() { return _sortLayer; }

        /// <summary>꼬리 하나를 거둔다 — 구슬이 사라지면 더 이상 늘리지 않고, 이미 그려진 꼬리는 <see cref="TrailTime"/> 만큼 남았다가 없어진다.</summary>
        void RetireTrail(Orb o, bool immediate)
        {
            if (o == null || o.Trail == null) return;
            var go = o.Trail; o.Trail = null;
            _worldTrails.Remove(go);
            var f = go.GetComponent<OrbTrail>(); if (f != null) f.Follow = null;
            var tr = go.GetComponent<TrailRenderer>(); if (tr != null) tr.emitting = false;
            if (immediate) { if (tr != null) tr.Clear(); UnityEngine.Object.Destroy(go); }
            else UnityEngine.Object.Destroy(go, TrailTime);
        }

        /// <summary>남은 꼬리를 즉시 없앤다 — 화면 전환·새 판(누수 0).</summary>
        void ClearWorldTrails()
        {
            var list = new List<GameObject>(_worldTrails);
            _worldTrails.Clear();
            foreach (var go in list) { if (go == null) continue; var f = go.GetComponent<OrbTrail>(); if (f != null) f.Follow = null; UnityEngine.Object.Destroy(go); }
        }

        /// <summary>UI 층에서 도는 구슬을 월드에서 따라다니는 꼬리 — 자기 <c>LateUpdate</c> 로 따라간다(구슬 트윈이 끝난 «뒤» 라 한 프레임도 안 밀린다).</summary>
        sealed class OrbTrail : MonoBehaviour
        {
            public RectTransform Follow;
            void LateUpdate()
            {
                if (Follow == null) return;
                transform.position = WorldCam.FromFrame(Follow.anchoredPosition, TrailZ);
            }
        }

        static Vector2 Bezier(Vector2 a, Vector2 c, Vector2 b, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * c + t * t * b;
        }

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
            if (o.Rt != null) { UnityEngine.Object.Destroy(o.Rt.gameObject); o.Rt = null; }
            _alive.Remove(o);
        }

        /// <summary>남은 구슬의 값을 즉시 적립하고 없앤다 — 사망·클리어 팝업이 흡수를 오래 기다리지 않게(0.6초 상한 · 주인 «무한 대기 금지»).</summary>
        public void FinishNow()
        {
            var list = new List<Orb>(_alive);
            _alive.Clear();
            foreach (var o in list) { if (o.Seq != null) { o.Seq.Kill(); o.Seq = null; } Arrive(o); RetireTrail(o, true); if (o.Rt != null) { UnityEngine.Object.Destroy(o.Rt.gameObject); o.Rt = null; } }
            ClearTrails(); ClearWorldTrails();
        }
        /// <summary>값 적립 없이 비운다 — 화면 전환·새 판(호출자가 표시값을 엔진 값으로 맞춘다).</summary>
        public void Clear()
        {
            var list = new List<Orb>(_alive);
            _alive.Clear();
            foreach (var o in list) { if (o.Seq != null) { o.Seq.Kill(); o.Seq = null; } o.OnArrive = null; RetireTrail(o, true); if (o.Rt != null) { UnityEngine.Object.Destroy(o.Rt.gameObject); o.Rt = null; } }
            ClearTrails(); ClearWorldTrails();
        }

        void Prune()
        {
            for (int i = _alive.Count - 1; i >= 0; i--) if (_alive[i].Rt == null) _alive.RemoveAt(i);
        }
    }
}
