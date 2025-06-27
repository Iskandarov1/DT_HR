using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Contract.Responses;
using DT_HR.Domain.Core.Primitives.Result;

namespace DT_HR.Application.Attendance.Queries.GetDailyAttendance;

public sealed record GetDailyAttendanceQuery(
    long ManagerTelegramUserId,
    DateOnly? Date = null) : IQuery<Result<DailyAttendanceResponse>>;
