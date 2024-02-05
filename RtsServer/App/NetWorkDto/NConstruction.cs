using RtsServer.App.Buttle.Dto;

namespace RtsServer.App.NetWorkDto
{
    public struct NConstruction
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public float Health { get; set; }
        public Vector2Int Position { get; set; }

        public NConstruction(int Id, string Code, float Health, Vector2Int Position)
        {
            this.Id = Id;
            this.Code = Code;
            this.Health = Health;
            this.Position = Position;
        }
    }
}
