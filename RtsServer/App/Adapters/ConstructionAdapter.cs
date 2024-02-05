using RtsServer.App.Buttle.Constructions;
using RtsServer.App.NetWorkDto;

namespace RtsServer.App.Adapters
{
    public class ConstructionAdapter
    {
        public static NConstruction Get(Construction construction)
        {
            return new NConstruction(construction.Id, construction.Code, construction.Health.Value, construction.Position);
        }
    }
}
