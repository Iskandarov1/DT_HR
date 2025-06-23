using System.ComponentModel.DataAnnotations.Schema;
using DT_HR.Domain.Core.Primitives;

namespace DT_HR.Domain.Entities;

public class ManagerPhoneNumber : AggregateRoot
{
    private ManagerPhoneNumber(string phoneNumber, string isActive)
    {
        PhoneNumber = phoneNumber;
        IsActive = true;
    }
    
    [Column("phone_number")] public string PhoneNumber { get; private set; }
    [Column("is_active")] public bool IsActive { get; private set; }

    public void Deactive() => IsActive = false;
    public void Activate() => IsActive = true;

}