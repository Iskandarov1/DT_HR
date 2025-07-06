using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Domain.Core.Primitives;
using DT_HR.Domain.Core.Primitives.Result;

namespace DT_HR.Application.Attendance.Queries.ExportAttendance;

public sealed record ExportAttendanceQuery(
    long TelegramUserId,
    DateOnly StartDate,
    DateOnly EndDate,
    string Language) : IQuery<Result<byte[]>>;