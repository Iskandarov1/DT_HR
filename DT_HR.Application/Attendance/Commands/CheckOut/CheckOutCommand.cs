using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Domain.Core.Primitives.Result;

namespace DT_HR.Application.Attendance.Commands.CheckOut;

public record CheckOutCommand(long TelegramUserId) : ICommand<Result<Guid>>;