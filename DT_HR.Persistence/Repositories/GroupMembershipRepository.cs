using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Domain.Core.Primitives.Maybe;
using DT_HR.Domain.Entities;
using DT_HR.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DT_HR.Persistence.Repositories;

internal sealed class GroupMembershipRepository(IDbContext dbContext) : GenericRepository<GroupMembership>(dbContext), IGroupMembershipRepository
{
    public async Task<Maybe<GroupMembership>> GetByUserAndGroupAsync(Guid userId, Guid groupId,
        CancellationToken cancellationToken = default)
    {
        return await FirstOrDefaultAsync(gm => gm.UserId == userId && gm.GroupId == groupId, cancellationToken);
    } 

    public async Task<IEnumerable<GroupMembership>> GetUserMembershipsAsync(Guid userId, CancellationToken cancellation = default)
    {
        return await DbContext.Set<GroupMembership>()
            .Include(gm => gm.Group)
            .Where(gm => gm.UserId == userId && gm.IsActive)
            .ToListAsync(cancellation);
    }

    public async Task<IEnumerable<GroupMembership>> GetActiveGroupMembersAsync(Guid groupId, CancellationToken cancellation = default)
    {
        return await DbContext.Set<GroupMembership>()
            .Include(gm => gm.Group)
            .Where(gm => gm.GroupId == groupId && gm.IsActive)
            .ToListAsync(cancellation);
    }

    public async Task<IEnumerable<User>> GetActiveGroupUsersAsync(Guid groupId, CancellationToken cancellation = default)
    {
        return await DbContext.Set<GroupMembership>()
            .Include(gm => gm.User)
            .Where(gm => gm.GroupId == groupId && gm.IsActive)
            .Select(gm => gm.User)
            .ToListAsync(cancellation);
    }

    public async Task<bool> IsUserMemberOfGroupAsync(Guid userId, Guid groupId, CancellationToken cancellation = default)
    {
        return await DbContext.Set<GroupMembership>()
            .AnyAsync(gm => gm.UserId == userId && gm.GroupId == groupId && gm.IsActive, cancellation);
    }

    public async Task<bool> IsUserAdminOfGroupAsync(Guid userId, Guid groupId, CancellationToken cancellation = default)
    {
        return await DbContext.Set<GroupMembership>()
            .AnyAsync(gm => gm.UserId == userId && gm.GroupId == groupId && gm.IsActive && gm.IsAdmin, cancellation);
    }

    public async Task<int> GetActiveGroupMemberCountAsync(Guid groupId, CancellationToken cancellation = default)
    {
        return await DbContext.Set<GroupMembership>()
            .CountAsync(gm => gm.GroupId == groupId && gm.IsActive, cancellation);
    }
}