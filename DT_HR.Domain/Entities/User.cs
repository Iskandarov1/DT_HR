using System.ComponentModel.DataAnnotations.Schema;
using DT_HR.Domain.Core.Primitives;

namespace DT_HR.Domain.Entities;

public class User : AggregateRoot
{
    private User(){}
    
    
    public User( 
    long telegramUserId,
    string phoneNumber ,
    string firstName,
    string lastName
    )
    {
        this.TelegramUserId = telegramUserId;
        this.PhoneNumber = phoneNumber;
        this.FirstName = firstName;
        this.LastName = lastName;
        this.Email = "";
        this.WorkStartTime = new TimeOnly(10, 0);
        this.WorkEndTime = new TimeOnly(19, 0);
        IsActive = true;
    }
    
    [Column("telegramUser_id")] public long TelegramUserId { get; private set; }
    [Column("phone_number")] public string PhoneNumber { get; private set; }
    [Column("first_name")] public string FirstName { get; private set; }
    [Column("last_name")] public string LastName { get; private set; }
    [Column("email")] public string Email { get; set; }
    
    [Column("work_start_time")] public TimeOnly WorkStartTime { get;  set; }

    [Column("work_end_time")] public TimeOnly WorkEndTime { get;  set; }
    [Column("is_active")] public bool IsActive { get; private set; }
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;


}