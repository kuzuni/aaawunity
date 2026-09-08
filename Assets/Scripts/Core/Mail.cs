using System.Collections.Generic;

namespace KkomaKnight.Core
{
    /// <summary>우편 한 통 — 지금은 <b>아레나 보상만</b> 들어온다(T243 · 주인 2026-09-08 11:5X «우편함은 아레나 보상만»).</summary>
    public sealed class MailItem
    {
        /// <summary>같은 우편이 두 번 쌓이지 않게 하는 이름(예: 시즌 1 의 순위 보상 = <c>arenaRank:s1</c>).</summary>
        public string Id = "";
        /// <summary>어떤 갈래인가 — <see cref="Mail.KindArena"/> 뿐이다(다른 갈래는 <see cref="Mail.Add"/> 가 받지 않는다).</summary>
        public string Kind = Mail.KindArena;
        public string Title = "";
        public string Desc = "";
        /// <summary>받을 것들 — <c>arena.json</c> 의 보상 칸과 같은 꼴(<c>item</c> + <c>amount</c>)이다.</summary>
        public readonly List<ArenaRankData.Reward> Rewards = new List<ArenaRankData.Reward>();
    }

    /// <summary>
    /// 우편함의 <b>내용물</b>(T243 · 순수 C# · 화면은 <c>Game.Mailbox</c>).
    /// <para>
    /// 주인 2026-09-08 11:5X — «우편함으로는 <b>아레나 보상만</b> 오게 하고 나머지는 걍 <b>즉시 지급</b>해. … 우편함은 아레나 보상만.»
    /// 그래서 이 함에 넣을 수 있는 갈래는 <see cref="KindArena"/> <b>하나뿐</b>이고, <see cref="Add"/> 가 그것을 <b>거절로</b> 지킨다 —
    /// 규칙을 주석이 아니라 코드가 들고 있어야 다음 사람이 «탐험 보상도 우편으로» 를 되살릴 수 없다.
    /// </para>
    /// <b>넣을 때 지급할 수 있는지 먼저 본다</b>(<see cref="CanPay"/>) — 세이브에 담을 자리가 없는 것을 우편함에 넣으면
    /// «받기를 눌러도 아무 일이 없는 우편» 이 생긴다. 그건 사람이 고장으로 읽고, 우리는 그 사이 보상을 잃는다(T228 결정 633 과 같은 갈래).
    /// </summary>
    public static class Mail
    {
        /// <summary>우편함에 들어올 수 있는 유일한 갈래.</summary>
        public const string KindArena = "arena";

        /// <summary>담을 자리가 있는 보상 이름(세이브 필드가 실제로 있는 것만).</summary>
        public const string ItemGold = "gold", ItemGem = "gem", ItemPetEgg = "petEgg", ItemArenaCoin = "arenaCoin";

        /// <summary>이 보상을 세이브에 담을 수 있는가 — 모르는 이름은 <b>우편함에 안 들어간다</b>(조용히 버리지 않는다).</summary>
        public static bool CanPay(string item)
            => item == ItemGold || item == ItemGem || item == ItemPetEgg || item == ItemArenaCoin || GachaKeys.IsKey(item);   // 키 3종 = T255

        /// <summary>우편함에 든 것(없으면 빈 목록 · 옛 세이브 호환).</summary>
        public static List<MailItem> Pending(SaveData s)
        {
            if (s == null) return new List<MailItem>();
            if (s.Mail == null) s.Mail = new List<MailItem>();
            return s.Mail;
        }

        /// <summary>받을 우편이 하나라도 있는가(≡ 알림 점이 이것만 본다 · T243 4항).</summary>
        public static bool Any(SaveData s) => Pending(s).Count > 0;

        /// <summary>
        /// 우편 한 통을 넣는다 — <b>아레나 갈래</b>이고 · <b>id 가 있고 처음 보는 것</b>이고 · <b>보상을 전부 담을 수 있어야</b> 들어간다.
        /// 하나라도 어긋나면 <c>false</c> 이고 <b>아무것도 안 바뀐다</b>.
        /// </summary>
        public static bool Add(SaveData s, MailItem m)
        {
            if (s == null || m == null) return false;
            if (m.Kind != KindArena) return false;                       // 주인 «우편함은 아레나 보상만»
            if (string.IsNullOrEmpty(m.Id)) return false;
            var list = Pending(s);
            foreach (var x in list) if (x.Id == m.Id) return false;      // 같은 우편 두 번 금지
            if (m.Rewards.Count == 0) return false;                      // 빈 우편은 넣지 않는다
            foreach (var r in m.Rewards)
                if (!CanPay(r.Item) || r.Amount <= 0) return false;      // 담을 자리가 없는 것은 애초에 안 받는다
            list.Add(m);
            return true;
        }

        /// <summary>우편 하나를 받는다 — 보상을 세이브에 더하고 목록에서 지운다. 받은 것을 한 줄로 돌려준다(없으면 null).</summary>
        public static string Claim(SaveData s, string id)
        {
            var list = Pending(s);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Id != id) continue;
                var m = list[i];
                foreach (var r in m.Rewards) Pay(s, r.Item, r.Amount);
                list.RemoveAt(i);
                return Summary(m);
            }
            return null;
        }

        /// <summary>받은 것 한 줄(«골드 1,000 · 다이아 30») — 화면이 토스트로 그대로 쓴다.</summary>
        public static string Summary(MailItem m)
        {
            if (m == null) return "";
            string s = "";
            foreach (var r in m.Rewards)
            {
                if (s.Length > 0) s += " · ";
                s += Name(r.Item) + " " + Num(r.Amount);
            }
            return s;
        }

        /// <summary>보상 이름 → 우리말(화면 문구는 한 곳에서 만든다).</summary>
        public static string Name(string item)
        {
            if (item == ItemGold) return "골드";
            if (item == ItemGem) return "다이아";
            if (item == ItemPetEgg) return "펫알";
            if (item == ItemArenaCoin) return "아레나 코인";
            if (GachaKeys.IsKey(item)) return GachaKeys.Name(item);   // T255 — 말은 GachaKeys 한 곳이 갖는다
            return item ?? "";
        }

        static string Num(double v) => System.Math.Round(v).ToString("#,0");

        static void Pay(SaveData s, string item, double amount)
        {
            if (s == null || amount <= 0) return;
            if (item == ItemGold) s.Gold += amount;
            else if (item == ItemGem) s.Gem += amount;
            else if (item == ItemPetEgg) s.PetEgg += amount;
            else if (item == ItemArenaCoin) s.ArenaCoin += amount;
            else if (GachaKeys.IsKey(item)) GachaKeys.Add(s, item, amount);   // T255 — 담는 자리도 GachaKeys 가 안다
            // 그 밖의 이름은 Add 가 이미 막았다 — 여기까지 오지 않는다.
        }
    }
}
