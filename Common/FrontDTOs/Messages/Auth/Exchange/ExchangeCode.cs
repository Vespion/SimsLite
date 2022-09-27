using ProtoBuf;

namespace FrontDTOs.Messages.Auth.Exchange{

[ProtoContract]
public class ExchangeCode
{
	[ProtoMember(1)]
	public string AuthorizationCode { get; set; }

	[ProtoMember(2)]
	public string Verifer { get; set; }
} }