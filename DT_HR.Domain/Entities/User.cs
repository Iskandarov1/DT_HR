using System.ComponentModel.DataAnnotations.Schema;
using DT_HR.Domain.Core.Primitives;
using DT_HR.Domain.Enumeration;

namespace DT_HR.Domain.Entities;

public class User : AggregateRoot
{
    private User(){}
    
    
    public User( 
    long telegramUserId,
    string phoneNumber ,
    string firstName,
    string lastName,
    DateOnly birthday,
    string language = "uz"
    )
    {
        this.TelegramUserId = telegramUserId;
        this.PhoneNumber = phoneNumber;
        this.FirstName = firstName;
        this.LastName = lastName;
        this.Email = "";
        this.Role = UserRole.Employee.Value;
        this.WorkStartTime = new TimeOnly(18, 55);
        this.WorkEndTime = new TimeOnly(23, 00);
        IsActive = true;
        this.Language = language;
        this.BirtDate = birthday;
    }
    
    [Column("telegramUser_id")] public long TelegramUserId { get; private set; }
    [Column("phone_number")] public string PhoneNumber { get; private set; }
    [Column("first_name")] public string FirstName { get; private set; }
    [Column("last_name")] public string LastName { get; private set; }
    [Column("email")] public string Email { get; set; }
    [Column("role")] public int Role { get; set; }
    [Column("work_start_time")] public TimeOnly WorkStartTime { get;  set; }
    [Column("work_end_time")] public TimeOnly WorkEndTime { get;  set; }
    [Column("is_active")] public bool IsActive { get; private set; }
    [Column("birth_date")] public DateOnly BirtDate { get; set; }
    [Column("language")] public string Language { get; private set; }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
    public void SetRole(UserRole role) => Role = role.Value;
    public bool IsManager() => Role == UserRole.Manager.Value;
    
    public void UpdateWorkHours(TimeOnly startTime, TimeOnly endTime)
    {
        WorkStartTime = startTime;
        WorkEndTime = endTime;
    }

    public void SetLanguage(string language)
    {
        Language = language;
    }

    public void SetBirthday(DateOnly date)
    {
        BirtDate = date;
    }


}