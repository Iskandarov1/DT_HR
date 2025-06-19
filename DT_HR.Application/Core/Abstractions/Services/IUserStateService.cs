using DT_HR.Domain.Enumeration;

namespace DT_HR.Application.Core.Abstractions.Services;

public interface IUserStateService
{
    Task<UserState> GetStateAsync(long userId);
    Task SetStateAsync(long userId, UserState state);
    Task RemoveStateAsync(long userId);
    Task ClearExpiredStatusAsync();
}

public class UserState
{
    public UserAction CurrentAction { get; set; }
    public AbsenceType? AbsenceType { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(30);
}

public enum UserAction
{
    None,
    Registering,
    CheckingIn,
    ReportingAbsence
    
}