using Application.DTOs.Incident;
using Application.Ports;
using AutoMapper;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class IncidentService : IIncidentService
    {
        private readonly IIncidentRepository _incidentRepository;
        private readonly IIncidentCategoryRepository _categoryRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<IncidentService> _logger;

        public IncidentService(
            IIncidentRepository incidentRepository,
            IIncidentCategoryRepository categoryRepository,
            IMapper mapper,
            ILogger<IncidentService> logger)
        {
            _incidentRepository = incidentRepository;
            _categoryRepository = categoryRepository;
            _mapper = mapper;
            _logger = logger;
            _logger.LogDebug("IncidentService initialized successfully");
        }

        public async Task<IncidentResponseDTO?> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Retrieving incident by ID: {IncidentId}", id);

            var incident = await _incidentRepository.GetByIdAsync(id);

            if (incident == null)
            {
                _logger.LogWarning("Incident not found: {IncidentId}", id);
                return null;
            }

            var dto = _mapper.Map<IncidentResponseDTO>(incident);
            _logger.LogInformation("Incident retrieved successfully: {IncidentId} - {Title}", id, incident.Title);
            return dto;
        }

        public async Task<IEnumerable<IncidentResponseDTO>> GetAllAsync()
        {
            _logger.LogInformation("Retrieving all incidents");

            var incidents = await _incidentRepository.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<IncidentResponseDTO>>(incidents);

            _logger.LogInformation("Retrieved {Count} incidents successfully", incidents.Count());
            return dtos;
        }

        public async Task<IEnumerable<IncidentResponseDTO>> GetByUserIdAsync(Guid userId)
        {
            _logger.LogInformation("Retrieving incidents for user: {UserId}", userId);

            var incidents = await _incidentRepository.GetByUserIdAsync(userId);
            var dtos = _mapper.Map<IEnumerable<IncidentResponseDTO>>(incidents);

            _logger.LogInformation("Retrieved {Count} incidents for user {UserId}", incidents.Count(), userId);
            return dtos;
        }

        public async Task<IEnumerable<IncidentResponseDTO>> GetByCategoryIdAsync(Guid categoryId)
        {
            _logger.LogInformation("Retrieving incidents by category: {CategoryId}", categoryId);

            var incidents = await _incidentRepository.GetByCategoryIdAsync(categoryId);
            var dtos = _mapper.Map<IEnumerable<IncidentResponseDTO>>(incidents);

            _logger.LogInformation("Retrieved {Count} incidents for category {CategoryId}", incidents.Count(), categoryId);
            return dtos;
        }

        public async Task<IEnumerable<IncidentResponseDTO>> GetByStatusIdAsync(int statusId)
        {
            _logger.LogInformation("Retrieving incidents by status: {StatusId}", statusId);

            var incidents = await _incidentRepository.GetByStatusIdAsync(statusId);
            var dtos = _mapper.Map<IEnumerable<IncidentResponseDTO>>(incidents);

            _logger.LogInformation("Retrieved {Count} incidents with status {StatusId}", incidents.Count(), statusId);
            return dtos;
        }

        public async Task<IncidentResponseDTO> CreateAsync(CreateIncidentRequestDTO dto, Guid userId)
        {
            _logger.LogInformation("Creating new incident: {Title} (User: {UserId}, Category: {CategoryId})",
                dto.Title, userId, dto.CategoryId);

            var categoryExists = await _categoryRepository.ExistsAsync(dto.CategoryId);
            if (!categoryExists)
            {
                _logger.LogWarning("Attempted to create incident with non-existent category: {CategoryId}", dto.CategoryId);
                throw new KeyNotFoundException($"Category {dto.CategoryId} not found");
            }

            var incident = _mapper.Map<Incident>(dto);
            incident.UserId = userId;
            incident.Id = Guid.NewGuid();

            if (!incident.IsValid())
            {
                _logger.LogWarning("Attempted to create invalid incident: {Title}", dto.Title);
                throw new InvalidOperationException("Incident data is invalid");
            }

            await _incidentRepository.AddAsync(incident);

            _logger.LogInformation("Incident created successfully: {IncidentId} - {Title}",
                incident.Id, incident.Title);

            var response = _mapper.Map<IncidentResponseDTO>(incident);
            return response;
        }

        public async Task UpdateAsync(Guid id, UpdateIncidentRequestDTO dto)
        {
            _logger.LogInformation("Updating incident: {IncidentId}", id);

            var incident = await _incidentRepository.GetByIdAsync(id);
            if (incident == null)
            {
                _logger.LogWarning("Attempted to update non-existent incident: {IncidentId}", id);
                throw new KeyNotFoundException($"Incident {id} not found");
            }

            _mapper.Map(dto, incident);

            if (!incident.IsValid())
            {
                _logger.LogWarning("Attempted to update incident with invalid data: {IncidentId}", id);
                throw new InvalidOperationException("Updated incident data is invalid");
            }

            await _incidentRepository.UpdateAsync(incident);

            _logger.LogInformation("Incident updated successfully: {IncidentId} - {Title}",
                incident.Id, incident.Title);
        }

        public async Task DeleteAsync(Guid id)
        {
            _logger.LogInformation("Deleting incident: {IncidentId}", id);

            var exists = await _incidentRepository.ExistsAsync(id);
            if (!exists)
            {
                _logger.LogWarning("Attempted to delete non-existent incident: {IncidentId}", id);
                throw new KeyNotFoundException($"Incident {id} not found");
            }

            await _incidentRepository.DeleteAsync(id);

            _logger.LogInformation("Incident deleted successfully: {IncidentId}", id);
        }

        public async Task AddCommentAsync(Guid incidentId, string comment, Guid authorId)
        {
            _logger.LogInformation("Adding comment to incident: {IncidentId} (Author: {AuthorId})",
                incidentId, authorId);

            var incident = await _incidentRepository.GetByIdAsync(incidentId);
            if (incident == null)
            {
                _logger.LogWarning("Attempted to add comment to non-existent incident: {IncidentId}", incidentId);
                throw new KeyNotFoundException($"Incident {incidentId} not found");
            }

            var update = new IncidentUpdate
            {
                Id = Guid.NewGuid(),
                IncidentId = incidentId,
                AuthorId = authorId,
                Comment = comment,
                UpdateType = "COMMENT",
                CreatedAt = DateTime.UtcNow
            };

            if (!update.IsValid())
            {
                _logger.LogWarning("Attempted to add invalid comment to incident: {IncidentId}", incidentId);
                throw new InvalidOperationException("Comment data is invalid");
            }

            await _incidentRepository.AddUpdateAsync(update);

            _logger.LogInformation("Comment added to incident successfully: {IncidentId} - {UpdateId}",
                incidentId, update.Id);
        }

        public async Task<IEnumerable<IncidentUpdateDTO>> GetUpdatesAsync(Guid incidentId)
        {
            _logger.LogInformation("Retrieving updates for incident: {IncidentId}", incidentId);

            var exists = await _incidentRepository.ExistsAsync(incidentId);
            if (!exists)
            {
                _logger.LogWarning("Attempted to retrieve updates for non-existent incident: {IncidentId}", incidentId);
                throw new KeyNotFoundException($"Incident {incidentId} not found");
            }

            var updates = await _incidentRepository.GetUpdatesByIncidentIdAsync(incidentId);
            var dtos = _mapper.Map<IEnumerable<IncidentUpdateDTO>>(updates);

            _logger.LogInformation("Retrieved {Count} updates for incident {IncidentId}", updates.Count(), incidentId);
            return dtos;
        }

        public async Task AssignToUserAsync(Guid incidentId, Guid newUserId, Guid currentUserId)
        {
            _logger.LogInformation("Reassigning incident {IncidentId} to user {NewUserId} by {CurrentUserId}",
                incidentId, newUserId, currentUserId);

            // Validar que el incidente existe
            var incident = await _incidentRepository.GetByIdAsync(incidentId);
            if (incident == null)
            {
                _logger.LogWarning("Attempted to reassign non-existent incident: {IncidentId}", incidentId);
                throw new KeyNotFoundException($"Incidente {incidentId} no encontrado");
            }

            // Validar que el nuevo usuario no sea vacío
            if (newUserId == Guid.Empty)
            {
                _logger.LogWarning("Attempted to assign to empty user ID");
                throw new ArgumentException("El ID del usuario no es valido");
            }

            var oldUserId = incident.UserId;

            // Solo actualizar si el usuario es diferente
            if (oldUserId == newUserId)
            {
                _logger.LogInformation("User ID is the same, skipping reassignment");
                return;
            }

            // Reasignar - solo cambiar el ID, no la navegación
            incident.UserId = newUserId;
            incident.UpdatedAt = DateTime.UtcNow;

            await _incidentRepository.UpdateAsync(incident);

            // Registrar en historial
            var update = new IncidentUpdate
            {
                Id = Guid.NewGuid(),
                IncidentId = incidentId,
                AuthorId = currentUserId,
                Comment = $"Incidente reasignado a otro usuario",
                UpdateType = "USER_REASSIGNMENT",
                OldValue = oldUserId.ToString(),
                NewValue = newUserId.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            await _incidentRepository.AddUpdateAsync(update);

            _logger.LogInformation("Incident {IncidentId} reassigned from {OldUserId} to {NewUserId}",
                incidentId, oldUserId, newUserId);
        }
    }
}