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
        /// true — использовать in-memory мок БД (без MySQL).
        /// false — использовать реальную MySQL.
        /// </summary>
        public const bool UseMockDatabase = true;

        // Запустить одиночный тестовый
        public const bool IsTestBattle = true;
    }
}
