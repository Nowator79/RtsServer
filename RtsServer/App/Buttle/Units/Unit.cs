using RtsServer.App.Buttle.Dto;
using RtsServer.App.Buttle.Navigator;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto;
using RtsServer.App.NetWorkResponseSender;

namespace RtsServer.App.Buttle.Units
{
    public class Unit
    {
        /// идентификаторы 
        public int Id { get; set; }
        public string Code { get; set; }

        // состояние здоровья
        public Health Health { get; set; }
        // текущая позиция
        public Vector2Float Position { get; protected set; }
        // текущая позиция в чанке, нужна для проверки изменения позиции
        public Vector2Int CurChunkPosition { get; protected set; }

        // характеристики
        public double Speed { get; protected set; } = 1;
        public double RotationSpeed { get; protected set; }

        // навигация    
        public Vector2Int TargetPosition { get; protected set; }
        public double Rotation { get; protected set; }
        public HashSet<Vector2Int> PathRout { get; private set; }
        protected INavigator Navigatior { get; set; }
        protected Action BeforeUpdateChunkPosition { get; set; }
        protected Action AfterUpdateChunkPosition { get; set; }
        // контекст  
        private Game Game { get; set; }

        protected const int KRotationSpeed = 1;
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
            AfterUpdateChunkPosition += () => {
              
            };
            BeforeUpdateChunkPosition += () => {
                
            };
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
            try
            {
                Navigatior.Start();
            }
            catch (Exception e)
            {
                //Console.WriteLine(e);
            }
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

            foreach (Player player in Game.Players)
            {
                UserClientTcp? userTcp = Game.ButtleManager.GameServer.TcpServer.GetClientByUserAuth(player.UserAuth);

                if (userTcp != null)
                {
                    new UnitPathNavSender(userTcp).SetDate(new NUnitPathNav(Id, routs)).SendMessage();
                }
            }

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
                    Vector2Float newPosition = Position + (targetFloat - Position).Normalize() * Speed * dTime / 100;
                    if (Vector2Float.DistanceSQRT(Position, newPosition) >= Vector2Float.DistanceSQRT(Position, targetFloat))
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
            double upAngle = RotationSpeed * KRotationSpeed * dTime;
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

            if (AngleToTarget % 180 < 5 || double.IsNaN(0 / AngleToTarget))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        
        private void CheckChunk()
        {
            if(CurChunkPosition != Position.ToInt())
            {
                MapButlle.ChunksType.ChunkBase tmp = Game.Map.GetArrayMap()[CurChunkPosition.X, CurChunkPosition.Y];
                tmp.UnitsInPoint.Remove(this);

                CurChunkPosition = Position.ToInt();

                MapButlle.ChunksType.ChunkBase tmp2 = Game.Map.GetArrayMap()[CurChunkPosition.X, CurChunkPosition.Y];
                tmp2.UnitsInPoint.Add(this);

            }
        }

        public void Update()
        {
            MoveToTarget();
            CheckChunk();
        }
    }
}
