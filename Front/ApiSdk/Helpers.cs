using System;
using System.IO;
using System.Threading.Tasks;
using ApiSdk.Exceptions;
using FrontDTOs;
using NetMQ;
using ProtoBuf;

namespace ApiSdk
{
    internal static class Helpers
    {
        internal static async Task HandleResponse(IThreadSafeInSocket socket, Action<int, ResponseWrapper> statusHandler = null)
	{
		var response = await socket.ReceiveBytesAsync();

		var responseStream = new MemoryStream(response);
		var wrapper = Serializer.Deserialize<ResponseWrapper>(responseStream);

		statusHandler?.Invoke(wrapper.Status, wrapper);

		if (!wrapper.IsSuccess())
		{
			if (statusHandler == null)
			{
				throw new ApiException(wrapper); //If an error response is possible it should be explicitly handled by the caller
			}
		}
	}
	
	internal static async Task<T> HandleResponse<T>(IThreadSafeInSocket socket, Action<int, ResponseWrapper> statusHandler = null)
	{
		var response = await socket.ReceiveBytesAsync();

		var responseStream = new MemoryStream(response);
		var wrapper = Serializer.Deserialize<ResponseWrapper>(responseStream);

		statusHandler?.Invoke(wrapper.Status, wrapper);

		if (!wrapper.IsSuccess())
		{
			if (statusHandler == null)
			{
				throw new ApiException(wrapper); //If an error response is possible it should be explicitly handled by the caller
			}

			return default;
		}

		var parsed = Serializer.Deserialize<ResponseWrapper<T>>(responseStream);

		return parsed.Data;
	}
    }
}
