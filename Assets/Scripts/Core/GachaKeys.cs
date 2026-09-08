using System;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 뽑기 <b>«키» 재화</b>(T255 · 주인 2026-09-09 «희귀 상자는 파란색 키로 1회 뽑기 가능. 보라색 키로는 전설 상자 1회 가능.
    /// 노랑 키로는 신화 상자 1회 가능. 펫알 1개로 펫 1회 뽑기 가능»).
    /// <para>
    /// <b>키는 «비용 수단» 만 바꾼다</b>(지시서 2항) — 확률·천장·결과는 다이아로 연 것과 <b>완전히 같다</b>.
    /// 그래서 이 절에는 뽑는 코드가 한 줄도 없다: <see cref="GearSystem.GachaPull"/> 은 그대로 두고 <b>값을 치르는 자리</b>만 여기로 온다.
    /// </para>
    /// <para>
    /// <b>색 → 상자</b>는 주인이 직접 말한 짝이다(파랑 = 희귀 · 보라 = 전설 · 노랑 = 신화). 열쇠의 <b>이름</b>은 아레나 상인 표가
    /// 이미 쓰는 것을 그대로 쓴다(«희귀 열쇠»·«에픽 열쇠»·«전설 열쇠» · <c>EventsScreen.Goods</c>) — 두 곳이 다른 말을 쓰면 안 된다(T254 머리말).
    /// 즉 <b>이름은 열쇠의 계열</b>이고 <b>여는 상자는 주인이 정한 짝</b>이라 둘이 한 칸씩 어긋나 보이는데, 그것이 주인이 준 규칙이다.
    /// </para>
    /// <para>
    /// <b>펫알</b>(<see cref="SaveData.PetEgg"/>)은 여기 <b>재화로만</b> 든다 — «펫 1회 뽑기» 는 아직 못 만든다(<c>PetScreen</c> 이
    /// «시스템이 생기면» 이라 적힌 껍데기라 뽑을 표도 엔진도 없다). 지어내지 않고(§1) 자리만 세워 둔다.
    /// </para>
    /// <para>저장(디스크 쓰기)은 부르는 쪽(게임 층) 몫이다 — 이 절은 순수 C# 이다(<see cref="Revive.Use"/>·<see cref="ArenaTickets.Spend"/> 와 같은 규약).</para>
    /// </summary>
    public static class GachaKeys
    {
        /// <summary>재화 이름 — 세이브 필드·우편 보상(<see cref="Mail.CanPay"/>)이 같은 글자를 쓴다.</summary>
        public const string Blue = "keyBlue", Purple = "keyPurple", Yellow = "keyYellow", Egg = Mail.ItemPetEgg;

        /// <summary><c>gacha.json</c> 의 상자 키.</summary>
        public const string BoxRare = "rare", BoxLegend = "legend", BoxMyth = "myth";

        /// <summary>이 이름이 «키» 인가(펫알은 키가 아니다 — 상자를 열지 않는다).</summary>
        public static bool IsKey(string item) => item == Blue || item == Purple || item == Yellow;

        /// <summary>키 → 그 키가 여는 상자(키가 아니면 <c>null</c>).</summary>
        public static string BoxOf(string key)
        {
            if (key == Blue) return BoxRare;
            if (key == Purple) return BoxLegend;
            if (key == Yellow) return BoxMyth;
            return null;
        }

        /// <summary>상자 → 그 상자를 여는 키(그런 키가 없는 상자면 <c>null</c> — 그때는 다이아로만 연다).</summary>
        public static string KeyOf(string boxKey)
        {
            if (boxKey == BoxRare) return Blue;
            if (boxKey == BoxLegend) return Purple;
            if (boxKey == BoxMyth) return Yellow;
            return null;
        }

        /// <summary>가진 개수(모르는 이름이면 0).</summary>
        public static double Count(SaveData s, string item)
        {
            if (s == null) return 0;
            if (item == Blue) return s.KeyBlue;
            if (item == Purple) return s.KeyPurple;
            if (item == Yellow) return s.KeyYellow;
            if (item == Egg) return s.PetEgg;
            return 0;
        }

        /// <summary>지급 — 음수·모르는 이름은 아무 일도 안 한다(보상 표가 오타를 내도 재화가 줄지 않는다).</summary>
        public static void Add(SaveData s, string item, double amount)
        {
            if (s == null || amount <= 0) return;
            if (item == Blue) s.KeyBlue += (int)Math.Round(amount);
            else if (item == Purple) s.KeyPurple += (int)Math.Round(amount);
            else if (item == Yellow) s.KeyYellow += (int)Math.Round(amount);
            else if (item == Egg) s.PetEgg += amount;
        }

        /// <summary>이 상자를 <b>키로</b> <paramref name="n"/> 회 열 수 있는가 — 그런 키가 없는 상자면 언제나 false.</summary>
        public static bool CanOpen(SaveData s, string boxKey, int n = 1)
        {
            if (s == null || n <= 0) return false;
            var key = KeyOf(boxKey);
            return key != null && Count(s, key) >= n;
        }

        /// <summary>
        /// 키 <paramref name="n"/> 개를 치르고 «열어도 좋다» 를 돌려준다 — 뽑는 것은 부르는 쪽이 <b>지금까지 쓰던 그 경로</b>로 한다.
        /// 모자라거나 그런 키가 없는 상자면 <b>한 개도 안 깎고</b> false.
        /// </summary>
        public static bool Open(SaveData s, string boxKey, int n = 1)
        {
            if (!CanOpen(s, boxKey, n)) return false;
            var key = KeyOf(boxKey);
            if (key == Blue) s.KeyBlue -= n;
            else if (key == Purple) s.KeyPurple -= n;
            else if (key == Yellow) s.KeyYellow -= n;
            return true;
        }

        /// <summary>재화 이름 → 아이콘 키 — <b>이미 있는 그림</b>이다(아레나 상인 표가 쓰는 것 · 새 그림 0).</summary>
        public static string Icon(string item)
        {
            if (item == Blue) return "ui.iconKeyBlue";
            if (item == Purple) return "ui.iconKeyPurple";
            if (item == Yellow) return "ui.iconKeyGold";
            if (item == Egg) return "pet.egg";
            return null;
        }

        /// <summary>재화 이름 → 우리말(아레나 상인 표와 같은 말 · 화면 문구는 한 곳에서 만든다).</summary>
        public static string Name(string item)
        {
            if (item == Blue) return "희귀 열쇠";
            if (item == Purple) return "에픽 열쇠";
            if (item == Yellow) return "전설 열쇠";
            if (item == Egg) return "펫알";
            return item ?? "";
        }
    }
}
