using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RtsServer.App.NetWorkResponseSender
{
    public abstract class NetWorkSenderBase : IDisposable
    {
        protected readonly UserClientTcp _clientApi;
        protected MainResponse _response;
        private bool _disposed;

        protected NetWorkSenderBase(UserClientTcp clientApi)
        {
            _clientApi = clientApi ?? throw new ArgumentNullException(nameof(clientApi));
            _response = new MainResponse("base", "/base/", "", "200");
        }

        public virtual NetWorkSenderBase SetDate(object data)
        {
            _response = _response?.SetBody(data) ?? throw new InvalidOperationException("Response not initialized");
            return this;
        }

        [Obsolete("Use SendAsync instead", error: false)]
        public void SendMessage()
        {
            _ = SendAsync(CancellationToken.None); // Fire-and-forget
        }

        public async Task SendAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            if (_clientApi == null || !_clientApi.IsConnected())
                throw new InvalidOperationException("Client is not connected");

            await _clientApi.WriteAsync(_response, cancellationToken).ConfigureAwait(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Освобождаем управляемые ресурсы (если есть)
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}