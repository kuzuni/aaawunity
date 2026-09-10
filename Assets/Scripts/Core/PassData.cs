using System;
using System.Collections.Generic;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 시즌 패스 보상 표 (<c>Assets/KkomaKnight/pass.json</c> · T322 · 주인 2026-09-09 «패스 부분 <b>1~100까지 있어야</b> 하고»).
    /// <para>
    /// ⚠ <b>주인이 값을 안 줬다</b>(T266 ⓑ 그대로) — 표에 든 다섯 줄(29~33)은 주인 <b>레퍼런스 그림</b>에 그려져 있던 것을 옮긴
    /// «표시용» 이고 게임 수치가 아니다. 나머지 줄은 <b>비워 둔다</b>: 화면이 «?» 로 그리게 하고 <b>수를 지어내지 않는다</b>.
    /// 지어내면 주인이 그것을 «정한 값» 으로 읽고, 그 뒤로는 무엇이 주인 것이고 무엇이 워커 것인지 아무도 못 가른다.
    /// </para>
    /// </summary>
    public sealed class PassData
    {
        /// <summary>보상 한 칸 — 아이콘 카탈로그 키와 화면에 적는 수량 글자.</summary>
        public readonly struct Reward
        {
            public readonly string Icon, Qty;
            public Reward(string icon, string qty) { Icon = icon; Qty = qty; }
            public bool Known => !string.IsNullOrEmpty(Icon);
        }

        /// <summary>패스 레벨의 끝 — 주인 «1~100까지».</summary>
        public int MaxLevel = 100;
        /// <summary>레벨 → 세 열(무료·유료 1·유료 2). <b>표에 없는 레벨은 아예 안 들어 있다</b>(빈 칸을 채워 두지 않는다).</summary>
        public readonly Dictionary<int, Reward[]> Levels = new Dictionary<int, Reward[]>();


        /// <summary>열 차례 — 화면의 세 열과 같은 순서다.</summary>
        public const int ColFree = 0, ColPaid1 = 1, ColPaid2 = 2, Cols = 3;
        static readonly string[] ColKey = { "free", "paid1", "paid2" };

        public static PassData Parse(string json) => From(new JNode(MiniJson.Parse(json)));

        public static PassData From(JNode j)
        {
            var d = new PassData { MaxLevel = (int)j["maxLevel"].Num(100) };
            if (d.MaxLevel < 1) throw new FormatException("pass.json: maxLevel 은 1 이상이어야 한다");
            // ⚑ T322 ⛑3 — «startLevel»(세이브가 말이 없을 때 화면이 설 레벨)은 **없애고 다시 못 들어오게 막았다.**
            //   잠깐 32 였다: 표가 비어 있던 동안 화면이 «?» 다섯 줄로 뜨지 않게 둔 자리였고, 무해했던 까닭은
            //   «표가 그 칸을 모르면 못 받는다»(Pass.CanClaim ⓓ) 하나뿐이었다. 주인 값이 100줄을 채우자 그 문이 열렸고
            //   «모두 받기»(T392)가 **갓 시작한 세이브에 다이아 5,600 을 줬다**(실측 · 32칸).
            //   ⇒ «지금 레벨» 은 세이브만 말한다. 표는 «무엇을 주나» 만 말하고 «어디까지 왔나» 는 못 말한다 —
            //     그 둘을 표가 같이 말하면, 표에 한 줄 적는 것으로 재화를 주게 된다.
            if (j.Has("startLevel"))
                throw new FormatException("pass.json: «startLevel» 은 못 쓴다 — 그 레벨까지의 모든 칸이 공짜가 된다(T322 ⛑3). 지금 레벨은 세이브(passLv)가 말한다");
            var lv = j["levels"];
            foreach (var k in lv.Keys)
            {
                if (!int.TryParse(k, out int n)) throw new FormatException("pass.json: levels 의 열쇠 «" + k + "» 가 수가 아니다");
                if (n < 1 || n > d.MaxLevel) throw new FormatException("pass.json: levels 의 레벨 " + n + " 이 1~" + d.MaxLevel + " 밖이다");
                var row = new Reward[Cols];
                for (int c = 0; c < Cols; c++)
                {
                    var e = lv[k][ColKey[c]];
                    row[c] = new Reward(e["icon"].Str(""), e["qty"].Str(""));
                }
                d.Levels[n] = row;
            }
            return d;
        }

        /// <summary>그 레벨 그 열의 보상 — <b>표에 없으면 «모르는 칸»</b>(<see cref="Reward.Known"/> 이 false)이다.</summary>
        public Reward At(int level, int col)
        {
            if (col < 0 || col >= Cols) return default;
            return Levels.TryGetValue(level, out var row) ? row[col] : default;
        }

        /// <summary>표가 값을 아는 레벨인가 — 세 열 중 하나라도 알면 true.</summary>
        public bool Known(int level)
        {
            if (!Levels.TryGetValue(level, out var row)) return false;
            for (int c = 0; c < Cols; c++) if (row[c].Known) return true;
            return false;
        }
    }
}
