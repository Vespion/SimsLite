using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FrontDTOs.Headers;
using NetMQ;
using ProtoBuf;

namespace ApiSdk
{
    public class MessageBuilder
    {
        private string _version;
        private object _payload;
        private readonly RequestHeaders _headers = new RequestHeaders
        {
            AdditionalProperties = new Dictionary<string, string>()
        };


        public MessageBuilder()
        {
            _version = "1";
        }

        public MessageBuilder SetAdditionalHeaderProperties(string header, string value)
        {
            if(_headers.AdditionalProperties == null)
            {
                _headers.AdditionalProperties = new Dictionary<string, string>();
            }
            _headers.AdditionalProperties[header] = value;
            return this;
        }

        public MessageBuilder SetAuthorizationHeader(string auth)
        {
            _headers.Authorization = auth;
            return this;
        }

        public MessageBuilder SetMessagePayload(object payload)
        {
            var payloadType = payload.GetType();
            var hasContractType = payloadType.GetCustomAttribute(typeof(ProtoContractAttribute)) != null;

            if (!hasContractType)
            {
                throw new InvalidDataException("Payload data must have a ProtoContract attribute");
            }
            _payload = payload;
            return this;
        }

        public MessageBuilder SetProtocolVersion(string version)
        {
            _version = version;
            return this;
        }

        public NetMQMessage Build()
        {
            var msg = new NetMQMessage();

            _headers.Type = _payload.GetType().AssemblyQualifiedName;

            msg.Append(_version); //Version frame

            using (var headerStream = new MemoryStream()){
                Serializer.Serialize(headerStream, _headers);
                msg.Append(headerStream.ToArray()); //Header frame
            }

            using (var bodyStream = new MemoryStream()){
                Serializer.Serialize(bodyStream, _payload);
                msg.Append(bodyStream.ToArray()); //Body frame
            }

            return msg;
        }
    }
}