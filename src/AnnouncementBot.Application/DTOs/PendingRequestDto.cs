using AnnouncementBot.Domain.Enums;

namespace AnnouncementBot.Application.DTOs;

public record PendingRequestDto(
    Guid Id,
    long RequesterId,
    AdminRequestType Type,
    string? Details,
    long? TargetId,
    DateTime CreatedAt);