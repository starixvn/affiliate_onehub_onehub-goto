using Fanex.Data.Repository;
using ONEHub.GOTO.Core.Commands;
using ONEHub.GOTO.Core.Models;
using ONEHub.GOTO.Core.Services.Interfaces;
using ONEHub.Shared.Commands;

namespace ONEHub.GOTO.Core.Services;

public class ClickTrackingService(IDynamicRepository dynamicRepository) : IClickTrackingService
{
    public async Task<CommandResult> InsertClickInfo(ClickTrackingEvent clickTrackingEvent)
    {
        var command = new ClickTrackingEventCommand
        {
            ClickId = clickTrackingEvent.ClickId,
            UpdatedTime = clickTrackingEvent.UpdatedTime,
            IP = clickTrackingEvent.IP,
            Browser = clickTrackingEvent.Browser,
            URLReferrer = clickTrackingEvent.URLReferrer,
            DomainReferrer = clickTrackingEvent.DomainReferrer,
            OS = clickTrackingEvent.OS,
            Device = clickTrackingEvent.Device,
            BrowserVersion = clickTrackingEvent.BrowserVersion,
            BrowserLanguage = clickTrackingEvent.BrowserLanguage,
            UserAgent = clickTrackingEvent.UserAgent,
            RedirectURL = clickTrackingEvent.RedirectURL,
            DeviceType = clickTrackingEvent.DeviceType,
            ClickTypeId = clickTrackingEvent.ClickTypeId,
            LinkId = clickTrackingEvent.LinkId
        };

        await dynamicRepository.ExecuteAsync(command);
        return new CommandResult
        {
            ErrorCode = command.ErrCode,
            ErrorMessage = command.ErrMsg
        };
    }
}
