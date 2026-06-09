namespace AnnouncementBot.Application.DTOs;

public record DeliveryDto(Guid Id, 
    Guid AnnouncementId, 
    long UserId, int ErrorCode, 
    DateTime? LastAttemptAt);