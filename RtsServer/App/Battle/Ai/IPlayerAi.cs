namespace RtsServer.App.Battle.Ai
{
    /// <summary>
    /// Контроллер ИИ, который можно повесить на <see cref="Player.Ai"/>.
    /// Разные реализации = разные поведения (экспансия, защита, раш…).
    /// </summary>
    public interface IPlayerAi
    {
        void Tick(Game game, Player player, double deltaSeconds);
    }
}
