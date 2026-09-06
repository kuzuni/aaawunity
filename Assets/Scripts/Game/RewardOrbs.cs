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
    /// 경로 = 짧게 위로 튀었다가(<see cref="HopSec"/>) 제어점을 위에 둔 2차 베지어로 목표까지(<see cref="FlySecMin"/>~<see cref="FlySecMax"/> · 구슬마다 <see cref="StepSec"/> 시차).
    /// 모든 트윈에 <c>SetLink</c>(T56 · 콘솔 노란 줄 0) · <see cref="FinishNow"/> 는 남은 값을 즉시 적립하고 비운다(무한 대기 금지).
    /// </summary>
    public sealed class RewardOrbs
    {
        /// <summary>구슬 오브젝트 이름 — PlayMode 테스트가 이 이름으로 «생겼다/사라졌다» 를 본다.</summary>
        public const string OrbName = "Orb";
        public const float HopSec = 0.15f;                    // 적 자리에서 위로 튀는 시간
        public const float FlySecMin = 0.35f, FlySecMax = 0.5f;   // 곡선 비행(주인 지시 0.35~0.55초 안)
        public const float StepSec = 0.035f;                  // 구슬 사이 시차
        public const float PopSec = 0.08f;                    // 도착 뒤 «작게 튀고» 사라지는 꼬리
        public const int MaxAlive = 40;                       // 화면 동시 상한 — 넘으면 개수를 줄인다(값은 그대로)

        sealed class Orb
        {
            public RectTransform Rt; public Sequence Seq; public double Value; public Action<double> OnArrive; public bool Done;
        }

        readonly RectTransform _layer;
        readonly List<Orb> _alive = new List<Orb>();

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
            float fly = count > 1 ? Mathf.Lerp(FlySecMin, FlySecMax, i / (float)(count - 1)) : FlySecMin;
            // 제어점을 위쪽에 둔 2차 베지어 — 거리에 비례해 부풀린다(가까우면 얕게)
            var mid = (hop + to) * 0.5f;
            var ctrl = mid + Vector2.up * Mathf.Max(sizePx * 2f, Vector2.Distance(hop, to) * 0.35f);
            var orb = new Orb { Rt = rt, Value = value, OnArrive = onArrive };
            var seq = DOTween.Sequence().SetLink(rt.gameObject);   // SetLink(T56) — 전투 종료로 층이 먼저 파괴돼도 경고 0
            if (i > 0) seq.AppendInterval(i * StepSec / sc);
            seq.Append(rt.DOScale(1f, HopSec / sc).SetEase(Ease.OutBack));
            seq.Join(rt.DOAnchorPos(hop, HopSec / sc).SetEase(Ease.OutQuad));
            seq.Append(DOVirtual.Float(0f, 1f, fly / sc, p => { if (rt != null) rt.anchoredPosition = Bezier(hop, ctrl, to, p); }).SetEase(Ease.InQuad));
            seq.AppendCallback(() => Arrive(orb));
            seq.Append(rt.DOScale(1.15f, PopSec * 0.4f / sc));
            seq.Append(rt.DOScale(0f, PopSec * 0.6f / sc));
            seq.OnComplete(() => Kill(orb));
            orb.Seq = seq;
            _alive.Add(orb);
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
            if (o.Rt != null) { UnityEngine.Object.Destroy(o.Rt.gameObject); o.Rt = null; }
            _alive.Remove(o);
        }

        /// <summary>남은 구슬의 값을 즉시 적립하고 없앤다 — 사망·클리어 팝업이 흡수를 오래 기다리지 않게(0.6초 상한 · 주인 «무한 대기 금지»).</summary>
        public void FinishNow()
        {
            var list = new List<Orb>(_alive);
            _alive.Clear();
            foreach (var o in list) { if (o.Seq != null) { o.Seq.Kill(); o.Seq = null; } Arrive(o); if (o.Rt != null) { UnityEngine.Object.Destroy(o.Rt.gameObject); o.Rt = null; } }
        }
        /// <summary>값 적립 없이 비운다 — 화면 전환·새 판(호출자가 표시값을 엔진 값으로 맞춘다).</summary>
        public void Clear()
        {
            var list = new List<Orb>(_alive);
            _alive.Clear();
            foreach (var o in list) { if (o.Seq != null) { o.Seq.Kill(); o.Seq = null; } o.OnArrive = null; if (o.Rt != null) { UnityEngine.Object.Destroy(o.Rt.gameObject); o.Rt = null; } }
        }

        void Prune()
        {
            for (int i = _alive.Count - 1; i >= 0; i--) if (_alive[i].Rt == null) _alive.RemoveAt(i);
        }
    }
}
