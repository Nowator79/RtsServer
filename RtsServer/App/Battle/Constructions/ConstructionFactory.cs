using RtsServer.App.Battle.Dto;

namespace RtsServer.App.Battle.Constructions
{
    public class ConstructionFactory
    {

        public static Construction GetByCode(string code, Vector2Int position)
        {
            Type? TestType = Type.GetType($"RtsServer.App.Battle.Constructions.{code}");

            //если класс не найден
            if (TestType != null)
            {
                //получаем конструктор
                Type[] types = new Type[] { typeof(Vector2Int) };
                System.Reflection.ConstructorInfo? ci = TestType.GetConstructor(types);
                if (ci == null) throw new Exception("Не найден конструктор");

                Construction? construction = (Construction)ci.Invoke(new object[] { position });
                if (construction == null) throw new Exception("Проблемы с обьектом");

                return construction;
            }
            throw new Exception("Не найден конструктор");
        }
    }
}
