using System;
using System.Collections.Generic;

namespace KkomaKnight.Core
{
    /// <summary>
    /// 장비 «레시피» 표 (<c>Assets/KkomaKnight/recipe.json</c> · T290 · 주인 2026-09-09 05:5X
    /// «걍 그 장비들 <b>강화하려면 레시피도 필요하게</b> 하기. <b>부위마다 레시피가 있게 해</b>(투구 레시피, 무기 레시피 이런 거).
    /// 그래서 <b>레벨 1 강화 때는 해당 레시피 2개 + 골드</b>면 됨. 그래서 <b>강화할 때마다 필요량 2개씩 늘어남</b>»).
    /// <para>
    /// 여기서 «강화» 는 이 게임의 <b>슬롯 강화</b>(부위 슬롯 Lv · <c>GearUi</c> 의 주황 버튼)다 —
    /// 장비의 <c>+N</c> 은 합성으로만 오르는 값이라 골드로 사는 자리가 아니다.
    /// </para>
    /// 값·개수는 전부 파일에서 온다 — 코드 상수 없음(부위 목록도 <c>gear.json parts</c> 와 같아야 하고 자가 그것을 잰다).
    /// </summary>
    public sealed class RecipeData
    {
        /// <summary>Lv L → L+1 에 드는 개수 = <c>PerLevel × (L+1)</c>. <b>0 이면 레시피가 아예 안 든다</b>(옛 그대로 골드만 · T290 4항).</summary>
        public int PerLevel;
        /// <summary>레시피가 있는 부위 — <c>gear.json parts</c> 와 <b>같은 여섯</b>이어야 한다.</summary>
        public string[] Parts = Array.Empty<string>();
        /// <summary>부위 → 화면에 띄우는 이름(«투구 레시피»). 주인이 든 예 그대로다.</summary>
        public readonly Dictionary<string, string> Name = new Dictionary<string, string>();

        public static RecipeData Parse(string json) => From(new JNode(MiniJson.Parse(json)));

        public static RecipeData From(JNode j)
        {
            var d = new RecipeData { PerLevel = j["perLevel"].Int(), Parts = j.Req("parts").StrArray() };
            foreach (var k in j["name"].Keys) d.Name[k] = j["name"][k].Str("");
            // 음수면 «강화할수록 레시피가 생긴다» 가 되고, 부위가 비면 어떤 강화도 이 표를 못 찾는다.
            if (d.PerLevel < 0) throw new FormatException("recipe.json: perLevel 은 0 이상이어야 한다");
            if (d.Parts.Length == 0) throw new FormatException("recipe.json: parts 가 비었다");
            return d;
        }

        public bool Has(string part) => Array.IndexOf(Parts, part) >= 0;
    }

    /// <summary>
    /// 레시피 규칙 (T290 · 순수 C# · 저장은 <see cref="SaveData.Recipes"/> 한 표 · <see cref="GachaKeys"/> 와 같은 꼴).
    /// <list type="bullet">
    /// <item><b>아이템 이름은 <c>recipe.&lt;부위&gt;</c></b>(<c>recipe.helm</c> …) — 보상 표·우편이 이 글자로 준다(<see cref="Mail.Give"/>).</item>
    /// <item><b>드는 개수는 슬롯 Lv 이 정한다</b> — Lv L 에서 L+1 로 올릴 때 <c>perLevel × (L+1)</c> 개.</item>
    /// <item><b>빼는 곳은 <see cref="GearSystem.SlotUp"/> 한 곳</b> — 골드와 레시피를 <b>같이</b> 확인하고 <b>같이</b> 뺀다.</item>
    /// </list>
    /// ⚠ <b>아이콘(<c>Recipes.Icon</c>)은 아직 없다</b> — 카탈로그에 두루마리·레시피 그림이 0건이라 주인 에셋에서 골라 등재해야 하고,
    /// 그림을 고르기 전에 키 이름만 먼저 적으면 «없는 키» 경고가 뜬다(<c>tools/check_catalog_keys.py</c>). 2회차에서 고른다.
    /// </summary>
    public static class Recipes
    {
        /// <summary>아이템 이름의 밑동 — <c>recipe.helm</c> 처럼 뒤에 부위가 붙는다.</summary>
        public const string Prefix = "recipe.";

