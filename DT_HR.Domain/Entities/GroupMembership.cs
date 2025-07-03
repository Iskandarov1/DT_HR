using System.ComponentModel.DataAnnotations.Schema;
using DT_HR.Domain.Core.Primitives;

namespace DT_HR.Domain.Entities;

public class GroupMembership : AggregateRoot
{
    private GroupMembership() { }

    public GroupMembership(Guid userId, Guid groupId, bool isAdmin = false)
    {
        UserId = userId;
        GroupId = groupId;
        IsAdmin = isAdmin;
        IsActive = true;
    }
    [Column("user_Id")] public Guid UserId { get; set; }
    [Column("group_id")] public Guid GroupId { get; set; }
    [Column("is_admin")] public bool IsAdmin { get; set; }
    [Column("is_active")] public bool IsActive { get; set; }

    public void MakeAdmin() => IsAdmin = true;
    public void RemoveAdmin() => IsAdmin = false;
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public User User { get; set; } = null!;
    public TelegramGroup Group { get; set; } = null!;
}