

using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Domain.Core.Primitives.Result;

namespace DT_HR.Application.Attendance.Commands.CheckIn;

public sealed record CheckInCommand(
    long TelegramUserId,
    double Latitude,
    double Longitude) : ICommand<Result<Guid>>;
