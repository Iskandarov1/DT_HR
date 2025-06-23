using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Contract.Responses;
using DT_HR.Domain.Core;
using DT_HR.Domain.Core.Errors;
using DT_HR.Domain.Core.Primitives.Result;
using DT_HR.Domain.Entities;
using DT_HR.Domain.Repositories;

namespace DT_HR.Application.Attendance.Queries.GetTodayAttendance;

public class GetTodayAttendanceQueryHandler(
    IUserRepository userRepository, 
    IDbContext dbContext) : IQueryHandler<GetTodayAttendanceQuery,Result<TodayAttendanceResponse>>
{
    public async Task<Result<TodayAttendanceResponse>> Handle(GetTodayAttendanceQuery request, CancellationToken cancellationToken)
    {
        var manager = await userRepository.GetByTelegramUserIdAsync(request.ManagerTelegramUserId, cancellationToken);

        if (manager.HasNoValue || !manager.Value.IsManager())
            return Result.Failure<TodayAttendanceResponse>(DomainErrors.User.InvalidPermissions);

        var today = DateOnly.FromDateTime(TimeUtils.Now);

        var query = from user in dbContext.Set<User>()
            where user.IsActive && !user.IsDelete
            join attendance in dbContext.Set<Domain.Entities.Attendance>() on new { UserId = user.Id, Date = today }
                equals new { attendance.Id, attendance.Date } into attendances
            from att in attendances.DefaultIfEmpty()
            select new { User = user, Atttendance = att };
    }
}