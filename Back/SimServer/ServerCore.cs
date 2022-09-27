using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetMQ;
using NetMQ.Sockets;

public sealed class ServerCore: IHostedService, IDisposable
{
	private readonly IServiceProvider _serviceProvider;
	private NetMQSocket _serverSocket;
	private NetMQPoller _poller;
	private NetMQQueue<NetMQMessage> _outbox;

	public ServerCore(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
	}

	/// <inheritdoc />
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		_outbox = new NetMQQueue<NetMQMessage>();
		_serverSocket = new RouterSocket();
		// _serverSocket.Options.CurveCertificate = NetMQCertificate.CreateFromSecretKey();
		
		_serverSocket.ReceiveReady += ServerSocketOnReceiveReady;
		_outbox.ReceiveReady += OutboxOnReceiveReady;

		_poller = new NetMQPoller() { _serverSocket, _outbox };

		_serverSocket.Bind("tcp://*:5575");
		_poller.RunAsync();
	}

	private void OutboxOnReceiveReady(object? sender, NetMQQueueEventArgs<NetMQMessage> e)
	{
		_serverSocket.SendMultipartMessage(e.Queue.Dequeue());
	}

	private void ServerSocketOnReceiveReady(object? sender, NetMQSocketEventArgs e)
	{
		// Console.WriteLine("Got signal");
		var message = e.Socket.ReceiveMultipartMessage();

		Task.Factory.StartNew(() =>
		{
			using var scope = _serviceProvider.CreateScope();
			var worker = _serviceProvider.GetRequiredService<ServerWorker>();
			worker.InitWorker(message, _outbox);
			worker.PreExecution();
			worker.Execute();
			
		}, TaskCreationOptions.DenyChildAttach);
		
		// var peerId = message[0];
		// var version = message[1].ConvertToString();
		// var header = JsonSerializer.Deserialize<ProtocolHeader>(message[2].ConvertToString());
		// var body = message[3].ConvertToString();
		//
		// Console.WriteLine($"Got from client: {body}");
		//
		// var response = new NetMQMessage();
		// response.Append(peerId);
		// response.Append(version);
		// response.Append("OK");
		//
		// e.Socket.SendMultipartMessage(response);
		// Console.WriteLine("Server frame sent");
	}

	/// <inheritdoc />
	public async Task StopAsync(CancellationToken cancellationToken)
	{
		// ReSharper disable once MethodHasAsyncOverload
		_poller.Stop();
		_serverSocket.ReceiveReady -= ServerSocketOnReceiveReady;
		_outbox.ReceiveReady -= OutboxOnReceiveReady;
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_poller.RemoveAndDispose(_serverSocket);
		_poller.RemoveAndDispose(_outbox);
		_poller.Dispose();
	}
}