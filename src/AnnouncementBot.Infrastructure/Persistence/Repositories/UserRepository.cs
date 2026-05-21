using AnnouncementBot.Domain.Entities;
using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AnnouncementBot.Infrastructure.Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;
        public UserRepository(AppDbContext context) 
        { 
            _context = context;
        }
        public async Task AddAsync(User entity, CancellationToken ct = default) 
            => await _context.Users.AddAsync(entity, ct);

        public async Task<bool> ExistsAsync(long id, CancellationToken ct = default)
            => await _context.Users.AnyAsync(u => u.Id == id, ct);
            
        public async Task<IReadOnlyList<User>> GetAllAdminsAsync(CancellationToken ct = default)
            => await _context.Users.Where(u => u.Role == UserRole.Admin).ToListAsync(ct);

        public async Task<User?> GetByIdAsync(long id, CancellationToken ct = default)
            => await _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

        public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
            => await _context.Users.FirstOrDefaultAsync(u => u.UserName == username, ct);
        public Task DeleteAsync(User entity, CancellationToken ct = default)
        {
            _context.Users.Remove(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(User entity, CancellationToken ct = default)
        {
            _context.Users.Update(entity);
            return Task.CompletedTask;
        }
    }
}
