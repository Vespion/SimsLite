using FrontDTOs.Headers;
using Microsoft.Extensions.Logging;
using NetMQ;
using ServerObjects;

public class ServerWorker: IDisposable
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<ServerWorker> _logger;
	private IDisposable _logReqScope;
	private NetMQMessage _message;
	private NetMQQueue<NetMQMessage> _queue;

	public ServerWorker(IServiceProvider serviceProvider, ILogger<ServerWorker> logger)
	{
		_serviceProvider = serviceProvider;
		_logger = logger;
	}

	public void InitWorker(NetMQMessage message, NetMQQueue<NetMQMessage> queue)
	{
		_message = message;
		_logReqScope = _logger.BeginScope(new KeyValuePair<string, byte[]>("PeerId", _message[0].ToByteArray()));
		_queue = queue;
	}

	public virtual void PreExecution()
	{
		var version = _message[1].ConvertToString();
		if (version != "1")
		{
			throw new NotSupportedException("Protocol Version not supported");
		}
	}

	public virtual void Execute()
	{
		var headers = ProtoBuf.Serializer.Deserialize<RequestHeaders>(new Span<byte>(_message[2].Buffer));

		var messageType = Type.GetType(headers.Type);

		using var bodyBuffer = new MemoryStream(_message[3].Buffer);
		
		var body = ProtoBuf.Serializer.Deserialize(messageType!, bodyBuffer);
		
		var handler = FetchHandler(messageType!);
		var responseWrapper = handler.Execute(headers, body);

		using var responseBuffer = new MemoryStream();
		ProtoBuf.Serializer.Serialize(responseBuffer, responseWrapper);
		
		var response = new NetMQMessage();
		response.Append(_message[0]);
		response.Append(responseBuffer.ToArray());
		
		_queue.Enqueue(response);
	}
	
	private IHandle FetchHandler(Type messageType)
	{
		var handler = typeof(IHandle<>);
		Type[] typeArgs = { messageType };
		var concreteHandlerType = handler.MakeGenericType(typeArgs);
		return (IHandle)(_serviceProvider.GetService(concreteHandlerType) ?? throw new NullReferenceException());
	}
	
	/// <inheritdoc />
	public void Dispose()
	{
		_logReqScope.Dispose();
	}
}