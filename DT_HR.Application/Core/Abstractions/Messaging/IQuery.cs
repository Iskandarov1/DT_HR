using MediatR;

namespace DT_HR.Application.Core.Abstractions.Messaging
{
    public interface IQuery<out TResponse> : IRequest<TResponse>
    {
    }
}