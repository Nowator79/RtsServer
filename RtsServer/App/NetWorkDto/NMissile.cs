using RtsServer.App.Battle.Dto;

namespace RtsServer.App.NetWorkDto
{
    public struct NMissile
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public Vector2Float Position { get; set; }
        
        //public double Rotation { get; set; }

        public NMissile(int Id, string Code, Vector2Float Position)
        {
            this.Id = Id;
            this.Code = Code;
            this.Position = Position;
            //this.Rotation = Rotation;
        }
    }
}
