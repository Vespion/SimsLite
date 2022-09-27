using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NetMQ;
using NetMQ.Sockets;

namespace ServerBase
{
    public sealed class ServerCore : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptions<ServerSettings> _serverSettings;
        private readonly NetMQSocket _serverSocket;
        private readonly NetMQPoller _poller;
        private readonly NetMQQueue<NetMQMessage> _outbox;

        public ServerCore(IServiceProvider serviceProvider, IOptions<ServerSettings> serverSettings)
        {
            _serviceProvider = serviceProvider;
            _serverSettings = serverSettings;

            _outbox = new NetMQQueue<NetMQMessage>();
            _serverSocket = new RouterSocket();

            _poller = new NetMQPoller { _serverSocket, _outbox };
        }

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _serverSocket.ReceiveReady += ServerSocketOnReceiveReady;
            _outbox.ReceiveReady += OutboxOnReceiveReady;

            if (!string.IsNullOrWhiteSpace(_serverSettings.Value.CertificateKey))
            {
                _serverSocket.Options.CurveServer = true;
                _serverSocket.Options.CurveCertificate = NetMQCertificate.CreateFromSecretKey(_serverSettings.Value.CertificateKey);
            }

            _serverSocket.Bind($"tcp://*:{_serverSettings.Value.ListeningPort ?? 5755}");
            _poller.RunAsync();

            return Task.CompletedTask;
        }

        private void OutboxOnReceiveReady(object? sender, NetMQQueueEventArgs<NetMQMessage> e)
        {
            _serverSocket.SendMultipartMessage(e.Queue.Dequeue());
        }

        private void ServerSocketOnReceiveReady(object? sender, NetMQSocketEventArgs e)
        {
            var message = e.Socket.ReceiveMultipartMessage();

            Task.Factory.StartNew(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var worker = _serviceProvider.GetRequiredService<ServerWorker>();
                worker.InitWorker(message, _outbox);
                worker.PreExecution();
                await worker.Execute();

            }, TaskCreationOptions.DenyChildAttach);
        }

        /// <inheritdoc />
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _serverSocket.ReceiveReady -= ServerSocketOnReceiveReady;

            while (!_outbox.IsEmpty && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(0.5), cancellationToken);
            }

            // ReSharper disable once MethodHasAsyncOverload
            _poller.Stop();
            _outbox.ReceiveReady -= OutboxOnReceiveReady;
            _serverSocket.Close();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _poller.RemoveAndDispose(_serverSocket);
            _poller.RemoveAndDispose(_outbox);
            _poller.Dispose();
        }
    }
}