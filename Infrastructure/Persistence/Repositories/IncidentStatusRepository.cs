using Application.Ports;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories
{
    public class IncidentStatusRepository : IIncidentStatusRepository
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<IncidentStatusRepository> _logger;

        public IncidentStatusRepository(AppDbContext context, IMapper mapper, ILogger<IncidentStatusRepository> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _logger.LogDebug("IncidentStatusRepository initialized successfully");
        }

        public async Task<IEnumerable<IncidentStatus>> GetAllAsync()
        {
            _logger.LogInformation("Retrieving all incident statuses from database");

            try
            {
                var entities = await _context.IncidentStatuses
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.OrderSequence)
                    .ToListAsync();

                var statuses = _mapper.Map<IEnumerable<IncidentStatus>>(entities);
                _logger.LogInformation("Retrieved {Count} incident statuses from database", entities.Count);
                return statuses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all incident statuses from database");
                throw;
            }
        }

        public async Task<IncidentStatus?> GetByIdAsync(int id)
        {
            _logger.LogDebug("Retrieving incident status by ID from database: {StatusId}", id);

            try
            {
                var entity = await _context.IncidentStatuses.FindAsync(id);

                if (entity == null)
                {
                    _logger.LogDebug("Incident status not found in database: {StatusId}", id);
                    return null;
                }

                var status = _mapper.Map<IncidentStatus>(entity);
                _logger.LogDebug("Incident status retrieved from database: {StatusId} - {Name}", id, entity.Name);
                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving incident status by ID: {StatusId}", id);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            _logger.LogDebug("Checking if incident status exists: {StatusId}", id);

            try
            {
                var exists = await _context.IncidentStatuses.AnyAsync(s => s.Id == id && s.IsActive);
                _logger.LogDebug("Incident status existence check for {StatusId}: {Exists}", id, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking incident status existence: {StatusId}", id);
                throw;
            }
        }
    }
}