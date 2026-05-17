namespace AnnouncementBot.Domain.Entities
{
    public class Template
    {
        public Guid Id { get; private set; }

        public string Name { get; private set; } = string.Empty;
        public string Text { get; private set; } = string.Empty;
        public long CreatedById { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private Template() { }

        public Template(string name, string text, long createdById)
        {
            Name = name;
            Text = text;
            CreatedById = createdById;
            CreatedAt = DateTime.UtcNow;
        }

        public void UpdateName(string name) => Name = name;
        public void UpdateText(string text) => Text = text;

    }
}
