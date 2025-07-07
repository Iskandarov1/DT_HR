using System.Windows.Input;
using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Domain.Core.Primitives.Result;

namespace DT_HR.Application.Users.Commands.UpdateWorkHourss;

public sealed record UpdateWorkHoursCommand(Guid UserId, TimeOnly WorkStartTime, TimeOnly WorkEndTime) : ICommand<Result>;