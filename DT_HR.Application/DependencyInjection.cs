using System.Reflection;
using DT_HR.Application.Attendance.Callbacks;
using DT_HR.Application.Attendance.Commands.CheckIn;
using DT_HR.Application.Attendance.Commands.CheckOut;
using DT_HR.Application.Attendance.Commands.MarkAbsent;
using DT_HR.Application.Attendance.InputHandler;
using DT_HR.Application.Core.Abstractions.Common;
using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Application.Resources;
using DT_HR.Domain.Core.Localizations;
using DT_HR.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace DT_HR.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        
        services.AddLocalization();
        services.AddScoped<ISharedViewLocalizer, ApplicationSharedViewLocalizer>();
        // Input Handlers
        services.AddScoped<IInputHandler<CheckInCommand>, CheckInInputHandler>();
        services.AddScoped<IInputHandler<CheckOutCommand>, CheckOutInputHandler>();
        services.AddScoped<IInputHandler<MarkAbsentCommand>, MarkAbsentInputHandler>();
        
        // Callbacks
        services.AddScoped<ICheckInCallbacks, CheckInCallbacks>();
        services.AddScoped<ICheckOutCallbacks, CheckOutCallbacks>();
        services.AddScoped<IMarkAbsentCallbacks, MarkAbsentCallbacks>();
            

        return services;
    }
    
}