using RtsServer.App.Buttle.Dto;

namespace RtsServer.App.NetWorkDto
{
    public struct NUnit
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public float Health { get; set; }
        public Vector2Float Position { get; set; }

        public NUnit(int Id, string Code, float Health, Vector2Float Position)
        {
            this.Id = Id;
            this.Code = Code;
            this.Health = Health;
            this.Position = Position;
        }
    }
}
