
using MediatR;

namespace DT_HR.Application.Core.Abstractions.Messaging;

// public interface ICommand : MediatR.IRequest
// {
// }
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}
