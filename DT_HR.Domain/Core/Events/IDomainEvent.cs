using MediatR;

namespace DT_HR.Domain.Core.Events
{
    /// <summary>
    /// Represents the interface for an event that is raised within the domain.
    /// </summary>
    public interface IDomainEvent : INotification
    {
    }
}
