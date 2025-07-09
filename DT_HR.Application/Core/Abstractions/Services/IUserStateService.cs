using DT_HR.Domain.Core;
using DT_HR.Domain.Enumeration;

namespace DT_HR.Application.Core.Abstractions.Services;

public interface IUserStateService
{
    Task<UserState?> GetStateAsync(long userId);
    Task SetStateAsync(long userId, UserState state);
    Task RemoveStateAsync(long userId);
    Task ClearExpiredStatusAsync();
}

public class UserState
{
    public UserAction CurrentAction { get; set; }
    public AbsenceType? AbsenceType { get; set; }
    public string Language { get; set; } = "uz";
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime CreatedAt { get; set; } = TimeUtils.Now;
    public DateTime ExpiresAt { get; set; } = TimeUtils.Now.AddMinutes(30);
}

public enum UserAction
{
    None,
    Registering,
    CheckingIn,
    CheckingOut,
    ReportingAbsence,
    CreatingEvent,
    SelectingLanguage,
    ExportDateSelection,
    ExportSelectingStartDate,
    ExportSelectingEndDate,
    SettingWorkStartTime,
    SettingWorkEndTime
}