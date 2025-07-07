using DT_HR.Domain.Entities;
  using DT_HR.Domain.Repositories;
  using DT_HR.Application.Core.Abstractions.Data;
  using Microsoft.Extensions.DependencyInjection;
  using Microsoft.Extensions.Hosting;
  using Microsoft.Extensions.Logging;

  namespace DT_HR.Infrastructure.Services;

  public class CompanySeedingService : IHostedService
  {
      private readonly IServiceProvider _serviceProvider;
      private readonly ILogger<CompanySeedingService> _logger;

      public CompanySeedingService(
          IServiceProvider serviceProvider,
          ILogger<CompanySeedingService> logger)
      {
          _serviceProvider = serviceProvider;
          _logger = logger;
      }

      public async Task StartAsync(CancellationToken cancellationToken)
      {
          try
          {
              _logger.LogInformation("Starting Company seeding service...");

              using var scope = _serviceProvider.CreateScope();
              var companyRepository = scope.ServiceProvider.GetRequiredService<ICompanyRepository>();
              var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

              // Check if company already exists
              var companyMaybe = await companyRepository.GetAsync(cancellationToken);

              if (companyMaybe.HasNoValue)
              {
                  _logger.LogInformation("No company found. Creating default company...");

                  // Create default company
                  var defaultCompany = new Company(
                      name: "DT Company",
                      defaultWorkStartTime: new TimeOnly(9, 0),   // 9:00 AM
                      defaultWorkEndTime: new TimeOnly(18, 0),    // 6:00 PM
                      timeZone: "UTC+5"
                  );

                  companyRepository.Insert(defaultCompany);
                  await unitOfWork.SaveChangesAsync(cancellationToken);

                  _logger.LogInformation("Default company created successfully with work hours 09:00-18:00");
              }
              else
              {
                  _logger.LogInformation("Company already exists. Skipping seeding.");
              }
          }
          catch (Exception ex)
          {
              _logger.LogError(ex, "Error occurred while seeding company data");
              // Don't throw - let the application start even if seeding fails
          }
      }

      public Task StopAsync(CancellationToken cancellationToken)
      {
          return Task.CompletedTask;
      }
  }