using AnnouncementBot.Domain.Enums;

namespace AnnouncementBot.Application.DTOs;

public record AdminDto(long Id, string? UserName, UserRole Role);
