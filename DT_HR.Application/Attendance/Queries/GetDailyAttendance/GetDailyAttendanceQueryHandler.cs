using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Contract.Responses;
using DT_HR.Domain.Core.Primitives.Result;
using DT_HR.Domain.Repositories;

namespace DT_HR.Application.Attendance.Queries.GetDailyAttendance;

public class GetDailyAttendanceQueryHandler(
    IDbContext dbContext,
    IUserRepository userRepository) : IQueryHandler<GetDailyAttendanceQuery,Result<DailyAttendanceResponse>>
{
    public Task<Result<DailyAttendanceResponse>> Handle(GetDailyAttendanceQuery request, CancellationToken cancellationToken)
    {
        
    }
}