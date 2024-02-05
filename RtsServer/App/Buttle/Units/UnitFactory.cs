using RtsServer.App.Buttle.Constructions;
using RtsServer.App.Buttle.Dto;

namespace RtsServer.App.Buttle.Units
{
    public class UnitFactory
    {
        public static Unit GetByCode(string code, Vector2Int position)
        {
            Type? TestType = Type.GetType($"RtsServer.App.Buttle.Units.{code}");

            //если класс не найден
            if (TestType != null)
            {
                //получаем конструктор
                Type[] types = new Type[] { typeof(Vector2Int) };
                System.Reflection.ConstructorInfo? ci = TestType.GetConstructor(types);
                if (ci == null) throw new Exception("Не найден конструктор");

                Unit? construction = (Unit)ci.Invoke(new object[] { position });
                if (construction == null) throw new Exception("Проблемы с обьектом");

                return construction;
            }
            throw new Exception("Не найден конструктор");
        }
    }
}
