
namespace AnnouncementBot.Domain.Entities
{
    public class Category
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public long CreatedById { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private Category() { }

        public Category(string name, long createdById)
        {
            Name = name;
            CreatedById = createdById;
            CreatedAt = DateTime.UtcNow;
        }

        public void UpdateName(string name) => Name = name;
    
    }
}
