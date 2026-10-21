namespace RtsServer.App
{
    public static class ConfigGameServer
    {
        // Показывать лог отправленных и полученных пакетов
        public const bool IsDebugNetWork = false;
        // Показывать таблицу игроков
        public const bool IsDebugGameUpdate = false;
        // Показывать логи расчета навигации
        public const bool IsDebugGameNavUpdate = false;
        public const bool IsDebugGameUsersInfoUpdate = false;
        public const bool IsDebugTimeUpdate = false;
        // Проверка статуса чанков
        public const bool IsDebugChunkStatus = false;

        // Порт TCP-сервера
        public const int Port = 7912;

        /// <summary>
        /// Включить веб-дашборд отладки (состояние игр, игроки, очереди).
        /// Открыть в браузере: http://localhost:{DebugDashboardPort}
        /// </summary>
        public const bool IsDebugDashboardEnabled = true;
        public const int DebugDashboardPort = 5050;

        public const bool IsEnabledClearConsole = false;

        /// <summary>
        /// true — in-memory мок. false — MySQL (см. MySqlConnectionString).
        /// Можно переопределить переменной окружения RTS_USE_MOCK_DB=1|0.
        /// </summary>
        public static bool UseMockDatabase =>
            GetEnvBool("RTS_USE_MOCK_DB", defaultValue: false);

        /// <summary>
        /// Строка подключения MySQL. Переопределение: RTS_MYSQL.
        /// </summary>
        public static string MySqlConnectionString =>
            Environment.GetEnvironmentVariable("RTS_MYSQL")
            ?? "server=localhost;user=root;password=root;database=rts_server;";

        // Запустить одиночный тестовый
        public const bool IsTestBattle = true;

        private static bool GetEnvBool(string name, bool defaultValue)
        {
            string? raw = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(raw))
                return defaultValue;
            return raw is "1" or "true" or "TRUE" or "yes" or "YES";
        }
    }
}
