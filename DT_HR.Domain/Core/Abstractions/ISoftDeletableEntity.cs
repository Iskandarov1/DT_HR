using System.ComponentModel.DataAnnotations.Schema;

namespace DT_HR.Domain.Core.Abstractions;
/// <summary>
/// Represents the marker interface for soft-deletable entities.
/// </summary>
public interface ISoftDeletableEntity
{
	/// <summary>
	/// Gets the date and time in UTC format the entity was deleted on.
	/// </summary>
	[Column("deleted_at")]
	DateTime? DeletedAt { get; }

	/// <summary>
	/// Gets a value indicating whether the entity has been deleted.
	/// </summary>
	[Column("is_deleted")]
	bool IsDelete { get; }
}
