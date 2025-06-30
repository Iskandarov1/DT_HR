using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Domain.Core.Primitives.Result;

namespace DT_HR.Application.Users.Commands.UserRole;

public sealed record ChangeUserRoleCommand(Guid UsedId, Domain.Enumeration.UserRole Role) : ICommand<Result>;
