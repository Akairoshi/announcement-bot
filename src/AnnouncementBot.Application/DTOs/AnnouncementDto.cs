namespace AnnouncementBot.Application.DTOs;

public record AnnouncementDto(
    Guid Id,
    string Text,
    string CategoryName,
    DateTime CreatedAt);