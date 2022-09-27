using FrontDTOs;
using FrontDTOs.Headers;
using JetBrains.Annotations;

namespace ServerObjects
{
    public interface IHandle<[MeansImplicitUse]in TRequestBody> : IHandle
    {
        public Task<ResponseWrapper> Execute(RequestHeaders headers, TRequestBody body);
    }

    public interface IHandle
    {
        public Task<ResponseWrapper> Execute(RequestHeaders headers, object body);
    }
}