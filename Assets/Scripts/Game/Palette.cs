using KkomaKnight.Core;
using UnityEngine;

namespace KkomaKnight.Game
{
    /// <summary>
    /// 색 — 전부 GUI Pro-MinimalGame(Theme_Light) 팔레트(카탈로그 col.*)에서 고른다(주인 결정 2026-09-05: «등급 색은 이 테마 색 중에서»).
    /// 등급·세트 → 테마 색 이름(green/blue/yellow/plum/…) 매핑도 여기 한 곳에 둔다 — 프리팹 변형(CardFrame_04_Green 등)을 고를 때 같은 이름을 쓴다.
    /// </summary>
    public static class Palette
    {
        public static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
        static Color Cat(string key, string fallback) => App.I != null && App.I.Assets != null ? App.I.Assets.Color("col." + key, Hex(fallback)) : Hex(fallback);

        // 테마 팔레트 (Button_01 Bg 오버라이드 값 · 폴백은 같은 값)
        public static Color Gray => Cat("gray", "#A39B9D");
        public static Color Green => Cat("green", "#85D048");
        public static Color Blue => Cat("blue", "#5BB0F0");
        public static Color Sky => Cat("sky", "#35A6E1");
        public static Color Yellow => Cat("yellow", "#FFCC00");
        public static Color Orange => Cat("orange", "#FF8612");
        public static Color Plum => Cat("plum", "#C76EF7");
        public static Color Red => Cat("red", "#FB5951");
        public static Color Brown => Cat("brown", "#B97A54");
        public static Color Mint => Cat("mint", "#03E4B7");
        public static Color Ink => Cat("ink", "#341B19");          // 진한 코코아(제목)
        public static Color InkSoft => Cat("inkSoft", "#633B37");  // 본문 갈색
        public static Color InkLight => Cat("inkLight", "#8B5C45");
        public static Color Cream => Cat("cream", "#F5E9D0");
        public static Color CreamDark => Cat("creamDark", "#E3CDAA");
        public static Color Dim => Cat("dim", "#12131A");
        public static Color Slate => Cat("slate", "#415760");
        public static readonly Color White = Color.white;
        public static readonly Color Bg = Hex("#141418");          // 프레임 밖(letterbox) — 카메라 배경과 같은 어두운 색

        // 데미지 팝 (ui.json fx.popShield/popHp 는 GameData 에서 읽는다 — 여기는 그 외)
        public static Color PopCrit => Yellow;
        public static Color PopHeal => Green;
        public static Color PopGold => Yellow;
        public static Color PopMiss => Gray;
        public static Color PopEvade => Sky;

        /// <summary>특전 등급(perks.json grade 0·1·2) → 테마 색 이름. 악마 카드는 plum.</summary>
        public static string PerkGradeName(int grade) => grade >= 2 ? "yellow" : grade == 1 ? "blue" : "green";
        public const string DevilName = "plum";
        /// <summary>장비 등급(gear.json rar 0~3) → 테마 색 이름.</summary>
        public static string RarName(int rar) => rar >= 3 ? "plum" : rar == 2 ? "yellow" : rar == 1 ? "blue" : "gray";
        /// <summary>장비 세트(crit/hpsh/evade) → 테마 색 이름.</summary>
        public static string SetName(string set) => set == "crit" ? "red" : set == "hpsh" ? "green" : "sky";
        public static Color ByName(string name)
        {
            switch (name)
            {
                case "green": return Green; case "blue": return Blue; case "sky": return Sky; case "yellow": return Yellow; case "orange": return Orange;
                case "plum": return Plum; case "red": return Red; case "brown": return Brown; case "mint": return Mint; default: return Gray;
            }
        }
        public static Color PerkColor(PerkDef p) => ByName(PerkGradeName(p.Grade));
        public static Color A(Color c, float a) { c.a = a; return c; }
    }

    /// <summary>아이콘 키 고르기 — 스탯·특전·장비. 카탈로그 키(pi.* / ui.* / gi.*) 만 돌려준다.</summary>
    public static class Icons
    {
        public static string Stat(string k)
        {
            switch (k)
            {
                case "dmg": return "pi.attack"; case "def": return "pi.defense"; case "aspd": return "pi.atk_spd"; case "counter": return "pi.fist";
                case "critR": return "pi.critical"; case "evade": return "ui.dodge"; case "critF": return "pi.damage"; case "steal": return "pi.drop";
                case "hp": return "pi.heart"; case "sh": return "pi.shield"; case "exp": return "pi.star"; default: return "pi.star";
            }
        }
        /// <summary>특전 id 의 어근으로 그림을 고른다 (같은 계열 N/R/L 은 같은 그림 · 등급은 색으로 구분).</summary>
        public static string Perk(string id)
        {
            string s = id.StartsWith("p_") ? id.Substring(2) : id;
            if (s.StartsWith("noble")) return "pi.crown";
            if (s.StartsWith("berserk")) return "pi.fire";
            if (s.StartsWith("giant")) return "pi.power";
            if (s.StartsWith("overkill")) return "pi.heart_round";
            if (s.StartsWith("spearAvatar")) return "pi.dagger";
            if (s.StartsWith("coll")) return "pi.star";
            if (s.StartsWith("cleave")) return "pi.dagger2";
            if (s.StartsWith("ignore") || s.StartsWith("shWall")) return "pi.block";
            if (s.StartsWith("shRef")) return "pi.swirl";
            if (s.StartsWith("thorns")) return "pi.damage";
            if (s.StartsWith("ward") || s.StartsWith("repair") || s.StartsWith("healRepair") || s.StartsWith("killRepair") || s.StartsWith("evRepair")) return "pi.shield";
            if (s.StartsWith("killDash")) return "pi.move_spd";
            if (s.StartsWith("killAtkStk") || s.StartsWith("killEvStk") || s.StartsWith("critStack")) return "pi.growth";
            if (s.StartsWith("exec")) return "pi.skull";
            if (s.StartsWith("stun")) return "pi.stun";
            if (s.IndexOf("Bolt") >= 0) return "pi.thunder";
            if (s.IndexOf("Axe") >= 0 || s.StartsWith("axe")) return "pi.axe";
            if (s.IndexOf("Arrow") >= 0 || s.StartsWith("arrow")) return "pi.arrowhead";
            if (s.IndexOf("Spear") >= 0 || s.StartsWith("spear")) return "pi.dagger";
            if (s.IndexOf("Heal") >= 0 || s.StartsWith("heal")) return "pi.heart";
            if (s.StartsWith("ct") || s.StartsWith("counter")) return "pi.fist";
            if (s.StartsWith("crit") || s.StartsWith("fullHp") || s.StartsWith("killSureCrit")) return "pi.critical";
            if (s.StartsWith("def")) return "pi.defense";
            if (s.StartsWith("aspd") || s.StartsWith("noShAspd")) return "pi.atk_spd";
            if (s.StartsWith("evade") || s.StartsWith("ev") || s.StartsWith("killEv")) return "ui.dodge";
            if (s.StartsWith("atk") || s.StartsWith("noShAtk")) return "pi.attack";
            if (s.StartsWith("nHeal")) return "pi.heart";
            return "pi.star";
        }
        public static string Gear(string part, string set) => "gi." + part + "." + set;
    }
}
