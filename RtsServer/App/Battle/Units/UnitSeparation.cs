using RtsServer.App.Battle.Dto;

namespace RtsServer.App.Battle.Units
{
    /// <summary>
    /// Круги юнитов отталкивают друг друга. Земля и воздух — отдельные слои
    /// (вертолёты не толкают танки и наоборот).
    /// </summary>
    public static class UnitSeparation
    {
        public static void Resolve(Game game)
        {
            if (game?.Units == null || game.Units.Count < 2)
                return;

            ResolveLayer(Collect(game, groundLayer: true));
            ResolveLayer(Collect(game, groundLayer: false));
        }

        private static List<Unit> Collect(Game game, bool groundLayer)
        {
            List<Unit> bodies = new(game.Units.Count);
            foreach (Unit unit in game.Units)
            {
                if (unit == null || unit.IsDestroyed || unit.CollisionRadius <= 0)
                    continue;

                bool isGround = unit.OccupiesGroundCell;
                if (groundLayer)
                {
                    if (!isGround)
                        continue;
                }
                else
                {
                    // Воздушный слой: летающие с радиусом (вертолёты).
                    if (isGround || !unit.FliesOverObstacles)
                        continue;
                }

                bodies.Add(unit);
            }

            return bodies;
        }

        private static void ResolveLayer(List<Unit> bodies)
        {
            if (bodies.Count < 2)
                return;

            for (int iter = 0; iter < 3; iter++)
            {
                for (int i = 0; i < bodies.Count; i++)
                {
                    Unit a = bodies[i];
                    if (a.IsDestroyed)
                        continue;

                    for (int j = i + 1; j < bodies.Count; j++)
                    {
                        Unit b = bodies[j];
                        if (b.IsDestroyed)
                            continue;
                        PushApart(a, b);
                    }
                }
            }
        }

        private static void PushApart(Unit a, Unit b)
        {
            double dx = a.Position.X - b.Position.X;
            double dy = a.Position.Y - b.Position.Y;
            double distSq = dx * dx + dy * dy;
            double minDist = a.CollisionRadius + b.CollisionRadius;
            if (distSq >= minDist * minDist)
                return;

            double dist = Math.Sqrt(distSq);
            Vector2Float dir;
            if (dist < 1e-4)
            {
                dir = new Vector2Float(a.Id <= b.Id ? 1 : -1, 0);
                dist = 0;
            }
            else
            {
                dir = new Vector2Float(dx / dist, dy / dist);
            }

            double overlap = minDist - dist;
            Vector2Float a0 = a.Position;
            Vector2Float b0 = b.Position;
            bool aMoved = a.TryMoveBody(a0 + dir * (overlap * 0.5));
            bool bMoved = b.TryMoveBody(b0 - dir * (overlap * 0.5));

            if (aMoved && !bMoved)
                a.TryMoveBody(a0 + dir * overlap);
            else if (!aMoved && bMoved)
                b.TryMoveBody(b0 - dir * overlap);
        }
    }
}
