namespace AnnouncementBot.Application.DTOs;

public record TemplateDto(
    Guid Id,
    string Name,
    string Text);