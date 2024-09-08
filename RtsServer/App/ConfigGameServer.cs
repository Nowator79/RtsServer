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
        public const bool IsDebugStartGame = true;
        public const bool IsDebugTimeUpdate = false;
        // Проверка статуса чанков
        public const bool IsDebugChunkStatus = false;
        
        public const bool IsEnabledClearConsole = false;

        public const bool IsTestBattle = true;
    }
}
