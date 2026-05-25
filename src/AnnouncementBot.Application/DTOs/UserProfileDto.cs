using AnnouncementBot.Domain.Enums;

namespace AnnouncementBot.Application.DTOs;

public record UserProfileDto(
    long UserId,
    string? UserName,
    UserRole Role,
    List<string> AccessibleOrSubscribedCategories);