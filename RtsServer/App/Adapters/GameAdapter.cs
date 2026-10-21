using RtsServer.App.Battle;
using RtsServer.App.NetWorkDto;

namespace RtsServer.App.Adapters
{
    public static class GameAdapter
    {
        public static NGame Get(Game game) => Get(game, syncCache: null, forceFull: true);

        public static NGame Get(Game game, GameSyncCache? syncCache, bool forceFull = false)
        {
            if (syncCache != null)
                return syncCache.Build(game, forceFull);

            List<NUnit> nUnits = new();
            List<NConstruction> nConstructions = new();
            List<NMissile> nMissiles = new();

            foreach (Battle.Units.Unit unit in game.Units)
                nUnits.Add(UnitAdapter.Get(unit));

            foreach (Battle.Constructions.Construction construction in game.Constructions)
                nConstructions.Add(ConstructionAdapter.Get(construction));

            foreach (Battle.Units.Missile missile in game.Missiles)
                nMissiles.Add(MissileAdapter.Get(missile));

            return new NGame(nUnits, nConstructions, nMissiles)
            {
                IsDelta = false,
                RemovedUnitIds = Array.Empty<int>(),
                RemovedConstructionIds = Array.Empty<int>(),
                RemovedMissileIds = Array.Empty<int>()
            };
        }
    }
}
