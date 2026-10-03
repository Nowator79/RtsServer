using System.Reflection;
using RtsServer.App.Battle.Dto;

namespace RtsServer.App.Battle.Constructions
{
    public class ConstructionFactory
    {
        private const string ConstructionsNamespace = "RtsServer.App.Battle.Constructions";

        /// <summary>Цена постройки — статическое поле BuildCost у класса постройки.</summary>
        public static int GetBuildCost(string code)
        {
            Type? type = ResolveType(code);
            if (type == null) return 0;
            FieldInfo? buildCost = type.GetField("BuildCost", BindingFlags.Public | BindingFlags.Static);
            if (buildCost?.GetValue(null) is int cost) return cost;
            return 0;
        }

        /// <summary>Размер здания в клетках (sizeX/sizeY). Если полей нет — 1×1.</summary>
        public static Vector2Int GetSize(string code)
        {
            Type? type = ResolveType(code);
            if (type == null) return new Vector2Int(1, 1);

            int sx = 1;
            int sy = 1;
            if (type.GetField("sizeX", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) is int x)
                sx = x;
            if (type.GetField("sizeY", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) is int y)
                sy = y;
            return new Vector2Int(sx, sy);
        }

        /// <summary>Время строительства в секундах (поле BuildTime). 0 — мгновенно.</summary>
        public static float GetBuildTime(string code)
        {
            Type? type = ResolveType(code);
            if (type == null) return 0f;
            FieldInfo? buildTime = type.GetField("BuildTime", BindingFlags.Public | BindingFlags.Static);
            if (buildTime?.GetValue(null) is float seconds) return seconds;
            if (buildTime?.GetValue(null) is int intSeconds) return intSeconds;
            return 0f;
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
