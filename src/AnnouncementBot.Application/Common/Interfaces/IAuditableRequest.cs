namespace AnnouncementBot.Application.Common.Interfaces;

public interface IAuditableRequest
{
    long ActorId { get; }      
    string ActionName { get; } 
    string EntityName { get; } 
    string GetEntityId();      
    string? Details { get; }   
}