using ProtoBuf;

namespace FrontDTOs.Messages.Auth.Register{

[ProtoContract]
public class KeyRegistration
{
	[ProtoMember(1)]
	public string DeviceName { get; set; }
	
	[ProtoMember(6)]
	public string DeviceId { get; set; }
	
	[ProtoMember(5)]
	public string UserEmail { get; set; }
	
	[ProtoMember(2)]
	public UserInfo User { get; set; }
	
	[ProtoMember(3)]
	public string Attestation { get; set; }
	
	[ProtoMember(4)]
	public string PublicKey { get; set; }
} }