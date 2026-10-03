using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RtsServer.App.DataBase.Dto;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWork.Tcp
{
    public sealed class UserClientTcp : IDisposable
    {
        private readonly ILogger<UserClientTcp> _logger;
        private readonly INetWorkServer _server;
        private readonly CancellationTokenSource _cts = new();
        private readonly SemaphoreSlim _writeLock = new(1, 1);
        private bool _disposed;

        public string Id { get; }
        public TcpClient TcpClient { get; }
        public NetworkStream Stream { get; }
        public UserAuth? User { get; private set; }
        public long LastPing { get; private set; }
        public int CountWrite { get; private set; }
        public int CountRead { get; private set; }

        public UserClientTcp(TcpClient tcpClient, INetWorkServer netWorkServer)
        {
            TcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
            Stream = tcpClient.GetStream();
            _server = netWorkServer ?? throw new ArgumentNullException(nameof(netWorkServer));

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            _logger = loggerFactory.CreateLogger<UserClientTcp>();

            Id = tcpClient.Client.RemoteEndPoint?.ToString() ?? Guid.NewGuid().ToString();
            UpdatePing();

            _logger.LogInformation("Клиент {ClientId} подключен", Id);
        }

        public void SetUser(UserAuth user) => User = user;

        public async Task ListenAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    _cts.Token
                );

                while (!linkedCts.Token.IsCancellationRequested)
                {
                    var response = await ReadAsync(linkedCts.Token).ConfigureAwait(false);
                    await _server.GetProcessor().Handler(response, this, linkedCts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Клиент {ClientId}: обработка остановлена", Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Клиент {ClientId}: ошибка обработки", Id);
            }
            finally
            {
                Dispose();
            }
        }

        public async Task<MainResponse> ReadAsync(CancellationToken cancellationToken)
        {
            using var reader = new StreamReader(Stream, Encoding.UTF8, leaveOpen: true);
            string? jsonLine;

            while ((jsonLine = await reader.ReadLineAsync()) != null)
            {
                try
                {
                    var response = JsonSerializer.Deserialize<MainResponse>(jsonLine);
                    if (response != null)
                    {
                        if (response.Action == "/gameBattle/unitSetTarget/")
                            _logger.LogDebug("Получено: {Json}", jsonLine);
                        return response;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Ошибка парсинга: {Data}", jsonLine);
                    throw;
                }
            }

            throw new IOException("Соединение закрыто");
        }

        public async Task WriteAsync(MainResponse response, CancellationToken cancellationToken)
        {
            if (response == null)
                throw new ArgumentNullException(nameof(response));
            if (_disposed || Stream == null)
            {
                _logger.LogWarning("Клиент {ClientId}: запись пропущена — соединение закрыто", Id);
                return;
            }

            try
            {
                string json = JsonSerializer.Serialize(response) + "\n";
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                await WriteBytesAsync(bytes, cancellationToken).ConfigureAwait(false);
                if (ConfigGameServer.IsDebugNetWork)
                    _logger.LogDebug("Отправлено {ClientId}: {Data}", Id, json.Trim());
            }
            catch (OperationCanceledException)
            {
                // Нормально при закрытии матча: _cts отменён, пока ещё шла запись.
                _logger.LogDebug("Клиент {ClientId}: запись отменена (Action: {Action})", Id, response.Action);
            }
            catch (ObjectDisposedException)
            {
                _logger.LogDebug("Клиент {ClientId}: поток закрыт при записи", Id);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Клиент {ClientId}: ошибка записи в поток", Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Клиент {ClientId}: неожиданная ошибка при записи (Action: {Action})", Id, response.Action);
                throw;
            }
        }

        /// <summary>
        /// Пишет уже сериализованный кадр. Один буфер можно слать нескольким клиентам.
        /// Записи сериализуются через _writeLock — без гонок и без параллельного забивания TCP.
        /// </summary>
        public async Task WriteBytesAsync(byte[] bytes, CancellationToken cancellationToken)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));
            if (_disposed || Stream == null)
            {
                _logger.LogWarning("Клиент {ClientId}: запись пропущена — соединение закрыто", Id);
                return;
            }

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_disposed || Stream == null)
                    return;
                await Stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
                CountWrite++;
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public void UpdatePing() => LastPing = DateTime.UtcNow.Ticks;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _logger.LogInformation("Клиент {ClientId}: отключение", Id);

            _cts.Cancel();

            try
            {
                Stream?.Dispose();
                TcpClient?.Dispose();
                _server.DisconnectUser(this);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Клиент {ClientId}: ошибка при освобождении ресурсов", Id);
            }
            finally
            {
                _writeLock.Dispose();
                _cts.Dispose();
                _logger.LogInformation("Клиент {ClientId}: ресурсы освобождены", Id);
            }
        }

        public bool IsConnected()
        {
            if (_disposed || !TcpClient.Connected)
                return false;

            // Проверка пинга (5 секунд - максимальный допустимый интервал)
            TimeSpan timeSinceLastPing = DateTime.UtcNow - new DateTime(LastPing);
            bool isPingValid = timeSinceLastPing.TotalSeconds <= 5;

            // Дополнительная проверка состояния сокета
            bool isSocketAlive = false;
            try
            {
                // Быстрая проверка без блокировки
                isSocketAlive = !(TcpClient.Client.Poll(0, SelectMode.SelectRead) &&
                                 TcpClient.Client.Available == 0);
            }
            catch
            {
                // Если возникла ошибка - сокет мертв
                return false;
            }

            return isPingValid && isSocketAlive;
        }
    }
}