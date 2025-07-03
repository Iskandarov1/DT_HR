using System.ComponentModel.DataAnnotations.Schema;
using DT_HR.Domain.Core.Abstractions;

namespace DT_HR.Domain.Core.Primitives;
/// <summary>
/// Represents the base class that all entities derive from.
/// </summary>
public abstract class Entity : IEquatable<Entity>, IAuditableEntity,ISoftDeletableEntity
{
	/// <summary>
	/// Initializes a new instance of the <see cref="Entity"/> class.
	/// </summary>
	/// <remarks>
	/// Required by EF Core.
	/// </remarks>
	protected Entity()
	{
		this.Id = Guid.NewGuid();
	}

	protected Entity(Guid id)
	{
		this.Id = id;
	}

	/// <summary>
	/// Gets or sets the entity identifier.
	/// </summary>
	[Column("id")] public Guid Id { get; set; }

	public static bool operator ==(Entity a, Entity b)
	{
		if (a is null && b is null)
		{
			return true;
		}

		if (a is null || b is null)
		{
			return false;
		}

		return a.Equals(b);
	}

	public static bool operator !=(Entity a, Entity b) => !(a == b);

	/// <inheritdoc />
	public bool Equals(Entity other)
	{
		if (other is null)
		{
			return false;
		}

		return ReferenceEquals(this, other) || Id == other.Id;
	}

	/// <inheritdoc />
	public override bool Equals(object obj)
	{
		if (obj is null)
		{
			return false;
		}

		if (ReferenceEquals(this, obj))
		{
			return true;
		}

		if (obj.GetType() != GetType())
		{
			return false;
		}

		if (!(obj is Entity other))
		{
			return false;
		}

		if (Id == Guid.Empty || other.Id == Guid.Empty)
		{
			return false;
		}

		return Id == other.Id;
	}

	/// <inheritdoc />
	public override int GetHashCode() => Id.GetHashCode() * 41;

	
	[Column("created_at")] public DateTime CreatedAt { get; set; }
	[Column("updated_at")] public DateTime? UpdatedAt { get; set; }
	
	[Column("deleted_at")] public DateTime? DeletedAt { get; }
	[Column("is_deleted")] public bool IsDelete { get; set; }
	
}
