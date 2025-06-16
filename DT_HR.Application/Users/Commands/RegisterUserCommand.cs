using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Domain.Core.Primitives.Result;

namespace DT_HR.Application.Users.Commands;

public sealed record RegisterUserCommand(
    long TelegramUserId,
    string PhoneNumber,
    string FirstName,
    string LastName) : ICommand<Result<Guid>>;