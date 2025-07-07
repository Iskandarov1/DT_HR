
using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Domain.Core.Primitives.Result;

namespace DT_HR.Application.Company.Commands.UpdateCompanyWorkHours;

public sealed record UpdateCompanyWorkHoursCommand (TimeOnly WorkStartTime, TimeOnly WorkEndTime) : ICommand<Result>;