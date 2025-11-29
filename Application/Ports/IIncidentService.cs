using Application.DTOs.Incident;

namespace Application.Ports
{
    public interface IIncidentService
    {
        Task<IncidentResponseDTO?> GetByIdAsync(Guid id);
        Task<IEnumerable<IncidentResponseDTO>> GetAllAsync();
        Task<IEnumerable<IncidentResponseDTO>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<IncidentResponseDTO>> GetByCategoryIdAsync(Guid categoryId);
        Task<IEnumerable<IncidentResponseDTO>> GetByStatusIdAsync(int statusId);
        Task<IncidentResponseDTO> CreateAsync(CreateIncidentRequestDTO dto, Guid userId);
        Task UpdateAsync(Guid id, UpdateIncidentRequestDTO dto);
        Task DeleteAsync(Guid id);
        Task AddCommentAsync(Guid incidentId, string comment, Guid authorId);
        Task<IEnumerable<IncidentUpdateDTO>> GetUpdatesAsync(Guid incidentId);
        Task AssignToUserAsync(Guid incidentId, Guid newUserId, Guid currentUserId);
    }
}