using Microsoft.Extensions.Logging;
using RtsServer.App;

// Конфигурация логгера
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});

var logger = loggerFactory.CreateLogger<GameServer>();

try
{
    const int port = 7912;
    CancellationToken cancellationToken = new CancellationToken();  
    // Создаем и запускаем сервер
    using var server = new GameServer(port, cancellationToken);
    await server.RunAsync();

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