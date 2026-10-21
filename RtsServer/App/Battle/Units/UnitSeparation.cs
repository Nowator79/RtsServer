using RtsServer.App.Battle.Dto;

namespace RtsServer.App.Battle.Units
{
    /// <summary>
    /// Круги юнитов отталкивают друг друга. Сдвиг, который упирается в воду,
    /// обрыв, скалу или постройку, не применяется.
    /// </summary>
    public static class UnitSeparation
    {
        public static void Resolve(Game game)
        {
            if (game?.Units == null || game.Units.Count < 2)
                return;

            List<Unit> bodies = new(game.Units.Count);
            foreach (Unit unit in game.Units)
            {
                if (unit == null || unit.IsDestroyed || !unit.OccupiesGroundCell || unit.CollisionRadius <= 0)
                    continue;
                bodies.Add(unit);
            }

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
