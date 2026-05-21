namespace AnnouncementBot.Domain.Interfaces
{
    public interface IRepository<TEntity, TId>
    {
        Task<TEntity?> GetByIdAsync(TId id, CancellationToken ct = default);
        Task AddAsync(TEntity entity, CancellationToken ct = default);
        Task UpdateAsync(TEntity entity, CancellationToken ct = default);
        Task DeleteAsync(TEntity entity, CancellationToken ct = default);
    }
}
