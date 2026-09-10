using System;
using System.Collections.Generic;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 시즌 패스 규칙 한 곳 (T322 ⓓ · 순수 C#) — <b>세이브(<see cref="SaveData"/>)와 표(<see cref="PassData"/>) 를 잇는 자리</b>.
    /// <para>
    /// 세이브는 «담는 자리» 이고 표는 «무엇을 주나» 이며, <b>«지금 받을 수 있나» 를 아는 것은 이 클래스 하나다</b> —
    /// 화면·자·나중에 설 지급 길목이 전부 여기에 물어본다(<see cref="Pets"/>·<see cref="ChapterChest"/> 와 같은 꼴).
    /// </para>
    /// <para>
    /// ⚠ <b>보상 값은 아직 주인 몫이다</b>(T266 ⓑ · T377 표) — 그래서 «준다» 는 여기 없다.
    /// 이 절이 세운 것은 <b>어디까지 왔고 무엇을 이미 받았는가</b> 뿐이고, 표가 그 칸을 모르면(<see cref="PassData.Reward.Known"/> 이 거짓)
    /// <see cref="CanClaim"/> 이 <b>거짓</b>이다 — 값이 없는데 «받기» 가 눌리면 «눌렀는데 아무 일도 안 나는 버튼» 이 된다(T257 · 결정 768).
    /// </para>
    /// </summary>
    public static class Pass
    {
        /// <summary>열 → 비트(<c>무료 1 · 유료1 2 · 유료2 4</c>). 세이브가 «레벨 → 이 비트들의 합» 을 담는다.</summary>
        public static int Bit(int col) => col < 0 || col >= PassData.Cols ? 0 : 1 << col;

        /// <summary>
        /// 지금 레벨 — <b>세이브만 말한다</b>(<see cref="SaveData.PassLv"/> 이 0 이면 1). <b>표의 상한으로 자른다</b>(세이브에 상한을 안 박는다 · 표가 100 → 50 으로 줄면 저절로 따라간다).
        /// <para>
        /// ⚑ <b>한때 «세이브가 말이 없으면 표가 답한다»(<c>startLevel</c>) 갈래가 있었다 — 걷어냈다</b>(T322 ⛑3).
        /// 표가 비어 있던 동안 화면이 «?» 만 보여 주지 않게 둔 자리였고, 무해했던 까닭은 <see cref="CanClaim"/> 의 넷째 조건 하나뿐이었다.
        /// 주인 값이 100줄을 채우자 그 문이 열렸고 «모두 받기» 가 <b>갓 시작한 세이브에 다이아 5,600 을 줬다</b>(실측 · 32칸).
        /// ⇒ <b>표는 «무엇을 주나» 만 말하고 «어디까지 왔나» 는 못 말한다</b> — 그 둘을 한 곳이 말하면 표에 한 줄 적는 것이 곧 재화 지급이 된다.
        /// </para>
        /// </summary>
        public static int Lv(SaveData s, PassData d)
        {
            int lv = s != null && s.PassLv >= 1 ? s.PassLv : 1;
            int max = d != null ? d.MaxLevel : int.MaxValue;
            return Math.Max(1, Math.Min(max, lv));
        }

        /// <summary>그 열을 살 필요가 있고 샀는가 — 무료 열은 늘 참.</summary>
        public static bool Bought(SaveData s, int col)
        {
            if (s == null) return false;
            if (col == PassData.ColFree) return true;
            if (col == PassData.ColPaid1) return s.PassPaid1;
            if (col == PassData.ColPaid2) return s.PassPaid2;
            return false;
        }

        /// <summary>이미 받은 칸인가.</summary>
        public static bool Claimed(SaveData s, int level, int col)
        {
            int bit = Bit(col);
            return bit != 0 && s != null && s.PassClaimed.TryGetValue(level, out int m) && (m & bit) != 0;
        }

        /// <summary>
        /// 지금 받을 수 있는 칸인가 — 넷이 <b>모두</b> 참이어야 한다:
        /// ⓐ 레벨에 닿았다 ⓑ 그 열을 샀다 ⓒ 아직 안 받았다 ⓓ <b>표가 그 칸을 안다</b>(값이 없으면 못 준다).
        /// </summary>
        public static bool CanClaim(SaveData s, PassData d, int level, int col)
        {
            if (s == null || level < 1 || Bit(col) == 0) return false;
            if (level > Lv(s, d)) return false;
            if (!Bought(s, col)) return false;
            if (Claimed(s, level, col)) return false;
            return d != null && d.At(level, col).Known;
        }

        /// <summary>
        /// 그 칸을 «받았다» 로 적는다 — <b>실제로 적었으면 참</b>. 못 받는 칸이면 <b>아무것도 안 하고 거짓</b>이다.
        /// <para>⚠ 재화를 주지 않는다 — 주는 값이 아직 없다(위 주석). 값이 서면 <b>부르는 쪽</b>이 그 순서를 짓는다.</para>
        /// </summary>
        public static bool Claim(SaveData s, PassData d, int level, int col)
        {
            if (!CanClaim(s, d, level, col)) return false;
            s.PassClaimed.TryGetValue(level, out int m);
            s.PassClaimed[level] = m | Bit(col);
            return true;
        }

        /// <summary>
        /// T392 — 칸의 그림 키 → 재화 이름(<see cref="Mail"/> 아이템). 키에 <c>gem</c> 이 들어 있으면 다이아 · <c>coin</c>/<c>gold</c> 면 골드 · 모르면 null(그 칸은 못 주니 <b>안 받는다</b>).
        /// <para>목록이 아니라 뜻으로 가르는 까닭은 T367 과 같다 — 새 그림이 생겨도 이 줄은 안 바뀐다.</para>
        /// </summary>
        public static string ItemOf(string icon)
        {
            if (string.IsNullOrEmpty(icon)) return null;
            var k = icon.ToLowerInvariant();
            if (k.Contains("gem")) return Mail.ItemGem;
            if (k.Contains("coin") || k.Contains("gold")) return Mail.ItemGold;
            return null;
        }

        /// <summary>
        /// T392(주인 2026-09-10 «패스도 전부 받기 버튼 있게 하라») — 지금 받을 수 있는 칸을 <b>전부</b> 받는다:
        /// 레벨 1~지금 · 열 셋(산 열만) · 아직 안 받은 · 표가 아는 칸을 위에서 아래로 <see cref="Claim"/> 하고 <see cref="Mail.Give"/> 로 준다.
        /// 돌려주는 것 = (그림 키, 수) 목록(빈 목록 = 받은 것 없음). 화면은 이 목록을 리워드 팝업에 넘기면 된다.
        /// <para>⚠ 순수 C# — 저장(디스크)은 부르는 쪽 몫(<see cref="ShopFree"/> 와 같은 규약). 그림 키를 모르거나 수가 수가 아니면 그 칸은 <b>건너뛰고 받은 것으로 적지 않는다</b>(값이 서면 그때 받힌다).</para>
        /// </summary>
        public static System.Collections.Generic.List<(string icon, int qty)> ClaimAll(SaveData s, PassData d)
        {
            var got = new System.Collections.Generic.List<(string icon, int qty)>();
            if (s == null || d == null) return got;
            int lv = Lv(s, d);
            for (int level = 1; level <= lv; level++)
                for (int col = 0; col < PassData.Cols; col++)
                {
                    if (!CanClaim(s, d, level, col)) continue;
                    var r = d.At(level, col);
                    var item = ItemOf(r.Icon);
                    if (item == null) continue;
                    if (!int.TryParse(r.Qty, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int q) || q <= 0) continue;
                    if (!Claim(s, d, level, col)) continue;
                    Mail.Give(s, item, q);
                    got.Add((r.Icon, q));
                }
            return got;
        }

        /// <summary>지금 받을 수 있는 칸이 하나라도 있나(«모두 받기» 버튼·빨간 점이 묻는 것).</summary>
        public static bool AnyClaimable(SaveData s, PassData d)
        {
            if (s == null || d == null) return false;
            int lv = Lv(s, d);
            for (int level = 1; level <= lv; level++)
                for (int col = 0; col < PassData.Cols; col++)
                    if (CanClaim(s, d, level, col)) return true;
            return false;
        }

        /// <summary>
        /// 세이브 정리 — <b>표를 안 보고</b> 할 수 있는 것만 한다(<see cref="SaveData.Normalize"/> 가 부르는 자리라 표가 없다).
        /// 레벨 상한은 <see cref="Lv"/> 가 읽을 때 자른다.
        /// </summary>
        public static void NormalizeSave(SaveData s)
        {
            if (s == null) return;
            if (s.PassLv < 0) s.PassLv = 0;               // 0 = «아직 말 안 했다»(표가 답한다) — 1 로 올리면 그 뜻이 사라진다
            if (s.PassClaimed == null) { s.PassClaimed = new Dictionary<int, int>(); return; }
            var bad = new List<int>();
            int all = 0; for (int c = 0; c < PassData.Cols; c++) all |= Bit(c);
            foreach (var kv in s.PassClaimed)
            {
                int m = kv.Value & all;                       // 모르는 비트는 지운다(열이 셋에서 줄어드는 날 쓰레기가 남지 않게)
                if (kv.Key < 1 || m == 0) bad.Add(kv.Key);
            }
            foreach (var k in bad) s.PassClaimed.Remove(k);
            var fix = new List<KeyValuePair<int, int>>();
            foreach (var kv in s.PassClaimed) if ((kv.Value & ~all) != 0) fix.Add(kv);
            foreach (var kv in fix) s.PassClaimed[kv.Key] = kv.Value & all;
        }
    }
}
