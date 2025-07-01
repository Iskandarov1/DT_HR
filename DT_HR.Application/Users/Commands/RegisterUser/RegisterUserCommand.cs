using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Domain.Core.Primitives.Result;

namespace DT_HR.Application.Users.Commands.RegisterUser;

public sealed record RegisterUserCommand(
    long TelegramUserId,
    string PhoneNumber,
    string FirstName,
    string LastName,
    DateOnly BirthDate,
    string Language) : ICommand<Result<Guid>>;