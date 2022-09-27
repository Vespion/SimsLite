using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NetMQ;
using NetMQ.Sockets;

namespace ApiSdk
{
    public class SimApiClient : IDisposable
    {
        public bool HasStarted { get; private set; }
        private readonly SemaphoreSlim _startSemaphoreSlim = new SemaphoreSlim(1, 1);
        internal readonly ClientSocket Socket;
        private readonly IOptions<ApiConfiguration> _options;
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly SimAuthenticationManager _authentication;

        public SimApiClient(IOptions<ApiConfiguration> options, SimAuthenticationManager authentication)
        {
            _options = options;
            _authentication = authentication;
            _authentication.Init(this);
            Socket = new ClientSocket();
        }

        public async Task StartAsync()
        {
            await _startSemaphoreSlim.WaitAsync();
            if (HasStarted)
            {
                return;
            }
            await Task.Run(() =>
            {
                var sslSocket = new DealerSocket($"tcp://{_options.Value.Host}:{_options.Value.AuthPort}");
                sslSocket.SendFrameEmpty();

                var certKey = sslSocket.ReceiveFrameString();
                Socket.Options.CurveCertificate = NetMQCertificate.FromPublicKey(certKey);
                Socket.Connect($"tcp://{_options.Value.Host}:{_options.Value.SimPort}");
                sslSocket.Dispose();
            });

            HasStarted = true;
            _startSemaphoreSlim.Release();
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            _startSemaphoreSlim.Dispose();
            Socket.Dispose();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}