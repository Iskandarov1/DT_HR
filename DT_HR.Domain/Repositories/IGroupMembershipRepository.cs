using DT_HR.Domain.Core.Primitives.Maybe;
using DT_HR.Domain.Entities;

namespace DT_HR.Domain.Repositories;

public interface IGroupMembershipRepository
{
   Task<Maybe<GroupMembership>> GetByUserAndGroupAsync(Guid userId, Guid groupId,
      CancellationToken cancellation = default);

   Task<IEnumerable<GroupMembership>> GetUserMembershipsAsync(Guid userId, CancellationToken cancellation = default);

   Task<IEnumerable<GroupMembership>>
      GetActiveGroupMembersAsync(Guid groupId, CancellationToken cancellation = default);

   Task<IEnumerable<User>> GetActiveGroupUsersAsync(Guid groupId, CancellationToken cancellation = default);
   Task<bool> IsUserMemberOfGroupAsync(Guid userId, Guid groupId, CancellationToken cancellation = default);
   Task<bool> IsUserAdminOfGroupAsync(Guid userId, Guid groupId, CancellationToken cancellation = default);
   Task<int> GetActiveGroupMemberCountAsync(Guid groupId, CancellationToken cancellation = default);

   void Insert(GroupMembership group);
   void Update(GroupMembership group);

}