using Domain.Entities;

namespace Application.Ports
{
    public interface IIncidentStatusRepository
    {
        Task<IEnumerable<IncidentStatus>> GetAllAsync();
        Task<IncidentStatus?> GetByIdAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}