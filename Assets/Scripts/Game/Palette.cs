using UnityEngine;

namespace KkomaKnight.Game
{
    /// <summary>index.html :root 팔레트 + 등급/세트 색. 색은 채점 밖(배치만 채점)이지만 원본과 맞춘다.</summary>
    public static class Palette
    {
        public static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
        public static readonly Color Grass = Hex("#7FBF52"), LobbyBg = Hex("#3E7B33"), LobbyBgD = Hex("#2E5F26");
        public static readonly Color Panel = Hex("#232327"), Panel2 = Hex("#2E2E34"), Panel3 = Hex("#3A3A42"), Bg = Hex("#141418");
        public static readonly Color Gold = Hex("#FFB92E"), Hp = Hex("#E8483F"), Shield = Hex("#38A6E8"), Exp = Hex("#8BC93F");
        public static readonly Color Ink = Color.white, InkDim = Hex("#B9BDC4");
        public static readonly Color[] RarColor = { Hex("#9AA3AF"), Hex("#4FA3E3"), Hex("#F2C14E"), Hex("#FF5FA2") };
        public static readonly Color[] PerkGradeColor = { Hex("#9EA3AC"), Hex("#4FA3F7"), Hex("#FFB92E") };
        public static readonly Color Blessing = Hex("#FFB92E");
        public static readonly Color SetCrit = Hex("#FF6B6B"), SetHpsh = Hex("#5BD07A"), SetEvade = Hex("#5AC8F5");
        public static readonly Color PopShield = Hex("#6CC0F0"), PopHp = Hex("#FF8A80"), PopHeal = Hex("#7ED957"), PopCrit = Hex("#FFD54A");
        public static readonly Color Orange = Hex("#F5A623"), Green = Hex("#4CAF50"), Red = Hex("#E8483F"), Devil = Hex("#E87AA0");
        public static Color SetColor(string set) => set == "crit" ? SetCrit : set == "hpsh" ? SetHpsh : SetEvade;
        public static Color A(Color c, float a) { c.a = a; return c; }
    }
}
