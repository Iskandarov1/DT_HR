using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Domain.Core.Primitives.Result;

namespace DT_HR.Application.Events.Commands;

public sealed record CreateEventCommand(string Description, DateTime EventTime) : ICommand<Result<Guid>>;