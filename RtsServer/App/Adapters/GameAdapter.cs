using RtsServer.App.Buttle;
using RtsServer.App.NetWorkDto;

namespace RtsServer.App.Adapters
{
    public static class GameAdapter
    {
        public static NGame Get(Game game)
        {
            List<NUser> NUsers = new();
            List<NUnit> NUnits = new();
            List<NConstruction> NConstructions = new();

            foreach (Buttle.Units.Unit unit in game.Units)
            {
                NUnits.Add(UnitAdapter.Get(unit));
            }

            foreach (Buttle.Constructions.Construction construction in game.Constructions)
            {
                NConstructions.Add(ConstructionAdapter.Get(construction));
            }

            return new NGame(NUnits, NConstructions);
        }
    }
}
