namespace AnnouncementBot.Application.Common.Interfaces;

public interface IConditionalAudit
{
    bool ShouldAudit(object? result);
}
