using DT_HR.Application.Core.Abstractions.Data;
using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Application.Core.Abstractions.Services;
using DT_HR.Domain.Core.Primitives.Result;
using DT_HR.Domain.Entities;
using DT_HR.Domain.Repositories;

namespace DT_HR.Application.Events.Commands;

public class CreateEventCommandHandler(
    IEventRepository eventRepository,
    IUnitOfWork unitOfWork, 
    IBackgroundTaskService taskService) : ICommandHandler<CreateEventCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var evt = new Event(request.Description, request.EventTime);
        eventRepository.Insert(evt);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await taskService.ScheduleEventReminderAsync(evt.Description, evt.EventTime, cancellationToken);
        return Result.Success(evt.Id);
    }
}