using System.Linq.Expressions;
using DT_HR.Domain.Core.Primitives;
using DT_HR.Domain.Core.Primitives.Maybe;
using Microsoft.EntityFrameworkCore;

namespace DT_HR.Application.Core.Abstractions.Data;

/// <summary>
/// Represents the application database context interface.
/// </summary>
public interface IDbContext
{
	/// <summary>
	/// Gets the database set for the specified entity type.
	/// </summary>
	/// <typeparam name="TEntity">The entity type.</typeparam>
	/// <returns>The database set for the specified entity type.</returns>
	DbSet<TEntity> Set<TEntity>()
		where TEntity : Entity;

	/// <summary>
	/// Gets the entity with the specified identifier.
	/// </summary>
	/// <typeparam name="TEntity">The entity type.</typeparam>
	/// <param name="id">The entity identifier.</param>
	/// <returns>The <typeparamref name="TEntity"/> with the specified identifier if it exists, otherwise null.</returns>
	Task<Maybe<TEntity>> GetBydIdAsync<TEntity>(Guid id, CancellationToken cancellationToken = default)
		where TEntity : Entity;

	/// <summary>
	/// Gets by collection of entity id.
	/// </summary>
	/// <param name="ids">The collection of entity id.</param>
	/// <returns>The maybe instance that may contain the items with the specified ids.</returns>	
	Task<Maybe<IEnumerable<TEntity>>> GetBulkAsync<TEntity>(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
		where TEntity : Entity;

	/// <summary>
	/// Inserts the specified entity into the database.
	/// </summary>
	/// <typeparam name="TEntity">The entity type.</typeparam>
	/// <param name="entity">The entity to be inserted into the database.</param>
	void Insert<TEntity>(TEntity entity)
		where TEntity : Entity;

	/// <summary>
	/// Inserts the specified entities into the database.
	/// </summary>
	/// <typeparam name="TEntity">The entity type.</typeparam>
	/// <param name="entities">The entities to be inserted into the database.</param>
	void InsertRange<TEntity>(IEnumerable<TEntity> entities)
		where TEntity : Entity;

	/// <summary>
	/// Updates the specified entities into the database.
	/// </summary>
	/// <typeparam name="TEntity">The entity type.</typeparam>
	/// <param name="entities">The entities to be updated into the database.</param>
	void UpdateRange<TEntity>(IEnumerable<TEntity> entities)
		where TEntity : Entity;

	/// <summary>
	/// Removes the specified entity from the database.
	/// </summary>
	/// <typeparam name="TEntity">The entity type.</typeparam>
	/// <param name="entity">The entity to be removed from the database.</param>
	void Remove<TEntity>(TEntity entity)
		where TEntity : Entity;


	/// <summary>
	/// Removes the specified entity from the database.
	/// </summary>
	/// <typeparam name="TEntity">The entity type.</typeparam>
	/// <param name="entity">The entity to be removed from the database.</param>
	void RemoveRange<TEntity>(IEnumerable<TEntity> entities)
		where TEntity : Entity;

	/// <summary>
	/// Gets the entity with the specified predicate.
	/// </summary>
	/// <typeparam name="TEntity">The entity type.</typeparam>
	/// <param name="predicate">The entity predicate.</param>
	/// <returns>The <typeparamref name="IQueryable<TEntity>"/> with the specified predicate if it exists, otherwise null.</returns>
	Maybe<IQueryable<TEntity>> Where<TEntity>(Expression<Func<TEntity, bool>> predicate)
		where TEntity : Entity;
}