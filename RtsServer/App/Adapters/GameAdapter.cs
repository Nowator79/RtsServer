using RtsServer.App.Battle;
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

            foreach (Battle.Units.Unit unit in game.Units)
            {
                NUnits.Add(UnitAdapter.Get(unit));
            }

            foreach (Battle.Constructions.Construction construction in game.Constructions)
            {
                NConstructions.Add(ConstructionAdapter.Get(construction));
            }

            return new NGame(NUnits, NConstructions);
        }
    }
}
