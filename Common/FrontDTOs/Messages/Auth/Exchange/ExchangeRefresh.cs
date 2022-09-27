using ProtoBuf;

namespace FrontDTOs.Messages.Auth.Exchange{

[ProtoContract]
public class ExchangeRefreshToken
{
	[ProtoMember(1)]
	public string RefreshToken { get; set; }
} }