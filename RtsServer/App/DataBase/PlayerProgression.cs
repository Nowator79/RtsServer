using RtsServer.App.DataBase.Dto;

namespace RtsServer.App.DataBase
{
    /// <summary>
    /// Прогрессия: до следующего уровня нужно 2000 * 2^(level-1) опыта.
    /// L1→L2 = 2000, L2→L3 = 4000, L3→L4 = 8000, ...
    /// </summary>
    public static class PlayerProgression
    {
        public const int XpWin = 500;
        public const int XpLoss = 100;
        /// <summary>Серебро (Credits) за победу.</summary>
        public const int CreditsWin = 5000;
        public const int BaseXpToLevel2 = 2000;
        private const int MaxLevel = 30;

        public static int NormalizeLevel(int level) => level < 1 ? 1 : level;

        /// <summary>Сколько опыта нужно на текущем уровне, чтобы подняться на +1.</summary>
        public static int XpRequiredForNextLevel(int currentLevel)
        {
            int level = NormalizeLevel(currentLevel);
            if (level >= MaxLevel)
                return int.MaxValue;

            // 2000 * 2^(level-1)
            int shift = level - 1;
            if (shift >= 30)
                return int.MaxValue;
            return BaseXpToLevel2 << shift;
        }

        public static int XpForResult(string result)
        {
            return string.Equals(result, "Victory", StringComparison.OrdinalIgnoreCase)
                ? XpWin
                : XpLoss;
        }

        public static int CreditsForResult(string result)
        {
            return string.Equals(result, "Victory", StringComparison.OrdinalIgnoreCase)
                ? CreditsWin
                : 0;
        }

        /// <summary>Добавляет опыт и пересчитывает Level. Experience — прогресс внутри текущего уровня.</summary>
        public static void AddExperience(UserAuth user, int amount)
        {
            if (user == null || amount <= 0)
                return;

            user.Level = NormalizeLevel(user.Level);
            if (user.Experience < 0)
                user.Experience = 0;

            long xp = user.Experience + (long)amount;
            while (user.Level < MaxLevel)
            {
                int need = XpRequiredForNextLevel(user.Level);
                if (xp < need)
                    break;
                xp -= need;
                user.Level++;
            }

            user.Experience = xp > int.MaxValue ? int.MaxValue : (int)xp;
        }
    }
}
