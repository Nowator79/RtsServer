using RtsServer.App.Buttle.Dto;
using RtsServer.App.Buttle.Navigator;

namespace RtsServer.App.Buttle.Units
{
    public class Unit
    {
        protected const int KRotationSpeed = 50;
        public Unit(string xmlId, Health health, Vector2Float position)
        {
            Code = xmlId;
            Health = health;
            Position = position;
            Navigatior = new GroundUnitNavigator();
            Navigatior.SetUnit(this);
            Rotation = 0;
        }

        public Unit(string xmlId, Health health, Vector2Int position)
        {
            Code = xmlId;
            Health = health;
            Position = position.GetFloat();
            Navigatior = new GroundUnitNavigator();
            Navigatior.SetUnit(this);
            Rotation = 0;
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

                // проверяем последнию точку навигации, если достгли ее то
                // уравниваем позицию цели к точке навигации и удаляем точку навигации
                if (Vector2Float.Distance(Position, targetFloat) < 0.05)
                {
                    SetNewPosition(targetFloat);
                    PathRout.Remove(lastTargetPoint);
                    return;
                }

                // Поворачиваемся до цели если поворот правильный идем к цели
                if (RotationToTarget(targetFloat))
                {
                    double dTime = Game.TimeSystem.GetDetlta();
                    Vector2Float newPosition = Position + (targetFloat - Position).Normalize() * Speed * dTime;
                    if (Vector2Float.Distance(Position, newPosition) >= Vector2Float.Distance(Position, targetFloat))
                    {
                        newPosition = targetFloat;
                    }
                    SetNewPosition(newPosition);
                }

            }
        }

        public bool RotationToTarget(Vector2Float Target)
        {
            Vector2Float GFacting = Vector2Float.VectorByVectorAndAngle(Position, -Rotation);// глобальная точка
            Vector2Float GFactingC = Vector2Float.VectorByAngle(-Rotation); // локальная точка
            double AngleToTarget = Vector2Float.AngleByVectorsAndRot(Position, GFactingC.Normalize(), Target);
            double typeAngle = Vector2Float.SideByVector(Position, GFacting, Target);

            double dTime = Game.TimeSystem.GetDetlta();
            double upAngle = RotationSpeed *  KRotationSpeed * dTime;
            double newAngle = Rotation;
            if (upAngle > AngleToTarget) upAngle = AngleToTarget;
            if (typeAngle > 0)
            {
                newAngle = Rotation + upAngle;
            }
            else if (typeAngle < 0)
            {
                newAngle = Rotation - upAngle;
            }

            if (Math.Abs(newAngle - Rotation) > 30)
            {
                Console.WriteLine("ERROR");
            }
            Rotation = newAngle;

            if (AngleToTarget%180 < 5 || double.IsNaN(0 / AngleToTarget))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// Идентификаторы 
        public int Id { get; set; }
        public string Code { get; set; }

        // Состояние 
        public Health Health { get; set; }
        public Vector2Float Position { get; protected set; }

        // характеристики
        public double Speed { get; protected set; } = 1;
        public double RotationSpeed { get; protected set; }

        // навигация
        public Vector2Int TargetPosition { get; protected set; }
        public double Rotation { get; protected set; }
        public HashSet<Vector2Int> PathRout { get; private set; }
        protected INavigator Navigatior { get; set; }

        // контекст  
        private Game Game { get; set; }
    }
}
