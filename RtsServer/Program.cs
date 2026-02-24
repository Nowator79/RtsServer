    using Microsoft.Extensions.Logging;
using RtsServer.App;
using RtsServer.App.DebugDashboard;

// Конфигурация логгера
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});

var logger = loggerFactory.CreateLogger<GameServer>();

try
{
    CancellationToken cancellationToken = CancellationToken.None;
    int port = ConfigGameServer.Port;

    // Создаем и запускаем сервер
    using var server = new GameServer(port, cancellationToken);
    await server.RunAsync();

    if (ConfigGameServer.IsDebugDashboardEnabled)
    {
        var dashboardPort = ConfigGameServer.DebugDashboardPort;
        _ = Task.Run(() => DebugDashboardRunner.RunAsync(server, dashboardPort, CancellationToken.None));
        logger.LogInformation("Дашборд отладки: http://localhost:{Port}", dashboardPort);
    }

    logger.LogInformation("Сервер запущен на порту {Port}. Нажмите Enter для остановки...", port);

    // Ожидаем команду остановки
    await Task.Run(() => Console.ReadLine());

    logger.LogInformation("Останавливаем сервер...");
    await server.StopAsync();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Критическая ошибка в работе сервера");
}
finally
{
    logger.LogInformation("Приложение завершило работу");
    await Task.Delay(1000); // Даем время для финализации логов
}