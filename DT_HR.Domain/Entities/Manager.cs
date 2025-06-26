using System.ComponentModel.DataAnnotations.Schema;
using DT_HR.Domain.Core.Primitives;

namespace DT_HR.Domain.Entities;

public class Manager : AggregateRoot
{
    private Manager() { }

    public Manager(
        long telegramUserId,
        string firstName,
        string lastName,
        string phoneNumber
        )
    {
        TelegramUserId = telegramUserId;
        PhoneNumber = phoneNumber;
        FirstName = firstName;
        LastName = lastName;
        PreferredLanguage = "uz";
        IsActive = true;
    }

    [Column("telegram_user_id")] public long TelegramUserId { get; private set; }
   [Column("first_name")] public string FirstName { get; private set; }
   [Column("last_name")] public string LastName { get; private set; }
   [Column("phone_number")] public string PhoneNumber { get; private set; }
   [Column("preferred_language")] public string PreferredLanguage { get; private set; }
   [Column("is_active")] public bool IsActive { get; private set; }


   public void SetLanguage(string language) => PreferredLanguage = language;
   public void Activate() => IsActive = true;
   public void Deactivate() => IsActive = false;
}