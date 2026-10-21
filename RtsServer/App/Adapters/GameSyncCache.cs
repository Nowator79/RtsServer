using RtsServer.App.Battle;
using RtsServer.App.Battle.Constructions;
using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Units;
using RtsServer.App.NetWorkDto;

namespace RtsServer.App.Adapters
{
    /// <summary>
    /// Хранит последний отправленный стейт и собирает полный снимок или дельту.
    /// Аллоцирует N* только для реально изменившихся сущностей (иначе GC на каждом тике).
    /// </summary>
    public sealed class GameSyncCache
    {
        private readonly Dictionary<int, NUnit> _units = new();
        private readonly Dictionary<int, NConstruction> _constructions = new();
        private readonly Dictionary<int, NMissile> _missiles = new();
        private int _deltasSinceFull;
        private const int ForceFullEvery = 100; // ~5 с при ~20 Гц

        private readonly List<NUnit> _changedUnits = new(128);
        private readonly List<NConstruction> _changedConstructions = new(32);
        private readonly List<NMissile> _changedMissiles = new(64);
        private readonly List<int> _removedUnits = new(32);
        private readonly List<int> _removedConstructions = new(16);
        private readonly List<int> _removedMissiles = new(32);
        private readonly HashSet<int> _liveUnits = new(128);
        private readonly HashSet<int> _liveConstructions = new(32);
        private readonly HashSet<int> _liveMissiles = new(64);

        public void Reset()
        {
            _units.Clear();
            _constructions.Clear();
            _missiles.Clear();
            _deltasSinceFull = 0;
        }

        public NGame Build(Game game, bool forceFull = false)
        {
            bool needFull = forceFull
                || (_units.Count == 0 && _constructions.Count == 0 && _missiles.Count == 0)
                || _deltasSinceFull >= ForceFullEvery;

            if (needFull)
            {
                _deltasSinceFull = 0;
                return BuildFull(game);
            }

            _deltasSinceFull++;
            return BuildDelta(game);
        }

        private NGame BuildFull(Game game)
        {
            _changedUnits.Clear();
            _changedConstructions.Clear();
            _changedMissiles.Clear();
            _units.Clear();
            _constructions.Clear();
            _missiles.Clear();

            foreach (Unit unit in game.Units)
            {
                NUnit n = UnitAdapter.Get(unit);
                _changedUnits.Add(n);
                _units[unit.Id] = n;
            }

            foreach (Construction c in game.Constructions)
            {
                NConstruction n = ConstructionAdapter.Get(c);
                _changedConstructions.Add(n);
                _constructions[c.Id] = n;
            }

            foreach (Missile m in game.Missiles)
            {
                NMissile n = MissileAdapter.Get(m);
                _changedMissiles.Add(n);
                _missiles[m.Id] = n;
            }

            return new NGame(
                new List<NUnit>(_changedUnits),
                new List<NConstruction>(_changedConstructions),
                new List<NMissile>(_changedMissiles))
            {
                IsDelta = false,
                RemovedUnitIds = Array.Empty<int>(),
                RemovedConstructionIds = Array.Empty<int>(),
                RemovedMissileIds = Array.Empty<int>()
            };
        }

        private NGame BuildDelta(Game game)
        {
            _changedUnits.Clear();
            _changedConstructions.Clear();
            _changedMissiles.Clear();
            _removedUnits.Clear();
            _removedConstructions.Clear();
            _removedMissiles.Clear();
            _liveUnits.Clear();
            _liveConstructions.Clear();
            _liveMissiles.Clear();

            foreach (Unit unit in game.Units)
            {
                _liveUnits.Add(unit.Id);
                if (!_units.TryGetValue(unit.Id, out NUnit prev) || !UnitMatches(prev, unit))
                {
                    NUnit n = UnitAdapter.Get(unit);
                    _changedUnits.Add(n);
                    _units[unit.Id] = n;
                }
            }

            foreach (KeyValuePair<int, NUnit> kv in _units)
            {
                if (!_liveUnits.Contains(kv.Key))
                    _removedUnits.Add(kv.Key);
            }
            for (int i = 0; i < _removedUnits.Count; i++)
                _units.Remove(_removedUnits[i]);

            foreach (Construction c in game.Constructions)
            {
                _liveConstructions.Add(c.Id);
                if (!_constructions.TryGetValue(c.Id, out NConstruction prev) || !ConstructionMatches(prev, c))
                {
                    NConstruction n = ConstructionAdapter.Get(c);
                    _changedConstructions.Add(n);
                    _constructions[c.Id] = n;
                }
            }

            foreach (KeyValuePair<int, NConstruction> kv in _constructions)
            {
                if (!_liveConstructions.Contains(kv.Key))
                    _removedConstructions.Add(kv.Key);
            }
            for (int i = 0; i < _removedConstructions.Count; i++)
                _constructions.Remove(_removedConstructions[i]);

            foreach (Missile m in game.Missiles)
            {
                _liveMissiles.Add(m.Id);
                if (!_missiles.TryGetValue(m.Id, out NMissile prev) || !MissileMatches(prev, m))
                {
                    NMissile n = MissileAdapter.Get(m);
                    _changedMissiles.Add(n);
                    _missiles[m.Id] = n;
                }
            }

            foreach (KeyValuePair<int, NMissile> kv in _missiles)
            {
                if (!_liveMissiles.Contains(kv.Key))
                    _removedMissiles.Add(kv.Key);
            }
            for (int i = 0; i < _removedMissiles.Count; i++)
                _missiles.Remove(_removedMissiles[i]);

            return new NGame(
                new List<NUnit>(_changedUnits),
                new List<NConstruction>(_changedConstructions),
                new List<NMissile>(_changedMissiles))
            {
                IsDelta = true,
                RemovedUnitIds = _removedUnits.Count == 0 ? Array.Empty<int>() : _removedUnits.ToArray(),
                RemovedConstructionIds = _removedConstructions.Count == 0 ? Array.Empty<int>() : _removedConstructions.ToArray(),
                RemovedMissileIds = _removedMissiles.Count == 0 ? Array.Empty<int>() : _removedMissiles.ToArray()
            };
        }

