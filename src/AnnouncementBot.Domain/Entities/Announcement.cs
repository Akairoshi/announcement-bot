namespace AnnouncementBot.Domain.Entities
{
    public class Announcement
    {
        public Guid Id {  get; private set; }
        public string Text { get; private set; } = string.Empty;
        public Guid CategoryId { get; private set; }
        public Guid? TemplateId { get; private set; }
        public long CreatedById { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private Announcement() { }

        public Announcement(string text, Guid categoryId, 
            long createdById ,Guid? templateId)
        {
            Text = text;
            CategoryId = categoryId;
            TemplateId = templateId;
            CreatedById = createdById; 
            CreatedAt = DateTime.UtcNow;
        }
    }
}
