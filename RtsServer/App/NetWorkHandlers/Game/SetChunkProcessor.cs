using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkHandlers.Game
{
    public class SetChunkProcessor : IProcessor
    {
        public void Handler(MainResponse response, GameServer context, UserClientTcp clientTcp)
        {
            App.Battle.Game game = context.BattleManager.Games.First();
            SetChunkBody setChunkBody = response.GetBody<SetChunkBody>();
            game.Map.Chunks[setChunkBody.Position.X * game.Map.Width + setChunkBody.Position.Y].Id = setChunkBody.ChunkId;
        }
    }
}
