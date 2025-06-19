using System.Reflection;
using DT_HR.Application.Attendance.Commands.CheckIn;
using DT_HR.Application.Attendance.Commands.MarkAbsent;
using DT_HR.Application.Core.Abstractions.Common;
using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace DT_HR.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        
        // Input Handlers
        services.AddScoped<IInputHandler<CheckInCommand>, CheckInInputHandler>();
        services.AddScoped<IInputHandler<MarkAbsentCommand>, MarkAbsentInputHandler>();
        
        // Callbacks
        services.AddScoped<ICheckInCallbacks, CheckInCallbacks>();
        services.AddScoped<IMarkAbsentCallbacks, MarkAbsentCallbacks>();
            

        return services;
    }
    
}