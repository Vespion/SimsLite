using ProtoBuf;

namespace FrontDTOs{

[ProtoContract]
[ProtoInclude(1, typeof(ResponseWrapper<object>))]
public class ResponseWrapper
{
	public int Status { get; set; }

	public bool IsSuccess()
	{
		return Status.ToString().StartsWith("2");
	}
}

[ProtoContract]
#pragma warning disable PBN0013
public class ResponseWrapper<TData>: ResponseWrapper
#pragma warning restore PBN0013
{
	public TData Data { get; set; }
} }