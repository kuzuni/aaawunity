namespace KkomaKnight.Game
{
    /// <summary>
    /// Hand-authored layouts for the two dungeon-only art sets.  Unlike
    /// <see cref="MapLayouts"/>, this file is not generated from the Layer Lab demo
    /// scenes.  Coordinates are fractions of the battle frame so replacing the
    /// source PNG resolution/PPU cannot move gameplay objects.
    /// </summary>
    public static class DungeonMapLayouts
    {
        public readonly struct Prop
        {
            public readonly int Art; public readonly float X, Y, Width, Height; public readonly bool Flip;
            public Prop(int art, float x, float y, float width, float height, bool flip = false)
            { Art = art; X = x; Y = y; Width = width; Height = height; Flip = flip; }
        }

        public const float Period = 1.15f;
        public static readonly Prop[] Expedition =
        {
            new Prop(1, 0.08f, 0.25f, 0.16f, 0.18f),
            new Prop(2, 0.43f, 0.57f, 0.12f, 0.12f, true),
            new Prop(3, 0.78f, 0.20f, 0.18f, 0.22f),
        };
        public static readonly Prop[] Hell =
        {
            new Prop(1, 0.10f, 0.23f, 0.18f, 0.21f),
            new Prop(2, 0.48f, 0.58f, 0.13f, 0.13f),
            new Prop(3, 0.82f, 0.19f, 0.17f, 0.23f, true),
        };

        public static Prop[] Of(string dungeonKey) => dungeonKey == "expedition" ? Expedition : dungeonKey == "hell" ? Hell : null;
        public static string Key(string dungeonKey, int art) => "env." + dungeonKey + ".prop" + art;
    }
}
