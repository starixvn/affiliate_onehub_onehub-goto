using ONEHub.GOTO.Core.Models;
using ONEHub.Shared.Commands;

namespace ONEHub.GOTO.Core.Services.Interfaces;

public interface IClickTrackingService
{
    Task<CommandResult> InsertClickInfo(ClickTrackingEvent clickTrackingEvent);
}
