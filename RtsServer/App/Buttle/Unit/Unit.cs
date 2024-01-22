using RtsServer.App.Buttle.Dto;
using RtsServer.App.Buttle.Navigator;

namespace RtsServer.App.Buttle.Units
{
    public class Unit
    {
        public Unit(string xmlId, Health health, Vector2Float position)
        {
            Code = xmlId;
            Health = health;
            Position = position;
            Navigatior = new GroundUnitNavigator();
            Navigatior.SetUnit(this);
        }

        public void SetGame(Game context)
        {
            Game = context;
            Navigatior.SetMap(Game.Map);
        }

        public void SetId(int Id)
        {
            this.Id = Id;
        }

        /// <summary>
        /// Устанавливает цель и просчитывает маршрут до цели
        /// </summary>
        /// <param name="TargetPosition"></param>
        /// <returns></returns>
        public Unit SetTargetPosition(Vector2Int TargetPosition)
        {

            this.TargetPosition = TargetPosition;
            Navigatior.Start();

            return this;
        }

        /// <summary>
        /// установить новую точку юнита, юниты у нас не могут телпеортироваться
        /// потому метод должен использоваться только в MoveToTarget()
        /// </summary>
        /// <param name="position"></param>
        private void SetNewPosition(Vector2Float position)
        {
            Position = position;
        }

        /// <summary>
        /// установить маршрут до цели, используется только навигатором
        /// </summary>
        /// <param name="routs"></param>
        public void SetRouts(HashSet<Vector2Int> routs)
        {
            PathRout = routs;
        }

        /// <summary>
        /// Метод с каждой новой итерацией продвигается к ближайшему чанку маршрута
        /// </summary>
        public void MoveToTarget()
        {
            if (PathRout != null && PathRout.Count > 0)
            {
                Vector2Int lastTargetPoint = PathRout.Last();
                Vector2Float targetFloat = lastTargetPoint.GetFloat();
                if (Vector2Float.Distance(Position, targetFloat) < 0.05)
                {
                    SetNewPosition(targetFloat);
                    PathRout.Remove(lastTargetPoint);
                }
                Vector2Float newPosition = Position + (targetFloat - Position).Normalize() * Speed * (float)Game.TimeSystem.GetDetlta() * 1000;
                SetNewPosition(newPosition);
            }
        }

        /// Идентификаторы 
        public int Id { get; set; }
        public string Code { get; set; }

        // Состояние 
        public Health Health { get; set; }
        public Vector2Float Position { get; private set; }

        // характеристики
        public float Speed { get; protected set; } = 1;

        // навигация
        public Vector2Int TargetPosition { get; private set; }
        public float Rotation { get; private set; }
        public HashSet<Vector2Int> PathRout { get; private set; }
        private INavigator Navigatior { get; set; }

        // контекст  
        private Game Game { get; set; }
    }
}
