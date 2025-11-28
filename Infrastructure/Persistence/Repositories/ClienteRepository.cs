using Application.Ports;
using Domain.Entities;
using Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence.Repositories
{
    public class ClienteRepository : IClienteRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ClienteRepository> _logger;

        public ClienteRepository(AppDbContext context, ILogger<ClienteRepository> logger)
        {
            _context = context;
            _logger = logger;
            _logger.LogDebug("ClienteRepository initialized successfully");
        }

        public async Task<IEnumerable<Cliente>> GetAllAsync()
        {
            _logger.LogDebug("Retrieving all clientes from database");
            
            try
            {
                var clientes = await _context.Clientes
                    .Select(c => new Cliente
                    {
                        Id = c.Id,
                        Cedula = c.Cedula ?? "",
                        Email = c.Email ?? "",
                        Telefono = c.Telefono ?? "",
                        Nombre = c.Nombre ?? "",
                        Apellido = c.Apellido ?? ""
                    })
                    .ToListAsync();
                
                _logger.LogDebug("Retrieved {Count} clientes from database", clientes.Count);
                return clientes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all clientes from database");
                throw;
            }
        }

        public async Task<Cliente?> GetByIdAsync(Guid id)
        {
            _logger.LogDebug("Retrieving cliente by ID from database: {ClienteId}", id);
            
            try
            {
                var entity = await _context.Clientes.FindAsync(id);
                
                if (entity == null)
                {
                    _logger.LogDebug("Cliente not found in database: {ClienteId}", id);
                    return null;
                }
                
                var cliente = new Cliente
                {
                    Id = entity.Id,
                    Cedula = entity.Cedula ?? "",
                    Email = entity.Email ?? "",
                    Telefono = entity.Telefono ?? "",
                    Nombre = entity.Nombre ?? "",
                    Apellido = entity.Apellido ?? ""
                };
                
                _logger.LogDebug("Cliente retrieved from database: {ClienteId} - {Nombre} {Apellido}", 
                    id, cliente.Nombre, cliente.Apellido);
                return cliente;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cliente by ID from database: {ClienteId}", id);
                throw;
            }
        }

        public async Task AddAsync(Cliente cliente)
        {
            _logger.LogDebug("Adding cliente to database: {ClienteId} - {Nombre} {Apellido} (Email: {Email}, Cedula: {Cedula})", 
                cliente.Id, cliente.Nombre, cliente.Apellido, cliente.Email, cliente.Cedula);
            
            try
            {
                var entity = new ClienteEntity
                {
                    Id = cliente.Id,
                    Cedula = cliente.Cedula,
                    Email = cliente.Email,
                    Telefono = cliente.Telefono,
                    Nombre = cliente.Nombre,
                    Apellido = cliente.Apellido
                };
                
                _context.Clientes.Add(entity);
                await _context.SaveChangesAsync();
                
                _logger.LogDebug("Cliente added to database successfully: {ClienteId} - {Nombre} {Apellido}", 
                    cliente.Id, cliente.Nombre, cliente.Apellido);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding cliente to database: {ClienteId} - {Nombre} {Apellido}", 
                    cliente.Id, cliente.Nombre, cliente.Apellido);
                throw;
            }
        }

        public async Task UpdateAsync(Cliente cliente)
        {
            _logger.LogDebug("Updating cliente in database: {ClienteId} - {Nombre} {Apellido}", 
                cliente.Id, cliente.Nombre, cliente.Apellido);
            
            try
            {
                var entity = await _context.Clientes.FindAsync(cliente.Id);
                if (entity != null)
                {
                    entity.Cedula = cliente.Cedula;
                    entity.Email = cliente.Email;
                    entity.Telefono = cliente.Telefono;
                    entity.Nombre = cliente.Nombre;
                    entity.Apellido = cliente.Apellido;
                    await _context.SaveChangesAsync();
                    
                    _logger.LogDebug("Cliente updated in database successfully: {ClienteId} - {Nombre} {Apellido}", 
                        cliente.Id, cliente.Nombre, cliente.Apellido);
                }
                else
                {
                    _logger.LogWarning("Attempted to update non-existent cliente: {ClienteId}", cliente.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cliente in database: {ClienteId} - {Nombre} {Apellido}", 
                    cliente.Id, cliente.Nombre, cliente.Apellido);
                throw;
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            _logger.LogDebug("Deleting cliente from database: {ClienteId}", id);
            
            try
            {
                var entity = await _context.Clientes.FindAsync(id);
                if (entity != null)
                {
                    _context.Clientes.Remove(entity);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogDebug("Cliente deleted from database successfully: {ClienteId} - {Nombre} {Apellido}", 
                        id, entity.Nombre, entity.Apellido);
                }
                else
                {
                    _logger.LogWarning("Attempted to delete non-existent cliente: {ClienteId}", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting cliente from database: {ClienteId}", id);
                throw;
            }
        }
    }
}
