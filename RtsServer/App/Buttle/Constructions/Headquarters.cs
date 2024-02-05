using RtsServer.App.Buttle.Dto;
namespace RtsServer.App.Buttle.Constructions
{
    public class Headquarters : Construction
    {
        public const int sizeX = 4;
        public const int sizeY = 4;
        public const int maxHelth = 1000;

        public Headquarters(Vector2Int position) : base(new Health(maxHelth, maxHelth), position, new(sizeX, sizeY), "Headquarters") { }
    }
}
