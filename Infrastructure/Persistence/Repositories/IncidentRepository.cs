using Application.Ports;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories
{
    public class IncidentRepository : IIncidentRepository
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<IncidentRepository> _logger;

        public IncidentRepository(AppDbContext context, IMapper mapper, ILogger<IncidentRepository> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _logger.LogDebug("IncidentRepository initialized successfully");
        }

        public async Task<IEnumerable<Incident>> GetAllAsync()
        {
            _logger.LogInformation("Retrieving all incidents from database");

            try
            {
                var entities = await _context.Incidents
                    .Include(i => i.User)
                    .Include(i => i.Category)
                    .Include(i => i.Status)
                    .Include(i => i.Updates)
                        .ThenInclude(u => u.Author)
                    .OrderByDescending(i => i.Priority)
                    .ThenByDescending(i => i.CreatedAt)
                    .ToListAsync();

                var incidents = _mapper.Map<IEnumerable<Incident>>(entities);
                _logger.LogInformation("Retrieved {Count} incidents from database", entities.Count);
                return incidents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all incidents from database");
                throw;
            }
        }

        public async Task<Incident?> GetByIdAsync(Guid id)
        {
            _logger.LogDebug("Retrieving incident by ID from database: {IncidentId}", id);

            try
            {
                var entity = await _context.Incidents
                    .Include(i => i.User)
                    .Include(i => i.Category)
                    .Include(i => i.Status)
                    .Include(i => i.Updates)
                        .ThenInclude(u => u.Author)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (entity == null)
                {
                    _logger.LogDebug("Incident not found in database: {IncidentId}", id);
                    return null;
                }

                var incident = _mapper.Map<Incident>(entity);
                _logger.LogDebug("Incident retrieved from database: {IncidentId} - {Title}", id, entity.Title);
                return incident;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving incident by ID from database: {IncidentId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Incident>> GetByUserIdAsync(Guid userId)
        {
            _logger.LogDebug("Retrieving incidents by user ID from database: {UserId}", userId);

            try
            {
                var entities = await _context.Incidents
                    .Where(i => i.UserId == userId)
                    .Include(i => i.Category)
                    .Include(i => i.Status)
                    .Include(i => i.Updates)
                        .ThenInclude(u => u.Author)
                    .OrderByDescending(i => i.Priority)
                    .ThenByDescending(i => i.CreatedAt)
                    .ToListAsync();

                var incidents = _mapper.Map<IEnumerable<Incident>>(entities);
                _logger.LogDebug("Retrieved {Count} incidents for user {UserId}", entities.Count, userId);
                return incidents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving incidents by user ID: {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<Incident>> GetByCategoryIdAsync(Guid categoryId)
        {
            _logger.LogDebug("Retrieving incidents by category ID from database: {CategoryId}", categoryId);

            try
            {
                var entities = await _context.Incidents
                    .Where(i => i.CategoryId == categoryId)
                    .Include(i => i.User)
                    .Include(i => i.Status)
                    .Include(i => i.Updates)
                        .ThenInclude(u => u.Author)
                    .OrderByDescending(i => i.Priority)
                    .ThenByDescending(i => i.CreatedAt)
                    .ToListAsync();

                var incidents = _mapper.Map<IEnumerable<Incident>>(entities);
                _logger.LogDebug("Retrieved {Count} incidents for category {CategoryId}", entities.Count, categoryId);
                return incidents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving incidents by category ID: {CategoryId}", categoryId);
                throw;
            }
        }

        public async Task<IEnumerable<Incident>> GetByStatusIdAsync(int statusId)
        {
            _logger.LogDebug("Retrieving incidents by status ID from database: {StatusId}", statusId);

            try
            {
                var entities = await _context.Incidents
                    .Where(i => i.StatusId == statusId)
                    .Include(i => i.User)
                    .Include(i => i.Category)
                    .Include(i => i.Updates)
                        .ThenInclude(u => u.Author)
                    .OrderByDescending(i => i.Priority)
                    .ThenByDescending(i => i.CreatedAt)
                    .ToListAsync();

                var incidents = _mapper.Map<IEnumerable<Incident>>(entities);
                _logger.LogDebug("Retrieved {Count} incidents with status {StatusId}", entities.Count, statusId);
                return incidents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving incidents by status ID: {StatusId}", statusId);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            _logger.LogDebug("Checking if incident exists: {IncidentId}", id);

            try
            {
                var exists = await _context.Incidents.AnyAsync(i => i.Id == id);
                _logger.LogDebug("Incident existence check for {IncidentId}: {Exists}", id, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking incident existence: {IncidentId}", id);
                throw;
            }
        }

        public async Task AddAsync(Incident incident)
        {
            _logger.LogInformation("Adding incident to database: {IncidentId} - {Title} (User: {UserId}, Category: {CategoryId})",
                incident.Id, incident.Title, incident.UserId, incident.CategoryId);

            try
            {
                var entity = _mapper.Map<IncidentEntity>(incident);
                _context.Incidents.Add(entity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Incident added to database successfully: {IncidentId} - {Title}",
                    incident.Id, incident.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding incident to database: {IncidentId} - {Title}",
                    incident.Id, incident.Title);
                throw;
            }
        }

        public async Task UpdateAsync(Incident incident)
        {
            _logger.LogInformation("Updating incident in database: {IncidentId} - {Title}",
                incident.Id, incident.Title);

            try
            {
                var entity = await _context.Incidents.FindAsync(incident.Id);
                if (entity != null)
                {
                    // Actualizar solo las propiedades escalares, no las navegaciones
                    entity.Title = incident.Title;
                    entity.Description = incident.Description;
                    entity.UserId = incident.UserId;
                    entity.CategoryId = incident.CategoryId;
                    entity.StatusId = incident.StatusId;
                    entity.Priority = incident.Priority;
                    entity.UpdatedAt = incident.UpdatedAt;
                    entity.ClosedAt = incident.ClosedAt;

                    // NO usar _mapper.Map aquí porque puede intentar actualizar navegaciones
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Incident updated in database successfully: {IncidentId} - {Title}",
                        incident.Id, incident.Title);
                }
                else
                {
                    _logger.LogWarning("Attempted to update non-existent incident: {IncidentId}", incident.Id);
                    throw new KeyNotFoundException($"Incident {incident.Id} not found");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating incident in database: {IncidentId} - {Title}",
                    incident.Id, incident.Title);
                throw;
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            _logger.LogInformation("Deleting incident from database: {IncidentId}", id);

            try
            {
                var entity = await _context.Incidents.FindAsync(id);
                if (entity != null)
                {
                    _context.Incidents.Remove(entity);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Incident deleted from database successfully: {IncidentId} - {Title}",
                        id, entity.Title);
                }
                else
                {
                    _logger.LogWarning("Attempted to delete non-existent incident: {IncidentId}", id);
                    throw new KeyNotFoundException($"Incident {id} not found");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting incident from database: {IncidentId}", id);
                throw;
            }
        }

        public async Task AddUpdateAsync(IncidentUpdate update)
        {
            _logger.LogDebug("Adding incident update to database: {UpdateId} - {IncidentId} (Author: {AuthorId})",
                update.Id, update.IncidentId, update.AuthorId);

            try
            {
                var entity = _mapper.Map<IncidentUpdateEntity>(update);
                _context.IncidentUpdates.Add(entity);
                await _context.SaveChangesAsync();

                _logger.LogDebug("Incident update added to database successfully: {UpdateId}", update.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding incident update to database: {UpdateId}", update.Id);
                throw;
            }
        }

        public async Task<IEnumerable<IncidentUpdate>> GetUpdatesByIncidentIdAsync(Guid incidentId)
        {
            _logger.LogDebug("Retrieving incident updates from database: {IncidentId}", incidentId);

            try
            {
                var entities = await _context.IncidentUpdates
                    .Where(u => u.IncidentId == incidentId)
                    .Include(u => u.Author)
                    .OrderByDescending(u => u.CreatedAt)
                    .ToListAsync();

                var updates = _mapper.Map<IEnumerable<IncidentUpdate>>(entities);
                _logger.LogDebug("Retrieved {Count} updates for incident {IncidentId}", entities.Count, incidentId);
                return updates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving incident updates: {IncidentId}", incidentId);
                throw;
            }
        }
    }
}