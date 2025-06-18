using System.ComponentModel.DataAnnotations.Schema;

namespace DT_HR.Domain.Core.Abstractions;

/// <summary>
/// Represents the marker interface for auditable entities.
/// </summary>
public interface IAuditableEntity
{
	/// <summary>
	/// Gets the created at date and time in UTC format.
	/// </summary>
	[Column("created_at")]
	DateTime CreatedAt { get; }

	[Column("updated_at")]
	DateTime? UpdatedAt { get; }	
}