        private static bool UnitMatches(NUnit cached, Unit unit)
        {
            if (cached.Id != unit.Id || cached.OwnerId != unit.OwnerId) return false;
            if (!string.Equals(cached.Code, unit.Code, StringComparison.Ordinal)) return false;
            if (Math.Abs(cached.Health - unit.Health.Value) > 0.5f) return false;
            if (Math.Abs(cached.MaxHealth - unit.Health.Max) > 0.5f) return false;

            float height = unit is AirUnit ? (float)unit.GetBodyHeight() : 0f;
            if (Math.Abs(cached.Height - height) > 0.04f) return false;
            if (Math.Abs(cached.Rotation - unit.Rotation) > 0.4) return false;
            if (!PosEquals(cached.Position, unit.Position, 0.04)) return false;
            if (Math.Abs(cached.Info.CurSpeed - unit.CurrentSpeed) > 0.05) return false;
            if (Math.Abs(cached.Info.MaxSpeed - unit.MaxSpeed) > 0.05) return false;

            int len = unit.AttackingPoints?.Length ?? 0;
            int cachedLen = cached.AttackPoints?.Length ?? 0;
            if (len != cachedLen) return false;
            for (int i = 0; i < len; i++)
            {
                if (Math.Abs(cached.AttackPoints![i].Rotation - unit.AttackingPoints![i].Rotation) > 0.5f)
                    return false;
                if (cached.AttackPoints[i].ShotSerial != unit.AttackingPoints[i].ShotSerial)
                    return false;
            }
            return true;
        }

        private static bool ConstructionMatches(NConstruction cached, Construction c)
        {
            if (cached.Id != c.Id || cached.OwnerId != c.OwnerId || cached.IsPowered != c.IsPowered)
                return false;
            if (!string.Equals(cached.Code, c.Code, StringComparison.Ordinal)) return false;

            Vector2Int pos = (Vector2Int)c.Position;
            if (cached.Position.X != pos.X || cached.Position.Y != pos.Y) return false;
            if (Math.Abs(cached.Health - c.Health.Value) > 0.5f) return false;
            if (Math.Abs(cached.MaxHealth - c.Health.Max) > 0.5f) return false;
            if (Math.Abs(cached.BuildProgress - c.BuildProgress) > 0.01f) return false;

            float productionProgress = 0f;
            string productionUnitCode = "";
            string productionQueue = "";
            if (c is MilitaryFactoryConstruction military)
            {
                productionProgress = military.ProductionProgress;
                productionUnitCode = military.CurrentProductionCode ?? "";
                productionQueue = military.ProductionQueueCsv;
            }
            else if (c is AirFactoryConstruction air)
            {
                productionProgress = air.ProductionProgress;
                productionUnitCode = air.CurrentProductionCode ?? "";
                productionQueue = air.ProductionQueueCsv;
            }
            else if (c is BarracksConstruction barracks)
            {
                productionProgress = barracks.ProductionProgress;
                productionUnitCode = barracks.CurrentProductionCode ?? "";
                productionQueue = barracks.ProductionQueueCsv;
            }

            if (Math.Abs(cached.ProductionProgress - productionProgress) > 0.02f) return false;
            if (!string.Equals(cached.ProductionUnitCode ?? "", productionUnitCode, StringComparison.Ordinal))
                return false;
            if (!string.Equals(cached.ProductionQueue ?? "", productionQueue, StringComparison.Ordinal))
                return false;
            return true;
        }

        private static bool MissileMatches(NMissile cached, Missile missile)
        {
            if (cached.Id != missile.Id) return false;
            if (!string.Equals(cached.Code, missile.Code, StringComparison.Ordinal)) return false;
            if (Math.Abs(cached.Height - (float)missile.Height) > 0.05f) return false;
            if (!PosEquals(cached.Position, missile.Position, 0.05)) return false;

            double horizSpeed = Math.Sqrt(missile.Velocity.X * missile.Velocity.X + missile.Velocity.Y * missile.Velocity.Y);
            float yaw = (float)(Math.Atan2(missile.Velocity.X, missile.Velocity.Y) * (180.0 / Math.PI));
            float pitch = horizSpeed > 1e-4
                ? (float)(Math.Atan2(missile.Velocity.Z, horizSpeed) * (180.0 / Math.PI))
                : (missile.Velocity.Z >= 0 ? 90f : -90f);

            if (Math.Abs(cached.RotationYaw - yaw) > 1f) return false;
            if (Math.Abs(cached.RotationPitch - pitch) > 1f) return false;
            return true;
        }

        private static bool PosEquals(Vector2Float a, Vector2Float b, double eps) =>
            Math.Abs(a.X - b.X) <= eps && Math.Abs(a.Y - b.Y) <= eps;
    }
}
