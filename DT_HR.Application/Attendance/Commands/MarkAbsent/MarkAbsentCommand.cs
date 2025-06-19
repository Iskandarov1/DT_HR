using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Domain.Core.Primitives.Result;
using DT_HR.Domain.Enumeration;

namespace DT_HR.Application.Attendance.Commands.MarkAbsent;

public sealed record MarkAbsentCommand(
    long TelegramUserId,
    string Reason,
    AbsenceType AbsenceType,
    DateTime? EstimatedArrivalTime = null
    ) : ICommand<Result<Guid>>;