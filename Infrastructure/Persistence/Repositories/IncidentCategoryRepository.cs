using Application.Ports;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories
{
    public class IncidentCategoryRepository : IIncidentCategoryRepository
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<IncidentCategoryRepository> _logger;

        public IncidentCategoryRepository(AppDbContext context, IMapper mapper, ILogger<IncidentCategoryRepository> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _logger.LogDebug("IncidentCategoryRepository initialized successfully");
        }

        public async Task<IEnumerable<IncidentCategory>> GetAllAsync()
        {
            _logger.LogInformation("Retrieving all incident categories from database");

            try
            {
                var entities = await _context.IncidentCategories
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                var categories = _mapper.Map<IEnumerable<IncidentCategory>>(entities);
                _logger.LogInformation("Retrieved {Count} incident categories from database", entities.Count);
                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all incident categories from database");
                throw;
            }
        }

        public async Task<IEnumerable<IncidentCategory>> GetActiveAsync()
        {
            _logger.LogInformation("Retrieving active incident categories from database");

            try
            {
                var entities = await _context.IncidentCategories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                var categories = _mapper.Map<IEnumerable<IncidentCategory>>(entities);
                _logger.LogInformation("Retrieved {Count} active incident categories from database", entities.Count);
                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active incident categories from database");
                throw;
            }
        }

        public async Task<IncidentCategory?> GetByIdAsync(Guid id)
        {
            _logger.LogDebug("Retrieving incident category by ID from database: {CategoryId}", id);

            try
            {
                var entity = await _context.IncidentCategories.FindAsync(id);

                if (entity == null)
                {
                    _logger.LogDebug("Incident category not found in database: {CategoryId}", id);
                    return null;
                }

                var category = _mapper.Map<IncidentCategory>(entity);
                _logger.LogDebug("Incident category retrieved from database: {CategoryId} - {Name}", id, entity.Name);
                return category;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving incident category by ID: {CategoryId}", id);
                throw;
            }
        }

        public async Task<IncidentCategory?> GetByNameAsync(string name)
        {
            _logger.LogDebug("Retrieving incident category by name from database: {Name}", name);

            try
            {
                var entity = await _context.IncidentCategories
                    .FirstOrDefaultAsync(c => c.Name == name);

                if (entity == null)
                {
                    _logger.LogDebug("Incident category not found in database: {Name}", name);
                    return null;
                }

                var category = _mapper.Map<IncidentCategory>(entity);
                _logger.LogDebug("Incident category retrieved from database: {Name}", name);
                return category;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving incident category by name: {Name}", name);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            _logger.LogDebug("Checking if incident category exists: {CategoryId}", id);

            try
            {
                var exists = await _context.IncidentCategories.AnyAsync(c => c.Id == id);
                _logger.LogDebug("Incident category existence check for {CategoryId}: {Exists}", id, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking incident category existence: {CategoryId}", id);
                throw;
            }
        }

        public async Task AddAsync(IncidentCategory category)
        {
            _logger.LogInformation("Adding incident category to database: {CategoryId} - {Name}", category.Id, category.Name);

            try
            {
                var entity = _mapper.Map<IncidentCategoryEntity>(category);
                _context.IncidentCategories.Add(entity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Incident category added to database successfully: {CategoryId} - {Name}",
                    category.Id, category.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding incident category to database: {CategoryId} - {Name}",
                    category.Id, category.Name);
                throw;
            }
        }

        public async Task UpdateAsync(IncidentCategory category)
        {
            _logger.LogInformation("Updating incident category in database: {CategoryId} - {Name}",
                category.Id, category.Name);

            try
            {
                var entity = await _context.IncidentCategories.FindAsync(category.Id);
                if (entity != null)
                {
                    _mapper.Map(category, entity);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Incident category updated in database successfully: {CategoryId} - {Name}",
                        category.Id, category.Name);
                }
                else
                {
                    _logger.LogWarning("Attempted to update non-existent incident category: {CategoryId}", category.Id);
                    throw new KeyNotFoundException($"Category {category.Id} not found");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating incident category in database: {CategoryId} - {Name}",
                    category.Id, category.Name);
                throw;
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            _logger.LogInformation("Deleting incident category from database: {CategoryId}", id);

            try
            {
                var entity = await _context.IncidentCategories.FindAsync(id);
                if (entity != null)
                {
                    _context.IncidentCategories.Remove(entity);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Incident category deleted from database successfully: {CategoryId} - {Name}",
                        id, entity.Name);
                }
                else
                {
                    _logger.LogWarning("Attempted to delete non-existent incident category: {CategoryId}", id);
                    throw new KeyNotFoundException($"Category {id} not found");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting incident category from database: {CategoryId}", id);
                throw;
            }
        }
    }
}