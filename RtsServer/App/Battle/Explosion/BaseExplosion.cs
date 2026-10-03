using RtsServer.App.Battle.Constructions;
using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.MapBattle;
using RtsServer.App.Battle.MapBattle.ChunksType;
using RtsServer.App.Battle.Units;

namespace RtsServer.App.Battle.Explosion
{
    public class BaseExplosion
    {
        public BaseExplosion(
            float range,
            float damage,
            Vector2Float position,
            Map map,
            IEnumerable<Construction>? constructions = null,
            IEnumerable<Unit>? units = null,
            int ownerId = -1,
            int? ignoreUnitId = null,
            ArmorDamageProfile? armorDamage = null)
        {
            Range = range;
            Damage = damage;
            Position = position;
            _map = map;
            _constructions = constructions;
            _units = units;
            _ownerId = ownerId;
            _ignoreUnitId = ignoreUnitId;
            _armorDamage = armorDamage ?? ArmorDamageProfile.Zero;
        }

        public float Range { get; private set; }
        public float Damage { get; private set; }
        public Vector2Float Position { get; private set; }
        private Map _map { get; set; }
        private readonly IEnumerable<Construction>? _constructions;
        private readonly IEnumerable<Unit>? _units;
        private readonly int _ownerId;
        private readonly int? _ignoreUnitId;
        private readonly ArmorDamageProfile _armorDamage;

        public void Explode()
        {
            var damaged = new HashSet<Unit>();

            List<ChunkBase> chunks = _map.GetChunksInRadius(Position, Range);
            foreach (var chunk in chunks)
            {
                foreach (var unit in chunk.UnitsInPoint.ToList())
                {
                    if (unit == null || unit.IsDestroyed || !damaged.Add(unit)) continue;
                    if (IsFriendlyOrShooter(unit)) continue;
                    ApplyDamage(unit.Position, unit.Health, unit.Armor, radiusBonus: 0f);
                }
            }

            // Запасной путь: самолёты / юниты вне UnitsInPoint всё равно получают урон по дистанции.
            if (_units != null)
            {
                foreach (Unit unit in _units)
                {
                    if (unit == null || unit.IsDestroyed || !damaged.Add(unit)) continue;
                    if (IsFriendlyOrShooter(unit)) continue;
                    ApplyDamage(unit.Position, unit.Health, unit.Armor, radiusBonus: 0f);
                }
            }

            if (_constructions == null) return;

            foreach (Construction construction in _constructions)
            {
                if (construction == null || construction.IsDestroyed) continue;
                // Свои здания не повреждаем сплэшем.
                if (_ownerId >= 0 && construction.OwnerId == _ownerId) continue;

                float distance = DistanceToConstructionFootprint(construction, Position);
                float buildingBonus = 0.4f;
                float effectiveRange = Range + buildingBonus;
                if (distance > effectiveRange) continue;

                float factor = 1f - (distance / effectiveRange);
                float actualDamage = _armorDamage.For(construction.Armor) * Math.Max(0f, factor);
                if (actualDamage > 0f)
                    construction.Health.TakeDamage(actualDamage);
            }
        }

        private bool IsFriendlyOrShooter(Unit unit)
        {
            if (_ignoreUnitId.HasValue && unit.Id == _ignoreUnitId.Value)
                return true;
            if (_ownerId >= 0 && unit.OwnerId == _ownerId)
                return true;
            return false;
        }

        /// <summary>Дистанция до ближайшей точки габарита здания (0 — внутри footprint).</summary>
        private static float DistanceToConstructionFootprint(Construction construction, Vector2Float point)
        {
            Vector2Int origin = construction.Position.ToInt();
            int sx = Math.Max(1, construction.Size.X);
            int sy = Math.Max(1, construction.Size.Y);

            double closestX = Math.Clamp(point.X, origin.X, origin.X + sx);
            double closestY = Math.Clamp(point.Y, origin.Y, origin.Y + sy);
            double dx = point.X - closestX;
            double dy = point.Y - closestY;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private void ApplyDamage(Vector2Float targetPos, Health health, ArmorType armor, float radiusBonus)
        {
            float distance = (float)Vector2Float.Distance(targetPos, Position);
            float effectiveRange = Range + radiusBonus;
            if (distance > effectiveRange) return;

            float factor = 1f - (distance / effectiveRange);
            float actualDamage = _armorDamage.For(armor) * Math.Max(0f, factor);
            if (actualDamage > 0f)
                health.TakeDamage(actualDamage);
        }
    }
}
