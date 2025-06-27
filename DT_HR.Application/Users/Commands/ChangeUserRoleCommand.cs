using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Domain.Core.Primitives.Result;
using DT_HR.Domain.Enumeration;

namespace DT_HR.Application.Users.Commands;

public sealed record ChangeUserRoleCommand(Guid UsedId, UserRole Role) : ICommand<Result>;
