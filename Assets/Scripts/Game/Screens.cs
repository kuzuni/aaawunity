namespace KkomaKnight.Game
{
    // 화면 뼈대 — 3단계(BattleScreen) · 4단계(Lobby/Gear/Forge/Shop) 에서 채운다.
    public sealed class LobbyScreen : Screen { public override string Name => "lobby"; protected override void Build() { } }
    public sealed class GearScreen : Screen { public override string Name => "gear"; protected override void Build() { } }
    public sealed class ForgeScreen : Screen { public override string Name => "forge"; protected override void Build() { } }
    public sealed class ShopScreen : Screen { public override string Name => "shop"; protected override void Build() { } }
    public sealed class BattleScreen : Screen { public override string Name => "battle"; protected override void Build() { } }
}
