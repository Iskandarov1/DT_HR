using System.Linq.Expressions;
using DT_HR.Application.Core.Abstractions.Common;
using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Domain.Core.Abstractions;
using DT_HR.Domain.Core.Primitives;
using DT_HR.Domain.Core.Primitives.Maybe;
using DT_HR.Domain.Entities;
using DT_HR.Persistence.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace DT_HR.Persistence;

/// <summary>
/// Represents the applications database context.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
/// </remarks>
/// <param name="options">The database context options.</param>
/// <param name="dateTime">The current date and time.</param>
/// <param name="mediator">The mediator.</param>
public sealed class ApplicationDbContext(
	DbContextOptions<ApplicationDbContext> options,
	IDateTime dateTime,
	IMediator mediator) : DbContext(options), IDbContext, IUnitOfWork
{
	public DbSet<User> Users => Set<User>();
	public DbSet<Attendance> AttendanceRSet => Set<Attendance>();
	public DbSet<Event> Events => Set<Event>();
	public DbSet<TelegramGroup> TelegramGroups => Set<TelegramGroup>();
	public DbSet<GroupMembership> GroupMemberships => Set<GroupMembership>();
	public DbSet<Company> Companies => Set<Company>();

	/// <inheritdoc />
	public new DbSet<TEntity> Set<TEntity>()
		where TEntity : Entity
		=> base.Set<TEntity>();

	/// <inheritdoc />
	public async Task<Maybe<TEntity>> GetBydIdAsync<TEntity>(Guid id, CancellationToken cancellationToken = default)
		where TEntity : Entity
		=> id == Guid.Empty ?
			Maybe<TEntity>.None :
			Maybe<TEntity>.From(await Set<TEntity>().FirstOrDefaultAsync(e => e.Id == id, cancellationToken));

	/// <inheritdoc />
	public async Task<Maybe<IEnumerable<TEntity>>> GetBulkAsync<TEntity>(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
		where TEntity : Entity
		=> Maybe<IEnumerable<TEntity>>.From(await Set<TEntity>().Where(e => ids.Contains(e.Id)).ToArrayAsync(cancellationToken));

	/// <inheritdoc />
	public void Insert<TEntity>(TEntity entity)
		where TEntity : Entity
		=> Set<TEntity>().Add(entity);

	/// <inheritdoc />
	public void InsertRange<TEntity>(IEnumerable<TEntity> entities)
		where TEntity : Entity
		=> Set<TEntity>().AddRange(entities);

	/// <inheritdoc />
	public void UpdateRange<TEntity>(IEnumerable<TEntity> entities)
		where TEntity : Entity
		=> Set<TEntity>().UpdateRange(entities);

	/// <inheritdoc />
	public new void Remove<TEntity>(TEntity entity)
		where TEntity : Entity
		=> Set<TEntity>().Remove(entity);

	public void RemoveRange<TEntity>(IEnumerable<TEntity> entities)
		where TEntity : Entity
		=> Set<TEntity>().RemoveRange(entities);

	/// <inheritdoc />
	public Maybe<IQueryable<TEntity>> Where<TEntity>(Expression<Func<TEntity, bool>> predicate)
		where TEntity : Entity
		=> Maybe<IQueryable<TEntity>>.From(Set<TEntity>().Where(predicate));

	/// <inheritdoc />
	public Task<int> ExecuteSqlAsync(string sql, IEnumerable<NpgsqlParameter> parameters, CancellationToken cancellationToken = default)
		=> Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);

	/// <summary>
	/// Saves all of the pending changes in the unit of work.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>The number of entities that have been saved.</returns>
	public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		var utcNow = dateTime.UtcNow;

		await this.UpdateAuditableEntities(utcNow, cancellationToken);

		await this.UpdateSoftDeletableEntities(utcNow, cancellationToken);

		return await base.SaveChangesAsync(cancellationToken);
	}

	/// <inheritdoc />
	public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
		=> Database.BeginTransactionAsync(cancellationToken);

	/// <inheritdoc />
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(System.Reflection.Assembly.GetExecutingAssembly());

		modelBuilder.ApplyUtcDateTimeConverter();
		base.OnModelCreating(modelBuilder);
	}


	/// <summary>
	/// Updates the entities implementing <see cref="IAuditableEntity"/> interface.
	/// </summary>
	/// <param name="utcNow">The current date and time in UTC format.</param>
	private async Task UpdateAuditableEntities(DateTime utcNow, CancellationToken cancellationToken = default)
	{
		foreach (EntityEntry<IAuditableEntity> entityEntry in ChangeTracker.Entries<IAuditableEntity>())
		{
			if (entityEntry.State == EntityState.Added)
			{
				entityEntry.Property(nameof(IAuditableEntity.CreatedAt)).CurrentValue = utcNow;
				
			}

			if (entityEntry.State == EntityState.Modified)
			{
				entityEntry.Property(nameof(IAuditableEntity.UpdatedAt)).CurrentValue = utcNow;
				
			}
		}
	}

	/// <summary>
	/// Updates the entities implementing <see cref="ISoftDeletableEntity"/> interface.
	/// </summary>
	/// <param name="utcNow">The current date and time in UTC format.</param>
	private async Task UpdateSoftDeletableEntities(DateTime utcNow, CancellationToken cancellationToken = default)
	{
		foreach (EntityEntry<ISoftDeletableEntity> entityEntry in ChangeTracker.Entries<ISoftDeletableEntity>())
		{
			if (entityEntry.State != EntityState.Deleted)
			{
				continue;
			}

			entityEntry.Property(nameof(ISoftDeletableEntity.DeletedAt)).CurrentValue = utcNow;

			entityEntry.Property(nameof(ISoftDeletableEntity.IsDelete)).CurrentValue = true;

			entityEntry.State = EntityState.Modified;
			

			UpdateDeletedEntityEntryReferencesToUnchanged(entityEntry);
		}
	}

	/// <summary>
	/// Updates the specified entity entry's referenced entries in the Deleted state to the modified state.
	/// This method is recursive.
	/// </summary>
	/// <param name="entityEntry">The entity entry.</param>
	private static void UpdateDeletedEntityEntryReferencesToUnchanged(EntityEntry entityEntry)
	{
		if (!entityEntry.References.Any())
		{
			return;
		}

		foreach (ReferenceEntry referenceEntry in entityEntry.References.Where(r => r.TargetEntry?.State == EntityState.Deleted))
		{
			referenceEntry.TargetEntry.State = EntityState.Unchanged;

			UpdateDeletedEntityEntryReferencesToUnchanged(referenceEntry.TargetEntry);
		}
	}
}
