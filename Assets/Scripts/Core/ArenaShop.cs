using System;
using System.Collections.Generic;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 상인(아레나 상점 · 화면 26) 상품표 (<c>Assets/KkomaKnight/arenaShop.json</c> · T209).
    /// 값·개수는 전부 파일에서 온다 — 코드 상수 없음(ROUTINE §1 «코드에 게임 수치를 직접 박지 않는다»).
    /// <para>
    /// ⚠ 이 수치는 워커가 고른 값이 아니라 <b>주인 레퍼런스 <c>docs/ref/26_arena_shop.jpg</c> 에서 읽은 값</b>이다(T209 4항).
    /// 그림에 카드마다 «Limit 5/5» 와 아레나 코인 값이 찍혀 있다. aaaw <c>data/</c> 에는 이 표가 없다(일곱 파일 중 어디에도).
    /// </para>
    /// <para>
    /// ⚠ <b>전설 열쇠·부활 토큰의 한도·값은 레퍼런스에서 잘려 안 보인다</b> — 지어내지 않았다. 표에 없거나 값이 0 인 칸은
    /// 화면이 종전처럼 «—» 로 낸다(<see cref="Entry.HasLimit"/>·<see cref="Entry.HasCost"/>). 없는 수를 만드는 것이 «—» 보다 나쁘다.
    /// </para>
    /// </summary>
    public sealed class ArenaShopData
    {
        public sealed class Entry
        {
            /// <summary>상품 키 — 화면의 상품 차례와 <see cref="ArenaShopData.Of"/> 로 맞춘다.</summary>
            public string Key = "";
            /// <summary>하루 한도(레퍼런스 «Limit 5/5» 의 뒤 수) — 0 이면 «모른다»(레퍼런스에서 잘린 칸).</summary>
            public int Max;
            /// <summary>아레나 코인 값 — 0 이면 «모른다».</summary>
            public double Cost;
            /// <summary>아이콘 오른쪽 아래 개수 배지 — 0 이면 배지를 안 그린다(레퍼런스도 열쇠에는 배지가 없다).</summary>
            public int Badge;
            public bool HasLimit => Max > 0;
            public bool HasCost => Cost > 0;
            public bool HasBadge => Badge > 0;
        }

        /// <summary>«한도 {have}/{max}» 글꼴 — 글자도 파일에서 온다(코드에 문구 박지 않기).</summary>
        public string LimitText = "한도 {have}/{max}";
        public readonly List<Entry> Goods = new List<Entry>();

        public Entry Of(string key)
        {
            if (key == null) return null;
            foreach (var e in Goods) if (e.Key == key) return e;
            return null;
        }

        /// <summary>
        /// 아레나 코인 값 글자 — <b>레퍼런스처럼 «10000»·«5000» 그대로</b> 쓴다(콤마도 K 도 없다).
        /// <para>
        /// <see cref="Game.UiKit.Fmt"/> 를 쓰면 1e4 이상이 «10K» 로 줄고 그 아래는 «5,000» 이 되어 <b>둘 다 레퍼런스와 다르다</b>
        /// (docs/ref/26 은 «10000»·«5000»·«20000» 이다). <c>BattleWorld.FootNum</c> 이 발밑 숫자에서 콤마를 뺀 것과 같은 갈래다 —
        /// 「우리 기본 표기」가 아니라 「그 화면의 레퍼런스 표기」를 따른다(T209 · 결정 546).
        /// </para>
        /// 값을 모르는 칸은 <paramref name="dash"/>(화면의 «—»).
        /// </summary>
        public string Cost(Entry e, string dash) => e == null || !e.HasCost ? dash : e.Cost.ToString("0");

        /// <summary>«한도 5/5» 처럼 채워 돌려준다 — 한도를 모르는 칸은 <paramref name="dash"/>(화면의 «—»).</summary>
        public string Limit(Entry e, string dash)
        {
            if (e == null || !e.HasLimit) return dash;
            return LimitText.Replace("{have}", e.Max.ToString()).Replace("{max}", e.Max.ToString());
        }

        public static ArenaShopData Parse(string json) => From(new JNode(MiniJson.Parse(json)));
        public static ArenaShopData From(JNode j)
        {
            var d = new ArenaShopData();
            var lt = j["limitText"].Str("");
            if (!string.IsNullOrEmpty(lt)) d.LimitText = lt;
            foreach (var e in j["goods"].Items())
            {
                var key = e["key"].Str("");
                if (string.IsNullOrEmpty(key)) throw new FormatException("arenaShop.json: goods[].key 가 비었다");
                d.Goods.Add(new Entry { Key = key, Max = (int)e["max"].Num(0), Cost = e["cost"].Num(0), Badge = (int)e["badge"].Num(0) });
            }
            if (d.Goods.Count == 0) throw new FormatException("arenaShop.json: goods 가 비었다");
            return d;
        }
    }
}
