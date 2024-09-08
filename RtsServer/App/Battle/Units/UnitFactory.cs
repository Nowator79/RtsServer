using RtsServer.App.Battle.Constructions;
using RtsServer.App.Battle.Dto;

namespace RtsServer.App.Battle.Units
{
    public class UnitFactory
    {
        public static Unit GetByCode(string code, Vector2Int position, int player)
        {
            Type? TestType = Type.GetType($"RtsServer.App.Battle.Units.{code}");

            //если класс не найден
            if (TestType != null)
            {
                //получаем конструктор
                Type[] types = new Type[] { typeof(Vector2Int), typeof(int) };
                System.Reflection.ConstructorInfo? ci = TestType.GetConstructor(types);
                if (ci == null) throw new Exception("Не найден конструктор");

                Unit? construction = (Unit)ci.Invoke([position, player]);
                if (construction == null) throw new Exception("Проблемы с объектом");

                return construction;
            }
            throw new Exception("Не найден конструктор");
        }
    }
}
