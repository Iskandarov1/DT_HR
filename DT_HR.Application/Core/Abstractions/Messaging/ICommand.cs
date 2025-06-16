using MediatR;

namespace DT_HR.Application.Core.Abstractions.Messaging;


public interface ICommand<out TResponse> : IRequest<TResponse>
{
}
