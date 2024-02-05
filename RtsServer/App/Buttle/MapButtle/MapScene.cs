using RtsServer.App.Buttle.Constructions;
using RtsServer.App.Buttle.MapButlle;
using RtsServer.App.Buttle.Units;

namespace RtsServer.App.Buttle.MapButtle
{
    public class MapScene
    {
        public Map Map { get; private set; }
        public Construction[] ConstructionAdditionalsForMap  { get; set; }
        public Unit[] Units { get; set; }

        public MapScene(Map Map, Construction[] ConstructionAdditionalsForMap, Unit[] Units)
        {
            this.Map = Map;
            this.ConstructionAdditionalsForMap = ConstructionAdditionalsForMap;
            this.ConstructionAdditionalsForMap = ConstructionAdditionalsForMap;
            this.Units = Units;
        }
    }
}
