using DT_HR.Application.Core.Abstractions.Messaging;
using DT_HR.Contract.Responses.MiniApp;
using DT_HR.Domain.Core.Primitives.Result;

namespace DT_HR.Application.MiniApp.Commands.VerifyLocationAndCheckIn;

public sealed record VerifyLocationAndCheckInCommand(
    long TelegramUserId,
    double Latitude,
    double Longitude,
    string LanguageCode,
    long Timestamp) : ICommand<Result<MiniAppCheckInResponse>>;