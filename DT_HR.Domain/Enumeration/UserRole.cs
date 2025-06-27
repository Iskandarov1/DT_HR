
using DT_HR.Domain.Core.Primitives;
namespace DT_HR.Domain.Enumeration;

public sealed class UserRole : Enumeration<UserRole>
{
    public static readonly UserRole Employee = new UserRole(0, "Employee");
    public static readonly UserRole Manager = new UserRole(1, "Manager");
    public static readonly UserRole Admin = new UserRole(2, "Admin");


    
    private UserRole(int value, string name): base(value,name) { }
}