using ProtoBuf;

namespace FrontDTOs.Messages.Auth{

public static class AuthFailures
{
	public const string EmailAlreadyRegistered = "EMAIL_IN_USE";
	public const string UserNotRegistered = "NO_REGISTRATION";
	public const string DeviceNotRegistered = "NO_KEY_REGISTRATION";
	public const string Timeout = "SESSION_TIMED_OUT";
	public const string ChallengeFailed = "CHALLENGE_FAILED";
}

[ProtoContract]
public class TokenResponse
{
	[ProtoMember(1)]
	public string AccessToken { get; set; }
	
	[ProtoMember(2)]
	public string RefreshToken { get; set; }
} }