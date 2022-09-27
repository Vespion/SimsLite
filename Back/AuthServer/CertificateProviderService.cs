using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using NetMQ;
using NetMQ.Sockets;
using ServerBase;

namespace AuthServer
{
    public sealed class CertificateProviderService : IHostedService, IDisposable
    {
        private readonly ILogger<CertificateProviderService> _log;
        private readonly INetMQSocket _socket;
        private readonly string _socketAddress;
        private readonly INetMQPoller _poller;
        private readonly string _publicCertificate;

        public CertificateProviderService(IOptions<ServerSettings> serverSettings, ILogger<CertificateProviderService> log, IOptions<CertificateProviderOptions> options)
        {
            _log = log;
            _socket = new RouterSocket();
            _poller = new NetMQPoller { _socket };
            _socketAddress = $"tcp://*:{options.Value.ListeningPort ?? 5574}";
            var cert = NetMQCertificate.CreateFromSecretKey(serverSettings.Value.CertificateKey ?? throw new CryptographicException("Configuration does not include a server key"));
            _publicCertificate = cert.PublicKeyZ85;
            _log.LogTrace("Service construction complete");
        }

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _socket.ReceiveReady += SocketOnReceiveReady;

            _log.LogDebug("Bound public certificate key to ({PublicKey})", _publicCertificate);

            _socket.Bind(_socketAddress);
            _poller.RunAsync();
            _log.LogInformation("Service ready at socket endpoint {Endpoint}", _socket.Options.LastEndpoint);
            return Task.CompletedTask;
        }

        private void SocketOnReceiveReady(object? sender, NetMQSocketEventArgs e)
        {
            var msg = e.Socket.ReceiveMultipartMessage();
            var identityFrame = msg[0];
            _log.LogInformation("Certificate request from '{PeerId}'", identityFrame.ConvertToString());
            e.Socket.SendMoreFrame(identityFrame.Buffer).SendFrame(_publicCertificate);
            _log.LogDebug("Completed sending certificate for '{PeerId}'", identityFrame.ConvertToString());
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _socket.ReceiveReady -= SocketOnReceiveReady;
            _socket.Close();

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _poller.RemoveAndDispose(_socket);
            _poller.Dispose();
        }
    }
}