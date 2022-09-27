using FrontDTOs;
using FrontDTOs.Headers;
using FrontDTOs.Messages;
using ServerObjects;

namespace Handlers;

public class StatusHandler: IHandle<StatusRequest>
{
	/// <inheritdoc />
	public ResponseWrapper Execute(RequestHeaders headers, StatusRequest body)
	{
		return new ResponseWrapper
		{
			Status = ApiStatusCode.Ok
		};
	}

	/// <inheritdoc />
	public ResponseWrapper Execute(RequestHeaders headers, object body)
	{
		return body switch
		{
			StatusRequest request => Execute(headers, request),
			_ => throw new NotImplementedException()
		};
	}
}