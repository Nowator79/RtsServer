using RtsServer.App.Buttle.Dto;

namespace RtsServer.App.NetWorkDto
{
    public struct NUnitPathNav
    {
        public NUnitPathNav(int Id, HashSet<Vector2Int> Path)
        {
            this.Id = Id;
            this.Path = Path;
        }

        public int Id { get; set; }
        public HashSet<Vector2Int> Path { get; set; }
    }
}
