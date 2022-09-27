using ProtoBuf;

namespace FrontDTOs.Messages.Auth{

	[ProtoContract]
public class CodeResponse
{
		[ProtoMember(1)]
	public string AuthCode { get; set; }
	
		[ProtoMember(2)]
	public string FailureReason { get; set; }
}}