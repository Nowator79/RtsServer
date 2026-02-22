using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.MapBattle;
using RtsServer.App.Battle.MapBattle.ChunksType;

namespace RtsServer.App.Battle.Explosion
{
    public class BaseExplosion
    {
        public BaseExplosion(float range, float damage, Vector2Float position, Map map)
        {
            Range = range;
            Damage = damage;
            Position = position;
            _map = map;
        }
        public float Range { get; private set; }
        public float Damage { get; private set; }
        public Vector2Float Position { get; private set; }
        private Map _map { get; set; }
        public void Explode()
        {
            List<ChunkBase> chunks = _map.GetChunksInRadius(Position, Range);

            foreach (var chunk in chunks)
            {
                foreach (var unit in chunk.UnitsInPoint) // TODO fix Modify list
                {
                    float distance = (float)Vector2Float.Distance(unit.Position, Position);
                    if (distance <= Range)
                    {
                        float factor = 1f - (distance / Range); // урон по убыванию
                        float actualDamage = Damage * factor;
                        unit.Health.TakeDamage(actualDamage);
                    }
                }
            }
        }
    }
}
