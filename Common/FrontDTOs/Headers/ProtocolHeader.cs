using System.Collections.Generic;
using ProtoBuf;

namespace FrontDTOs.Headers{

[ProtoContract]
public class RequestHeaders
{
    [ProtoMember(1)]
    public string Type { get; set; }
        
    [ProtoMember(2)]
    public string Authorization { get; set; }
        
    [ProtoMember(100)]
    public Dictionary<string, string> AdditionalProperties { get; set; }
}
}