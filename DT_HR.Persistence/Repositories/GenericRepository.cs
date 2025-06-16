using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Domain.Core.Primitives;
using DT_HR.Domain.Core.Primitives.Maybe;
using Microsoft.EntityFrameworkCore;

namespace DT_HR.Persistence.Repositories;

/// <summary>
/// Represents the generic repository with the most common repository methods.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
internal abstract class GenericRepository<TEntity>
	where TEntity : Entity
{
	/// <summary>
	/// Initializes a new instance of the <see cref="GenericRepository{TEntity}"/> class.
	/// </summary>
	/// <param name="dbContext">The database context.</param>
	protected GenericRepository(IDbContext dbContext) => DbContext = dbContext;

	/// <summary>
	/// Gets the database context.
	/// </summary>
	protected IDbContext DbContext { get; }

	/// <summary>
	/// Gets the entity with the specified identifier.
	/// </summary>
	/// <param name="id">The entity identifier.</param>
	/// <returns>The maybe instance that may contain the entity with the specified identifier.</returns>
	public async Task<Maybe<TEntity>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => await DbContext.GetBydIdAsync<TEntity>(id);

	/// <summary>
	/// Gets by collection of entity id.
	/// </summary>
	/// <param name="ids">The collection of entity id.</param>
	/// <returns>The maybe instance that may contain the items with the specified ids.</returns>	
	public async Task<Maybe<IEnumerable<TEntity>>> GetBulkAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) => await DbContext.GetBulkAsync<TEntity>(ids, cancellationToken);

	/// <summary>
	/// Inserts the specified entity into the database.
	/// </summary>
	/// <param name="entity">The entity to be inserted into the database.</param>
	public void Insert(TEntity entity) => DbContext.Insert(entity);

	/// <summary>
	/// Inserts the specified entities to the database.
	/// </summary>
	/// <param name="entities">The entities to be inserted into the database.</param>
	public void InsertRange(IEnumerable<TEntity> entities) => DbContext.InsertRange(entities);

	/// <summary>
	/// Updates the specified entity in the database.
	/// </summary>
	/// <param name="entity">The entity to be updated.</param>
	public void Update(TEntity entity) => DbContext.Set<TEntity>().Update(entity);

	/// <summary>
	/// Updates the specified entities to the database.
	/// </summary>
	/// <param name="entities">The entities to be updated into the database.</param>
	public void UpdateRange(IEnumerable<TEntity> entities) => DbContext.UpdateRange(entities);

	/// <summary>
	/// Removes the specified entity from the database.
	/// </summary>
	/// <param name="entity">The entity to be removed from the database.</param>
	public void Remove(TEntity entity) => DbContext.Remove(entity);


	/// <summary>
	/// Removes the specified entities from the database.
	/// </summary>
	/// <param name="entities">The entities to be removed from the database.</param>
	public void RemoveRange(IEnumerable<TEntity> entities) => DbContext.RemoveRange(entities);
	
	/// <summary>
	/// Gets the single entity that meets the predicate.
	/// </summary>
	/// <param name="predicate">The predicate.</param>
	/// <returns>The maybe instance that may contain the single entity that meets the predicate.</returns>
	protected async Task<Maybe<TEntity>> FirstOrDefaultAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
		await DbContext.Set<TEntity>().FirstOrDefaultAsync(predicate, cancellationToken);

	/// <summary>
	/// Gets the single entity that meets the predicate.
	/// </summary>
	/// <param name="predicate">The predicate.</param>
	/// <returns>The maybe instance that may contain the single entity that meets the predicate.</returns>
	protected async Task<Maybe<TEntity>> SingleOrDefaultAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
		await DbContext.Set<TEntity>().SingleOrDefaultAsync(predicate, cancellationToken);

	/// <summary>
	/// Gets the single entity that meets the predicate.
	/// </summary>
	/// <param name="predicate">The predicate.</param>
	/// <returns>The maybe instance that may contain the single entity that meets the predicate.</returns>
	public Maybe<IQueryable<TEntity>> Where(System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate) => DbContext.Where(predicate);
}
