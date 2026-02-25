using System.Reflection;
using RtsServer.App.Battle.Dto;

namespace RtsServer.App.Battle.Constructions
{
    public class ConstructionFactory
    {
        private const string ConstructionsNamespace = "RtsServer.App.Battle.Constructions";

        /// <summary>Цена постройки — статическое поле BuildCost у класса постройки. Общая для всех зданий одного типа.</summary>
        public static int GetBuildCost(string code)
        {
            Type? type = ResolveType(code);
            if (type == null) return 0;
            FieldInfo? buildCost = type.GetField("BuildCost", BindingFlags.Public | BindingFlags.Static);
            if (buildCost?.GetValue(null) is int cost) return cost;
            return 0;
        }

        public static Construction GetByCode(string code, Vector2Int position, int player)
        {
            Type? testType = ResolveType(code);
            if (testType == null)
                throw new Exception($"Не найден конструктор для кода: {code}");

            Type[] argTypes = { typeof(Vector2Int), typeof(int) };
            ConstructorInfo? ci = testType.GetConstructor(argTypes);
            if (ci == null) throw new Exception("Не найден конструктор");

            var construction = (Construction?)ci.Invoke(new object[] { position, player });
            if (construction == null) throw new Exception("Проблемы с объектом");

            return construction;
        }

        private static Type? ResolveType(string code)
        {
            Type? type = Type.GetType($"{ConstructionsNamespace}.{code}");
            if (type != null) return type;
            // код может быть "Headquarters", а класс — HeadquartersConstruction: ищем по статическому полю Code
            foreach (Type t in typeof(Construction).Assembly.GetTypes())
            {
                if (!t.IsClass || t.IsAbstract || !t.IsSubclassOf(typeof(Construction))) continue;
                if (t.GetField("Code", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) is string codeField && codeField == code)
                    return t;
            }
            return null;
        }
    }
}
