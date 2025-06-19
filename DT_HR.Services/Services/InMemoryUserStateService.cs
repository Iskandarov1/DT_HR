using System.Collections.Concurrent;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Domain.Core.Primitives.Result;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace DT_HR.Services.Services;

public class InMemoryUserStateService(ILogger<InMemoryUserStateService> logger) : IUserStateService, IHostedService
{
    private readonly ConcurrentDictionary<long, UserState> _states = new();
    private Timer? _cleanUpTimer;
    
    public Task<UserState?> GetStateAsync(long userId)
    {
        if (_states.TryGetValue(userId, out var state))
        {
            if (state.ExpiresAt < DateTime.UtcNow)
            {
                _states.TryRemove(userId, out _);
                return Task.FromResult<UserState?>(null);
            }
            return Task.FromResult<UserState?>(state);
        }

        return Task.FromResult<UserState?>(null);
    }

    public Task SetStateAsync(long userId, UserState state)
    {
        _states[userId] = state;
        logger.LogDebug("State set for the user {UserId}:{Action}",userId,state.CurrentAction);
        return Task.CompletedTask;
    }

    public Task RemoveStateAsync(long userId)
    {
        if (_states.TryRemove(userId , out var removedState))
        {
            logger.LogDebug("State removed for the user {UserId}:{Action}",userId,removedState.CurrentAction);
        }
        return Task.CompletedTask;
    }

    public Task ClearExpiredStatusAsync()
    {
        var now = DateTime.UtcNow;
        var expiredUsers = _states
            .Where(kvp => kvp.Value.ExpiresAt < now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var userId in expiredUsers)
        {
            _states.TryRemove(userId, out _);
        }

        if (expiredUsers.Any())
        {
            logger.LogInformation("Cleared {Count} expired user states ", expiredUsers.Count);
        }
        
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cleanUpTimer = new Timer(
            async _ => ClearExpiredStatusAsync(),
            null,
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5));
        
        logger.LogInformation("User state service started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cleanUpTimer?.Dispose();
        logger.LogInformation("User state service stopped");
        return Task.CompletedTask;
    }
}