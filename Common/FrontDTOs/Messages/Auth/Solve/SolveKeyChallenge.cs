using System;
using ProtoBuf;

namespace FrontDTOs.Messages.Auth.Solve{

[ProtoContract]
public class SolveKeyChallenge
{
	[ProtoMember(1)]
	public Guid ChallengeId { get; set; }
	
	[ProtoMember(2)]
	public string SignedChallenge { get; set; }
} }