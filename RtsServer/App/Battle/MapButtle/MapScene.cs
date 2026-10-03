using RtsServer.App.Battle.Constructions;
using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Units;

namespace RtsServer.App.Battle.MapBattle
{
    public class MapScene
    {
        public const int DefaultStartingResources = 1000;

        public Map Map { get; private set; }
        public Construction[] ConstructionAdditionalsForMap { get; set; }
        public Unit[] Units { get; set; }

        /// <summary>Стартовые ресурсы каждому игроку.</summary>
        public int StartingResources { get; set; } = DefaultStartingResources;

        /// <summary>Стартовая позиция камеры по Id игрока (PlayerOwnerNum).</summary>
        public Dictionary<int, Vector2Int> CameraStartsByPlayer { get; set; } = new();

        public MapScene(Map map, Construction[] constructions, Unit[] units)
        {
            Map = map;
            ConstructionAdditionalsForMap = constructions;
            Units = units;
        }

        public Vector2Int GetCameraStart(int playerId)
        {
            if (CameraStartsByPlayer != null && CameraStartsByPlayer.TryGetValue(playerId, out Vector2Int pos))
                return pos;
            return new Vector2Int(0, 0);
        }
    }
}
