using RtsServer.App.Battle.Dto;

namespace RtsServer.App.FileSystem.Dto
{
    /// <summary>Стартовые параметры игрока на сцене (камера и т.п.).</summary>
    public struct FPlayerStart
    {
        public int PlayerOwnerNum { get; set; }
        public Vector2Int CameraPosition { get; set; }
    }
}
