using Domain.Entities;

namespace Application.Ports
{
    public interface IIncidentRepository
    {
        Task<IEnumerable<Incident>> GetAllAsync();
        Task<Incident?> GetByIdAsync(Guid id);
        Task<IEnumerable<Incident>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<Incident>> GetByCategoryIdAsync(Guid categoryId);
        Task<IEnumerable<Incident>> GetByStatusIdAsync(int statusId);
        Task<bool> ExistsAsync(Guid id);
        Task AddAsync(Incident incident);
        Task UpdateAsync(Incident incident);
        Task DeleteAsync(Guid id);
        Task AddUpdateAsync(IncidentUpdate update);
        Task<IEnumerable<IncidentUpdate>> GetUpdatesByIncidentIdAsync(Guid incidentId);
    }
}