namespace AnnouncementBot.Application.DTOs;

public record CategorySubscriptionDto(
    Guid Id,
    string Name,
    bool IsSubscribed);