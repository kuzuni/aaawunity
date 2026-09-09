using DG.Tweening;
using KkomaKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace KkomaKnight.Game
{
    /// <summary>
    /// T318 — 퀘스트 «이동» 의 <b>길잡이</b> + 목적지 버튼 위 <b>손가락 힌트</b>
    /// (주인 2026-09-09 11:0X «이동 버튼 누르면 해당 거 가능하게 이동 · 도전해야 하면 손가락으로 가리키면서 클릭하라는 식으로 힌트»).
    /// <para>
    /// <b>어디로 가는가는 표가 정한다</b>(<c>quest.json</c> 의 <c>go = {screen, point}</c> · <see cref="QuestData.Go"/>). 여기는
    /// 그 낱말 여섯(<see cref="QuestData.GoScreens"/>)을 <b>화면 여는 손잡이</b>로 옮기는 갈래와, 그 화면에서 이름이 <c>point</c> 인
    /// <b>켜진</b> 버튼을 찾아 손가락을 세우는 일만 한다. 낱말이 늘면 갈래도 늘어야 하고 그것은 <c>QuestGoPlayTests</c> 가 표의 줄마다 밟는다.
    /// </para>
    /// <para>
    /// 손가락(<see cref="HintFinger"/>)은 <b>버튼의 자식</b>으로 선다 — 화면이 닫히거나 팝업이 치워지면(<c>UiKit.Clear</c>) 저절로 같이 사라진다.
    /// 끄는 조건 셋은 2항 그대로: 그 버튼을 누르거나 · 화면을 벗어나거나(<c>OnDisable</c>) · 표의 <c>lifeSec</c> 이 지나면. 한 번에 힌트는 하나(<see cref="Current"/>).
    /// 튜토리얼이 아니다 — 저장하지 않는다.
    /// </para>
    /// </summary>
    public static class QuestGo
    {
        /// <summary>손가락 오브젝트 이름(버튼 자식) · 빛 테두리 이름(버튼 «뒤» 형제) — 자가 이 이름으로 찾는다.</summary>
        public const string HintName = "Hint", GlowName = "HintGlow";
        /// <summary>빛 테두리 = 버튼보다 이만큼 큰 글로우 서클(<c>ui.glow1</c> · 새 그림 0) 한 장이 버튼 뒤에서 한 번 떠올랐다 꺼진다.</summary>
        public const float GlowScale = 1.35f, GlowAlpha = 0.9f;

        /// <summary>지금 서 있는 손가락(없으면 null). 새로 세우면 앞 것은 먼저 치운다.</summary>
        public static HintFinger Current { get; private set; }

        /// <summary>
        /// «이동» — 목적지 화면을 열고 그 버튼에 손가락을 세운다. <paramref name="go"/> 가 null 이면(로그인류) 아무 일도 안 한다.
        /// 돌려주는 값 = 손가락이 선 버튼(못 찾으면 null · 경고 한 줄 — 빨간 줄이 아니다: 화면은 열렸고 힌트만 없다).
        /// </summary>
        public static RectTransform Open(App app, QuestData.Go go)
        {
            if (app == null || go == null) return null;
            Transform root;
            switch (go.Screen)
            {
                case "lobby": app.Overlay.Close(); app.ShowScreen("lobby"); root = app.Current.Root; break;
                // T107 — 이벤트 화면은 던전 페이지부터. Open 이 팝업을 닫고 페이지까지 고른다.
                case "dungeon": EventsScreen.Open(app, EventsScreen.PageDungeon); root = app.Current.Root; break;
                // «상자» 절은 스크롤 맨 위(1 = 레퍼런스 10)다 — 첫 카드의 «1회»(One) 가 거기 있다.
                case "chest": app.Overlay.Close(); app.ShowScreen("shop"); app.GetScreen<ShopScreen>()?.ScrollTo(1f); root = app.Current.Root; break;
                case "forge": app.Overlay.Close(); app.ShowScreen("forge"); root = app.Current.Root; break;
                case "pet": app.Overlay.Close(); app.ShowScreen("pet"); root = app.Current.Root; break;
                // 탐험은 로비 위의 팝업이다 — 버튼은 팝업 층(Overlay.Root)에서 찾는다.
                case "expedition": app.Overlay.Close(); app.ShowScreen("lobby"); LobbyPopups.Expedition(app); root = app.Overlay.Root; break;
                default:
                    Debug.LogWarning("[QuestGo] 모르는 목적지 — " + go.Screen + " (표의 낱말은 " + string.Join("·", QuestData.GoScreens) + ")");
                    return null;
            }
            var target = FindActive(root, go.Points);
            if (target == null)
            {
                Debug.LogWarning("[QuestGo] 손가락이 가리킬 버튼을 못 찾았다 — " + go.Screen + " › " + string.Join("|", go.Points));
                return null;
            }
            Hint(app, target);
            return target;
        }

        /// <summary>지금 화면(팝업이 열려 있으면 팝업 층)에서 이름이 <paramref name="point"/> 인 켜진 버튼에 손가락을 세운다. 못 찾으면 null.</summary>
        public static RectTransform Hint(App app, string point)
        {
            if (app == null || string.IsNullOrEmpty(point)) return null;
            Transform root = app.Overlay != null && app.Overlay.IsOpen ? app.Overlay.Root : (app.Current != null ? app.Current.Root : null);
            var target = FindActive(root, new[] { point });
            if (target == null) return null;
            Hint(app, target);
            return target;
        }

        /// <summary>
        /// 손가락 + 빛 테두리를 <paramref name="target"/> 에 세운다. 값은 전부 표(<see cref="QuestData.HintSpec"/>)에서 —
        /// <paramref name="lifeSec"/> 만 자가 짧게 줄 수 있다(8초를 실제로 기다리는 자는 느린 자다).
        /// </summary>
        public static HintFinger Hint(App app, RectTransform target, float? lifeSec = null)
        {
            var spec = app != null && app.Data != null && app.Data.Quest != null ? app.Data.Quest.Hint : null;
            if (target == null || spec == null) return null;
            Dismiss();   // 한 번에 하나

            // ⓐ 빛 테두리 — 버튼 «뒤» 형제(같은 앵커·같은 자리 · GlowScale 배)에 글로우 서클 한 장 · 떠올랐다 꺼지고 스스로 사라진다.
            //    버튼 «안»(자식)에 두면 버튼 그림 위에 얹혀 글자를 덮는다 — 뒤에 두어야 «테두리» 로 읽힌다.
            GameObject glowGo = null;
            var parent = target.parent;
            if (parent != null)
            {
                var grt = UiKit.Rect(parent, GlowName);
                grt.anchorMin = target.anchorMin; grt.anchorMax = target.anchorMax; grt.pivot = target.pivot;
                grt.anchoredPosition = target.anchoredPosition; grt.sizeDelta = target.sizeDelta;
                grt.localScale = new Vector3(GlowScale, GlowScale, 1f);
                grt.SetSiblingIndex(target.GetSiblingIndex());   // 버튼 바로 앞 형제 = 버튼 «뒤» 에 그려진다(버튼은 한 칸 뒤로 밀린다)
                var gi = grt.gameObject.AddComponent<Image>();
                gi.sprite = app.Assets != null ? app.Assets.Sprite(UiKit.GlowKey) : null; gi.preserveAspect = false; gi.raycastTarget = false;
                gi.color = Palette.A(Palette.Yellow, 0f);
                float half = Mathf.Max(0.05f, (float)spec.GlowSec * 0.5f);
                DOTween.Sequence().SetUpdate(true).SetLink(grt.gameObject)
                    .Append(gi.DOFade(GlowAlpha, half).SetEase(Ease.OutQuad))
                    .Append(gi.DOFade(0f, half).SetEase(Ease.InQuad))
                    .OnComplete(() => { if (grt != null) Object.Destroy(grt.gameObject); });
                glowGo = grt.gameObject;
            }

            // ⓑ 손가락 — 버튼 오른쪽 아래 모서리에 손끝이 닿게(그림은 왼쪽 위를 가리키는 손 · 실측: 손끝이 그림의 왼쪽 위 1/5 자리) · 위아래로 콕콕.
            var hand = UiKit.Icon(target, HintName, "pi.hand", Palette.White);
            var hrt = hand.rectTransform;
            float px = (float)spec.IconPx;
            hrt.anchorMin = hrt.anchorMax = new Vector2(1f, 0f); hrt.pivot = new Vector2(0.5f, 0.5f);
            hrt.sizeDelta = new Vector2(px, px);
            hrt.anchoredPosition = new Vector2(px * 0.30f, -px * 0.30f);   // 가운데를 모서리 밖 30% 에 — 손끝(왼쪽 위)이 버튼 안으로 든다
            hrt.SetAsLastSibling();
            var bob = hrt.DOAnchorPosY(hrt.anchoredPosition.y + (float)spec.BobPx, Mathf.Max(0.05f, (float)spec.PeriodSec))
                         .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(hrt.gameObject);

            var f = hrt.gameObject.AddComponent<HintFinger>();
            f.Init(target, glowGo, lifeSec ?? (float)spec.LifeSec, bob);
            Current = f;
            return f;
        }

        /// <summary>서 있는 손가락을 치운다(없으면 아무 일 없음).</summary>
        public static void Dismiss()
        {
            if (Current != null) Current.Dismiss();
            Current = null;
        }

        internal static void Forget(HintFinger f) { if (Current == f) Current = null; }

        /// <summary><paramref name="root"/> 아래에서 이름이 <paramref name="names"/> 중 하나이고 <b>켜져 있는</b> 첫 것 — 이름 순서가 우선순위다(대장간 «합성» 은 주황이 켜져 있으면 주황).</summary>
        public static RectTransform FindActive(Transform root, string[] names)
        {
            if (root == null || names == null) return null;
            var all = root.GetComponentsInChildren<Transform>(true);
            foreach (var n in names)
            {
                if (string.IsNullOrEmpty(n)) continue;
                foreach (var t in all)
                    if (t != null && t.name == n && t.gameObject.activeInHierarchy && t is RectTransform rt) return rt;
            }
            return null;
        }
    }

    /// <summary>
    /// T318 2항 — 버튼 자식으로 선 손가락 하나의 <b>수명</b>. 끄는 조건 셋: 그 버튼을 누르거나(<c>onClick</c>) · 화면을 벗어나거나(<c>OnDisable</c> —
    /// 부모 화면이 꺼지거나 팝업이 치워질 때) · <see cref="Life"/> 초(unscaled · 팝업 시간 정지와 무관)가 지나면. 사라질 때 빛 테두리도 같이 치운다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HintFinger : MonoBehaviour
    {
        public RectTransform Target;
        public float Life;
        GameObject _glow; Button _button; Tween _bob; float _t0; bool _dying;

        internal void Init(RectTransform target, GameObject glow, float life, Tween bob)
        {
            Target = target; _glow = glow; Life = life; _bob = bob; _t0 = Time.realtimeSinceStartup;
            _button = target != null ? target.GetComponent<Button>() : null;
            if (_button != null) _button.onClick.AddListener(Dismiss);
        }

        /// <summary>이 손가락이 선 지 몇 초(unscaled).</summary>
        public float Age => Time.realtimeSinceStartup - _t0;

        void Update()
        {
            if (Age >= Life) Dismiss();
        }

        void OnDisable() { Dismiss(); }   // 화면을 벗어났다(부모가 꺼졌다) — 다시 켜질 때 옛 손가락이 되살아나지 않게

        void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(Dismiss);
            QuestGo.Forget(this);
        }

        public void Dismiss()
        {
            if (_dying) return;
            _dying = true;
            if (_bob != null && _bob.IsActive()) _bob.Kill();
            if (_glow != null) Destroy(_glow);
            QuestGo.Forget(this);
            Destroy(gameObject);
        }
    }
}
