using DT_HR.Domain.Core.Primitives;

namespace DT_HR.Domain.Entities;

public class User : AggregateRoot
{
    private User(){}
    
    
    public User( 
    long telegramUserId,
    long phoneNumber ,
    string firstName,
    string lastName,
    string email
    )
    {
        this.TelegramUserId = telegramUserId;
        this.PhoneNumber = phoneNumber;
        this.FistName = firstName;
        this.LastName = lastName;
        this.Email = email;
        IsActive = true;
    }
    
    public long TelegramUserId { get; private set; }
    public long PhoneNumber { get; private set; }
    public string FistName { get; private set; }
    public string LastName { get; private set; }
    public bool IsActive { get; private set; }
    public string Email { get; set; }

    public void Deactivate() => IsActive = false;
    
    
}