        /// <summary>부위 → 아이템 이름(<c>recipe.helm</c>). 빈 부위면 빈 글자.</summary>
        public static string Item(string part) => string.IsNullOrEmpty(part) ? "" : Prefix + part;

        /// <summary>이 이름이 레시피인가 — <b>표를 안 본다</b>(표가 없어도 «이건 레시피다» 는 알아야 우편이 안 흘린다).</summary>
        public static bool IsRecipe(string item) => item != null && item.Length > Prefix.Length && item.StartsWith(Prefix, StringComparison.Ordinal);

        /// <summary>레시피 이름 → 부위(레시피가 아니면 <c>null</c>).</summary>
        public static string PartOf(string item) => IsRecipe(item) ? item.Substring(Prefix.Length) : null;

        /// <summary>가진 개수(모르는 부위·빈 세이브는 0).</summary>
        public static int Count(SaveData s, string part)
        {
            if (s == null || s.Recipes == null || string.IsNullOrEmpty(part)) return 0;
            int v; return s.Recipes.TryGetValue(part, out v) ? (v > 0 ? v : 0) : 0;
        }

        /// <summary>
        /// 지급 — <b>표에 없는 부위도 담는다</b>(<see cref="Achievement.Add"/> 와 같은 뜻 · 표가 늘어날 때 그 전에 받은 것이 사라지지 않게).
        /// 0 이하는 아무 일도 안 한다(보상 표가 오타를 내도 개수가 줄지 않는다 · <see cref="GachaKeys.Add"/> 규약).
        /// </summary>
        public static void Add(SaveData s, string part, int n)
        {
            if (s == null || string.IsNullOrEmpty(part) || n <= 0) return;
            if (s.Recipes == null) s.Recipes = new Dictionary<string, int>();
            long v = (long)Count(s, part) + n;
            s.Recipes[part] = v > int.MaxValue ? int.MaxValue : (int)v;
        }

        /// <summary>빼기 — <b>모자라면 한 개도 안 뺀다</b>(false). 거래가 반만 이뤄지는 자리를 만들지 않는다.</summary>
        public static bool Spend(SaveData s, string part, int n)
        {
            if (n <= 0) return true;
            if (s == null || Count(s, part) < n) return false;
            s.Recipes[part] = Count(s, part) - n;
            return true;
        }

        /// <summary>슬롯 Lv <paramref name="lv"/> → <paramref name="lv"/>+1 에 드는 레시피 수 = <c>perLevel × (lv+1)</c>. 표가 없거나 <c>perLevel 0</c> 이면 0.</summary>
        public static int Need(RecipeData d, int lv)
        {
            if (d == null || d.PerLevel <= 0) return 0;
            if (lv < 0) lv = 0;
            long n = (long)d.PerLevel * (lv + 1);
            return n > int.MaxValue ? int.MaxValue : (int)n;
        }

        /// <summary>지금 그 부위를 한 단계 올릴 <b>레시피</b>가 있는가(골드는 <see cref="GearSystem.SlotUp"/> 이 따로 본다).</summary>
        public static bool CanUp(RecipeData d, SaveData s, string part, int lv) => Count(s, part) >= Need(d, lv);

        /// <summary>화면에 띄우는 이름 — 표에 없으면 «레시피»(부위 이름을 지어내지 않는다).</summary>
        public static string Name(RecipeData d, string part)
        {
            string n;
            if (d != null && part != null && d.Name.TryGetValue(part, out n) && !string.IsNullOrEmpty(n)) return n;
            return "레시피";
        }
    }
}
