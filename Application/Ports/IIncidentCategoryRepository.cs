using Domain.Entities;

namespace Application.Ports
{
    public interface IIncidentCategoryRepository
    {
        Task<IEnumerable<IncidentCategory>> GetAllAsync();
        Task<IEnumerable<IncidentCategory>> GetActiveAsync();
        Task<IncidentCategory?> GetByIdAsync(Guid id);
        Task<IncidentCategory?> GetByNameAsync(string name);
        Task<bool> ExistsAsync(Guid id);
        Task AddAsync(IncidentCategory category);
        Task UpdateAsync(IncidentCategory category);
        Task DeleteAsync(Guid id);
    }
}