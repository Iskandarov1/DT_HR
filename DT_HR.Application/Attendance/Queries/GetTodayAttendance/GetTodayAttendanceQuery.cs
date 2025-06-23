using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Contract.Responses;
using DT_HR.Domain.Core.Primitives.Result;

namespace DT_HR.Application.Attendance.Queries.GetTodayAttendance;

public sealed record GetTodayAttendanceQuery (long ManagerTelegramUserId) : IQuery<Result<TodayAttendanceResponse>>;