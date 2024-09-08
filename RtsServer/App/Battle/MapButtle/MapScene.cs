using RtsServer.App.Battle.Constructions;
using RtsServer.App.Battle.MapButlle;
using RtsServer.App.Battle.Units;

namespace RtsServer.App.Battle.MapBattle
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
