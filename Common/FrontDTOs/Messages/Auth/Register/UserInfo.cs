using ProtoBuf;

namespace FrontDTOs.Messages.Auth.Register{

[ProtoContract]
public class UserInfo
{
	[ProtoMember(2)]
	public string FirstName { get; set; }
		
	[ProtoMember(3)]
	public string LastName { get; set; }
	
	[ProtoMember(4)]
	public bool IsTeacher { get; set; }
} }