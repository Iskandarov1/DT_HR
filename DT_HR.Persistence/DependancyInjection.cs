using DT_HR.Application.Core.Abstractions.Common;
using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Domain.Repositories;
using DT_HR.Persistence.Infrastructure;
using DT_HR.Persistence.Repositories;
using DT_HR.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DT_HR.Persistence;

public static class DependencyInjection
{
  
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(ConnectionString.SettingsKey)
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        
        services.AddSingleton(new ConnectionString(connectionString));

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            
            if (configuration.GetValue<bool>("EnableSensitiveDataLogging", false))
            {
                options.EnableSensitiveDataLogging();
            }
            
            if (configuration.GetValue<bool>("EnableDetailedErrors", false))
            {
                options.EnableDetailedErrors();
            }
        });
        
        

        services.AddScoped<IDbContext>(serviceProvider => serviceProvider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.TryAddScoped<IUserRepository, UserRepository>();


        return services;
    }
}