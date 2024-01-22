using RtsServer.Classes.Buttle.Dto;
using RtsServer.Classes.Buttle.Map.ChunksType;
using RtsServer.Classes.NetWork.Tcp;
using RtsServer.Classes.Processor.Response;

namespace RtsServer.Classes.Processor.Game
{
    public class SetChunkProcessor : ProcessorBase
    {
        public override void Handler(MainResponse response, GameServer context, UserClientTcp clientTcp)
        {
            Console.WriteLine(response.action);
            Buttle.Game game = context.ButtleManager.Games.First();
            Vector2Int position = response.GetBody<Vector2Int>();
            Console.WriteLine(response.body);
            game.Map.Chunks[position.X * game.Map.Size + position.Y] = game.Map.ChunkPool.chunks[ChunkWoter.NameChunk];
        }
    }
}
