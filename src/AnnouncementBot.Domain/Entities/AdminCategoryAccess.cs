
namespace AnnouncementBot.Domain.Entities
{
    public class AdminCategoryAccess
    {
        public long AdminId { get; private set; }
        public Guid CategoryId { get; private set; }

        private AdminCategoryAccess() { }

        public AdminCategoryAccess(long adminId, Guid categoryId)
        {
            AdminId = adminId;
            CategoryId = categoryId;
        
        }

    }
}
