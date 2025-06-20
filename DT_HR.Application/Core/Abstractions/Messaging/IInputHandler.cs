using DT_HR.Domain.Core.Primitives.Result;
using DT_HR.Application.Core.Abstractions.Messaging;


namespace DT_HR.Application.Core.Abstractions.Messaging;

/// <summary>
/// Represents an input handler that validates command input before processing
/// </summary>
/// <typeparam name="TCommand">The command type</typeparam>
public interface IInputHandler<in TCommand>
    
{
    /// <summary>
    /// Validates the command input
    /// </summary>
    /// <param name="command">The command to validate</param>
    /// <returns>Success if valid, Failure with error if invalid</returns>
    Task<Result> ValidateAsync(TCommand command, CancellationToken cancellationToken = default);
}