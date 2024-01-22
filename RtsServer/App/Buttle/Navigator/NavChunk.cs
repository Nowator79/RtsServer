using RtsServer.App.Buttle.Dto;
using System.Diagnostics.Metrics;

namespace RtsServer.App.Buttle.Navigator
{
    public struct NavChunk
    {
        public NavChunk(Vector2Int position, int height, int id)
        {
            Position = position;
            Height = height;
            Id = id;
        }

        public Vector2Int Position { get; private set; }
        public float TargetRange { get; set; }
        public int StepsCount { get; set; }
        public int Height { get; set; }
        public int Id { get; set; }

       
    }
}
