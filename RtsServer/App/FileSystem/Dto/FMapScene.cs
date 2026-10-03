using RtsServer.App.Battle.Dto;

namespace RtsServer.App.FileSystem.Dto
{
    public struct FMapScene
    {
        public string MapCode { get; set; }
        public FConstruction[] Constructions { get; set; }
        public FUnit[] Units { get; set; }

        /// <summary>Стартовые ресурсы каждому игроку при старте матча.</summary>
        public int StartingResources { get; set; }

        /// <summary>Стартовые позиции камеры по игрокам (PlayerOwnerNum).</summary>
        public FPlayerStart[] PlayerStarts { get; set; }
    }
}
