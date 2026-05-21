using AnnouncementBot.Domain.Entities;

namespace AnnouncementBot.Domain.Interfaces
{
    public interface IUserRepository : IRepository<User, long>
    {
        Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
        Task<bool> ExistAsync(long id, CancellationToken ct = default);
        Task<IReadOnlyList<User>> GetAllAdminsAsync(CancellationToken ct = default);
    }
}
