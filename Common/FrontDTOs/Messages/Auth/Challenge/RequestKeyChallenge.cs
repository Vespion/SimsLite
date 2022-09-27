using System;
using ProtoBuf;

namespace FrontDTOs.Messages.Auth.Challenge{

[ProtoContract]
public class RequestKeyChallenge
{
	[ProtoMember(1)]
	public string UserEmail { get; set; }
	
	[ProtoMember(2)]
	public string DeviceId { get; set; }
	
	[ProtoMember(3)]
	public string VeriferChallenge { get; set; }
}

[ProtoContract]
public class KeyChallenge
{
	[ProtoMember(1)]
	public Guid ChallengeId { get; set; }
	
	[ProtoMember(2)]
	public string Challenge { get; set; }
} }